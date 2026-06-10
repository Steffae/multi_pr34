using FishNet.Object;
using UnityEngine;

public class HeartPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 1;  // +1 сердечко

    private PickupManager _manager;
    private Vector3 _spawnPosition;

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void Update()
    {
        // Вращение для красоты
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        if (!player.IsAlive.Value) return;

        // Не больше 9 сердечек
        if (player.HP.Value >= 9) return;

        player.HP.Value = Mathf.Min(9, player.HP.Value + _healAmount);

        Debug.Log($"[Server] Player {player.Nickname.Value} picked up heart. Hearts: {player.HP.Value}");

        if (_manager != null)
        {
            _manager.OnHeartPickedUp(_spawnPosition);
        }

        base.Despawn(gameObject);
    }
}