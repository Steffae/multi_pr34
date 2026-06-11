using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Lobby Panel")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private TMP_Text _lobbyStatusText;
    [SerializeField] private TMP_Text _lobbyReadyText;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _backToMenuButton;

    [Header("Countdown")]
    [SerializeField] private GameObject _countdownPanel;
    [SerializeField] private TMP_Text _countdownText;

    [Header("Results Panel")]
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private TMP_Text _resultsText;
    [SerializeField] private Button _rematchButton;
    [SerializeField] private Button _exitAfterMatchButton;

    [Header("Gameplay HUD")]
    [SerializeField] private GameObject _gameplayHUD;
    [SerializeField] private TMP_Text _matchTimerText;

    private bool _isReady = false;
    private bool _individualRematch = false;
    private ConnectionUI _connectionUI;

    private static LobbyUI _instance;
    public static LobbyUI Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    public void ShowResultsWithWinner(string winnerText)
    {
        if (_resultsPanel != null) _resultsPanel.SetActive(true);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_countdownPanel != null) _countdownPanel.SetActive(false);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(false);

        if (_resultsText != null)
        {
            _resultsText.text = winnerText;
        }
    }

    private void Start()
    {
        _connectionUI = FindObjectOfType<ConnectionUI>();

        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged += OnGameStateChanged;
            GameManager.OnLocalConnectedPlayersChanged += OnConnectedPlayersChanged;
            GameManager.OnLocalPlayersReadyChanged += OnPlayersReadyChanged;
            GameManager.OnLocalMatchTimerChanged += OnMatchTimerChanged;
            GameManager.OnLocalCountdownTimerChanged += OnCountdownTimerChanged;
        }

        if (_readyButton != null)
            _readyButton.onClick.AddListener(OnReadyButtonPressed);
        if (_backToMenuButton != null)
            _backToMenuButton.onClick.AddListener(OnBackToMenuPressed);
        if (_rematchButton != null)
            _rematchButton.onClick.AddListener(OnRematchPressed);
        if (_exitAfterMatchButton != null)
            _exitAfterMatchButton.onClick.AddListener(OnExitAfterMatchPressed);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged -= OnGameStateChanged;
            GameManager.OnLocalConnectedPlayersChanged -= OnConnectedPlayersChanged;
            GameManager.OnLocalPlayersReadyChanged -= OnPlayersReadyChanged;
            GameManager.OnLocalMatchTimerChanged -= OnMatchTimerChanged;
            GameManager.OnLocalCountdownTimerChanged -= OnCountdownTimerChanged;
        }
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        _individualRematch = false;
        switch (newState)
        {
            case GameManager.GameState.WaitingForPlayers:
                ShowWaitingForPlayers();
                break;
            case GameManager.GameState.ReadyCheck:
                ShowReadyCheck();
                break;
            case GameManager.GameState.Countdown:
                ShowCountdown();
                break;
            case GameManager.GameState.InProgress:
                ShowGameplay();
                break;
            case GameManager.GameState.ShowingResults:
                ShowResults();
                break;
        }
    }

    private void OnConnectedPlayersChanged(int players)
    {
        UpdateLobbyStatus();
        UpdateReadyButtonState();
    }

    private void OnPlayersReadyChanged(int readyCount, int totalPlayers)
    {
        if (_lobbyReadyText != null && GameManager.Instance != null &&
            GameManager.Instance.CurrentState.Value == GameManager.GameState.ReadyCheck)
        {
            _lobbyReadyText.text = $"Ready: {readyCount}/{totalPlayers}";
        }
        UpdateReadyButtonState();
    }

    private void OnMatchTimerChanged(float time)
    {
        if (_matchTimerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            _matchTimerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void OnCountdownTimerChanged(float time)
    {
        if (_countdownText != null)
        {
            int seconds = Mathf.CeilToInt(time);
            _countdownText.text = $"Game starts in: {seconds}";
        }
    }

    private void UpdateReadyButtonState()
    {
        if (_readyButton == null) return;
        if (GameManager.Instance == null) return;

        bool isReadyCheck = (GameManager.Instance.CurrentState.Value == GameManager.GameState.ReadyCheck);
        bool canReady = _individualRematch || isReadyCheck;
        bool hasPlayers = (GameManager.Instance.ConnectedPlayers.Value >= 2);

        _readyButton.interactable = canReady && hasPlayers;

        // Меняем цвет кнопки при нажатии
        if (_isReady && _readyButton.interactable)
        {
            ColorBlock colors = _readyButton.colors;
            colors.normalColor = Color.deepPink;
            colors.selectedColor = Color.deepPink;
            _readyButton.colors = colors;
        }
        else
        {
            ColorBlock colors = _readyButton.colors;
            colors.normalColor = Color.white;
            colors.selectedColor = Color.white;
            _readyButton.colors = colors;
        }
    }

    private void UpdateLobbyStatus()
    {
        if (_lobbyStatusText != null && GameManager.Instance != null)
        {
            int players = GameManager.Instance.ConnectedPlayers.Value;
            _lobbyStatusText.text = $"Waiting for players: {players}/2";
        }
    }

    private void OnReadyButtonPressed()
    {
        _isReady = !_isReady;

        if (GameManager.Instance != null)
        {
            // Получаем ID текущего клиента
            int myClientId = FishNet.InstanceFinder.ClientManager.Connection.ClientId;
            Debug.Log($"[LobbyUI] Sending ready status: {_isReady} from client {myClientId}");
            GameManager.Instance.SetPlayerReadyServerRpc(_isReady, myClientId);
        }

        UpdateReadyButtonState();
    }

    private void OnBackToMenuPressed()
    {
        if (FishNet.InstanceFinder.ClientManager.Started)
        {
            FishNet.InstanceFinder.ClientManager.StopConnection();
        }

        if (_connectionUI != null)
        {
            _connectionUI.DisconnectAndReturnToMenu();
        }

        ShowMenu();
        _isReady = false;
    }

    private void OnRematchPressed()
    {
        if (GameManager.Instance != null)
        {
            int myClientId = FishNet.InstanceFinder.ClientManager.Connection.ClientId;
            GameManager.Instance.RequestRestartServerRpc(myClientId);
        }
    }

    private void OnExitAfterMatchPressed()
    {
        OnBackToMenuPressed();
    }

    private void ShowWaitingForPlayers()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
        if (_countdownPanel != null) _countdownPanel.SetActive(false);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);

        if (_lobbyStatusText != null) _lobbyStatusText.gameObject.SetActive(true);
        if (_lobbyReadyText != null) _lobbyReadyText.gameObject.SetActive(false);
        if (_readyButton != null) _readyButton.gameObject.SetActive(false);

        UpdateLobbyStatus();
    }

    public void ShowLobbyFromRematch()
    {
        _individualRematch = true;
        ShowReadyCheck();
    }

    public void ShowReadyCheck()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
        if (_countdownPanel != null) _countdownPanel.SetActive(false);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);

        if (_lobbyStatusText != null) _lobbyStatusText.gameObject.SetActive(false);
        if (_lobbyReadyText != null) _lobbyReadyText.gameObject.SetActive(true);
        if (_readyButton != null) _readyButton.gameObject.SetActive(true);

        if (_lobbyReadyText != null && GameManager.Instance != null)
        {
            int connected = GameManager.Instance.ConnectedPlayers.Value;
            _lobbyReadyText.text = $"Ready: 0/{connected}";
        }

        // Сбрасываем состояние готовности при входе в ReadyCheck
        _isReady = false;
        UpdateReadyButtonState();
    }

    private void ShowCountdown()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_countdownPanel != null) _countdownPanel.SetActive(true);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
    }

    private void ShowGameplay()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_countdownPanel != null) _countdownPanel.SetActive(false);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(true);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
    }

    private void ShowResults()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_countdownPanel != null) _countdownPanel.SetActive(false);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(true);

        // Текст уже установлен через ShowResultsObserversRpc
    }

    private void ShowMenu()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_countdownPanel != null) _countdownPanel.SetActive(false);
        if (_gameplayHUD != null) _gameplayHUD.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
    }

}