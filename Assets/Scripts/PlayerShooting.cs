using FishNet.Object;
using FishNet.Object.Synchronizing;
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

    public readonly SyncVar<int> CurrentAmmo = new SyncVar<int>(10);

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerNetwork = GetComponent<PlayerNetwork>();
        Debug.Log($"[PlayerShooting] Awake - InputHandler: {_inputHandler != null}");
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (IsServerInitialized)
        {
            CurrentAmmo.Value = _maxAmmo;
        }

        if (_inputHandler != null)
        {
            _inputHandler.OnShootInput += OnShootInput;
            Debug.Log("[PlayerShooting] Subscribed to shoot input");
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (_inputHandler != null)
        {
            _inputHandler.OnShootInput -= OnShootInput;
        }
    }

    private void OnShootInput()
    {
        Debug.Log($"[PlayerShooting] OnShootInput called. OwnerId: {OwnerId}, LocalClientId: {LocalConnection.ClientId}");

        // Временно убираем проверку владельца для отладки
        // if (OwnerId != LocalConnection.ClientId) return;

        if (_firePoint == null)
        {
            Debug.LogError("[PlayerShooting] FirePoint is null!");
            return;
        }

        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value)
        {
            Debug.Log("[PlayerShooting] Player is dead, can't shoot");
            return;
        }

        Debug.Log("[PlayerShooting] Calling ShootServerRpc");
        ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 position, Vector3 direction)
    {
        Debug.Log($"[Server] ShootServerRpc called. HP: {_playerNetwork?.HP.Value}, Ammo: {CurrentAmmo.Value}");

        if (_playerNetwork != null && _playerNetwork.HP.Value <= 0)
        {
            Debug.Log("[Server] Shoot failed: player is dead");
            return;
        }

        if (CurrentAmmo.Value <= 0)
        {
            Debug.Log("[Server] Shoot failed: no ammo");
            return;
        }

        if (Time.time < _lastShotTime + _cooldown)
        {
            Debug.Log("[Server] Shoot failed: cooldown");
            return;
        }

        _lastShotTime = Time.time;
        CurrentAmmo.Value--;

        Debug.Log($"[Server] Shooting! Ammo left: {CurrentAmmo.Value}");

        GameObject projectileObj = Instantiate(_projectilePrefab, position + direction * 1.2f, Quaternion.LookRotation(direction));
        NetworkObject networkObject = projectileObj.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            base.Spawn(networkObject, base.Owner);
            Debug.Log("[Server] Projectile spawned");
        }
        else
        {
            Debug.LogError("[Server] Projectile prefab has no NetworkObject component!");
            Destroy(projectileObj);
        }
    }
}