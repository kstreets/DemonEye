using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class Player {

    private static GameManager gm;
    private static Limitter attackLimiter;
    
    private const float playerSpeed = 0.55f;
    private const float attackCooldown = 0.1f;
    private const float pickupRadius = 0.22f;

    public static void Init(GameManager gameManager) {
        gm = gameManager;
        gm.interactPrompt.SetActive(false);
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
        
        const int playerInventoryWidth = 3;
        const int playerInventoryHeight = 4;
        InstantiateInventorySlots(gm.inventoryParent, playerInventoryWidth, playerInventoryHeight); 
        gm.inventoryParent.gameObject.SetActive(false);
        
        const int cachedLootInventoryWidth = 3;
        const int cachedLootInventoryHeight = 4;
        InstantiateInventorySlots(gm.lootInventoryParent, cachedLootInventoryWidth, cachedLootInventoryHeight); 
        gm.lootInventoryParent.gameObject.SetActive(false);

        void InstantiateInventorySlots(RectTransform parent, int width, int height) {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    Vector3 pos = new(parent.position.x, parent.position.y, 0f);
                    Vector3 offset = new(inventorySlotSizeWithPadding * i, -(inventorySlotSizeWithPadding * j), 0f);
                    GameObject slot = GameObject.Instantiate(gm.inventorySlotPrefab, pos + offset, Quaternion.identity, parent);
                    GameObject.Instantiate(gm.inventoryItemPrefab, pos + offset, Quaternion.identity, slot.transform);
                }
            }
        }
    }

    private static void UpdateInventory() {
        if (gm.inventoryInputAction.WasPressedThisFrame()) {
            gm.inventoryParent.gameObject.SetActive(!gm.inventoryParent.gameObject.activeSelf);
            gm.inventoryIsOpen = gm.inventoryParent.gameObject.activeSelf;
            gm.crosshairTrans.gameObject.SetActive(!gm.inventoryIsOpen);
            Cursor.visible = gm.inventoryIsOpen;

            if (!gm.inventoryIsOpen) {
                // Disable loot inventory in case it is active, it won't always be
                gm.lootInventoryParent.gameObject.SetActive(false);
                return;
            }

            // Refresh player inventory display
            {
                foreach (Transform child in gm.inventoryParent.transform) {
                    child.GetComponentInChildren<InvetoryItemUI>().Clear();
                }

                for (int i = 0; i < gm.playerInventory.Count; i++) {
                    GameManager.InventoryItem item = gm.playerInventory[i];
                    gm.inventoryParent.GetChild(i).GetComponentInChildren<InvetoryItemUI>().Set(item.itemData.inventorySprite, item.count);
                }
            }

            // Add nearby items to loot inventory
            {
                Collider2D[] cols = Physics2D.OverlapCircleAll(gm.player.position, pickupRadius, Masks.ItemMask);
                if (cols.Length <= 0) return;
                
                gm.lootInventoryParent.gameObject.SetActive(true);

                for (int i = 0; i < cols.Length; i++) {
                    Collider2D col = cols[i];
                    ItemData itemData = col.GetComponent<Item>().itemData;
                    gm.lootInventoryParent.GetChild(i).GetChild(0).GetComponent<Image>().sprite = itemData.inventorySprite;
                }
            }
        }
    }

    private static void CheckForItemInteraction() {
        Collider2D col = Physics2D.OverlapCircle(gm.player.position, pickupRadius, Masks.ItemMask);
        gm.interactPrompt.SetActive(col);
        if (col) {
            gm.interactPrompt.transform.position = gm.mainCamera.WorldToScreenPoint(col.transform.position + new Vector3(0f, 0.1f, 0f));
            if (gm.interactInputAction.WasPressedThisFrame()) {
                gm.AddItemToPlayerInventory(col.GetComponent<Item>().itemData); 
                GameObject.Destroy(col.gameObject);
            }
        }
    }
    
    
}