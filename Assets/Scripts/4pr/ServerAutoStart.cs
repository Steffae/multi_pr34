using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        // Application.isBatchMode = true, когда Unity запущен без графики (headless/Dedicated Server)
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Headless mode detected. Starting server automatically...");
            InstanceFinder.ServerManager.StartConnection();
        }
        else
        {
            Debug.Log("[ServerAutoStart] Not in batch mode - server will be started manually via UI");
        }
    }
}