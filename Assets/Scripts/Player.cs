using UnityEngine;
using UnityEngine.InputSystem;

public static class Player {

    private static Transform playerTransform;
    private static InputAction moveInputAction;

    private static float playerSpeed = 0.5f;

    public static void Init(Transform _playerTransform) {
        playerTransform = _playerTransform;
        moveInputAction = InputSystem.actions.FindAction("Move");
    }
    
    public static void Update() {
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        playerTransform.position += new Vector3(moveInput.x, moveInput.y, 0f) * playerSpeed * Time.deltaTime;
    }
    
}
