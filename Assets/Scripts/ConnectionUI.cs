using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;

    // Сохраняем ник локально до появления сетевого объекта игрока.
    public static string PlayerNickname { get; private set; } = "Player";

    private void Start()
    {
        // Подписываемся на событие изменения состояния сети
        NetworkManager.Singleton.OnClientStarted += HideMenu;
        NetworkManager.Singleton.OnServerStarted += HideMenu;
    }

    private void OnDestroy()
    {
        // Отписываемся при уничтожении
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= HideMenu;
            NetworkManager.Singleton.OnServerStarted -= HideMenu;
        }
    }

    public void StartAsHost()
    {
        SaveNickname();
        // Хост одновременно является сервером и клиентом.
        NetworkManager.Singleton.StartHost();
    }

    public void StartAsClient()
    {
        SaveNickname();
        // Клиент только подключается к уже запущенному хосту/серверу.
        NetworkManager.Singleton.StartClient();
    }

    private void SaveNickname()
    {
        // Нормализуем ввод, чтобы сервер не получил пустую строку.
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void HideMenu()
    {
        // Скрываем панель меню после успешного подключения
        if (_menuPanel != null)
        {
            _menuPanel.SetActive(false);
        }

        // Также можно скрыть весь Canvas
        gameObject.SetActive(false);
    }
}