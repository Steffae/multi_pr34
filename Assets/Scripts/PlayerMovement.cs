using Unity.Netcode;
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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (_inputHandler != null)
        {
            _inputHandler.OnMoveInput += OnMoveInput;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

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
        // Двигается только владелец И только если жив
        if (!IsOwner) return;

        // Проверяем, жив ли игрок
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