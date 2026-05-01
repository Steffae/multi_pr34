using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;

    private void Start()
    {
        // Если не назначен в инспекторе - ищем автоматически
        if (_playerNetwork == null)
        {
            _playerNetwork = GetComponent<PlayerNetwork>();
            Debug.Log($"[PlayerView] Auto-found PlayerNetwork: {_playerNetwork != null}");
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        Debug.Log($"[PlayerView] OnStartNetwork - PlayerNetwork: {_playerNetwork != null}");
        Debug.Log($"[PlayerView] NicknameText: {_nicknameText != null}, HPText: {_hpText != null}");

        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnChange += OnNicknameChanged;
            _playerNetwork.HP.OnChange += OnHpChanged;

            // Показываем начальные значения (если они уже установлены)
            UpdateNickname(_playerNetwork.Nickname.Value);
            UpdateHP(_playerNetwork.HP.Value);

            Debug.Log($"[PlayerView] Subscribed. Initial - Name: '{_playerNetwork.Nickname.Value}', HP: {_playerNetwork.HP.Value}");
        }
        else
        {
            Debug.LogError("[PlayerView] PlayerNetwork is null!");
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnChange -= OnNicknameChanged;
            _playerNetwork.HP.OnChange -= OnHpChanged;
        }
    }

    private void OnNicknameChanged(string oldValue, string newValue, bool asServer)
    {
        Debug.Log($"[PlayerView] Nickname changed: '{oldValue}' -> '{newValue}'");
        UpdateNickname(newValue);
    }

    private void OnHpChanged(int oldValue, int newValue, bool asServer)
    {
        Debug.Log($"[PlayerView] HP changed: {oldValue} -> {newValue}");
        UpdateHP(newValue);
    }

    private void UpdateNickname(string nickname)
    {
        if (_nicknameText != null)
        {
            _nicknameText.text = nickname;
            Debug.Log($"[PlayerView] Updated nickname text to: {nickname}");
        }
        else
        {
            Debug.LogError("[PlayerView] Nickname text is null!");
        }
    }

    private void UpdateHP(int hp)
    {
        if (_hpText != null)
        {
            _hpText.text = $"HP: {hp}";
        }
    }
}