using FishNet.Object;
using UnityEngine;

public class AmmoPickup : NetworkBehaviour
{
    [SerializeField] private int _ammoAmount = 1;  // +1 патрон

    private PickupManager _manager;
    private Vector3 _spawnPosition;

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        if (!player.IsAlive.Value) return;

        var shooting = other.GetComponent<PlayerShooting>();
        if (shooting == null) return;

        // Проверяем, не полный ли у игрока боезапас
        if (shooting.CurrentAmmo.Value >= 3)
        {
            Debug.Log($"[AmmoPickup] Player {player.Nickname.Value} already has max ammo (3)");
            return;  // Не собираем, если уже 3 патрона
        }

        shooting.AddAmmo(_ammoAmount);

        Debug.Log($"[AmmoPickup] Player {player.Nickname.Value} picked up ammo. Ammo: {shooting.CurrentAmmo.Value}");

        if (_manager != null)
        {
            _manager.OnAmmoPickedUp(_spawnPosition);
        }

        base.Despawn(gameObject);
    }
}