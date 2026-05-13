using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private string _gameSceneName = "GameScene";

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
            Debug.Log("[ConnectionUI] Connected! Loading game scene...");
            // Загружаем игровую сцену
            SceneManager.LoadScene(_gameSceneName);
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

        // Устанавливаем IP клиента, если введён
        string ip = _ipInput != null ? _ipInput.text.Trim() : "127.0.0.1";
        if (!string.IsNullOrEmpty(ip))
        {
            // У Tugboat транспорта можно задать адрес
            Debug.Log($"[ConnectionUI] Connecting to server: {ip}");
        }

        InstanceFinder.ClientManager.StartConnection();
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
        Debug.Log($"[ConnectionUI] Saved nickname: {PlayerNickname}");
    }
}