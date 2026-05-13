using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _lobbyPanel;

    public static string PlayerNickname { get; private set; } = "Player";

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

        // Устанавливаем IP клиента
        string ip = "127.0.0.1";
        if (_ipInput != null && !string.IsNullOrWhiteSpace(_ipInput.text))
        {
            ip = _ipInput.text.Trim();
        }

        Debug.Log($"[ConnectionUI] Connecting to server: {ip}");

        // Настраиваем транспорт Tugboat
        Tugboat transport = InstanceFinder.TransportManager.Transport as Tugboat;
        if (transport != null)
        {
            transport.SetClientAddress(ip);
            transport.SetPort((ushort)7770);
        }

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
        if (_menuPanel != null) _menuPanel.SetActive(false);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
    }
}