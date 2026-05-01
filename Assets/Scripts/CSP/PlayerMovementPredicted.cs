using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementPredicted : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;
    private float _verticalVelocity;
    private Vector2 _moveInput;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (_inputHandler != null)
        {
            _inputHandler.OnMoveInput += OnMoveInput;
        }

        base.TimeManager.OnTick += OnTick;
        base.TimeManager.OnPostTick += OnPostTick;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (_inputHandler != null)
        {
            _inputHandler.OnMoveInput -= OnMoveInput;
        }

        if (base.TimeManager != null)
        {
            base.TimeManager.OnTick -= OnTick;
            base.TimeManager.OnPostTick -= OnPostTick;
        }
    }

    private void OnMoveInput(Vector2 input)
    {
        _moveInput = input;
    }

    private void OnTick()
    {
        if (base.IsOwner)
        {
            MoveData md = new MoveData
            {
                Horizontal = _moveInput.x,
                Vertical = _moveInput.y
            };
            Replicate(md);
        }
        else
        {
            Replicate(default);
        }

        if (base.IsServerInitialized)
        {
            CreateReconcile();
        }
    }

    private void OnPostTick()
    {
    }

    public new void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            Position = transform.position,
            VerticalVelocity = _verticalVelocity
        };
        Reconcile(rd);
    }

    [Replicate]
    private void Replicate(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        if (!_characterController.enabled) return;

        PlayerNetwork playerNetwork = GetComponent<PlayerNetwork>();
        if (playerNetwork != null && !playerNetwork.IsAlive.Value) return;

        Vector3 move = new Vector3(md.Horizontal, 0f, md.Vertical).normalized * _speed;

        if (!_characterController.isGrounded)
        {
            _verticalVelocity += _gravity * (float)base.TimeManager.TickDelta;
        }
        else
        {
            _verticalVelocity = -0.1f;
        }

        move.y = _verticalVelocity;
        _characterController.Move(move * (float)base.TimeManager.TickDelta);
    }

    [Reconcile]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        _characterController.enabled = false;
        transform.position = rd.Position;
        _verticalVelocity = rd.VerticalVelocity;
        _characterController.enabled = true;
    }
}