using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 300f;
    [SerializeField] private float _resultShowDuration = 5f;
    [SerializeField] private float _countdownDuration = 5f;

    [Header("Game State")]
    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);
    public readonly SyncVar<float> CountdownTimer = new SyncVar<float>(0f);
    public readonly SyncVar<int> PlayersReadyCount = new SyncVar<int>(0);

    public enum GameState
    {
        WaitingForPlayers,
        ReadyCheck,
        Countdown,
        InProgress,
        ShowingResults
    }

    public static GameManager Instance { get; private set; }

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

    private Dictionary<int, bool> _playerReadyStatus = new Dictionary<int, bool>();
    private Dictionary<int, bool> _rematchVotes = new Dictionary<int, bool>();
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
        PlayersReadyCount.OnChange -= OnPlayersReadyCountChanged;

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

            _playerReadyStatus[conn.ClientId] = false;
            UpdateReadyCount();

            if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
            {
                CurrentState.Value = GameState.ReadyCheck;
                Debug.Log("[GameManager] Enough players! Waiting for ready status...");
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            Debug.Log($"[GameManager] Player disconnected. ClientId={conn.ClientId}");

            _playerReadyStatus.Remove(conn.ClientId);
            UpdateReadyCount();

            StartCoroutine(DelayedUpdatePlayersCount());
        }
    }

    private System.Collections.IEnumerator DelayedUpdatePlayersCount()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateConnectedPlayersCount();
        Debug.Log($"[GameManager] Delayed update. Players now: {ConnectedPlayers.Value}/{_requiredPlayers}");

        if (ConnectedPlayers.Value < _requiredPlayers)
        {
            if (CurrentState.Value == GameState.ReadyCheck || CurrentState.Value == GameState.Countdown)
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

        // Во время показа результатов — учитываем голоса за рематч
        if (CurrentState.Value == GameState.ShowingResults)
        {
            if (AllPlayersRematched() && AllPlayersReady())
            {
                Debug.Log("[GameManager] All players rematched and ready! Starting match...");
                _rematchVotes.Clear();
                ResetReadyStatus();
                StartCountdown();
            }
            return;
        }

        if (CurrentState.Value == GameState.ReadyCheck && AllPlayersReady())
        {
            Debug.Log("[GameManager] All players ready! Starting countdown...");
            StartCountdown();
        }
    }

    private bool AllPlayersRematched()
    {
        foreach (var conn in base.ServerManager.Clients.Values)
        {
            if (!_rematchVotes.ContainsKey(conn.ClientId) || !_rematchVotes[conn.ClientId])
                return false;
        }
        return _rematchVotes.Count > 0;
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
                    pn.ResetForMatch();
                    Debug.Log($"[GameManager] Reset player {pn.Nickname.Value}");
                }
            }
        }
    }

    private void CheckWinCondition()
    {
        foreach (var conn in base.ServerManager.Clients.Values)
        {
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null && pn.IsAlive.Value && pn.HP.Value >= 9)
                {
                    EndMatchWithWinner(pn.OwnerId, pn.Nickname.Value);
                    return;
                }
            }
        }
    }

    private void EndMatchWithWinner(int winnerClientId, string winnerName)
    {
        CurrentState.Value = GameState.ShowingResults;
        ShowResultsObserversRpc(winnerName, winnerClientId);
        Debug.Log($"[GameManager] Match ended! Winner: {winnerName} (client {winnerClientId})");
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        ShowResultsObserversRpc("", -1);
        Debug.Log("[GameManager] Match ended! Time's up!");
    }

    [ObserversRpc]
    private void ShowResultsObserversRpc(string winnerName, int winnerClientId)
    {
        if (LobbyUI.Instance != null)
        {
            int myId = FishNet.InstanceFinder.ClientManager.Connection.ClientId;
            string resultText;
            if (winnerClientId < 0)
                resultText = "Ничья!\nНикто не набрал 9 сердечек";
            else if (winnerClientId == myId)
                resultText = $"Вы победитель!\n{winnerName} набрал 9 сердечек!";
            else
                resultText = $"Вы проиграли!\n{winnerName} набрал 9 сердечек!";

            LobbyUI.Instance.ShowResultsWithWinner(resultText);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc(int clientId)
    {
        if (!IsServerInitialized) return;
        if (CurrentState.Value != GameState.ShowingResults) return;

        if (_rematchVotes.ContainsKey(clientId) && _rematchVotes[clientId])
            return;

        _rematchVotes[clientId] = true;
        Debug.Log($"[GameManager] Player {clientId} voted for rematch");

        // Сбрасываем готовность, чтобы лобби показывало 0/2
        ResetReadyStatus();

        // Переводим этого игрока в лобби индивидуально
        if (base.ServerManager.Clients.TryGetValue(clientId, out var conn))
        {
            ShowRematchLobbyTargetRpc(conn);
        }

        // Если все проголосовали — общий переход в ReadyCheck
        bool allVoted = true;
        foreach (var c in base.ServerManager.Clients.Values)
        {
            if (!_rematchVotes.ContainsKey(c.ClientId) || !_rematchVotes[c.ClientId])
            {
                allVoted = false;
                break;
            }
        }

        if (allVoted)
        {
            Debug.Log("[GameManager] All players rematched!");
            _rematchVotes.Clear();
            ResetReadyStatus();
            CurrentState.Value = GameState.ReadyCheck;
        }
    }

    [TargetRpc]
    private void ShowRematchLobbyTargetRpc(NetworkConnection target)
    {
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.ShowLobbyFromRematch();
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

            CheckWinCondition();

            if (MatchTimer.Value <= 0f)
            {
                MatchTimer.Value = 0f;
                EndMatch();
            }
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

    private void OnPlayersReadyCountChanged(int oldValue, int newValue, bool asServer)
    {
        OnLocalPlayersReadyChanged?.Invoke(newValue, ConnectedPlayers.Value);
    }
}