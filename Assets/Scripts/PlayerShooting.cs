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
        if (!IsOwner) return;
        if (_firePoint == null) return;
        if (_playerNetwork != null && !_playerNetwork.IsAlive.Value) return;

        ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(Vector3 position, Vector3 direction)
    {
        if (_playerNetwork != null && _playerNetwork.HP.Value <= 0) return;
        if (CurrentAmmo.Value <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        CurrentAmmo.Value--;

        GameObject projectileObj = Instantiate(_projectilePrefab, position + direction * 1.2f, Quaternion.LookRotation(direction));
        NetworkObject networkObject = projectileObj.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            base.Spawn(networkObject, base.Owner);
        }
        else
        {
            Destroy(projectileObj);
        }
    }
}