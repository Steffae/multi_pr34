using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Headless mode detected. Starting server automatically...");
            InstanceFinder.ServerManager.StartConnection();
        }
        else
        {
            Debug.Log("[ServerAutoStart] Not in batch mode - waiting for manual start");
            // В редакторе сервер не запускаем автоматически
        }
    }
}