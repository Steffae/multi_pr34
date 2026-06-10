using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerUI : NetworkBehaviour
{
    [Header("Ammo Display")]
    [SerializeField] private TMP_Text _ammoText;

    [Header("Respawn Timer")]
    [SerializeField] private GameObject _respawnTimerPanel;
    [SerializeField] private TMP_Text _respawnTimerText;

    private PlayerShooting _playerShooting;
    private PlayerNetwork _playerNetwork;
    private bool _isDead = false;

    private void Awake()
    {
        // Ищем компоненты на родительском объекте
        _playerShooting = GetComponentInParent<PlayerShooting>();
        _playerNetwork = GetComponentInParent<PlayerNetwork>();

        Debug.Log($"[PlayerUI] Awake - Found PlayerShooting: {_playerShooting != null}, PlayerNetwork: {_playerNetwork != null}");
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Только свой игрок видит свой UI
        if (OwnerId != base.LocalConnection.ClientId)
        {
            gameObject.SetActive(false);
            return;
        }

        Debug.Log($"[PlayerUI] OnStartNetwork - OwnerId={OwnerId}, LocalClientId={LocalConnection.ClientId}");

        if (_playerShooting != null)
        {
            _playerShooting.CurrentAmmo.OnChange += OnAmmoChanged;
            OnAmmoChanged(0, _playerShooting.CurrentAmmo.Value, false);
        }
        else
        {
            Debug.LogError("[PlayerUI] PlayerShooting component not found on parent!");
        }

        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnChange += OnIsAliveChanged;
        }

        // Скрываем таймер респавна в начале
        if (_respawnTimerPanel != null)
            _respawnTimerPanel.SetActive(false);

        // Подписываемся на смену состояния игры
        if (GameManager.Instance != null)
        {
            GameManager.OnLocalGameStateChanged += OnGameStateChanged;
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (OwnerId != base.LocalConnection.ClientId) return;

        if (_playerShooting != null)
            _playerShooting.CurrentAmmo.OnChange -= OnAmmoChanged;
        if (_playerNetwork != null)
            _playerNetwork.IsAlive.OnChange -= OnIsAliveChanged;
        if (GameManager.Instance != null)
            GameManager.OnLocalGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        bool shouldShow = (newState == GameManager.GameState.InProgress);

        if (_ammoText != null)
            _ammoText.gameObject.SetActive(shouldShow);
        if (_respawnTimerPanel != null)
            _respawnTimerPanel.SetActive(shouldShow && _isDead);
    }

    private void OnAmmoChanged(int oldValue, int newValue, bool asServer)
    {
        if (_ammoText != null)
        {
            _ammoText.text = $"Balls: {newValue}";
            Debug.Log($"[PlayerUI] Ammo updated: {newValue}");
        }
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue, bool asServer)
    {
        _isDead = !newValue;

        if (_respawnTimerPanel != null)
        {
            bool shouldShowTimer = (GameManager.Instance != null &&
                GameManager.Instance.CurrentState.Value == GameManager.GameState.InProgress && _isDead);
            _respawnTimerPanel.SetActive(shouldShowTimer);
        }

        if (_isDead)
        {
            StartCoroutine(RespawnTimerCoroutine());
        }
    }

    private IEnumerator RespawnTimerCoroutine()
    {
        float timer = 3f;

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