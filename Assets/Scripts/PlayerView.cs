using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _heartsOverheadText;  // Текст сердечек над головой

    private void Start()
    {
        if (_playerNetwork == null)
        {
            _playerNetwork = GetComponent<PlayerNetwork>();
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnChange += OnNicknameChanged;
            _playerNetwork.HP.OnChange += OnHeartsChanged;

            UpdateNickname(_playerNetwork.Nickname.Value);
            UpdateHeartsOverhead(_playerNetwork.HP.Value);
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (_playerNetwork != null)
        {
            _playerNetwork.Nickname.OnChange -= OnNicknameChanged;
            _playerNetwork.HP.OnChange -= OnHeartsChanged;
        }
    }

    private void OnNicknameChanged(string oldValue, string newValue, bool asServer)
    {
        UpdateNickname(newValue);
    }

    private void OnHeartsChanged(int oldValue, int newValue, bool asServer)
    {
        UpdateHeartsOverhead(newValue);
    }

    private void UpdateNickname(string nickname)
    {
        if (_nicknameText != null)
        {
            _nicknameText.text = nickname;
        }
    }

    private void UpdateHeartsOverhead(int hearts)
    {
        if (_heartsOverheadText != null)
        {
            _heartsOverheadText.text = $"{hearts}";
        }
    }
}