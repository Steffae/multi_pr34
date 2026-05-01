using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;

    public static string PlayerNickname { get; private set; } = "Player";

    private void Start()
    {
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
            // Скрываем меню после подключения
            HideMenu();
        }
    }

    public void StartAsHost()
    {
        SaveNickname();
        // Запускаем сервер и клиент
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
    }

    public void StartAsClient()
    {
        SaveNickname();
        // Подключаемся к серверу
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
        Debug.Log("[ConnectionUI] Connection started - hiding menu");
        if (_menuPanel != null)
        {
            _menuPanel.SetActive(false);
        }
    }
}