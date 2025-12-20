using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public float gravity = -20f;

    private CharacterController _cc;
    private PlayerInputActions _input;
    private Vector2 _moveInput;
    private Vector3 _velocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _input.Enable();
        _input.Gameplay.Move.performed += OnMove;
        _input.Gameplay.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        _input.Gameplay.Move.performed -= OnMove;
        _input.Gameplay.Move.canceled -= OnMove;
        _input.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void Update()
    {
        Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);

        _cc.Move(move * moveSpeed * Time.deltaTime);

        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }
}
