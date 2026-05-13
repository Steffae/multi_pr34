using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;
    [SerializeField] private float _resultShowDuration = 5f;
    [SerializeField] private float _countdownDuration = 5f;

    [Header("Game State")]
    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);
    public readonly SyncVar<float> CountdownTimer = new SyncVar<float>(0f);

    public enum GameState
    {
        WaitingForPlayers,
        StartingSoon,
        InProgress,
        ShowingResults
    }

    public static GameManager Instance { get; private set; }

    // События для локального UI
    public delegate void GameStateChangedHandler(GameState newState);
    public static event GameStateChangedHandler OnLocalGameStateChanged;

    public delegate void ConnectedPlayersChangedHandler(int players);
    public static event ConnectedPlayersChangedHandler OnLocalConnectedPlayersChanged;

    public delegate void MatchTimerChangedHandler(float time);
    public static event MatchTimerChangedHandler OnLocalMatchTimerChanged;

    public delegate void CountdownTimerChangedHandler(float time);
    public static event CountdownTimerChangedHandler OnLocalCountdownTimerChanged;

    private bool _matchInProgress = false;
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
            Debug.Log($"[GameManager] Player connected. ClientId={conn.ClientId}. Players: {base.ServerManager.Clients.Count}/{_requiredPlayers}");
            UpdateConnectedPlayersCount();

            if (CurrentState.Value == GameState.WaitingForPlayers
                && ConnectedPlayers.Value >= _requiredPlayers
                && !_countdownInProgress)
            {
                Debug.Log("[GameManager] Conditions met. Starting countdown!");
                StartCountdown();
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            Debug.Log($"[GameManager] Player DISCONNECTED. ClientId={conn.ClientId}. Raw Clients.Count={base.ServerManager.Clients.Count}. State={CurrentState.Value}, CountdownInProgress={_countdownInProgress}");

            // Задерживаем обновление на 0.5 секунды — даём серверу время удалить клиента
            StartCoroutine(DelayedUpdatePlayersCount());
        }
    }

    private System.Collections.IEnumerator DelayedUpdatePlayersCount()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateConnectedPlayersCount();
        Debug.Log($"[GameManager] Delayed update. Players now: {ConnectedPlayers.Value}/{_requiredPlayers}");

        // Проверяем отмену отсчёта
        if (CurrentState.Value == GameState.StartingSoon
            && ConnectedPlayers.Value < _requiredPlayers)
        {
            Debug.Log("[GameManager] Countdown CANCELLED - not enough players!");
            CancelCountdown();
        }
    }

    private void CancelCountdown()
    {
        Debug.Log("[GameManager] CancelCountdown() called");
        _countdownInProgress = false;
        CountdownTimer.Value = 0f;
        CurrentState.Value = GameState.WaitingForPlayers;
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        if (CurrentState.Value == GameState.StartingSoon)
        {
            // Проверка: если игроков стало меньше — отмена
            if (ConnectedPlayers.Value < _requiredPlayers)
            {
                Debug.Log($"[GameManager] Update: Not enough players ({ConnectedPlayers.Value}/{_requiredPlayers}). Cancelling countdown!");
                CancelCountdown();
                return;
            }

            CountdownTimer.Value -= Time.deltaTime;
            if (CountdownTimer.Value <= 0f)
            {
                CountdownTimer.Value = 0f;
                Debug.Log("[GameManager] Countdown finished! Starting match!");
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

    private void StartCountdown()
    {
        _countdownInProgress = true;
        CurrentState.Value = GameState.StartingSoon;
        CountdownTimer.Value = _countdownDuration;
        Debug.Log($"[GameManager] Starting countdown: {_countdownDuration} seconds");
    }

    private void StartMatch()
    {
        _countdownInProgress = false;
        _matchInProgress = true;
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;

        // Сброс всех игроков
        ResetAllPlayers();

        Debug.Log("[GameManager] Match started! Players reset.");
    }

    private void ResetAllPlayers()
    {
        if (!base.IsServerInitialized) return;

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        foreach (var conn in base.ServerManager.Clients.Values)
        {
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    // Сброс HP
                    pn.HP.Value = 100;
                    // Сброс IsAlive
                    pn.IsAlive.Value = true;

                    Debug.Log($"[GameManager] Reset player {pn.Nickname.Value}: HP=100");
                }

                PlayerShooting ps = nob.GetComponent<PlayerShooting>();
                if (ps != null)
                {
                    // Сброс патронов
                    ps.CurrentAmmo.Value = 10;
                    Debug.Log($"[GameManager] Reset ammo for player");
                }

                // Перемещение на точку спавна
                if (spawnPoints.Length > 0)
                {
                    int idx = Random.Range(0, spawnPoints.Length);
                    nob.transform.position = spawnPoints[idx].transform.position;
                }
            }
        }
    }

    private void EndMatch()
    {
        _matchInProgress = false;
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[GameManager] Match ended! Showing results...");
        Invoke(nameof(ResetToLobby), _resultShowDuration);
    }

    private void ResetToLobby()
    {
        MatchTimer.Value = 0f;
        CountdownTimer.Value = 0f;
        _countdownInProgress = false;
        CurrentState.Value = GameState.WaitingForPlayers;
        Debug.Log($"[GameManager] Lobby reset. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");

        // Небольшая задержка перед проверкой на новый матч
        if (ConnectedPlayers.Value >= _requiredPlayers)
        {
            Debug.Log("[GameManager] Enough players after reset. Starting new countdown!");
            StartCountdown();
        }
        else
        {
            Debug.Log("[GameManager] Not enough players after reset. Waiting...");
        }
    }

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
}