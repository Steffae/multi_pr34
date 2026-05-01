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

        if (OwnerId == base.LocalConnection.ClientId) return;

        if (_inputHandler != null)
        {
            _inputHandler.OnAttackInput += OnAttackInput;
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (OwnerId != base.LocalConnection.ClientId) return;

        if (_inputHandler != null)
        {
            _inputHandler.OnAttackInput -= OnAttackInput;
        }
    }

    private void OnAttackInput()
    {
        if (OwnerId != base.LocalConnection.ClientId) return;
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
        if (OwnerId != base.LocalConnection.ClientId || target == null) return;
        DealDamageServerRpc(target.NetworkObject.ObjectId, _damage);
    }

    [ServerRpc]
    private void DealDamageServerRpc(int targetObjectId, int damage)
    {
        // Ищем объект по NetworkObjectId
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

        Debug.Log($"[Server] {_playerNetwork.Nickname.Value} attacked {targetPlayer.Nickname.Value} for {damage} damage. HP: {nextHp}");
    }
}