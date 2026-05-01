using FishNet;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        // Подписываемся на событие запуска сервера
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }

        // Если сервер уже запущен (проверяем через ServerManager)
        if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started)
        {
            SpawnAll();
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    private void OnServerConnectionState(FishNet.Transporting.ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            Debug.Log("[PickupManager] Server started - spawning pickups");
            SpawnAll();
        }
    }

    private void SpawnAll()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[PickupManager] No spawn points assigned!");
            return;
        }

        foreach (var point in _spawnPoints)
        {
            if (point != null)
            {
                SpawnPickup(point.position);
            }
        }
    }

    public void OnPickedUp(Vector3 position)
    {
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        if (_healthPickupPrefab == null)
        {
            Debug.LogError("[PickupManager] Health pickup prefab is null!");
            return;
        }

        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);

        HealthPickup pickup = go.GetComponent<HealthPickup>();
        if (pickup != null)
        {
            pickup.Init(this);
        }

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            // Спавним объект в сети
            InstanceFinder.ServerManager.Spawn(networkObject);
            Debug.Log($"[PickupManager] Spawned health pickup at {position}");
        }
        else
        {
            Debug.LogError("[PickupManager] Prefab has no NetworkObject component!");
            Destroy(go);
        }
    }
}