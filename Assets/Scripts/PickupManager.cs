using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupManager : MonoBehaviour
{
    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject _heartPickupPrefab;
    [SerializeField] private GameObject _ammoPickupPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _heartSpawnPoints;
    [SerializeField] private Transform[] _ammoSpawnPoints;

    [Header("Settings")]
    [SerializeField] private float _respawnDelay = 10f;

    private Dictionary<Vector3, bool> _heartSpawnStatus = new Dictionary<Vector3, bool>();
    private Dictionary<Vector3, bool> _ammoSpawnStatus = new Dictionary<Vector3, bool>();

    private void Start()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }

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
        SpawnAllHearts();
        SpawnAllAmmo();
    }

    private void SpawnAllHearts()
    {
        if (_heartSpawnPoints == null || _heartSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[PickupManager] No heart spawn points assigned!");
            return;
        }

        foreach (var point in _heartSpawnPoints)
        {
            if (point != null)
            {
                _heartSpawnStatus[point.position] = true;
                SpawnHeart(point.position);
            }
        }
    }

    private void SpawnAllAmmo()
    {
        if (_ammoSpawnPoints == null || _ammoSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[PickupManager] No ammo spawn points assigned!");
            return;
        }

        foreach (var point in _ammoSpawnPoints)
        {
            if (point != null)
            {
                _ammoSpawnStatus[point.position] = true;
                SpawnAmmo(point.position);
            }
        }
    }

    public void OnHeartPickedUp(Vector3 position)
    {
        _heartSpawnStatus[position] = false;
        StartCoroutine(RespawnHeartAfterDelay(position));
    }

    public void OnAmmoPickedUp(Vector3 position)
    {
        _ammoSpawnStatus[position] = false;
        StartCoroutine(RespawnAmmoAfterDelay(position));
    }

    private IEnumerator RespawnHeartAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        if (!_heartSpawnStatus[position])
        {
            _heartSpawnStatus[position] = true;
            SpawnHeart(position);
        }
    }

    private IEnumerator RespawnAmmoAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        if (!_ammoSpawnStatus[position])
        {
            _ammoSpawnStatus[position] = true;
            SpawnAmmo(position);
        }
    }

    private void SpawnHeart(Vector3 position)
    {
        if (_heartPickupPrefab == null)
        {
            Debug.LogError("[PickupManager] Heart pickup prefab is null!");
            return;
        }

        GameObject go = Instantiate(_heartPickupPrefab, position, Quaternion.identity);

        HeartPickup pickup = go.GetComponent<HeartPickup>();
        if (pickup != null)
        {
            pickup.Init(this);
        }

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            InstanceFinder.ServerManager.Spawn(networkObject);
            Debug.Log($"[PickupManager] Spawned heart at {position}");
        }
    }

    private void SpawnAmmo(Vector3 position)
    {
        if (_ammoPickupPrefab == null)
        {
            Debug.LogError("[PickupManager] Ammo pickup prefab is null!");
            return;
        }

        GameObject go = Instantiate(_ammoPickupPrefab, position, Quaternion.identity);

        AmmoPickup pickup = go.GetComponent<AmmoPickup>();
        if (pickup != null)
        {
            pickup.Init(this);
        }

        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            InstanceFinder.ServerManager.Spawn(networkObject);
            Debug.Log($"[PickupManager] Spawned ammo at {position}");
        }
    }
}