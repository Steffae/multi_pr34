using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
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

        // Нельзя атаковать себя
        if (target.OwnerId == OwnerId) return;

        // НЕ АТАКУЕМ МЁРТВЫХ
        if (!target.IsAlive.Value) return;

        // НЕ АТАКУЕМ ЕСЛИ У ЦЕЛИ 0 СЕРДЕЧЕК
        if (target.HP.Value <= 0) return;

        _hasHit = true;

        // Забираем 1 сердечко у цели
        target.HP.Value = target.HP.Value - 1;

        // Добавляем сердечко стрелку
        var shooter = GetShooterPlayer();
        if (shooter != null && shooter.IsAlive.Value && shooter.HP.Value < 9)
        {
            shooter.HP.Value = shooter.HP.Value + 1;
            Debug.Log($"[Server] {shooter.Nickname.Value} stole a heart from {target.Nickname.Value}! Now: shooter={shooter.HP.Value}, target={target.HP.Value}");
        }

        DespawnProjectile();
    }

    private PlayerNetwork GetShooterPlayer()
    {
        if (OwnerId < 0) return null;

        foreach (var nob in FishNet.InstanceFinder.ServerManager.Objects.Spawned.Values)
        {
            if (nob.OwnerId == OwnerId)
            {
                return nob.GetComponent<PlayerNetwork>();
            }
        }
        return null;
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