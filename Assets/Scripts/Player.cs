using UnityEngine;
using UnityEngine.InputSystem;

public static class Player {

    private static GameManager gm;
    private const float playerSpeed = 0.55f;
    private const float attackCooldown = 0.1f;

    private static Limitter attackLimiter;

    public static void Init(GameManager gameManager) {
        gm = gameManager;
    }
    
    public static void Update() {
        Vector2 moveInput = gm.moveInputAction.ReadValue<Vector2>();
        gm.player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (playerSpeed * Time.deltaTime);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        gm.crosshairTrans.position = mousePos;

        if (gm.attackInputAction.IsPressed() && attackLimiter.TimeHasPassed(attackCooldown)) {
            Vector2 mouseWorldPos = gm.mainCamera.ScreenToWorldPoint(mousePos);

            Vector2 velocity = (mouseWorldPos - gm.player.PositionV2()).normalized * 2.1f;
            float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
            
            Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            GameObject projectile = GameObject.Instantiate(gm.projectilePrefab, gm.player.position, projectileRotation);
            
            gm.projectiles.Add(new() {
                timeAlive = 0f,
                trans = projectile.transform,
                velocity = velocity 
            });
        }
    }
    
}