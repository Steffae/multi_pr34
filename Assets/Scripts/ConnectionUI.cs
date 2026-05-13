using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _lobbyPanel;  // добавить

    public static string PlayerNickname { get; private set; } = "Player";

    private void Start()
    {
        // При старте: меню видно, лобби скрыто
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
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            HideMenu();
        }
    }

    public void StartAsHost()
    {
        SaveNickname();
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
    }

    public void StartAsClient()
    {
        SaveNickname();
        InstanceFinder.ClientManager.StartConnection();
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        Debug.Log($"[ConnectionUI] Saved nickname: {PlayerNickname}");
    }

    private void HideMenu()
    {
        Debug.Log("[ConnectionUI] Connection started - hiding menu, showing lobby");
        if (_menuPanel != null)
        {
            _menuPanel.SetActive(false);
        }
        if (_lobbyPanel != null)
        {
            _lobbyPanel.SetActive(true);
        }
    }
}