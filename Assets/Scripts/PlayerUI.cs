using FishNet.Object;
using TMPro;
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
        _playerShooting = GetComponentInParent<PlayerShooting>();
        _playerNetwork = GetComponentInParent<PlayerNetwork>();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Скрываем UI для чужих игроков
        if (OwnerId != base.LocalConnection.ClientId)
        {
            gameObject.SetActive(false);
            return;
        }

        // Подписываемся на изменения
        if (_playerShooting != null)
        {
            _playerShooting.CurrentAmmo.OnChange += OnAmmoChanged;
            // Показываем начальное значение
            OnAmmoChanged(0, _playerShooting.CurrentAmmo.Value, false);
        }

        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnChange += OnIsAliveChanged;
            OnIsAliveChanged(true, _playerNetwork.IsAlive.Value, false);
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (OwnerId != base.LocalConnection.ClientId) return;

        if (_playerShooting != null)
        {
            _playerShooting.CurrentAmmo.OnChange -= OnAmmoChanged;
        }

        if (_playerNetwork != null)
        {
            _playerNetwork.IsAlive.OnChange -= OnIsAliveChanged;
        }
    }

    private void OnAmmoChanged(int oldValue, int newValue, bool asServer)
    {
        if (_ammoText != null)
        {
            _ammoText.text = $"Ammo: {newValue}";
        }
    }

    private void OnIsAliveChanged(bool oldValue, bool newValue, bool asServer)
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