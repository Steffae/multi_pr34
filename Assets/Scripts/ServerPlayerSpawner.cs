using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;

    // Храним подключения, для которых ещё не заспавнен игрок
    private Dictionary<int, NetworkConnection> _pendingConnections = new Dictionary<int, NetworkConnection>();
    private bool _isMatchStarted = false;

    private void Start()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        // Подписываемся на изменение состояния игры
        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged += OnGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.InProgress && !_isMatchStarted)
        {
            _isMatchStarted = true;
            // Спавним всех ожидающих игроков
            SpawnAllPendingPlayers();
        }
        else if (newState == GameManager.GameState.WaitingForPlayers || newState == GameManager.GameState.ReadyCheck)
        {
            _isMatchStarted = false;
        }
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!InstanceFinder.ServerManager.Started) return;

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Сохраняем подключение, но не спавним сразу
            _pendingConnections[conn.ClientId] = conn;
            Debug.Log($"[ServerPlayerSpawner] Player {conn.ClientId} connected, waiting for match start");
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            _pendingConnections.Remove(conn.ClientId);
            Debug.Log($"[ServerPlayerSpawner] Player {conn.ClientId} disconnected");
        }
    }

    private void SpawnAllPendingPlayers()
    {
        foreach (var conn in _pendingConnections.Values)
        {
            SpawnPlayer(conn);
        }
    }

    private void SpawnPlayer(NetworkConnection ownerConnection)
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[ServerPlayerSpawner] Player prefab is null!");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition(ownerConnection.ClientId);
        NetworkObject playerObject = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);

        // Устанавливаем ник до спавна
        PlayerNetwork pn = playerObject.GetComponent<PlayerNetwork>();
        if (pn != null)
        {
            pn.SetNicknameServerRpc(ConnectionUI.PlayerNickname);
            // Устанавливаем начальные значения
            pn.HP.Value = 1; // 1 сердечко
        }

        // Сброс патронов
        PlayerShooting ps = playerObject.GetComponent<PlayerShooting>();
        if (ps != null)
        {
            ps.ResetAmmo();
        }

        InstanceFinder.ServerManager.Spawn(playerObject.gameObject, ownerConnection);
        Debug.Log($"[ServerPlayerSpawner] Spawned player for ClientId: {ownerConnection.ClientId}");
    }

    private Vector3 GetSpawnPosition(int clientId)
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            // Разные точки для разных игроков
            int index = clientId % _spawnPoints.Length;
            return _spawnPoints[index].position;
        }

        return new Vector3(0f, 1.5f, 0f);
    }
}