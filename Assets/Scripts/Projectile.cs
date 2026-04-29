using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _lifetime = 5f;

    private bool _hasHit = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Запускаем таймер на уничтожение только после спавна
            Invoke(nameof(DespawnProjectile), _lifetime);
        }
    }

    private void Update()
    {
        // Двигаем снаряд вперёд
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Только сервер обрабатывает попадание
        if (!IsServer) return;

        // Защита от множественных попаданий в одном кадре
        if (_hasHit) return;

        var target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;

        // Не наносим урон самому себе
        if (target.OwnerClientId == OwnerClientId) return;

        _hasHit = true;

        // Наносим урон
        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;

        Debug.Log($"[Server] Projectile hit {target.Nickname.Value} for {_damage} damage. HP: {newHp}");

        // Уничтожаем снаряд
        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        CancelInvoke();

        // Проверяем, что объект действительно заспавнен в сети
        if (IsSpawned)
        {
            NetworkObject.Despawn(destroy: true);
        }
        else
        {
            // Если ещё не заспавнен — просто уничтожаем локально
            Destroy(gameObject);
        }
    }
}