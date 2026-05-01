using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;
    private FishNet.Connection.NetworkConnection _hostConnection;

    private void Start()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            _hostConnection = InstanceFinder.ClientManager.Connection;
            StartCoroutine(SpawnPlayerDelayed(_hostConnection));
        }
    }

    private void OnRemoteConnectionState(FishNet.Connection.NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Спавним только если это не хост-соединение
            if (_hostConnection == null || connection.ClientId != _hostConnection.ClientId)
            {
                StartCoroutine(SpawnPlayerDelayed(connection));
            }
        }
    }

    private IEnumerator SpawnPlayerDelayed(FishNet.Connection.NetworkConnection connection)
    {
        yield return new WaitForSeconds(1f);
        SpawnPlayer(connection);
    }

    private void SpawnPlayer(FishNet.Connection.NetworkConnection ownerConnection)
    {
        if (_playerPrefab == null) return;
        if (!InstanceFinder.ServerManager.Started) return;
        if (ownerConnection == null || !ownerConnection.IsValid) return;

        Vector3 spawnPosition = GetRandomSpawnPosition();
        NetworkObject playerObject = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);

        InstanceFinder.ServerManager.Spawn(playerObject.gameObject, ownerConnection);
        Debug.Log($"[PlayerSpawner] Spawned player for ClientId: {ownerConnection.ClientId}, OwnerId: {playerObject.OwnerId}");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].transform.position;
        }
        return new Vector3(0f, 1.5f, 0f);
    }
}