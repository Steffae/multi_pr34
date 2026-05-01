using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Material pinkMat;
    [SerializeField] private GameObject canvas;

    public readonly SyncVar<string> Nickname = new SyncVar<string>("Player");
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);

    private bool _nicknameSent = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;

        SetPlayerColor(true);
    }

    private void Update()
    {
        // Отправляем ник ТОЛЬКО если это наш локальный игрок
        if (!_nicknameSent && IsOwner && IsClientInitialized)
        {
            _nicknameSent = true;
            SetNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    // ... остальные методы без изменений ...
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
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(2f);
        HideModelObserversRpc(true);
        yield return new WaitForSeconds(3f);
        TeleportToRandomSpawnPoint();
        yield return new WaitForSeconds(2f);
        HideModelObserversRpc(false);
        HP.Value = 100;
        IsAlive.Value = true;
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
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = newValue;
    }

    private void SetPlayerColor(bool isAlive)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        if (!isAlive) { renderer.material.color = Color.black; return; }
        if (OwnerId == 0) renderer.material.color = Color.pink;
        else if (pinkMat != null) renderer.material = pinkMat;
    }

    private void TeleportToRandomSpawnPoint()
    {
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnObjects.Length > 0)
        {
            int idx = Random.Range(0, spawnObjects.Length);
            transform.position = spawnObjects[idx].transform.position;
        }
        else
        {
            transform.position = new Vector3(Random.Range(1f, 5f), 1.5f, Random.Range(1f, 5f));
        }
    }
}