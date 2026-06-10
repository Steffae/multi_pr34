using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Material pinkMat;
    [SerializeField] private GameObject canvas;

    public readonly SyncVar<string> Nickname = new SyncVar<string>("Player");
    public readonly SyncVar<int> HP = new SyncVar<int>(1);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

    private bool _nicknameSent = false;

    private static Vector3[] _cachedSpawnPoints;
    private static bool _spawnPointsCached = false;

    private void CacheSpawnPoints()
    {
        if (_spawnPointsCached) return;

        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnObjects.Length > 0)
        {
            _cachedSpawnPoints = new Vector3[spawnObjects.Length];
            for (int i = 0; i < spawnObjects.Length; i++)
            {
                _cachedSpawnPoints[i] = spawnObjects[i].transform.position;
                Debug.Log($"[PlayerNetwork] Cached spawn point {i}: {_cachedSpawnPoints[i]}");
            }
            _spawnPointsCached = true;
        }
        else
        {
            Debug.LogWarning("[PlayerNetwork] No spawn points found with tag 'SpawnPoint'");
            _cachedSpawnPoints = new Vector3[0];
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        CacheSpawnPoints();

        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;

        SetPlayerColor(true);
    }

    private void Update()
    {
        if (!_nicknameSent && IsOwner && IsClientInitialized)
        {
            _nicknameSent = true;
            SetNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        Nickname.OnChange -= OnNicknameChanged;
        HP.OnChange -= OnHpChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNicknameServerRpc(string nickname)
    {
        Nickname.Value = string.IsNullOrWhiteSpace(nickname)
            ? $"Player_{OwnerId}"
            : nickname.Trim();
    }

    private void OnNicknameChanged(string oldValue, string newValue, bool asServer) { }

    private void OnHpChanged(int oldValue, int newValue, bool asServer)
    {
        if (!IsServerInitialized) return;

        if (newValue <= 0 && IsAlive.Value)
        {
            Debug.Log($"[PlayerNetwork] {Nickname.Value} died! Starting respawn.");
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        Debug.Log($"[PlayerNetwork] RespawnRoutine START for {Nickname.Value}");

        yield return new WaitForSeconds(3f);

        HideModelObserversRpc(true);

        TeleportToCachedSpawnPoint();

        // Сброс патронов
        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.ResetAmmo();
        }


        HP.Value = 1;
        IsAlive.Value = true;

        HideModelObserversRpc(false);

        Debug.Log($"[PlayerNetwork] RespawnRoutine END for {Nickname.Value} at {transform.position}");
    }

    private void TeleportToCachedSpawnPoint()
    {
        Vector3 targetPos;
        if (_cachedSpawnPoints != null && _cachedSpawnPoints.Length > 0)
        {
            int idx = Random.Range(0, _cachedSpawnPoints.Length);
            targetPos = _cachedSpawnPoints[idx];
            Debug.Log($"[PlayerNetwork] {Nickname.Value} teleported to cached spawn point {idx}: {targetPos}");
        }
        else
        {
            targetPos = new Vector3(Random.Range(1f, 5f), 1.5f, Random.Range(1f, 5f));
            Debug.Log($"[PlayerNetwork] {Nickname.Value} teleported to fallback position: {targetPos}");
        }

        ApplyTeleport(targetPos);
    }

    private void ApplyTeleport(Vector3 position)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();

        TeleportObserversRpc(position);
    }

    [ObserversRpc]
    private void TeleportObserversRpc(Vector3 position)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = position;
        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();
    }

    [ObserversRpc]
    private void HideModelObserversRpc(bool hide)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = !hide;
        if (canvas != null) canvas.SetActive(!hide);
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue, bool asServer)
    {
        SetPlayerColor(newValue);
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = newValue;
    }

    private void SetPlayerColor(bool isAlive)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        if (!isAlive) { renderer.material.color = Color.black; return; }
        if (OwnerId == 0) renderer.material.color = Color.pink;
        else if (pinkMat != null) renderer.material = pinkMat;
    }

    public void ResetForMatch()
    {
        if (!IsServerInitialized) return;
        HP.Value = 1;
        IsAlive.Value = true;

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.ResetAmmo();
        }

        TeleportToCachedSpawnPoint();
    }
}