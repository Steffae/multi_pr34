using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Serializing;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 300f; // 5 минут = 300 секунд
    [SerializeField] private float _resultShowDuration = 5f;
    [SerializeField] private float _countdownDuration = 5f;

    [Header("Game State")]
    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);
    public readonly SyncVar<float> CountdownTimer = new SyncVar<float>(0f);

    // Синхронизация готовности игроков (словарь ClientId → IsReady)
    private Dictionary<int, bool> _playerReadyStatus = new Dictionary<int, bool>();
    public readonly SyncVar<int> PlayersReadyCount = new SyncVar<int>(0);

    public int GetPlayersReadyCount() => PlayersReadyCount.Value;

    public enum GameState
    {
        WaitingForPlayers,  // Ждём игроков (лобби)
        ReadyCheck,         // Оба игрока есть, ждём нажатия "Готов"
        Countdown,          // Оба готовы, идёт обратный отсчёт
        InProgress,         // Матч идёт
        ShowingResults      // Показываем результаты
    }

    public static GameManager Instance { get; private set; }

    // События для UI
    public delegate void GameStateChangedHandler(GameState newState);
    public static event GameStateChangedHandler OnLocalGameStateChanged;

    public delegate void ConnectedPlayersChangedHandler(int players);
    public static event ConnectedPlayersChangedHandler OnLocalConnectedPlayersChanged;

    public delegate void PlayersReadyChangedHandler(int readyCount, int totalPlayers);
    public static event PlayersReadyChangedHandler OnLocalPlayersReadyChanged;

    public delegate void MatchTimerChangedHandler(float time);
    public static event MatchTimerChangedHandler OnLocalMatchTimerChanged;

    public delegate void CountdownTimerChangedHandler(float time);
    public static event CountdownTimerChangedHandler OnLocalCountdownTimerChanged;

    private bool _countdownInProgress = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        CurrentState.OnChange += OnGameStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        MatchTimer.OnChange += OnMatchTimerChanged;
        CountdownTimer.OnChange += OnCountdownTimerChanged;
        PlayersReadyCount.OnChange += OnPlayersReadyCountChanged;

        if (base.IsServerInitialized)
        {
            base.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
            UpdateConnectedPlayersCount();
            Debug.Log($"[GameManager] Server started. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        CurrentState.OnChange -= OnGameStateChanged;
        ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;
        MatchTimer.OnChange -= OnMatchTimerChanged;
        CountdownTimer.OnChange -= OnCountdownTimerChanged;

        if (base.IsServerInitialized)
        {
            base.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
        }
    }

    private void UpdateConnectedPlayersCount()
    {
        if (base.IsServerInitialized)
        {
            ConnectedPlayers.Value = base.ServerManager.Clients.Count;
        }
    }

    private void OnPlayerConnectionChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!base.IsServerInitialized) return;

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            Debug.Log($"[GameManager] Player connected. ClientId={conn.ClientId}");
            UpdateConnectedPlayersCount();

            // Новый игрок подключается — он не готов
            _playerReadyStatus[conn.ClientId] = false;
            UpdateReadyCount();

            // Проверяем, нужно ли перейти в режим ожидания готовности
            if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
            {
                CurrentState.Value = GameState.ReadyCheck;
                Debug.Log("[GameManager] Enough players! Waiting for ready status...");
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            Debug.Log($"[GameManager] Player disconnected. ClientId={conn.ClientId}");

            // Удаляем игрока из словаря готовности
            _playerReadyStatus.Remove(conn.ClientId);
            UpdateReadyCount();

            // Отложенное обновление счётчика
            StartCoroutine(DelayedUpdatePlayersCount());
        }
    }

    private System.Collections.IEnumerator DelayedUpdatePlayersCount()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateConnectedPlayersCount();
        Debug.Log($"[GameManager] Delayed update. Players now: {ConnectedPlayers.Value}/{_requiredPlayers}");

        // Если игроков снова меньше требуемого — возвращаемся в ожидание
        if (ConnectedPlayers.Value < _requiredPlayers)
        {
            if (CurrentState.Value == GameState.ReadyCheck ||
                CurrentState.Value == GameState.Countdown)
            {
                Debug.Log("[GameManager] Not enough players! Returning to waiting state...");
                ResetReadyStatus();
                CurrentState.Value = GameState.WaitingForPlayers;
                CountdownTimer.Value = 0f;
                _countdownInProgress = false;
            }
        }
        else if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
        {
            CurrentState.Value = GameState.ReadyCheck;
            Debug.Log("[GameManager] Enough players again! Waiting for ready status...");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(bool isReady, int clientId)
    {
        Debug.Log($"[GameManager] SetPlayerReadyServerRpc called from client {clientId}, isReady={isReady}");

        if (!_playerReadyStatus.ContainsKey(clientId))
        {
            Debug.Log($"[GameManager] Adding new player {clientId} to ready dictionary");
            _playerReadyStatus[clientId] = false;
        }

        _playerReadyStatus[clientId] = isReady;
        UpdateReadyCount();

        Debug.Log($"[GameManager] Player {clientId} ready status: {isReady}. Ready count: {PlayersReadyCount.Value}/{_playerReadyStatus.Count}");

        if (CurrentState.Value == GameState.ReadyCheck && AllPlayersReady())
        {
            Debug.Log("[GameManager] All players ready! Starting countdown...");
            StartCountdown();
        }
    }

    private void UpdateReadyCount()
    {
        int ready = 0;
        foreach (var kvp in _playerReadyStatus)
        {
            if (kvp.Value) ready++;
        }
        PlayersReadyCount.Value = ready;

        Debug.Log($"[GameManager] UpdateReadyCount: {ready} ready out of {_playerReadyStatus.Count} players");

        // Оповещаем UI
        OnLocalPlayersReadyChanged?.Invoke(ready, _playerReadyStatus.Count);
    }

    private bool AllPlayersReady()
    {
        if (_playerReadyStatus.Count < _requiredPlayers)
        {
            Debug.Log($"[GameManager] AllPlayersReady: Not enough players. Have {_playerReadyStatus.Count}, need {_requiredPlayers}");
            return false;
        }

        foreach (var kvp in _playerReadyStatus)
        {
            if (!kvp.Value)
            {
                Debug.Log($"[GameManager] AllPlayersReady: Player {kvp.Key} is not ready");
                return false;
            }
        }

        Debug.Log("[GameManager] AllPlayersReady: TRUE!");
        return true;
    }

    private void ResetReadyStatus()
    {
        _playerReadyStatus.Clear();

        // Перезаполняем для всех текущих игроков
        foreach (var conn in base.ServerManager.Clients.Values)
        {
            _playerReadyStatus[conn.ClientId] = false;
        }

        UpdateReadyCount();
    }

    private void StartCountdown()
    {
        _countdownInProgress = true;
        CurrentState.Value = GameState.Countdown;
        CountdownTimer.Value = _countdownDuration;
        Debug.Log($"[GameManager] Starting countdown: {_countdownDuration} seconds");
    }

    private void StartMatch()
    {
        _countdownInProgress = false;
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;

        ResetPlayersForMatch();

        Debug.Log("[GameManager] Match started!");
    }

    private void ResetPlayersForMatch()
    {
        if (!base.IsServerInitialized) return;

        foreach (var conn in base.ServerManager.Clients.Values)
        {
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    //pn.ResetForMatch();
                    Debug.Log($"[GameManager] Reset player {pn.Nickname.Value}");
                }
            }
        }
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        if (CurrentState.Value == GameState.Countdown)
        {
            CountdownTimer.Value -= Time.deltaTime;
            if (CountdownTimer.Value <= 0f)
            {
                CountdownTimer.Value = 0f;
                StartMatch();
            }
        }
        else if (CurrentState.Value == GameState.InProgress)
        {
            MatchTimer.Value -= Time.deltaTime;
            if (MatchTimer.Value <= 0f)
            {
                MatchTimer.Value = 0f;
                EndMatch();
            }
        }
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[GameManager] Match ended!");
    }

    // События для UI
    private void OnGameStateChanged(GameState oldValue, GameState newValue, bool asServer)
    {
        Debug.Log($"[GameManager] GameState: {oldValue} -> {newValue}");
        OnLocalGameStateChanged?.Invoke(newValue);
    }

    private void OnConnectedPlayersChanged(int oldValue, int newValue, bool asServer)
    {
        OnLocalConnectedPlayersChanged?.Invoke(newValue);
    }

    private void OnMatchTimerChanged(float oldValue, float newValue, bool asServer)
    {
        OnLocalMatchTimerChanged?.Invoke(newValue);
    }

    private void OnCountdownTimerChanged(float oldValue, float newValue, bool asServer)
    {
        OnLocalCountdownTimerChanged?.Invoke(newValue);
    }

    private void OnPlayersReadyCountChanged(int oldValue, int newValue, bool asServer)
    {
        OnLocalPlayersReadyChanged?.Invoke(newValue, ConnectedPlayers.Value);
    }
}