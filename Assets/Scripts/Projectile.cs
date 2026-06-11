using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private float _lifetime = 5f;

    private bool _hasHit = false;
    private PlayerNetwork _shooter;

    public void Init(PlayerNetwork shooter)
    {
        _shooter = shooter;
    }

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
        Debug.Log($"[Projectile] shooter={(_shooter != null ? _shooter.Nickname.Value + " hp=" + _shooter.HP.Value : "NULL")}, target={target.Nickname.Value} hp={target.HP.Value}");
        if (_shooter != null && _shooter.HP.Value < 9)
        {
            _shooter.HP.Value = _shooter.HP.Value + 1;
            Debug.Log($"[Server] {_shooter.Nickname.Value} stole a heart from {target.Nickname.Value}! Now: shooter={_shooter.HP.Value}, target={target.HP.Value}");
        }
        else if (_shooter != null)
        {
            Debug.Log($"[Projectile] Shooter HP not < 9, not stealing. shooterHP={_shooter.HP.Value}");
        }

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