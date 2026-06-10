using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;  // Точки спавна (2 штуки)

    private Dictionary<int, int> _playerSpawnIndex = new Dictionary<int, int>();
    private int _nextSpawnIndex = 0;

    private void Start()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

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
        if (newState == GameManager.GameState.InProgress)
        {
            SpawnAllPendingPlayers();
        }
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!InstanceFinder.ServerManager.Started) return;

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Запоминаем для какого клиента какой индекс спавна
            _playerSpawnIndex[conn.ClientId] = _nextSpawnIndex;
            _nextSpawnIndex++;

            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState.Value == GameManager.GameState.InProgress)
            {
                SpawnPlayer(conn);
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            _playerSpawnIndex.Remove(conn.ClientId);
        }
    }

    private void SpawnAllPendingPlayers()
    {
        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            SpawnPlayer(conn);
        }
    }

    private void SpawnPlayer(NetworkConnection ownerConnection)
    {
        if (_playerPrefab == null) return;

        Vector3 spawnPosition = GetSpawnPositionForClient(ownerConnection.ClientId);
        NetworkObject playerObject = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);

        PlayerNetwork pn = playerObject.GetComponent<PlayerNetwork>();
        if (pn != null)
        {
            pn.SetNicknameServerRpc(ConnectionUI.PlayerNickname);
            pn.HP.Value = 1;
        }

        InstanceFinder.ServerManager.Spawn(playerObject.gameObject, ownerConnection);
        Debug.Log($"[ServerPlayerSpawner] Spawned player for client {ownerConnection.ClientId} at spawn point {_playerSpawnIndex[ownerConnection.ClientId]}");
    }

    public Vector3 GetSpawnPositionForClient(int clientId)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("[ServerPlayerSpawner] No spawn points assigned!");
            return Vector3.zero;
        }

        int spawnIndex = 0;
        if (_playerSpawnIndex.ContainsKey(clientId))
        {
            spawnIndex = _playerSpawnIndex[clientId] % _spawnPoints.Length;
        }

        return _spawnPoints[spawnIndex].position;
    }

    // Этот метод можно вызывать из PlayerNetwork при респавне
    public Vector3 GetSpawnPointForPlayer(int clientId)
    {
        return GetSpawnPositionForClient(clientId);
    }
}