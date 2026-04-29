using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        // Подписываемся на событие запуска сервера
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        // Если сервер уже запущен (например, при перезагрузке сцены)
        if (NetworkManager.Singleton.IsServer)
        {
            OnServerStarted();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("[PickupManager] Server started - spawning pickups");
        SpawnAll();
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
            networkObject.Spawn();
            Debug.Log($"[PickupManager] Spawned health pickup at {position}");
        }
        else
        {
            Debug.LogError("[PickupManager] Prefab has no NetworkObject component!");
            Destroy(go);
        }
    }
}