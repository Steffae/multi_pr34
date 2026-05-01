using FishNet.Object;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new(0f, 4.5f, -6f);
    [SerializeField] private AudioListener audioListener;

    private Camera _cam;
    private bool _initialized = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        // Никаких проверок здесь, всё делаем в Update
    }

    private void Update()
    {
        if (!_initialized && IsSpawned)
        {
            Initialize();
        }

        if (_cam != null)
        {
            _cam.transform.position = transform.position + _offset;
            _cam.transform.LookAt(transform.position);
        }
    }

    private void Initialize()
    {
        // Проверяем владельца через OwnerId
        bool isMine = (OwnerId == LocalConnection.ClientId);

        if (!isMine)
        {
            if (audioListener != null)
                audioListener.enabled = false;
            _initialized = true;
            enabled = false;
        }
        else
        {
            _cam = Camera.main;
            if (_cam != null)
            {
                _initialized = true;
                Debug.Log("[PlayerCamera] Camera set to follow this player");
            }
        }
    }
}