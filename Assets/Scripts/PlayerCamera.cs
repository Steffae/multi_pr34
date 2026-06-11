using FishNet.Object;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new(0f, 5.5f, -4f);
    [SerializeField] private float _lookAheadDistance = 3f;
    [SerializeField] private AudioListener audioListener;

    private Camera _cam;
    private bool _initialized = false;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
    }

    private void Update()
    {
        if (!_initialized && IsSpawned)
        {
            Initialize();
        }

        if (_cam != null)
        {
            Vector3 rotatedOffset = transform.rotation * _offset;
            _cam.transform.position = transform.position + rotatedOffset;
            _cam.transform.LookAt(transform.position + transform.forward * _lookAheadDistance);
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