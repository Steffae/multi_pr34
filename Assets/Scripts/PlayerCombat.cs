using FishNet.Object;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 3f;

    private PlayerInputHandler _inputHandler;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (_inputHandler != null)
        {
            _inputHandler.OnAttackInput += OnAttackInput;
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (_inputHandler != null)
        {
            _inputHandler.OnAttackInput -= OnAttackInput;
        }
    }

    private void OnAttackInput()
    {
        // Проверяем владельца
        if (OwnerId != LocalConnection.ClientId) return;
        TryFindAndAttack();
    }

    private void TryFindAndAttack()
    {
        PlayerNetwork[] allPlayers = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        PlayerNetwork closestTarget = null;

        foreach (PlayerNetwork player in allPlayers)
        {
            if (player == _playerNetwork) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= _attackRange)
            {
                closestTarget = player;
                break;
            }
        }

        if (closestTarget != null)
        {
            TryAttack(closestTarget);
        }
    }

    public void TryAttack(PlayerNetwork target)
    {
        if (OwnerId != LocalConnection.ClientId || target == null) return;
        DealDamageServerRpc(target.NetworkObject.ObjectId, _damage);
    }

    [ServerRpc]
    private void DealDamageServerRpc(int targetObjectId, int damage)
    {
        NetworkObject targetObject = null;
        foreach (NetworkObject nob in FishNet.InstanceFinder.ServerManager.Objects.Spawned.Values)
        {
            if (nob.ObjectId == targetObjectId)
            {
                targetObject = nob;
                break;
            }
        }

        if (targetObject == null) return;

        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();

        if (targetPlayer == null || targetPlayer == _playerNetwork)
            return;

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance > _attackRange)
            return;

        int nextHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
        targetPlayer.HP.Value = nextHp;
    }
}