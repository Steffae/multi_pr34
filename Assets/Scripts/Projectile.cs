using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _lifetime = 5f;

    private bool _hasHit = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (base.IsServerInitialized)
        {
            Invoke(nameof(DespawnProjectile), _lifetime);
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;
        if (_hasHit) return;

        var target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;

        // Не наносим урон самому себе
        if (target.OwnerId == OwnerId) return;

        _hasHit = true;

        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;

        Debug.Log($"[Server] Projectile hit {target.Nickname.Value} for {_damage} damage. HP: {newHp}");

        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        CancelInvoke();

        if (base.IsSpawned)
        {
            base.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}