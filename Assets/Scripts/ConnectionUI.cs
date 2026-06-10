using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _lobbyPanel;

    public static string PlayerNickname { get; private set; } = "Player";
    public static string ServerIP { get; private set; } = "172.24.217.139";

    private bool _isConnecting = false;

    private void Start()
    {
        if (_menuPanel != null) _menuPanel.SetActive(true);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);

        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        _isConnecting = false;

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("[ConnectionUI] Connected to server!");
            HideMenu();
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.Log("[ConnectionUI] Disconnected from server.");

            // Если не в игре, показываем меню с ошибкой
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState.Value == GameManager.GameState.WaitingForPlayers)
            {
                ShowMenuWithError("Connection failed or server closed");
            }
        }
    }

    public void ConnectToServer()
    {
        if (_isConnecting) return;

        SaveNickname();
        SaveIP();

        _isConnecting = true;

        Debug.Log($"[ConnectionUI] Connecting to server: {ServerIP}:7770");

        Tugboat transport = InstanceFinder.TransportManager.Transport as Tugboat;
        if (transport != null)
        {
            transport.SetClientAddress(ServerIP);
            transport.SetPort(7770);
        }

        InstanceFinder.ClientManager.StartConnection();
    }

    public void DisconnectAndReturnToMenu()
    {
        if (InstanceFinder.ClientManager.Started)
        {
            InstanceFinder.ClientManager.StopConnection();
        }

        // Возвращаемся в лобби-сцену
        if (_menuPanel != null) _menuPanel.SetActive(true);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        Debug.Log($"[ConnectionUI] Saved nickname: {PlayerNickname}");
    }

    private void SaveIP()
    {
        string rawValue = _ipInput != null ? _ipInput.text : string.Empty;
        ServerIP = string.IsNullOrWhiteSpace(rawValue) ? "172.24.217.139" : rawValue.Trim();
    }

    private void HideMenu()
    {
        Debug.Log("[ConnectionUI] Connection started - hiding menu, showing lobby");
        if (_menuPanel != null) _menuPanel.SetActive(false);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
    }

    private void ShowMenuWithError(string error)
    {
        Debug.LogError($"[ConnectionUI] {error}");
        if (_menuPanel != null) _menuPanel.SetActive(true);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
    }
}