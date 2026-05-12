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

    [Header("Game State")]
    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);

    public enum GameState
    {
        WaitingForPlayers,
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

        // Подписываемся на изменения SyncVar
        CurrentState.OnChange += OnGameStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        MatchTimer.OnChange += OnMatchTimerChanged;

        if (base.IsServerInitialized)
        {
            // Сервер отслеживает подключения игроков
            base.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;

            // Сразу считаем текущих игроков
            ConnectedPlayers.Value = base.ServerManager.Clients.Count;

            Debug.Log($"[GameManager] Server started. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        CurrentState.OnChange -= OnGameStateChanged;
        ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;
        MatchTimer.OnChange -= OnMatchTimerChanged;

        if (base.IsServerInitialized)
        {
            base.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
        }
    }

    private void OnPlayerConnectionChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!base.IsServerInitialized) return;

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Игрок подключился
            ConnectedPlayers.Value = base.ServerManager.Clients.Count;
            Debug.Log($"[GameManager] Player connected. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");

            if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
            {
                StartMatch();
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Игрок отключился
            ConnectedPlayers.Value = base.ServerManager.Clients.Count;
            Debug.Log($"[GameManager] Player disconnected. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");
        }
    }

    private void StartMatch()
    {
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;
        Debug.Log("[GameManager] Match started!");
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;
        if (CurrentState.Value != GameState.InProgress) return;

        MatchTimer.Value -= Time.deltaTime;

        if (MatchTimer.Value <= 0f)
        {
            MatchTimer.Value = 0f;
            EndMatch();
        }
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[GameManager] Match ended! Showing results...");
        Invoke(nameof(ResetToLobby), _resultShowDuration);
    }

    private void ResetToLobby()
    {
        MatchTimer.Value = 0f;
        CurrentState.Value = GameState.WaitingForPlayers;
        Debug.Log("[GameManager] Lobby reset. Waiting for players...");
    }

    // Хуки SyncVar — вызываются на всех клиентах при изменении
    private void OnGameStateChanged(GameState oldValue, GameState newValue, bool asServer)
    {
        Debug.Log($"[GameManager] GameState: {oldValue} -> {newValue}");
        OnLocalGameStateChanged?.Invoke(newValue);
    }

    private void OnConnectedPlayersChanged(int oldValue, int newValue, bool asServer)
    {
        Debug.Log($"[GameManager] ConnectedPlayers: {oldValue} -> {newValue}");
        OnLocalConnectedPlayersChanged?.Invoke(newValue);
    }

    private void OnMatchTimerChanged(float oldValue, float newValue, bool asServer)
    {
        OnLocalMatchTimerChanged?.Invoke(newValue);
    }
}