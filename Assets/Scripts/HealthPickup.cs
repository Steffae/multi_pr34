using Unity.Netcode;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;
    [SerializeField] private float _rotationSpeed = 90f;

    private PickupManager _manager;
    private Vector3 _spawnPosition;

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void Update()
    {
        // Визуальное вращение
        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Только сервер обрабатывает подбор
        if (!IsServer) return;

        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Мёртвый не подбирает
        if (!player.IsAlive.Value) return;

        // Не лечить при полном HP
        if (player.HP.Value >= 100) return;

        // Восстанавливаем HP
        player.HP.Value = Mathf.Min(100, player.HP.Value + _healAmount);

        Debug.Log($"[Server] Player {player.Nickname.Value} picked up health. HP: {player.HP.Value}");

        // Сообщаем менеджеру о подборе
        if (_manager != null)
        {
            _manager.OnPickedUp(_spawnPosition);
        }

        // Уничтожаем аптечку
        NetworkObject.Despawn(destroy: true);
    }
}