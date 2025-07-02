using UnityEngine;
using UnityEngine.InputSystem;

public static class Player {

    private static GameManager gm;
    private const float playerSpeed = 0.55f;
    private const float attackCooldown = 0.1f;

    private static Limitter attackLimiter;

    public static void Init(GameManager gameManager) {
        gm = gameManager;
        InitInventory();
        Cursor.visible = false;
    }
    
    public static void Update() {
        CheckForItemInteraction();
        UpdateInventory();
        
        if (gm.inventoryIsOpen) {
            return;
        }
        
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
    
    private static void InitInventory() {
        const int inventorySlotSizeWithPadding = 110;
        const int invecntoryWidth = 3;
        const int invecntoryHeight = 4;
        
        for (int i = 0; i < invecntoryWidth; i++) {
            for (int j = 0; j < invecntoryHeight; j++) {
                Vector3 pos = new(gm.inventoryParent.position.x, gm.inventoryParent.position.y, 0f);
                pos += new Vector3(inventorySlotSizeWithPadding * i, -(inventorySlotSizeWithPadding * j), 0f);
                GameObject.Instantiate(gm.inventorySlotPrefab, pos, Quaternion.identity, gm.inventoryParent.transform);
            }
        }
        
        gm.inventoryParent.gameObject.SetActive(false);
    }

    private static void UpdateInventory() {
        if (gm.inventoryInputAction.WasPressedThisFrame()) {
            gm.inventoryParent.gameObject.SetActive(!gm.inventoryParent.gameObject.activeSelf);
            gm.inventoryIsOpen = gm.inventoryParent.gameObject.activeSelf;
            gm.crosshairTrans.gameObject.SetActive(!gm.inventoryIsOpen);
            Cursor.visible = gm.inventoryIsOpen;
        }
    }

    private static void CheckForItemInteraction() {
        const float pickupRadius = 0.1f;
        Collider2D col = Physics2D.OverlapCircle(gm.player.position, pickupRadius, Masks.ItemMask);
        if (col != null) {
            
        }
    }
    
    
}