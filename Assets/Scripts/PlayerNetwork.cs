using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Material pinkMat;
    [SerializeField] private GameObject canvas;

    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private static List<Transform> _spawnPoints = new List<Transform>();
    private NetworkTransform _networkTransform;

    private void Awake()
    {
        _networkTransform = GetComponent<NetworkTransform>();
    }

    public override void OnNetworkSpawn()
    {
        // Собираем точки спавна один раз
        if (_spawnPoints.Count == 0)
        {
            GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            foreach (GameObject obj in spawnObjects)
            {
                _spawnPoints.Add(obj.transform);
            }

            if (_spawnPoints.Count == 0)
            {
                Debug.LogWarning("[PlayerNetwork] No spawn points found with tag 'SpawnPoint'.");
            }
        }

        // Телепортируем только на сервере
        if (IsServer)
        {
            StartCoroutine(DelayedInitialTeleport());
        }

        SetPlayerColor(true);

        HP.OnValueChanged += OnHpChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;

        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    private IEnumerator DelayedInitialTeleport()
    {
        yield return null;
        TeleportToRandomSpawnPoint();
    }

    private void TeleportToRandomSpawnPoint()
    {
        Vector3 newPosition;

        if (_spawnPoints.Count > 0)
        {
            int idx = Random.Range(0, _spawnPoints.Count);
            newPosition = _spawnPoints[idx].position;
        }
        else
        {
            newPosition = new Vector3(Random.Range(1f, 5f), 1.5f, Random.Range(1f, 5f));
        }

        // Применяем телепортацию локально (для хоста)
        ApplyTeleport(newPosition);

        // Отправляем RPC клиентам
        TeleportClientRpc(newPosition);

        Debug.Log($"[Server] Teleported player to {newPosition}");
    }

    private void ApplyTeleport(Vector3 newPosition)
    {
        if (_networkTransform != null)
        {
            _networkTransform.enabled = false;
        }

        transform.position = newPosition;

        StartCoroutine(ReenableNetworkTransform());
    }

    private IEnumerator ReenableNetworkTransform()
    {
        yield return null;
        if (_networkTransform != null)
        {
            _networkTransform.enabled = true;
        }
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 newPosition)
    {
        // Применяем только для чистых клиентов (не сервер)
        if (!IsServer)
        {
            ApplyTeleport(newPosition);
            Debug.Log($"[Client] Teleported to {newPosition}");
        }
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= OnHpChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    private void OnHpChanged(int prev, int next)
    {
        if (!IsServer) return;

        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        Debug.Log($"[Server] Player {Nickname.Value} died. Respawning in 5 seconds...");

        // 0-2 сек: чёрный цвет уже установлен через OnIsAliveChanged

        // Ждём 2 секунды
        yield return new WaitForSeconds(2f);

        // Скрываем модель на всех клиентах
        HideModelClientRpc(true);

        // Ждём ещё 6 секунд
        yield return new WaitForSeconds(3f);

        // Телепортируем
        if (IsServer)
        {
            TeleportToRandomSpawnPoint();
        }

        yield return new WaitForSeconds(2f);

        // Показываем модель
        HideModelClientRpc(false);

        yield return null;

        HP.Value = 100;
        IsAlive.Value = true;

        Debug.Log($"[Server] Player {Nickname.Value} respawned at {transform.position}");
    }

    [ClientRpc]
    private void HideModelClientRpc(bool hide)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = !hide;
        }
        canvas.SetActive(!hide);
    }

    private void OnIsAliveChanged(bool prev, bool next)
    {
        SetPlayerColor(next);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = next;
        }
    }

    private void SetPlayerColor(bool isAlive)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        if (!isAlive)
        {
            renderer.material.color = Color.black;
            return;
        }

        if (OwnerClientId == 0)
            renderer.material.color = Color.pink;
        else
            renderer.material = pinkMat;
    }

#pragma warning disable CS0618
    [ServerRpc(RequireOwnership = false)]
#pragma warning restore CS0618
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }
}