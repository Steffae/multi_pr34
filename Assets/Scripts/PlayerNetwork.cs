using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class SkinData
{
    public GameObject prefab;
    public Vector3 offset;
}

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Material pinkMat;
    [SerializeField] private GameObject canvas;
    [SerializeField] private SkinData[] _skinData;

    public readonly SyncVar<string> Nickname = new SyncVar<string>("Player");
    public readonly SyncVar<int> HP = new SyncVar<int>(1);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<int> SelectedSkin = new SyncVar<int>(0);

    private bool _nicknameSent = false;
    private bool _skinSent = false;
    private GameObject _activeSkinInstance;

    public static PlayerNetwork LocalInstance { get; private set; }
    public static int PendingSkinIndex = -1;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (base.Owner.IsLocalClient)
            LocalInstance = this;

        Nickname.OnChange += OnNicknameChanged;
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;

        SetPlayerColor(true);

        int initialSkin = SelectedSkin.Value;
        if (base.Owner.IsLocalClient && PendingSkinIndex >= 0)
            initialSkin = PendingSkinIndex;
        ApplySkin(initialSkin);
    }

    private void OnDestroy()
    {
        if (_activeSkinInstance != null)
            Destroy(_activeSkinInstance);
    }

    private void Update()
    {
        if (!IsOwner || !IsClientInitialized) return;

        if (!_nicknameSent)
        {
            _nicknameSent = true;
            SetNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        if (!_skinSent && PendingSkinIndex >= 0)
        {
            _skinSent = true;
            int idx = PendingSkinIndex;
            PendingSkinIndex = -1;
            SetSkinServerRpc(idx);
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

    [ServerRpc(RequireOwnership = false)]
    public void SetSkinServerRpc(int skinIndex)
    {
        int clamped = Mathf.Clamp(skinIndex, 0, 2);
        Debug.Log($"[PlayerNetwork] SetSkinServerRpc owner={OwnerId} index={clamped}");
        SelectedSkin.Value = clamped;
        ApplySkinObserversRpc(clamped);
    }

    [ObserversRpc]
    private void ApplySkinObserversRpc(int skinIndex)
    {
        Debug.Log($"[PlayerNetwork] ApplySkinObserversRpc owner={OwnerId} index={skinIndex}");
        ApplySkin(skinIndex);
    }

    private void ApplySkin(int skinIndex)
    {
        Debug.Log($"[PlayerNetwork] ApplySkin owner={OwnerId} index={skinIndex} data={_skinData?.Length}");
        if (_activeSkinInstance != null)
        {
            Destroy(_activeSkinInstance);
            _activeSkinInstance = null;
        }

        if (skinIndex < 0 || _skinData == null || skinIndex >= _skinData.Length || _skinData[skinIndex].prefab == null)
        {
            Debug.LogWarning($"[PlayerNetwork] ApplySkin FAILED: idx={skinIndex} data=null:{_skinData == null} len={(_skinData != null ? _skinData.Length : 0)}");
            return;
        }

        SkinData data = _skinData[skinIndex];
        _activeSkinInstance = Instantiate(data.prefab, transform);
        _activeSkinInstance.transform.localPosition = data.offset;
        _activeSkinInstance.transform.localRotation = Quaternion.identity;

        Renderer capsuleRenderer = GetComponent<Renderer>();
        if (capsuleRenderer != null)
            capsuleRenderer.enabled = false;
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

        TeleportToSpawnPoint();

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

    private void TeleportToSpawnPoint()
    {
        Vector3 targetPos;

        if (ServerPlayerSpawner.Instance != null)
        {
            targetPos = ServerPlayerSpawner.Instance.GetSpawnPositionForClient(OwnerId);
            Debug.Log($"[PlayerNetwork] {Nickname.Value} teleported to spawn point for client {OwnerId}: {targetPos}");
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
        if (renderer != null && renderer.enabled) renderer.enabled = !hide;
        if (_activeSkinInstance != null)
            _activeSkinInstance.SetActive(!hide);
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

        // Сбрасываем движение, чтобы игрока не выбрасывало после телепорта
        var movement = GetComponent<PlayerMovementPredicted>();
        if (movement != null)
        {
            movement.ResetState();
        }

        TeleportToSpawnPoint();
    }
}