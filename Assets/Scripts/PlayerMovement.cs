using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
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
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (_inputHandler != null)
        {
            _inputHandler.OnMoveInput -= OnMoveInput;
        }
    }

    private void OnMoveInput(Vector2 input)
    {
        _moveInput = input;
    }

    private void Update()
    {
        // Если OwnerId = -1, объект еще не инициализирован
        if (OwnerId < 0) return;

        // Проверяем через IsOwner
        if (!IsOwner) return;

        PlayerNetwork playerNetwork = GetComponent<PlayerNetwork>();
        if (playerNetwork != null && !playerNetwork.IsAlive.Value) return;

        Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized * _speed;

        _verticalVelocity += _gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        _characterController.Move(move * Time.deltaTime);

        if (_characterController.isGrounded)
            _verticalVelocity = 0f;
    }
}