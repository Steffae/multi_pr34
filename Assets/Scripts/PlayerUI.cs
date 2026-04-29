using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _respawnTimerText;

    private PlayerShooting _playerShooting;
    private PlayerNetwork _playerNetwork;
    private bool _isDead;

    private void Awake()
    {
        // Используем GetComponentInParent, так как UI на дочернем объекте
        _playerShooting = GetComponentInParent<PlayerShooting>();
        _playerNetwork = GetComponentInParent<PlayerNetwork>();

        Debug.Log($"[PlayerUI] Awake - Shooting: {_playerShooting != null}, Network: {_playerNetwork != null}");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerUI] OnNetworkSpawn - IsOwner: {IsOwner}, OwnerClientId: {OwnerClientId}");

        // Скрываем UI для чужих игроков
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        // Подписываемся на изменения
        if (_playerShooting != null)
        {
            _playerShooting.CurrentAmmo.OnValueChanged += OnAmmoChanged;
            Debug.Log($"[PlayerUI] Subscribed to Ammo changes, current value: {_playerShooting.CurrentAmmo.Value}");
        }
        else
        {
            Debug.LogError("[PlayerUI] PlayerShooting is null!");
        }

        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnValueChanged += OnIsAliveChanged;
            OnIsAliveChanged(true, _playerNetwork.IsAlive.Value);
        }

        // Задержка для синхронизации NetworkVariable
        StartCoroutine(InitializeUIWithDelay());
    }

    private System.Collections.IEnumerator InitializeUIWithDelay()
    {
        yield return null;
        yield return null;

        Debug.Log($"[PlayerUI] Delayed init - Ammo value: {(_playerShooting != null ? _playerShooting.CurrentAmmo.Value : -1)}");

        if (_playerShooting != null)
        {
            OnAmmoChanged(0, _playerShooting.CurrentAmmo.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (_playerShooting != null)
        {
            _playerShooting.CurrentAmmo.OnValueChanged -= OnAmmoChanged;
        }

        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnValueChanged -= OnIsAliveChanged;
        }
    }

    private void OnAmmoChanged(int oldValue, int newValue)
    {
        Debug.Log($"[PlayerUI] OnAmmoChanged: {oldValue} -> {newValue}, Text exists: {_ammoText != null}");

        if (_ammoText != null)
        {
            _ammoText.text = $"Ammo: {newValue}";
        }
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue)
    {
        _isDead = !newValue;

        if (_respawnTimerText != null)
        {
            _respawnTimerText.gameObject.SetActive(_isDead);
        }

        if (_isDead)
        {
            StartCoroutine(RespawnTimerCoroutine());
        }
    }

    private System.Collections.IEnumerator RespawnTimerCoroutine()
    {
        float timer = 5f;

        while (timer > 0 && _isDead)
        {
            if (_respawnTimerText != null)
            {
                _respawnTimerText.text = $"Respawning in: {Mathf.CeilToInt(timer)}";
            }
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        if (_respawnTimerText != null)
        {
            _respawnTimerText.text = "";
        }
    }
}