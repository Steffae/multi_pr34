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
        Debug.Log($"[PlayerInputHandler] Awake - InputActions created: {_inputActions != null}");
    }

    private void OnEnable()
    {
        if (_inputActions == null)
        {
            Debug.LogError("[PlayerInputHandler] InputActions is null!");
            return;
        }

        _inputActions.Player.Move.performed += OnMovePerformed;
        _inputActions.Player.Move.canceled += OnMoveCanceled;
        _inputActions.Player.Attack.performed += OnAttackPerformed;
        _inputActions.Player.Shoot.performed += OnShootPerformed;
        _inputActions.Player.Enable();

        Debug.Log("[PlayerInputHandler] Input actions enabled");
    }

    private void OnDisable()
    {
        if (_inputActions == null) return;

        _inputActions.Player.Move.performed -= OnMovePerformed;
        _inputActions.Player.Move.canceled -= OnMoveCanceled;
        _inputActions.Player.Attack.performed -= OnAttackPerformed;
        _inputActions.Player.Shoot.performed -= OnShootPerformed;
        _inputActions.Player.Disable();

        Debug.Log("[PlayerInputHandler] Input actions disabled");
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        //Debug.Log($"[PlayerInputHandler] Move performed: {input}");
        OnMoveInput?.Invoke(input);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        //Debug.Log("[PlayerInputHandler] Move canceled");
        OnMoveInput?.Invoke(Vector2.zero);
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        //Debug.Log("[PlayerInputHandler] Attack performed");
        OnAttackInput?.Invoke();
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        //Debug.Log("[PlayerInputHandler] Shoot performed");
        OnShootInput?.Invoke();
    }
}