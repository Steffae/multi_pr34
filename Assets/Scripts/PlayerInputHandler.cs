using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions _inputActions;

    public event Action<Vector2> OnMoveInput;
    public event Action OnAttackInput;
    public event Action OnShootInput;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Move.performed += OnMovePerformed;
        _inputActions.Player.Move.canceled += OnMoveCanceled;
        _inputActions.Player.Attack.performed += OnAttackPerformed;
        _inputActions.Player.Shoot.performed += OnShootPerformed;
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= OnMovePerformed;
        _inputActions.Player.Move.canceled -= OnMoveCanceled;
        _inputActions.Player.Attack.performed -= OnAttackPerformed;
        _inputActions.Player.Shoot.performed -= OnShootPerformed;
        _inputActions.Player.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        OnMoveInput?.Invoke(context.ReadValue<Vector2>());
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        OnMoveInput?.Invoke(Vector2.zero);
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnAttackInput?.Invoke();
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        OnShootInput?.Invoke();
    }
}