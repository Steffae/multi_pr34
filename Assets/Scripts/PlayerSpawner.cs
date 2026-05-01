using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;

    private void Start()
    {
        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            StartCoroutine(SpawnPlayerWithDelay());
        }
    }

    private IEnumerator SpawnPlayerWithDelay()
    {
        // Ждем 2 секунды чтобы всё точно загрузилось
        yield return new WaitForSeconds(2f);

        if (_playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player prefab is not assigned!");
            yield break;
        }

        if (!InstanceFinder.ServerManager.Started)
        {
            Debug.LogError("[PlayerSpawner] Server is not started!");
            yield break;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        NetworkObject playerObject = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);

        var clients = InstanceFinder.ServerManager.Clients;

        if (clients.Count > 0)
        {
            foreach (var kvp in clients)
            {
                var connection = kvp.Value;
                if (connection.IsValid)
                {
                    InstanceFinder.ServerManager.Spawn(playerObject.gameObject, connection);
                    Debug.Log($"[PlayerSpawner] Spawned player with OwnerId: {playerObject.OwnerId}");
                    yield break;
                }
            }
        }

        Debug.LogWarning("[PlayerSpawner] No valid clients, spawning without owner");
        InstanceFinder.ServerManager.Spawn(playerObject.gameObject);
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