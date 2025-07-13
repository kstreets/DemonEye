using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {

    public List<ItemData> allItems;
    public ItemData demonEyeItem;
    public EyeModifier fireRateModifier;
    public EyeModifier triShotModifier;
    
    public Transform player;
    public Camera mainCamera;
    public RectTransform crosshairTrans;
    public Transform resourceSpawnParent;
    public Transform exitPortalSpawnParent;

    public Transform hideoutParent;
    public Transform hellRaidParent;
    
    public GameObject projectilePrefab;
    public GameObject laserPrefab;
    public GameObject gemRockPrefab;
    public GameObject deadBodyPrefab;
    public GameObject exitPortalPrefab;
    public EnemyData defaultEnemy;

    public BaseAttack defaultAttack;
    public BaseAttack curBaseAttack;
    
    public EnemyWaveManager waveManager;
    
    [Header("Spawn Positions")]
    public Vector3 hideoutSpawnPosition;
    public Vector3 hellSpawnPosition;

    [Header("UI")]
    public RectTransform playerInventoryParent;
    public RectTransform lootInventoryParent;
    public RectTransform stashInventoryParent;
    public RectTransform crucibleParent;
    public GameObject inventorySlotPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject interactPrompt;
    public TextMeshProUGUI exitPortalStatusText;
    public Button crucibleForgeButton;
    
    public GameObject crucibleAttackSlot;
    public GameObject crucibleEyeSlot;
    public GameObject[] crucibleRuneSlots;
    
    [Header("Controls")]
    public InputAction moveInputAction;
    public InputAction attackInputAction;
    public InputAction interactInputAction;
    public InputAction inventoryInputAction;
    public InputAction selectItemInputAction;
    public InputAction splitStackInputAction;
    
    [NonSerialized] public List<Projectile> projectiles = new();
    [NonSerialized] public List<Enemy> enemies = new();
    public List<EyeModifier> equipedModifiers = new();
    
    public Dictionary<GameObject, Enemy> enemyLookup = new();
    
    private static Dictionary<string, ItemData> itemDataLookup = new();

    private Timer exitPortalTimer;

    private State hideoutState;
    private State raidState;
    private StateMachine gameStateMachine = new();
    
    private void Start() {
        Cursor.visible = false;
        
        foreach (ItemData itemData in allItems) {
            itemDataLookup.Add(itemData.uuid, itemData);
        }

        InitInventory();
        BuildSavePaths();
        LoadInventory(playerInventory);
        LoadInventory(stashInventory);

        Eye.Init(this);
        Eye.baseAttack = defaultAttack;
        
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        selectItemInputAction = InputSystem.actions.FindAction("SelectItem");
        splitStackInputAction = InputSystem.actions.FindAction("SplitStack");

        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEndter, OnRaidStateExit);
        
        crucibleForgeButton.onClick.AddListener(() => OnButtonClick(crucibleForgeButton));
    }

    private void Update() {
        gameStateMachine.Tick();
    }

    private void FixedUpdate() {
        FixedUpdateEnemies();
    }

    private void OnApplicationQuit() {
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
    }

    private void UpdateTimers() {
        exitPortalTimer.Tick();
        discoverLootTimer.Tick();
    }


    private void OnHideoutStateEnter() {
        hideoutParent.gameObject.SetActive(true);
        player.transform.position = hideoutSpawnPosition;
    }

    private void OnHideoutStateExit() {
        hideoutParent.gameObject.SetActive(false);
    }

    private void OnHideoutStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        UpdateCrucible();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
    }

    private void OnRaidStateEndter() {
        hellRaidParent.gameObject.SetActive(true);
        player.transform.position = hellSpawnPosition;
        InitExitPortal();
        InitWave();
        SpawnResources();
        AstarPath.active.Scan();
    }

    private void OnRaidStateExit() {
        ClearResources();
        ClearEnemies();
        ClearProjectiles();
        hellRaidParent.gameObject.SetActive(false);
    }

    private void OnRaidStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        SpawnEnemies();
        UpdateEnemies();
        UpdateWave();
    }
    
    
    private const float playerSpeed = 1.55f;
    private Limitter attackLimiter;
    private List<Collider2D> playerContacts = new(10);
    
    private void UpdatePlayer() {
        Eye.modifers = equipedModifiers;
        
        if (InventoryIsOpen) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (playerSpeed * Time.deltaTime);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        crosshairTrans.position = mousePos;

        if (attackInputAction.IsPressed() && Eye.CanShootPrimary()) {
            Eye.ShootPrimary();
        }
        Eye.Update();
    }
    
    private void CheckForInteractions() { 
        interactPrompt.SetActive(false);
        
        Collider2D playerCol = player.GetComponent<Collider2D>();
        int size = playerCol.GetContacts(playerContacts);
        
        for (int i = 0; i < size; i++) {
            Collider2D col = playerContacts[i];
            
            if (col.CompareTag(Tags.Pickup)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    TryAddItemToInventory(playerInventory, col.GetComponent<Item>().itemData); 
                    Destroy(col.gameObject);
                }
            }

            if (col.CompareTag(Tags.Crucible)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    OpenPlayerInventory();
                    OpenCrucibleInventory();
                }
            }
            
            if (col.CompareTag(Tags.Stash)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    OpenPlayerInventory();
                    OpenStashInventory();
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInvetoryPtr = deadBodyInventoriesLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                gameStateMachine.SetStateIfNotCurrent(hideoutState);
            }

            if (col.CompareTag(Tags.HellPortal)) {
                gameStateMachine.SetStateIfNotCurrent(raidState);
            }
        } 
    }

    private void EnableInteractionPrompt(Vector3 position) {
        interactPrompt.SetActive(true);
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position + new Vector3(0f, 0.1f, 0f));
    }
    
    
    private void UpdateCrucible() {
        if (ButtonIsPressed(crucibleForgeButton)) {
            InventoryItem eyeItem = null;
            foreach (InventorySlot slot in crucibleInventory) {
                if (slot.acceptableItemType == ItemData.ItemType.Eye) {
                    eyeItem = slot.item;
                    break;
                }
            }

            if (eyeItem == null) return;
            
            eyeItem.itemDataUuid = demonEyeItem.uuid;
            eyeItem.modifierUuids = new();
            
            foreach (InventorySlot slot in crucibleInventory) {
                if (slot.acceptableItemType == ItemData.ItemType.Eye || slot.item == null) continue;
                
                if (slot.acceptableItemType == ItemData.ItemType.Rune) {
                    eyeItem.modifierUuids.Add(slot.item.itemDataUuid);
                }
                if (slot.acceptableItemType == ItemData.ItemType.BaseAttack) {
                    eyeItem.baseAttackUuid = slot.item.itemDataUuid;
                }
                slot.item = null;
            }
            
            RefreshInventoryDisplay(crucibleInventory, crucibleParent);
        }
    }

    
    [Serializable]
    public class InventoryItem {
        public string itemDataUuid;
        public string baseAttackUuid;
        public List<string> modifierUuids;
        public int count;
        
        [NonSerialized] public bool notDiscovered;
        
        public ItemData Data => itemDataLookup[itemDataUuid];
        public bool IsFullStack => count == Data.maxStackCount;
    }

    public class InventorySlot {
        public InventoryItem item;
        public ItemData.ItemType? acceptableItemType;
        public bool disallowItemStacking;
        
        public bool AcceptsAllItems => !acceptableItemType.HasValue;
    }
    
    [NonSerialized] public InventorySlot[] playerInventory;
    [NonSerialized] public InventorySlot[] stashInventory;
    [NonSerialized] public InventorySlot[] crucibleInventory;
    [NonSerialized] public InventorySlot[] lootInvetoryPtr;

    private Timer discoverLootTimer;
    private int discoverLootIndex;
    
    private bool InventoryIsOpen => playerInventoryParent.gameObject.activeSelf;
    private bool StashIsOpen => stashInventoryParent.gameObject.activeSelf;
    private bool CrucibleIsOpen => crucibleParent.gameObject.activeSelf;
    private bool LootInventoryIsOpen => lootInventoryParent.gameObject.activeSelf;
    
    private void InitInventory() {
        crucibleParent.gameObject.SetActive(false);
        
        const int inventorySlotSizeWithPadding = 110;
        
        const int playerInventoryWidth = 3;
        const int playerInventoryHeight = 4;
        InitInventoryUiWithSlots(playerInventoryParent, playerInventoryWidth, playerInventoryHeight);
        playerInventory = new InventorySlot[playerInventoryWidth * playerInventoryHeight];
        playerInventory.InitalizeWithDefault();
        
        const int cachedLootInventoryWidth = 3;
        const int cachedLootInventoryHeight = 4;
        InitInventoryUiWithSlots(lootInventoryParent, cachedLootInventoryWidth, cachedLootInventoryHeight); 
        
        const int stashInventoryWidth = 3;
        const int stashInventoryHeight = 4;
        InitInventoryUiWithSlots(stashInventoryParent, stashInventoryWidth, stashInventoryHeight); 
        stashInventory = new InventorySlot[stashInventoryWidth * stashInventoryHeight];
        stashInventory.InitalizeWithDefault();

        crucibleInventory = new InventorySlot[5];  // 5 will be the default for now
        crucibleInventory.InitalizeWithDefault();
        for (int i = 0; i < crucibleInventory.Length; i++) {
            InventorySlotUI slotUi = crucibleParent.GetChild(i).GetComponent<InventorySlotUI>();
            crucibleInventory[i].acceptableItemType = slotUi.onlyAcceptedItemType;
            crucibleInventory[i].disallowItemStacking = slotUi.disallowItemStacking;
        }

        void InitInventoryUiWithSlots(RectTransform parent, int width, int height) {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    Vector3 pos = new(parent.position.x, parent.position.y, 0f);
                    Vector3 offset = new(inventorySlotSizeWithPadding * i, -(inventorySlotSizeWithPadding * j), 0f);
                    GameObject slot = Instantiate(inventorySlotPrefab, pos + offset, Quaternion.identity, parent);
                    Instantiate(inventoryItemPrefab, pos + offset, Quaternion.identity, slot.transform);
                }
            }
            parent.gameObject.SetActive(false);
        }
    }
    
    private void UpdateInventory() {
        if (inventoryInputAction.WasPressedThisFrame()) {
            if (!InventoryIsOpen) {
                OpenPlayerInventory();
            }
            else {
                ClosePlayerInventory();
            }

            if (StashIsOpen) CloseStashInventory();
            if (LootInventoryIsOpen) CloseLootInventory();
            if (CrucibleIsOpen) CloseCrucibleInventory();

            // Add nearby items to loot inventory
            {
                // Collider2D[] cols = Physics2D.OverlapCircleAll(player.position, interactionRadius, Masks.ItemMask);
                // if (cols.Length <= 0) return;
                //
                // lootInventoryParent.gameObject.SetActive(true);
                //
                // for (int i = 0; i < cols.Length; i++) {
                //     Collider2D col = cols[i];
                //     ItemData itemData = col.GetComponent<Item>().itemData;
                //     lootInventoryParent.GetChild(i).GetChild(0).GetComponent<Image>().sprite = itemData.inventorySprite;
                // }
            }
        }

        if (!InventoryIsOpen && (!StashIsOpen || !LootInventoryIsOpen)) return;
        
        if (!selectItemInputAction.WasPressedThisFrame() && !splitStackInputAction.WasPressedThisFrame()) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        if (InventoryIsOpen && StashIsOpen) {
            if (InventoryIsHovered(playerInventoryParent, out int playerSlotIndex)) {
                MoveItemBetweenInventories(playerInventory, stashInventory, playerSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(stashInventory, stashInventoryParent);
            }
            else if (InventoryIsHovered(stashInventoryParent, out int stashSlotIndex)) {
                MoveItemBetweenInventories(stashInventory, playerInventory, stashSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(stashInventory, stashInventoryParent);
            }
            return;
        }

        if (InventoryIsOpen && LootInventoryIsOpen) {
            if (InventoryIsHovered(lootInventoryParent, out int lootSlotIndex)) {
                MoveItemBetweenInventories(lootInvetoryPtr, playerInventory, lootSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(lootInvetoryPtr, lootInventoryParent);
            }
            return;
        }

        if (InventoryIsOpen && CrucibleIsOpen) {
            if (InventoryIsHovered(playerInventoryParent, out int playerSlotIndex)) {
                MoveItemBetweenInventories(playerInventory, crucibleInventory, playerSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(crucibleInventory, crucibleParent);
            }
            else if (InventoryIsHovered(crucibleParent, out int crucibleSlotIndex)) {
                MoveItemBetweenInventories(crucibleInventory, playerInventory, crucibleSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(crucibleInventory, crucibleParent);
            }
        }

        void MoveItemBetweenInventories(InventorySlot[] fromInventory, InventorySlot[] toInventory, int hoveredSlotIndex) {
            InventoryItem inventoryItem = GetInventoryItem(fromInventory, hoveredSlotIndex);
            if (inventoryItem == null || inventoryItem.notDiscovered) return;

            if (splitStackInputAction.WasPressedThisFrame() && inventoryItem.count > 1) {
                int firstHalf = inventoryItem.count / 2;
                int secondHalf = inventoryItem.count - firstHalf;
                InventoryAddResult splitResult = TryAddItemToInventory(toInventory, inventoryItem.Data, secondHalf);
                if (splitResult.type == InventoryAddResult.ResultType.Success) {
                    AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, firstHalf);
                }
                else if (splitResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
                    int keepItemCount = inventoryItem.count - splitResult.addedCount;
                    AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, keepItemCount);
                }
                return;
            }
            
            InventoryAddResult addResult = TryAddItemToInventory(toInventory, inventoryItem.Data, inventoryItem.count);
            if (addResult.type == InventoryAddResult.ResultType.Success) {
                RemoveItemFromInventory(fromInventory, hoveredSlotIndex);
            }
            else if (addResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - addResult.addedCount;
                AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, keepItemCount);
            }
        }

        bool InventoryIsHovered(Transform inventoryParent, out int hoveredSlotIndex) {
            hoveredSlotIndex = GetHoveredInventorySlot(inventoryParent);
            return hoveredSlotIndex >= 0;
        }

        int GetHoveredInventorySlot(Transform inventoryParent) {
            for (int i = 0; i < inventoryParent.childCount; i++) {
                RectTransform rectTrans = inventoryParent.GetChild(i).GetComponent<RectTransform>();
                bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
                if (mouseInRect) {
                    return i;
                }
            }
            return -1;
        }
    }

    public struct InventoryAddResult {
        public enum ResultType { Success, Failure, FailureToAddAll };
        public ResultType type;
        public int addedCount;
    }

    public InventoryAddResult TryAddItemToInventory(InventorySlot[] inventory, ItemData itemData, int count = 1) {
        InventoryAddResult result = new() {
            type = InventoryAddResult.ResultType.Failure
        };

        // If we can stack the item then we just do that
        foreach (InventorySlot slot in inventory) {
            if (slot.item == null || slot.disallowItemStacking || slot.item.IsFullStack || slot.item.Data != itemData) continue;

            int overflowAmount = (count + slot.item.count) - slot.item.Data.maxStackCount;
            if (overflowAmount > 0) {
                int addCount = slot.item.Data.maxStackCount - slot.item.count;
                
                slot.item.count += addCount;
                count = overflowAmount;
                
                result.addedCount += addCount;
                result.type = InventoryAddResult.ResultType.FailureToAddAll;
                continue;
            }
            
            slot.item.count += count;
            result.type = InventoryAddResult.ResultType.Success;
            return result;
        }

        // Otherwise add to empty inventory slot
        foreach (InventorySlot slot in inventory) {
            if (slot.item != null) continue;
            
            bool slotCanAcceptItemType = slot.AcceptsAllItems || slot.acceptableItemType == itemData.itemType;
            if (!slotCanAcceptItemType) continue;

            int addCount = slot.disallowItemStacking ? 1 : Mathf.Clamp(count, 0, itemData.maxStackCount);
            
            InventoryItem newItem = new() {
                itemDataUuid = itemData.uuid,
                count = addCount,
            };
            slot.item = newItem;
            
            result.type = addCount == count ? InventoryAddResult.ResultType.Success : InventoryAddResult.ResultType.FailureToAddAll;
            result.addedCount = addCount;
            return result;
        }
        
        return result;
    }
    
    private void RemoveItemFromInventory(InventorySlot[] inventory, int slotIndex) {
        inventory[slotIndex].item = null;
    }
    
    private InventoryItem GetInventoryItem(InventorySlot[] inventory, int slotIndex) {
        if (slotIndex < 0 || slotIndex >= inventory.Length) {
            return null;
        }
        return inventory[slotIndex].item;
    }
    
    private void AdjustItemCountInInventory(InventorySlot[] inventory, int slotIndex, int newCount) {
        InventoryItem item = GetInventoryItem(inventory, slotIndex);
        item.count = newCount;
    }

    private void RefreshInventoryDisplay(InventorySlot[] inventory, Transform inventoryParent) {
        if (!inventoryParent.gameObject.activeSelf) return;
        
        foreach (Transform child in inventoryParent.transform) {
            child.GetComponentInChildren<InventoryItemUI>()?.Clear();
        }

        for (int i = 0; i < inventory.Length; i++) {
            InventoryItem item = inventory[i].item;
            if (item == null || item.notDiscovered) continue;
            inventoryParent.GetChild(i).GetComponentInChildren<InventoryItemUI>().Set(item.Data, item.count);
        }
    }

    private void OpenPlayerInventory() {
        playerInventoryParent.gameObject.SetActive(true);
        crosshairTrans.gameObject.SetActive(false);
        Cursor.visible = true;
        RefreshInventoryDisplay(playerInventory, playerInventoryParent);
    }

    private void ClosePlayerInventory() {
        playerInventoryParent.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    private void OpenStashInventory() {
        stashInventoryParent.gameObject.SetActive(true);
        RefreshInventoryDisplay(stashInventory, stashInventoryParent);
    }

    private void CloseStashInventory() {
        stashInventoryParent.gameObject.SetActive(false);
    }

    private void OpenLootInventory() {
        discoverLootIndex = -1;
        lootInventoryParent.gameObject.SetActive(true);
        
        foreach (Transform child in lootInventoryParent.transform) {
            child.GetComponentInChildren<InventoryItemUI>().Clear();
        }

        for (int i = 0; i < lootInvetoryPtr.Length; i++) {
            if (lootInvetoryPtr[i].item == null) continue;
            if (lootInvetoryPtr[i].item.notDiscovered) {
                discoverLootIndex = i;
                break;
            }
            InventoryItem item = lootInvetoryPtr[i].item;
            lootInventoryParent.GetChild(i).GetComponentInChildren<InventoryItemUI>().Set(item.Data, item.count);
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        discoverLootTimer.SetTime(1f);
        discoverLootTimer.EndAction ??= () => {
            InventoryItem item = lootInvetoryPtr[discoverLootIndex].item;
            item.notDiscovered = false;
            
            lootInventoryParent.GetChild(discoverLootIndex).GetComponentInChildren<InventoryItemUI>().Set(item.Data, item.count);
            
            discoverLootIndex++;
            if (discoverLootIndex < lootInvetoryPtr.Length) {
                discoverLootTimer.SetTime(1f);
            }
        };
    }

    private void CloseLootInventory() {
        lootInventoryParent.gameObject.SetActive(false);
        discoverLootTimer.Stop();
    }

    private void OpenCrucibleInventory() {
        crucibleParent.gameObject.SetActive(true);
    }

    private void CloseCrucibleInventory() {
        crucibleParent.gameObject.SetActive(false); 
    }
    
    
    public struct Projectile {
        public Transform trans;
        public float timeAlive;
        public Vector2 velocity;
    }
    
    private void UpdateProjectiles() {
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.timeAlive += Time.deltaTime;
            proj.trans.position += proj.velocity.ToVector3() * Time.deltaTime;
            projectiles[i] = proj;
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, 0.1f, Masks.DamagableMask);
            if (col) {
                HandleDamage(col);
                Destroy(projectiles[i].trans.gameObject);
                projectiles.RemoveAt(i);
            }
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].timeAlive > 5f) {
                Destroy(projectiles[i].trans.gameObject);
                projectiles.RemoveAt(i);
            }
        }
    }

    private void ClearProjectiles() {
        foreach (Projectile projectile in projectiles) {
            Destroy(projectile.trans.gameObject);
        }
        projectiles.Clear();
    }

    public void HandleDamage(Collider2D col) {
        if (!col) return;
        
        if (col.CompareTag(Tags.Enemy)) {
            // We damage all very close enemies to elimate long trains
            Collider2D[] cols = Physics2D.OverlapCircleAll(col.transform.position, 0.12f, Masks.EnemyMask);
            foreach (Collider2D eCol in cols) {
                Enemy enemy = enemyLookup[eCol.gameObject];
                enemy.health -= 50;
            }
        }
        else {
            Mineable mineable = col.GetComponent<Mineable>();
            mineable.health -= 50;

            Vector3 spawnPos = col.transform.position + RandomOffset360(0.25f, 0.5f);
            spawnedResources.Add(Instantiate(mineable.dropPrefab, spawnPos, Quaternion.identity));

            if (mineable.health <= 0) {
                Destroy(mineable.gameObject);
            }
            
            AstarPath.active.UpdateGraphs(mineable.GetComponent<Collider2D>().bounds);
        }
    }
    

    public class Enemy {
        public Transform trans;
        public Rigidbody2D rigidbody;
        public EnemyData data;
        public PathData pathData = new();
        public int health;
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }
    }
    
    public class PathData {
        public ABPath abPath;
        public int waypointIndex;
        public bool isBeingCalculated;
        public float lastUpdateTime;
        
        public bool HasPath => abPath != null;
    }

    private void SpawnEnemies() {
        if (waveManager.enemiesLeftToSpawn <= 0 || enemies.Count > waveManager.enemySpawnLimit) return;

        Vector2 randomSpawnPos = player.position + RandomOffset360(3f, 4f);
        
        NNInfo info = AstarPath.active.graphs[0].GetNearest(randomSpawnPos, NNConstraint.Walkable);
        GameObject enemy = Instantiate(defaultEnemy.enemyPrefab, info.position, Quaternion.identity);
        
        enemies.Add(new() {
            trans = enemy.transform,
            data = defaultEnemy,
            rigidbody = enemy.GetComponent<Rigidbody2D>(),
            health = 100,
        });
        enemyLookup.Add(enemy, enemies[^1]);

        waveManager.enemiesLeftToSpawn--;
    }
    
    private void UpdateEnemies() {
        for (int i = enemies.Count - 1; i >= 0; i--) {
            if (enemies[i].health <= 0) {
                // Drop items from enemy 
                {
                    EnemyData.ItemDrop[] itemDrops = enemies[i].data.itemDrops;
                    foreach (EnemyData.ItemDrop itemDrop in itemDrops) {
                        float randomChance = Random.value;
                        if (randomChance < itemDrop.dropChance) {
                            GameObject drop = Instantiate(itemDrop.itemPrefab, enemies[i].trans.position, Quaternion.identity);
                            spawnedResources.Add(drop);
                        }
                    }
                }

                enemyLookup.Remove(enemies[i].trans.gameObject);
                Destroy(enemies[i].trans.gameObject);
                enemies.RemoveAt(i);
            }
        }

        foreach (Enemy enemy in enemies) {
            if ((enemy.pathData.HasPath && Time.time - enemy.pathData.lastUpdateTime <= 0.5f) || enemy.pathData.isBeingCalculated) continue;
            
            ABPath abPath = ABPath.Construct(enemy.position, player.position, path => {
                path.Claim(this);
                enemy.pathData.abPath?.Release(this);
                enemy.pathData.abPath = path as ABPath;
                enemy.pathData.waypointIndex = 1;
                enemy.pathData.isBeingCalculated = false;
                enemy.pathData.lastUpdateTime = Time.time;
            });
            
            AstarPath.StartPath(abPath);
            enemy.pathData.isBeingCalculated = true;
        }
    }

    private void FixedUpdateEnemies() {
        foreach (Enemy enemy in enemies) {
            if (enemy.pathData.abPath == null) continue;
            
            PathData pathData = enemy.pathData;
            
            bool usingPath = enemy.pathData.abPath.vectorPath.Count >= 2 && pathData.waypointIndex < pathData.abPath.vectorPath.Count;
            
            if (usingPath && Vector2.Distance(enemy.position, pathData.abPath.vectorPath[pathData.waypointIndex].ToVector2()) < 0.5f) {
                pathData.waypointIndex++;
            }
            
            usingPath = usingPath && pathData.waypointIndex < pathData.abPath.vectorPath.Count;

            Vector2 targetPos = usingPath ? pathData.abPath.vectorPath[pathData.waypointIndex] : player.position;
            Vector2 movePos = Vector2.MoveTowards(enemy.position, targetPos, 0.4f * Time.fixedDeltaTime);
            enemy.rigidbody.MovePosition(movePos);
        }
    }

    private void ClearEnemies() {
        foreach (Enemy enemy in enemies) {
            Destroy(enemy.trans.gameObject);
        }
        enemies.Clear();
        enemyLookup.Clear();
    }


    [Serializable]
    public class EnemyWaveManager {
        public float minTimeBetweenWaves;
        public float maxTimeBetweenWaves;
        public int startingWaveSize;
        public int startingSpawnLimit;
        public int waveSizeIncrement;

        public float curTimeBetweenWave;
        public int curWaveCount;
        public int enemiesLeftToSpawn;
        public int enemySpawnLimit;
    }

    private void InitExitPortal() {
        exitPortalTimer.SetTime(Random.Range(35f, 45f));
        
        exitPortalTimer.UpdateAction ??= () => {
            int totalSeconds = (int)exitPortalTimer.CurTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string formattedTime = $"{minutes}:{seconds:D2}";
            exitPortalStatusText.text = $"Exit Portal Countdown: {formattedTime}";
        };
        
        exitPortalTimer.EndAction ??= () => {
            int randomSpawnIndex = Random.Range(0, exitPortalSpawnParent.childCount);
            Transform exitPortalParent = exitPortalSpawnParent.GetChild(randomSpawnIndex);
            Instantiate(exitPortalPrefab, exitPortalParent.position, Quaternion.identity, exitPortalParent);
            exitPortalStatusText.text = $"Exit Portal: { exitPortalParent.name }";
        };
    }
    
    private void InitWave() {
        waveManager.curTimeBetweenWave = Random.Range(waveManager.minTimeBetweenWaves, waveManager.maxTimeBetweenWaves);
    }
    
    private void UpdateWave() {
        EnemyWaveManager wm = waveManager;
        
        wm.curTimeBetweenWave -= Time.deltaTime;
        if (wm.curTimeBetweenWave > 0f) return;

        wm.curTimeBetweenWave = Random.Range(wm.minTimeBetweenWaves, wm.maxTimeBetweenWaves);
        wm.enemiesLeftToSpawn = wm.startingWaveSize + wm.waveSizeIncrement * wm.curWaveCount;
        wm.enemySpawnLimit = wm.startingSpawnLimit + wm.waveSizeIncrement * wm.curWaveCount;
        wm.curWaveCount++;
    }
    

    public Vector3 RandomOffset360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }


    private List<GameObject> spawnedResources = new();
    private Dictionary<GameObject, InventorySlot[]> deadBodyInventoriesLookup = new();

    private void SpawnResources() {
        List<Transform> spawnPoints = resourceSpawnParent.GetComponentsInChildren<Transform>().ToList();
        spawnPoints.RemoveAt(0); // Remove resourceSpawnParent
        
        int gemRocksToSpawn = Random.Range(10, 13);
        for (int i = 0; i < gemRocksToSpawn; i++) {
            SpawnResource(gemRockPrefab, true);
        }
        
        int deadBodiesToSpawn = Random.Range(5, 10);
        for (int i = 0; i < deadBodiesToSpawn; i++) {
            int randomInventorySize = Random.Range(2, 6);
            InventorySlot[] inventory = new InventorySlot[randomInventorySize];
            
            for (int j = 0; j < randomInventorySize; j++) {
                InventoryItem lootItem = new() {
                    itemDataUuid = allItems[0].uuid, 
                    count = Random.Range(1, 10),
                    notDiscovered = true,
                };
                inventory[j].item = lootItem;
            }
            
            GameObject body = SpawnResource(deadBodyPrefab, false);
            deadBodyInventoriesLookup.Add(body, inventory);
        }

        GameObject SpawnResource(GameObject resourcePrefab, bool cutsNavmesh) {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnTrans = spawnPoints[randomIndex];
            spawnPoints.RemoveAt(randomIndex);
            
            GameObject resource = Instantiate(resourcePrefab, spawnTrans.position, resourcePrefab.transform.rotation);
            spawnedResources.Add(resource);

            if (cutsNavmesh) {
                AstarPath.active.UpdateGraphs(resource.GetComponent<Collider2D>().bounds);
            }

            return resource;
        }
    }

    private void ClearResources() {
        foreach (GameObject resource in spawnedResources) {
            Destroy(resource);
        }
        spawnedResources.Clear();
        deadBodyInventoriesLookup.Clear();
    }


    private string inventorySavePath;
    private string stashSavePath;
    private string crucibleSavePath;
    private List<InventoryItem> cachedInventoryForSaving = new(50);

    private void BuildSavePaths() {
        inventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
        crucibleSavePath = $"{Application.persistentDataPath}/crucible";
    }

    private string GetSavePath(InventorySlot[] inventory) {
        if (inventory == playerInventory)   return inventorySavePath;
        if (inventory == stashInventory)    return stashSavePath;
        if (inventory == crucibleInventory) return crucibleSavePath;
        return string.Empty;
    }
    
    private void SaveInventory(InventorySlot[] inventory) {
        cachedInventoryForSaving.Clear();
        foreach (InventorySlot slot in inventory) {
            cachedInventoryForSaving.Add(slot.item);     
        }
        SaveToFile(GetSavePath(inventory), cachedInventoryForSaving);
    }

    private void LoadInventory(InventorySlot[] inventory) {
        List<InventoryItem> items = LoadFromFile<List<InventoryItem>>(GetSavePath(inventory));
        CopyItemsToInventory(items, inventory);
    }

    private void CopyItemsToInventory(List<InventoryItem> items, InventorySlot[] toInventory) {
        if (items == null || toInventory == null) return;
        
        for (int i = 0; i < toInventory.Length; i++) {
            toInventory[i].item = items[i];
        }
    }

    private void SaveToFile(string path, object obj) {
        BinaryFormatter bf = new();
        using FileStream file = File.Create(path);
        bf.Serialize(file, obj);
    }

    private T LoadFromFile<T>(string path) where T : class {
        if (File.Exists(path)) {
            BinaryFormatter bf = new();
            using FileStream file = File.Open(path, FileMode.Open);
            return (T)bf.Deserialize(file);
        }
        return null;
    }


    private Dictionary<Button, float> clickedButtonLookup = new();
    
    private void OnButtonClick(Button clickedButton) {
        clickedButtonLookup[clickedButton] = Time.time;
    }

    private bool ButtonIsPressed(Button button) {
        if (clickedButtonLookup.TryGetValue(button, out float value)) {
            return value == Time.time;
        }
        return false;
    }
    
}
