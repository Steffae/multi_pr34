using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupManager : MonoBehaviour
{
    private static PickupManager _instance;
    public static PickupManager Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
    }

    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject _heartPickupPrefab;
    [SerializeField] private GameObject _ammoPickupPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _heartSpawnPoints;
    [SerializeField] private Transform[] _ammoSpawnPoints;

    [Header("Settings")]
    [SerializeField] private float _respawnDelay = 10f;
    [SerializeField] private int _maxTotalHearts = 12;

    private Dictionary<Vector3, bool> _heartSpawnStatus = new Dictionary<Vector3, bool>();
    private Dictionary<Vector3, bool> _ammoSpawnStatus = new Dictionary<Vector3, bool>();
    private int _activeHeartCount = 0;
    private static bool _hasSpawnedPickups = false;

    private void Start()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }

        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged += OnGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }

        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Countdown &&
            InstanceFinder.ServerManager != null &&
            InstanceFinder.ServerManager.Started)
        {
            ResetAllHearts();
        }
    }

    private void OnServerConnectionState(FishNet.Transporting.ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started && !_hasSpawnedPickups)
        {
            _hasSpawnedPickups = true;
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
        _activeHeartCount--;
        StartCoroutine(RespawnHeartAfterDelay(position));
    }

    public void OnAmmoPickedUp(Vector3 position)
    {
        _ammoSpawnStatus[position] = false;
        StartCoroutine(RespawnAmmoAfterDelay(position));
    }

    private int GetTotalHearts()
    {
        int hpTotal = 0;
        foreach (var nob in InstanceFinder.ServerManager.Objects.Spawned.Values)
        {
            var pn = nob.GetComponent<PlayerNetwork>();
            if (pn != null)
                hpTotal += pn.HP.Value;
        }
        return hpTotal + _activeHeartCount;
    }

    public void ResetAllHearts()
    {
        StopAllCoroutines();

        // Деспавним все активные пикапы на поле
        List<NetworkObject> toDespawn = new List<NetworkObject>();
        foreach (var nob in InstanceFinder.ServerManager.Objects.Spawned.Values)
        {
            if (nob.GetComponent<HeartPickup>() != null || nob.GetComponent<AmmoPickup>() != null)
                toDespawn.Add(nob);
        }
        foreach (var nob in toDespawn)
        {
            InstanceFinder.ServerManager.Despawn(nob);
        }

        _activeHeartCount = 0;
        _heartSpawnStatus.Clear();
        _ammoSpawnStatus.Clear();

        // Спавним заново с учётом нового капа
        SpawnAllHearts();
        SpawnAllAmmo();
        Debug.Log($"[PickupManager] Reset all pickups for new match");
    }

    public bool CanPickupHeart()
    {
        return GetTotalHearts() <= _maxTotalHearts;
    }

    private bool CanSpawnHeart()
    {
        return GetTotalHearts() < _maxTotalHearts;
    }

    private IEnumerator RespawnHeartAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        if (!_heartSpawnStatus[position])
        {
            if (!CanSpawnHeart())
            {
                Debug.Log($"[PickupManager] Total hearts already {_maxTotalHearts}, skipping heart respawn");
                yield break;
            }
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

        if (!CanSpawnHeart())
        {
            Debug.Log($"[PickupManager] Total hearts cap ({_maxTotalHearts}) reached, not spawning heart");
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
            _activeHeartCount++;
            Debug.Log($"[PickupManager] Spawned heart at {position}. Active hearts: {_activeHeartCount}");
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