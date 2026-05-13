using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private TMP_Text _lobbyStatusText;
    [SerializeField] private TMP_Text _matchTimerText;
    [SerializeField] private TMP_Text _countdownText;
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private TMP_Text _resultsText;

    private void OnEnable()
    {
        GameManager.OnLocalGameStateChanged += OnGameStateChanged;
        GameManager.OnLocalConnectedPlayersChanged += OnConnectedPlayersChanged;
        GameManager.OnLocalMatchTimerChanged += OnMatchTimerChanged;
        GameManager.OnLocalCountdownTimerChanged += OnCountdownTimerChanged;
    }

    private void OnDisable()
    {
        GameManager.OnLocalGameStateChanged -= OnGameStateChanged;
        GameManager.OnLocalConnectedPlayersChanged -= OnConnectedPlayersChanged;
        GameManager.OnLocalMatchTimerChanged -= OnMatchTimerChanged;
        GameManager.OnLocalCountdownTimerChanged -= OnCountdownTimerChanged;
    }

    private void Start()
    {
        ShowLobby();
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.WaitingForPlayers:
                ShowLobby();
                break;
            case GameManager.GameState.StartingSoon:
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
        if (_lobbyStatusText != null && GameManager.Instance != null
            && GameManager.Instance.CurrentState.Value == GameManager.GameState.WaitingForPlayers)
        {
            _lobbyStatusText.text = $"Ожидание игроков: {players}/2";
        }
    }

    private void OnMatchTimerChanged(float time)
    {
        if (_matchTimerText != null)
        {
            int seconds = Mathf.CeilToInt(time);
            _matchTimerText.text = $"Время: {seconds} сек";
        }
    }

    private void OnCountdownTimerChanged(float time)
    {
        if (_countdownText != null)
        {
            int seconds = Mathf.CeilToInt(time);
            _countdownText.text = $"Игра начнётся через: {seconds}";
        }
    }

    private void ShowLobby()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
        if (_matchTimerText != null) _matchTimerText.gameObject.SetActive(false);
        if (_countdownText != null) _countdownText.gameObject.SetActive(false);
        if (_lobbyStatusText != null) _lobbyStatusText.gameObject.SetActive(true);

        // Обновить текст статуса
        if (GameManager.Instance != null)
        {
            _lobbyStatusText.text = $"Ожидание игроков: {GameManager.Instance.ConnectedPlayers.Value}/2";
        }
    }

    private void ShowCountdown()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
        if (_matchTimerText != null) _matchTimerText.gameObject.SetActive(false);
        if (_countdownText != null) _countdownText.gameObject.SetActive(true);
        if (_lobbyStatusText != null) _lobbyStatusText.gameObject.SetActive(false);
    }

    private void ShowGameplay()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(false);
        if (_matchTimerText != null) _matchTimerText.gameObject.SetActive(true);
        if (_countdownText != null) _countdownText.gameObject.SetActive(false);
    }

    private void ShowResults()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_resultsPanel != null) _resultsPanel.SetActive(true);
        if (_matchTimerText != null) _matchTimerText.gameObject.SetActive(false);
        if (_countdownText != null) _countdownText.gameObject.SetActive(false);

        if (_resultsText != null)
        {
            _resultsText.text = "Матч завершён!\nВозврат в лобби...";
        }
    }
}