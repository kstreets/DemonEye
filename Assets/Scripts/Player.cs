using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Player {

    private static GameManager gm;
    private static Limitter attackLimiter;
    
    private const float playerSpeed = 0.55f;
    private const float attackCooldown = 0.1f;
    private const float interactionRadius = 0.1f;

    public static void Init(GameManager gameManager) {
        gm = gameManager;
        gm.interactPrompt.SetActive(false);
        InitInventory();
        Cursor.visible = false;
    }
    
    public static void Update() {
        CheckForInteractions();
        UpdateInventory();
        
        if (InventoryIsOpen) return;
        
        Vector2 moveInput = gm.moveInputAction.ReadValue<Vector2>();
        gm.player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (playerSpeed * Time.deltaTime);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        gm.crosshairTrans.position = mousePos;

        if (gm.attackInputAction.IsPressed() && attackLimiter.TimeHasPassed(attackCooldown)) {
            Vector2 mouseWorldPos = gm.mainCamera.ScreenToWorldPoint(mousePos);

            Vector2 velocity = (mouseWorldPos - gm.player.PositionV2()).normalized * 2.1f;
            float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
            
            Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            GameObject projectile = GameObject.Instantiate(gm.projectilePrefab, gm.player.position + new Vector3(0f, 0.1f, 0f), projectileRotation);
            
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
        InitInventoryWithSlots(gm.inventoryParent, playerInventoryWidth, playerInventoryHeight); 
        
        const int cachedLootInventoryWidth = 3;
        const int cachedLootInventoryHeight = 4;
        InitInventoryWithSlots(gm.lootInventoryParent, cachedLootInventoryWidth, cachedLootInventoryHeight); 
        
        const int stashInventoryWidth = 3;
        const int stashInventoryHeight = 4;
        InitInventoryWithSlots(gm.stashInventoryParent, stashInventoryWidth, stashInventoryHeight); 

        void InitInventoryWithSlots(RectTransform parent, int width, int height) {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    Vector3 pos = new(parent.position.x, parent.position.y, 0f);
                    Vector3 offset = new(inventorySlotSizeWithPadding * i, -(inventorySlotSizeWithPadding * j), 0f);
                    GameObject slot = GameObject.Instantiate(gm.inventorySlotPrefab, pos + offset, Quaternion.identity, parent);
                    GameObject.Instantiate(gm.inventoryItemPrefab, pos + offset, Quaternion.identity, slot.transform);
                }
            }
            parent.gameObject.SetActive(false);
        }
    }

    private static void UpdateInventory() {
        if (gm.inventoryInputAction.WasPressedThisFrame()) {
            if (!InventoryIsOpen) {
                OpenPlayerInventory();
            }
            else {
                ClosePlayerInventory();
            }

            if (StashIsOpen) {
                CloseStashInventory();
            }

            if (!InventoryIsOpen) {
                // Disable loot inventory in case it is active, it won't always be
                gm.lootInventoryParent.gameObject.SetActive(false);
                return;
            }

            // Add nearby items to loot inventory
            {
                Collider2D[] cols = Physics2D.OverlapCircleAll(gm.player.position, interactionRadius, Masks.ItemMask);
                if (cols.Length <= 0) return;
                
                gm.lootInventoryParent.gameObject.SetActive(true);

                for (int i = 0; i < cols.Length; i++) {
                    Collider2D col = cols[i];
                    ItemData itemData = col.GetComponent<Item>().itemData;
                    gm.lootInventoryParent.GetChild(i).GetChild(0).GetComponent<Image>().sprite = itemData.inventorySprite;
                }
            }
        }

        if (!InventoryIsOpen || !StashIsOpen) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        int hoveredSlotIndex = -1;
        bool inventoryHovered = true;
        
        for (int i = 0; i < gm.inventoryParent.childCount; i++) {
            RectTransform rectTrans = gm.inventoryParent.GetChild(i).GetComponent<RectTransform>();
            bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
            if (mouseInRect) {
                hoveredSlotIndex = i;
                break;
            }
        }
        
        for (int i = 0; i < gm.stashInventoryParent.childCount; i++) {
            RectTransform rectTrans = gm.stashInventoryParent.GetChild(i).GetComponent<RectTransform>();
            bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
            if (mouseInRect) {
                hoveredSlotIndex = i;
                inventoryHovered = false;
                break;
            }
        }

        if (hoveredSlotIndex < 0) return;
        
        if (gm.attackInputAction.WasPressedThisFrame()) {
            if (inventoryHovered) {
                GameManager.InventoryItem inventoryItem = gm.GetPlayerInventoryItem(hoveredSlotIndex);
                if (inventoryItem == null) return;
                gm.AddItemToStashInventory(inventoryItem.itemData, inventoryItem.count);
                gm.RemoveItemFromPlayerInventory(hoveredSlotIndex);
            }
            else {
                GameManager.InventoryItem inventoryItem = gm.GetStashInventoryItem(hoveredSlotIndex);
                if (inventoryItem == null) return;
                gm.AddItemToPlayerInventory(inventoryItem.itemData, inventoryItem.count);
                gm.RemoveItemFromStashInventory(hoveredSlotIndex);
            }
        }
    }

    private static void OpenPlayerInventory() {
        gm.inventoryParent.gameObject.SetActive(true);
        gm.crosshairTrans.gameObject.SetActive(false);
        Cursor.visible = true;
        gm.RefreshPlayerInventoryDisplay();
    }

    private static void ClosePlayerInventory() {
        gm.inventoryParent.gameObject.SetActive(false);
        gm.crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    private static void OpenStashInventory() {
        gm.stashInventoryParent.gameObject.SetActive(true);
        gm.RefreshStashInventoryDisplay();
    }

    private static void CloseStashInventory() {
        gm.stashInventoryParent.gameObject.SetActive(false);
    }
    
    private static List<Collider2D> playerContacts = new(10);
    
    private static void CheckForInteractions() { 
        gm.interactPrompt.SetActive(false);
        
        Collider2D playerCol = gm.player.GetComponent<Collider2D>();
        int size = playerCol.GetContacts(playerContacts);
        
        for (int i = 0; i < size; i++) {
            Collider2D col = playerContacts[i];
            
            if (col.CompareTag(Tags.Pickup)) {
                gm.interactPrompt.SetActive(true);
                gm.interactPrompt.transform.position = gm.mainCamera.WorldToScreenPoint(col.transform.position + new Vector3(0f, 0.1f, 0f));
                if (gm.interactInputAction.WasPressedThisFrame()) {
                    gm.AddItemToPlayerInventory(col.GetComponent<Item>().itemData); 
                    GameObject.Destroy(col.gameObject);
                }
            }

            if (col.CompareTag(Tags.Crucible)) {
                gm.interactPrompt.SetActive(true);
                gm.interactPrompt.transform.position = gm.mainCamera.WorldToScreenPoint(col.transform.position + new Vector3(0f, 0.1f, 0f));
            }
            
            if (col.CompareTag(Tags.Stash)) {
                gm.interactPrompt.SetActive(true);
                gm.interactPrompt.transform.position = gm.mainCamera.WorldToScreenPoint(col.transform.position + new Vector3(0f, 0.1f, 0f));
                if (gm.interactInputAction.WasPressedThisFrame()) {
                    OpenPlayerInventory();
                    OpenStashInventory();
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        } 
    }

    private static bool InventoryIsOpen => gm.inventoryParent.gameObject.activeSelf;
    
    private static bool StashIsOpen => gm.stashInventoryParent.gameObject.activeSelf;

}