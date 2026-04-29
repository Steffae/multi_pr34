using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private int _maxAmmo = 10;

    private PlayerInputHandler _inputHandler;
    private PlayerNetwork _playerNetwork;
    private float _lastShotTime;

    public NetworkVariable<int> CurrentAmmo = new(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentAmmo.Value = _maxAmmo;
        }

        if (!IsOwner) return;

        if (_inputHandler != null)
        {
            _inputHandler.OnShootInput += OnShootInput;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (_inputHandler != null)
        {
            _inputHandler.OnShootInput -= OnShootInput;
        }
    }

    private void OnShootInput()
    {
        if (!IsOwner) return;
        if (_firePoint == null) return;

        // Локальная проверка для быстрого отклика
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 position, Vector3 direction, ServerRpcParams rpcParams = default)
    {
        // 1. Жив ли игрок?
        if (_playerNetwork != null && _playerNetwork.HP.Value <= 0)
        {
            Debug.Log("[Server] Shoot failed: player is dead");
            return;
        }

        // 2. Есть ли патроны?
        if (CurrentAmmo.Value <= 0)
        {
            Debug.Log("[Server] Shoot failed: no ammo");
            return;
        }

        // 3. Прошёл ли кулдаун?
        if (Time.time < _lastShotTime + _cooldown)
        {
            Debug.Log("[Server] Shoot failed: cooldown");
            return;
        }

        _lastShotTime = Time.time;
        CurrentAmmo.Value--;

        Debug.Log($"[Server] Shooting! Ammo left: {CurrentAmmo.Value}");

        // Спавним снаряд
        GameObject projectileObj = Instantiate(_projectilePrefab, position + direction * 1.2f, Quaternion.LookRotation(direction));
        NetworkObject networkObject = projectileObj.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        }
        else
        {
            Debug.LogError("[Server] Projectile prefab has no NetworkObject component!");
            Destroy(projectileObj);
        }
    }

    // Метод для восстановления патронов
    public void AddAmmo(int amount)
    {
        if (!IsServer) return;
        CurrentAmmo.Value = Mathf.Min(_maxAmmo, CurrentAmmo.Value + amount);
    }

    // Метод для полного восстановления патронов при респавне
    public void RefillAmmo()
    {
        if (!IsServer) return;
        CurrentAmmo.Value = _maxAmmo;
    }
}