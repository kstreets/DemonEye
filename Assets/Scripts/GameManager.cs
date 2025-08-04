using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Pathfinding;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;

public partial class GameManager : MonoBehaviour {

    public List<Item> allItems;
    public CoreAttack defaultCoreAttack;
    public List<ItemPool> traderLevelPools;

    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public RectTransform crosshairTrans;
    public Transform exitPortalSpawnParent;

    public Transform smallMapParent;

    public GameObject playerPrefab;
    public GameObject laserPrefab;
    public GameObject gemRockPrefab;
    public GameObject altarPrefab;
    public GameObject deadBodyPrefab;
    public GameObject exitPortalPrefab;
    public EnemyData defaultEnemy;

    public CoreAttack defaultAttack;
    public Item demonEyeItem;
    
    public ItemPool deadBodyPool;
    public DropPool altarDropPool;
    public DropPool rockDropPool;

    [Header("Spawn Positions")]
    public Vector3 hellSpawnPosition;
    
    [Foldout("UI/Prefabs")]
    public GameObject inventorySlotPrefab;
    public GameObject inventoryItemPrefab;
    [EndFoldout]

    [Foldout("UI/MiscRefs")]
    public GameObject itemDescPopup;
    public Button enterNextRaidButton;
    public RectTransform hideoutHeaderParent;
    [EndFoldout]
    
    [Foldout("UI/HideoutTabs")]
    public RectTransform hideoutTabsParent;
    public Sprite tabNonSelectedSprite;
    public Sprite tabSelectedSprite;
    public Button characterTabButton;
    public Button eyeForgeTabButton;
    public Button traderTabButton;
    [EndFoldout]

    [Foldout("UI/PlayerPanel")]
    public RectTransform playerPanel;
    public RectTransform playerPocketParent;
    public RectTransform playerBackpackParent;
    public RectTransform playerPocketsBackpackParent;
    public RectTransform playerInventoryParent;
    [EndFoldout]
    
    [Foldout("UI/StashPanel")]
    public RectTransform stashPanel;
    public RectTransform stashInventoryParent;
    public TextMeshProUGUI stashValueText;
    [EndFoldout]
    
    [Foldout("UI/EyeForgePanel")]
    public RectTransform eyeForgePanel;
    public RectTransform crucibleParent;
    public Button crucibleForgeButton;
    [EndFoldout]
    
    [Foldout("UI/TraderPanel")]
    public RectTransform traderTransactionPanel;
    public RectTransform traderInventoryPanel;
    public RectTransform traderInventoryParent;
    public RectTransform traderTransactionInventoryParent;
    public TextMeshProUGUI traderTransactionInfoText;
    public Button traderDealButton;
    [EndFoldout]

    [Foldout("UI/InRaid")]
    public RectTransform lootInventoryPanel;
    public RectTransform lootInventoryParent;
    public GameObject interactPrompt;
    public TextMeshProUGUI exitPortalStatusText;
    [EndFoldout]
    
    [Header("Controls")]
    public InputAction moveInputAction;
    public InputAction attackInputAction;
    public InputAction interactInputAction;
    public InputAction inventoryInputAction;
    public InputAction selectItemInputAction;
    public InputAction splitStackInputAction;
    
    [NonSerialized] public List<Entity> entities = new();
    [NonSerialized] public Dictionary<GameObject, Entity> entityLookup = new();
    
    [NonSerialized] public List<Projectile> projectiles = new();
    
    [NonSerialized] public List<Enemy> enemies = new();
    [NonSerialized] public Dictionary<GameObject, Enemy> enemyLookup = new();
    
    public static Dictionary<string, Item> itemDataLookup = new();
    public static Dictionary<string, EyeModifier> eyeModifierLookup = new();
    public static Dictionary<string, CoreAttack> baseAttackLookup = new();

    private Timer exitPortalTimer;

    private State hideoutState;
    private State raidState;
    private StateMachine gameStateMachine = new();
    
    private void Start() {
        foreach (Item itemData in allItems) {
            if (itemData is EyeModifier mod) {
                eyeModifierLookup.Add(mod.uuid, mod);
            }
            else if (itemData is CoreAttack core) {
                baseAttackLookup.Add(core.uuid, core);
            }
            itemDataLookup.Add(itemData.uuid, itemData);
        }

        InitHideoutUI();
        InitInventory();
        BuildSavePaths();
        LoadInventory(playerInventory);
        LoadInventory(stashInventory);
        InitButtonCallbacks();
        AddItemsToTraderInventory(0);
        SetStashValue(0);

        equipedEye = new() { coreAttack = defaultAttack };
        
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        selectItemInputAction = InputSystem.actions.FindAction("SelectItem");
        splitStackInputAction = InputSystem.actions.FindAction("SplitStack");

        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEnter, OnRaidStateExit);
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
        
        RefreshInventoryDisplay(playerInventory);
        RefreshInventoryDisplay(stashInventory);
        RefreshInventoryDisplay(crucibleInventory);
        RefreshInventoryDisplay(transactionInventory);
    }

    private void OnHideoutStateExit() {
    }

    private void OnHideoutStateUpdate() {
        UpdateInventory();
    }

    private void OnRaidStateEnter() {
        playerPanel.gameObject.SetActive(false);
        stashPanel.gameObject.SetActive(false);
        eyeForgePanel.gameObject.SetActive(false);
        traderInventoryPanel.gameObject.SetActive(false);
        traderTransactionPanel.gameObject.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
        smallMapParent.gameObject.SetActive(false);
        hideoutHeaderParent.gameObject.SetActive(false);
        hideoutTabsParent.gameObject.SetActive(false);

        smallMapParent.gameObject.SetActive(true);
        Map map = smallMapParent.GetComponent<Map>();
        player = SpawnLevelEntity<Entity>(playerPrefab, hellSpawnPosition, Quaternion.identity);
        cinemachineCamera.Follow = player.trans;
        
        AstarPath.active.Scan();
        InitExitPortal();
        InitWave(map.waves);
        SpawnResources(map.resourceParent);
    }

    private void OnRaidStateExit() {
        DestroyLevelEntities();
        ClearProjectiles();
        smallMapParent.gameObject.SetActive(false);
    }

    private void OnRaidStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        CheckForEquipmentChange();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        UpdateWave();
        UpdateEnemies();
    }


    private Entity player;
    private const float playerSpeed = .75f;
    private Limitter attackLimiter;
    private List<Collider2D> playerContacts = new(10);
    
    private void UpdatePlayer() {
        if (InventoryIsOpen) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (playerSpeed * Time.deltaTime);

        if (moveInput.x < 0) {
            player.spriteRenderer.flipX = true;
        }
        else if (moveInput.x > 0) {
            player.spriteRenderer.flipX = false;
        }
        
        if (moveInput != Vector2.zero) {
            player.animator.Play("PlayerRun");
        }
        else {
            player.animator.Play("PlayerIdle");
        }
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        crosshairTrans.position = mousePos;

        if (attackInputAction.IsPressed() && CanShootPrimary()) {
            ShootPrimary();
        }
        UpdateEye();
    }
    
    private void CheckForInteractions() { 
        interactPrompt.SetActive(false);
        
        Collider2D playerCol = player.collider;
        int size = playerCol.GetContacts(playerContacts);
        
        for (int i = 0; i < size; i++) {
            Collider2D col = playerContacts[i];
            
            if (col.CompareTag(Tags.Pickup)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    TryAddItemToInventory(playerInventory, col.GetComponent<ItemReference>().item, 1); 
                    DestroyEntity(col.gameObject);
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInvetoryPtr.slots = deadBodySlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                gameStateMachine.SetStateIfNotCurrent(hideoutState);
            }
        } 
    }

    private void EnableInteractionPrompt(Vector3 position) {
        interactPrompt.SetActive(true);
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position + new Vector3(0f, 0.1f, 0f));
    }

    private InventoryItem prevEquippedEyeItem;
    
    private void CheckForEquipmentChange() {
        InventoryItem curItem = playerInventory.slots[0].item;

        if (prevEquippedEyeItem == curItem) return;
        
        prevEquippedEyeItem = curItem;
        
        if (curItem == null) {
            equipedEye = new() { coreAttack = defaultAttack };
            return;
        }

        equipedEye = eyeInstanceFromItemId[curItem.itemDataUuid];
    }
    
    
    [Serializable]
    public class InventoryItem {
        public string itemDataUuid;
        public List<string> modifierUuids;
        public int count = 1;

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public Item _itemRef; // Used for items created at runtime, like demon eyes

        public Item ItemRef => _itemRef ? _itemRef : itemDataLookup[itemDataUuid];
        public bool IsFullStack => count == ItemRef.maxStackCount;

        public InventoryItem(Item item = null, int count = 1) {
            if (item == null) return;
            this.itemDataUuid = item.uuid;
            this.count = count;
        }
        
        public InventoryItem Clone() {
            InventoryItem clonedItem = new() {
                itemDataUuid = itemDataUuid,
                count = count,
                notDiscovered = notDiscovered,
                _itemRef = ItemRef,
            };

            if (modifierUuids != null) {
                foreach (string modifierUuid in modifierUuids) {
                    clonedItem.modifierUuids ??= new();     
                    clonedItem.modifierUuids.Add(modifierUuid);
                }
            }

            return clonedItem;
        }

    }

    public class InventorySlot {
        public InventoryItem item;
        public InventorySlotUI ui;
    }

    public class Inventory {
        public InventorySlot[] slots;
        public RectTransform parent;
    }
    
    [NonSerialized] public Inventory playerInventory;
    [NonSerialized] public Inventory stashInventory;
    [NonSerialized] public Inventory crucibleInventory;
    [NonSerialized] public Inventory traderInventory;
    [NonSerialized] public Inventory transactionInventory;
    [NonSerialized] public Inventory lootInvetoryPtr;
    [NonSerialized] public List<Inventory> allInventories = new();

    private Timer discoverLootTimer;
    private int discoverLootIndex;

    private int stashValue;

    private enum TransactionInvetoryState { Empty, Buying, Selling }
    private TransactionInvetoryState transactionState;
    
    private bool InventoryIsOpen => playerPanel.gameObject.activeInHierarchy;
    private bool LootInventoryIsOpen => lootInventoryPanel.gameObject.activeInHierarchy;

    private bool OnCharacterTab => characterTabButton.image.sprite == tabSelectedSprite;
    private bool OnEyeForgeTab => eyeForgeTabButton.image.sprite == tabSelectedSprite;
    private bool OnTradingTab => traderTabButton.image.sprite == tabSelectedSprite;
    
    private void InitInventory() {
        const int pocketSize = 6;
        const int backpackSize = 9;
        const int playerInventorySize = pocketSize + backpackSize;
        SpawnUiSlots(playerPocketParent, pocketSize);
        SpawnUiSlots(playerBackpackParent, backpackSize);
        playerInventory = CreateInventory(playerInventoryParent, playerInventorySize + 3); 
        
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(lootInventoryParent, cachedLootInventorySize); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventorySize);
        
        const int stashInventorySize = 12;
        SpawnUiSlots(stashInventoryParent, stashInventorySize);
        stashInventory = CreateInventory(stashInventoryParent, stashInventorySize);
        
        const int traderInventorySize = 12;
        SpawnUiSlots(traderInventoryParent, traderInventorySize);
        traderInventory = CreateInventory(traderInventoryParent, traderInventorySize);
        
        const int transactionInventorySize = 20;
        SpawnUiSlots(traderTransactionInventoryParent, transactionInventorySize);
        transactionInventory = CreateInventory(traderTransactionInventoryParent, transactionInventorySize);

        const int crucibleInventorySize = 9;
        // Spawn crucible slots
        { 
            const int crucibleVeinSize = crucibleInventorySize - 1;
            Vector2 crucibleCenter = crucibleParent.position;
            GameObject centerSlot = Instantiate(inventorySlotPrefab, crucibleCenter, Quaternion.identity, crucibleParent);
            Instantiate(inventoryItemPrefab, crucibleCenter, Quaternion.identity, centerSlot.transform);

            InventorySlotUI centerSlotUi = centerSlot.GetComponent<InventorySlotUI>();
            centerSlotUi.disallowItemStacking = true;
            centerSlotUi.acceptsAllTypes = false;
            centerSlotUi.onlyAcceptedItemType = Item.ItemType.Eye;
            
            for (int i = 0; i < crucibleVeinSize; i++) {
                float deg = 360f / crucibleVeinSize * i;
                Vector2 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 150f;
                GameObject slot = Instantiate(inventorySlotPrefab, crucibleCenter + spawnDir, Quaternion.identity, crucibleParent);
                Instantiate(inventoryItemPrefab, crucibleCenter + spawnDir, Quaternion.identity, slot.transform);
                InventorySlotUI veinSlot = slot.GetComponent<InventorySlotUI>();
                veinSlot.disallowItemStacking = true;
                veinSlot.acceptsAllTypes = false;
                veinSlot.onlyAcceptedItemType = Item.ItemType.Vein;
            }
        }
        crucibleInventory = CreateInventory(crucibleParent, crucibleInventorySize);
        
        void SpawnUiSlots(RectTransform parent, int numSlots) {
            for (int i = 0; i < numSlots; i++) {
                GameObject slot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity, parent);
                Instantiate(inventoryItemPrefab, Vector3.zero, Quaternion.identity, slot.transform);
            }
        }
        
        Inventory CreateInventory(RectTransform uiParent, int slotCount) {
            Inventory inventory = new() {
                parent = uiParent,
                slots = new InventorySlot[slotCount]
            };
            inventory.slots.InitalizeWithDefault();

            InventorySlotUI[] slotUis = inventory.parent.GetComponentsInChildren<InventorySlotUI>(true);
            for (int i = 0; i < inventory.slots.Length; i++) {
                inventory.slots[i].ui = slotUis[i];
            }
            
            allInventories.Add(inventory);
            return inventory;
        }
    }
    
    private void UpdateInventory() {
        bool inRaid = gameStateMachine.CurState == raidState;
        
        if (inRaid) {
            if (inventoryInputAction.WasPressedThisFrame()) {
                if (!InventoryIsOpen) {
                    OpenPlayerInventory();
                }
                else {
                    ClosePlayerInventory();
                }
                if (LootInventoryIsOpen) {
                    CloseLootInventory();
                }
            }
            
            if (!InventoryIsOpen && !LootInventoryIsOpen) {
                HideItemTooltip();
                return;
            }
        }

        InventoryHoverInfo invHoverInfo = UpdateInventoryHover();
        UpdateItemtooltip();
        HandleItemClicked();

        void UpdateItemtooltip() {
            if (!TryGetItemFromHoverInfo(out InventoryItem _)) {
                HideItemTooltip();
                return;
            }
             
            const float hoverTimeUntilTooltip = 0.32f;
            bool spentEnoughTimeHovering = invHoverInfo.timeSpentHovering >= hoverTimeUntilTooltip;
            if (spentEnoughTimeHovering) {
                ShowItemTooltip(invHoverInfo);
            }
            else {
                HideItemTooltip();
            }
        }

        void HandleItemClicked() {
            if (!selectItemInputAction.WasPressedThisFrame() && !splitStackInputAction.WasPressedThisFrame()) return;

            Inventory hoveredInventory = invHoverInfo.hoveredInventory;
            if (hoveredInventory == null) return;

            bool hoveredItemIsEquipment = TryGetItemFromHoverInfo(out InventoryItem hoveredItem) ? hoveredItem.ItemRef.type == Item.ItemType.DemonEye : false;
            
            Inventory destinationInventory = null;

            if (inRaid) {
                if (hoveredInventory == playerInventory && LootInventoryIsOpen) {
                    destinationInventory = lootInvetoryPtr;
                }
                else if (hoveredInventory == lootInvetoryPtr) {
                    destinationInventory = playerInventory;
                }
            }
            else if (OnCharacterTab) {
                if (hoveredInventory == playerInventory) {
                    destinationInventory = stashInventory;
                }
                else if (hoveredInventory == stashInventory) {
                    destinationInventory = playerInventory;
                }
            }
            else if (OnEyeForgeTab) {
                if (hoveredInventory == stashInventory) {
                    destinationInventory = hoveredItemIsEquipment ? playerInventory : crucibleInventory;
                }
                else if (hoveredInventory == crucibleInventory) {
                    destinationInventory = stashInventory;
                }
                else if (hoveredInventory == playerInventory) {
                    destinationInventory = stashInventory;
                }
            }
            else if (OnTradingTab) {
                if (transactionState == TransactionInvetoryState.Buying) {
                    if (hoveredInventory == traderInventory) {
                        destinationInventory = transactionInventory;
                    }
                    else if (hoveredInventory == transactionInventory) {
                        destinationInventory = traderInventory;
                    }
                }
                else if (transactionState == TransactionInvetoryState.Selling) {
                    if (hoveredInventory == stashInventory) {
                        destinationInventory = transactionInventory;
                    }
                    else if (hoveredInventory == transactionInventory) {
                        destinationInventory = stashInventory;
                    }
                }
                else {
                    if (hoveredInventory == traderInventory) {
                        destinationInventory = transactionInventory;
                        transactionState = TransactionInvetoryState.Buying;
                    }
                    else if (hoveredInventory == stashInventory) {
                        destinationInventory = transactionInventory;
                        transactionState = TransactionInvetoryState.Selling;
                    }
                }
            }
            
            if (destinationInventory == null) return;

            MoveItemBetweenInventories(hoveredInventory, destinationInventory, invHoverInfo.hoveredSlotIndex);
            RefreshInventoryDisplay(hoveredInventory);
            RefreshInventoryDisplay(destinationInventory);

            if (OnTradingTab) {
                if (GetInventoryItemCount(transactionInventory) <= 0) {
                    transactionState = TransactionInvetoryState.Empty;
                }
                RefreshTransactionUI();
            }
        }

        bool TryGetItemFromHoverInfo(out InventoryItem hoveredItem) {
            hoveredItem = null;
            
            int hoveredSlot = invHoverInfo.hoveredSlotIndex;
            Inventory hoveredInventory = invHoverInfo.hoveredInventory;
            
            if (hoveredInventory == null) return false;
            if (!hoveredInventory.slots.IndexInRange(hoveredSlot)) return false;
            if (hoveredInventory.slots[hoveredSlot].item == null) return false;
            if (hoveredInventory.slots[hoveredSlot].item.notDiscovered) return false;
            
            hoveredItem = hoveredInventory.slots[hoveredSlot].item;
            return true;
        } 
    }

    private void AddItemsToTraderInventory(int traderLevel) {
        ItemPool itemPool = traderLevelPools[traderLevel];
        Item traderItem = itemPool.GetItemFromPool();
        TryAddItemToInventory(traderInventory, traderItem, traderItem.maxStackCount);
        RefreshInventoryDisplay(traderInventory);
    }

    public struct InventoryHoverInfo {
        public Inventory hoveredInventory;
        public int hoveredSlotIndex;
        public float timeSpentHovering;
    }

    private InventoryHoverInfo lastHoverInfo;
    
    private InventoryHoverInfo UpdateInventoryHover() {
        InventoryHoverInfo info = new();
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        foreach (Inventory inventory in allInventories) {
            if (!inventory.parent.gameObject.activeInHierarchy) continue;
            
            Vector2 localMousePos = inventory.parent.InverseTransformPoint(mousePos);
            Bounds localUiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(inventory.parent);
            if (!localUiBounds.Contains(localMousePos)) continue;
            
            info.hoveredInventory = inventory;
            info.hoveredSlotIndex = GetHoveredInventorySlot(inventory);
            
            if (info.hoveredInventory == lastHoverInfo.hoveredInventory && info.hoveredSlotIndex == lastHoverInfo.hoveredSlotIndex) {
                info.timeSpentHovering = lastHoverInfo.timeSpentHovering + Time.deltaTime;
            }
            else {
                info.timeSpentHovering = 0f;
            }
            
            break;
        }

        lastHoverInfo = info;
        return info;
    }
    
    private int GetHoveredInventorySlot(Inventory inventory) {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        for (int i = 0; i < inventory.slots.Length; i++) {
            RectTransform rectTrans = inventory.slots[i].ui.GetComponent<RectTransform>();
            bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
            if (mouseInRect) {
                return i;
            }
        }
        return -1;
    }

    public struct InventoryAddResult {
        public enum ResultType { Success, Failure, FailureToAddAll };
        public ResultType type;
        public int addedCount;
    }
    
    public InventoryAddResult TryAddItemToInventory(Inventory inventory, Item item, int count) {
        InventoryItem newInventoryItem = new(item, count);
        return TryAddItemToInventory(inventory, newInventoryItem);
    }

    public InventoryAddResult TryAddItemToInventory(Inventory inventory, InventoryItem item) {
        InventoryAddResult result = new() {
            type = InventoryAddResult.ResultType.Failure
        };

        int count = item.count;

        // If we can stack the item then we just do that
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null || slot.ui.disallowItemStacking || slot.item.IsFullStack || slot.item.itemDataUuid != item.itemDataUuid) continue;

            int overflowAmount = (count + slot.item.count) - slot.item.ItemRef.maxStackCount;
            if (overflowAmount > 0) {
                int addCount = slot.item.ItemRef.maxStackCount - slot.item.count;
                
                slot.item.count += addCount;
                count = overflowAmount;
                
                result.addedCount += addCount;
                result.type = InventoryAddResult.ResultType.FailureToAddAll;
                continue;
            }
            
            slot.item.count += count;
            result.addedCount += count;
            result.type = InventoryAddResult.ResultType.Success;
            return result;
        }

        // Otherwise add to empty inventory slot
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item != null) continue;
            
            bool slotCanAcceptItemType = slot.ui.acceptsAllTypes || slot.ui.onlyAcceptedItemType == item.ItemRef.type;
            if (!slotCanAcceptItemType) continue;

            int addCount = slot.ui.disallowItemStacking ? 1 : Mathf.Clamp(count, 0, item.ItemRef.maxStackCount);
            bool canMoveCleanly = addCount == count;
            
            if (canMoveCleanly) {
                slot.item = item;
                result.type = InventoryAddResult.ResultType.Success;
                result.addedCount = count;
                return result;
            }

            InventoryItem newItem = item.Clone();
            newItem.count = addCount;
            slot.item = newItem;
            
            result.type = InventoryAddResult.ResultType.FailureToAddAll;
            result.addedCount = addCount;
            return result;
        }
        
        return result;
    }

    private void MoveItemBetweenInventories(Inventory fromInventory, Inventory toInventory, int slotIndex) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, slotIndex);
        if (inventoryItem == null || inventoryItem.notDiscovered) return;

        if (OnTradingTab) {
            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = 1;
            
            InventoryAddResult traderMoveResult = TryAddItemToInventory(toInventory, newItem);
            if (traderMoveResult.type is InventoryAddResult.ResultType.Success or InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - traderMoveResult.addedCount;
                AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
            }
            return;
        }

        if (splitStackInputAction.WasPressedThisFrame() && inventoryItem.count > 1) {
            int firstHalf = inventoryItem.count / 2;
            int secondHalf = inventoryItem.count - firstHalf;

            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = secondHalf;
            
            InventoryAddResult splitResult = TryAddItemToInventory(toInventory, newItem);
            if (splitResult.type == InventoryAddResult.ResultType.Success) {
                AdjustItemCountInInventory(fromInventory, slotIndex, firstHalf);
            }
            else if (splitResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - splitResult.addedCount;
                AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
            }
            return;
        }

        MoveEntireItemStack(fromInventory, toInventory, slotIndex);
    }

    private bool MoveEntireItemStack(Inventory fromInventory, Inventory toInventory, int slotIndex) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, slotIndex);
        if (inventoryItem == null) {
            return false;
        }
        
        InventoryAddResult moveResult = TryAddItemToInventory(toInventory, inventoryItem);
        if (moveResult.type == InventoryAddResult.ResultType.Success) {
            RemoveItemFromInventory(fromInventory, slotIndex);
        }
        else if (moveResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
            int keepItemCount = inventoryItem.count - moveResult.addedCount;
            AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
        }
        
        return moveResult.type == InventoryAddResult.ResultType.Success;
    }

    private void ClearInventory(Inventory inventory) {
        for (int i = 0; i < inventory.slots.Length; i++) {
            RemoveItemFromInventory(inventory, i);
        }
    }

    private void ShowItemTooltip(InventoryHoverInfo info) {
        InventorySlot hoveredSlot = info.hoveredInventory.slots[info.hoveredSlotIndex];
        TextMeshProUGUI tooltipText = itemDescPopup.GetComponentInChildren<TextMeshProUGUI>();
        
        if (tooltipText.text != string.Empty) {
            itemDescPopup.SetActive(true);
        }
        
        if (hoveredSlot.item.ItemRef.type == Item.ItemType.DemonEye) {
            DemonEyeInstance eyeInstance = eyeInstanceFromItemId[hoveredSlot.item.itemDataUuid];
            string eyeDescription = "";
            foreach (DemonEyeInstance.EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += eyeModifierLookup[modInstance.modId].GetModifierDescription(modInstance.stackCount) + "\n";
            }
            tooltipText.text = eyeDescription;
        }
        else {
            tooltipText.text = hoveredSlot.item.ItemRef.GetDescription();
        }
        
        Vector2 toolTipPos = hoveredSlot.ui.transform.position;
        float slotWidth = hoveredSlot.ui.GetComponent<RectTransform>().rect.width;
        float slotHeight = hoveredSlot.ui.GetComponent<RectTransform>().rect.height;
        toolTipPos += new Vector2(slotWidth / 2 + 20, slotHeight / 2 + 20);
        itemDescPopup.transform.position = toolTipPos;

        Rect rect = itemDescPopup.GetComponent<RectTransform>().rect;
        int minHeight = 80;
        rect.height = Mathf.Clamp(tooltipText.GetComponent<RectTransform>().rect.height, minHeight, Mathf.Infinity);
        itemDescPopup.GetComponent<RectTransform>().sizeDelta = new(rect.width, rect.height);
    }

    private void HideItemTooltip() {
        itemDescPopup.GetComponentInChildren<TextMeshProUGUI>().text = string.Empty;
        itemDescPopup.SetActive(false);
    }
    
    private void RemoveItemFromInventory(Inventory inventory, int slotIndex) {
        inventory.slots[slotIndex].item = null;
    }
    
    private InventoryItem GetInventoryItem(Inventory inventory, int slotIndex) {
        if (slotIndex < 0 || slotIndex >= inventory.slots.Length) {
            return null;
        }
        return inventory.slots[slotIndex].item;
    }
    
    private void AdjustItemCountInInventory(Inventory inventory, int slotIndex, int newCount) {
        InventoryItem item = GetInventoryItem(inventory, slotIndex);
        item.count = newCount;
        if (item.count <= 0) {
            RemoveItemFromInventory(inventory, slotIndex);
        }
    }

    public void RefreshInventoryDisplay(Inventory inventory) {
        foreach (InventorySlot slot in inventory.slots) {
            slot.ui.GetComponentInChildren<InventoryItemUI>()?.Clear();
        }

        for (int i = 0; i < inventory.slots.Length; i++) {
            InventoryItem item = inventory.slots[i].item;
            if (item == null || item.notDiscovered) continue;
            inventory.slots[i].ui.GetComponentInChildren<InventoryItemUI>().Set(item.ItemRef, item.count);
        }
    }

    private int GetInventoryItemCount(Inventory inventory) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            count++;
        }
        return count;
    }

    private enum InventoryValueType { Buy, Sell, Xp }

    private int GetInventoryValue(Inventory inventory, InventoryValueType valueType) {
        int value = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            switch (valueType) {
                case InventoryValueType.Buy:
                    value += slot.item.ItemRef.buyPrice * slot.item.count;
                    break;
                case InventoryValueType.Sell:
                    value += slot.item.ItemRef.sellPrice * slot.item.count;
                    break;
                case InventoryValueType.Xp:
                    value += slot.item.ItemRef.traderXp * slot.item.count;
                    break;
            }
        }
        return value;
    }

    private void OpenPlayerInventory() {
        playerPanel.gameObject.SetActive(true);
        crosshairTrans.gameObject.SetActive(false);
        Cursor.visible = true;
        RefreshInventoryDisplay(playerInventory);
    }

    private void ClosePlayerInventory() {
        playerPanel.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    private void OpenLootInventory() {
        discoverLootIndex = -1;
        lootInventoryPanel.gameObject.SetActive(true);
        
        foreach (Transform child in lootInventoryParent.transform) {
            child.GetComponentInChildren<InventoryItemUI>().Clear();
        }

        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].item == null) continue;
            if (lootInvetoryPtr.slots[i].item.notDiscovered) {
                discoverLootIndex = i;
                break;
            }
            InventoryItem item = lootInvetoryPtr.slots[i].item;
            lootInventoryParent.GetChild(i).GetComponentInChildren<InventoryItemUI>().Set(item.ItemRef, item.count);
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        discoverLootTimer.SetTime(1f);
        discoverLootTimer.EndAction ??= () => {
            InventoryItem item = lootInvetoryPtr.slots[discoverLootIndex].item;
            item.notDiscovered = false;
            
            lootInventoryParent.GetChild(discoverLootIndex).GetComponentInChildren<InventoryItemUI>().Set(item.ItemRef, item.count);
            
            discoverLootIndex++;
            if (discoverLootIndex < lootInvetoryPtr.slots.Length) {
                discoverLootTimer.SetTime(1f);
            }
        };
    }

    private void CloseLootInventory() {
        lootInventoryPanel.gameObject.SetActive(false);
        discoverLootTimer.Stop();
    }

    
    public struct Projectile {
        public Transform trans;
        public float timeAlive;
        public Vector2 velocity;
        public DemonEyeInstance EyeInstanceSpawnedFrom;
    }
    
    private void UpdateProjectiles() {
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.timeAlive += Time.deltaTime;
            proj.trans.position += proj.velocity.ToVector3() * Time.deltaTime;
            projectiles[i] = proj;
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, 0.1f, Masks.DamagableMask);
            if (col) {
                HandleDamage(proj.EyeInstanceSpawnedFrom, col);
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

    private void HandleDamage(DemonEyeInstance eyeInstance, Collider2D col) {
        if (!col) return;
        
        Entity entity = entityLookup[col.gameObject];
        entity.lastDamageTime = Time.time;
        
        if (col.CompareTag(Tags.Enemy)) {
            Enemy enemy = enemyLookup[col.gameObject];
            foreach (DemonEyeInstance.EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeModifierLookup[modInstance.modId].AddInstanceToEnemy(enemy, modInstance.stackCount);
            }
            enemy.health -= (int)eyeInstance.coreAttack.damage;
        }
        else {
            entity.health -= (int)eyeInstance.coreAttack.damage;

            if (Random.value <= 0.35f) { // Random chance to spawn drop on each hit
                Vector3 spawnPos = col.transform.position + RandomOffset360(0.25f, 0.5f);
                SpawnLevelEntity<Entity>(rockDropPool.GetDropFromPool(), spawnPos, Quaternion.identity);
            }

            if (entity.health <= 0) {
                AstarPath.active.UpdateGraphs(entity.collider.bounds);
                DestroyEntity(entity);
            }
        }
    }


    private int damageFlashTintPropertyId = Shader.PropertyToID("_DamageFlashTint");
    
    public class Enemy : Entity {
        public EnemyData data;
        public PathData pathData = new();
        public MaterialPropertyBlock matPropertyBlock = new();
        public BleedModInstance bleed;
        public SlowInstance slow;
    }
    
    public class PathData {
        public ABPath abPath;
        public int waypointIndex;
        public bool isBeingCalculated;
        public float lastUpdateTime;
        
        public bool HasPath => abPath != null;
    }

    private void UpdateEnemies() {
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];

            if (enemy.bleed != null) {
                BleedModInstance bleed = enemy.bleed;
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    enemy.health -= bleed.bleedDamage;
                    bleed.lastBleedTime = Time.time;
                }
            }

            // Assign material properties like damage flash
            {
                if (Time.time - enemy.lastDamageTime < 0.08f) {
                    enemy.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 1f);
                }
                else {
                    enemy.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 0f);
                }
                enemy.spriteRenderer.SetPropertyBlock(enemy.matPropertyBlock);
            }
            
            if (enemy.health <= 0) {
                // Drop items from enemy 
                {
                    EnemyData.ItemDrop[] itemDrops = enemy.data.itemDrops;
                    foreach (EnemyData.ItemDrop itemDrop in itemDrops) {
                        float randomChance = Random.value;
                        if (randomChance < itemDrop.dropChance) {
                            SpawnLevelEntity<Entity>(itemDrop.itemPrefab, enemy.position, Quaternion.identity);
                        }
                    }
                }

                // Add enemy soul to nearby altar
                {
                    Altar closestAltar = null;
                    float closestDistance = float.MaxValue;
                    foreach (Altar altar in activeAltars) {
                        float dist = Vector2.Distance(altar.gameObject.transform.position, enemy.position);
                        if (dist < closestDistance) {
                            closestDistance = dist;
                            closestAltar = altar;
                        }
                    }

                    const float maxSoulDistFromAltar = 3f;
                    if (closestAltar != null && closestDistance < maxSoulDistFromAltar) {
                        closestAltar.soulCompletion += 0.025f;
                        if (closestAltar.soulCompletion >= 1f) {
                            SpawnLevelEntity<Entity>(altarDropPool.GetDropFromPool(), closestAltar.gameObject.transform.position + new Vector3(0f, 0.3f, 0f), Quaternion.identity);
                            activeAltars.Remove(closestAltar);
                        }
                    }
                    
                }

                DestroyEntity(enemies[i]);
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

            float speed = enemy.data.speed;
            if (enemy.slow != null) {
                speed = Mathf.Clamp(speed - enemy.slow.speedReduction, 0.05f, enemy.data.speed);
                if (Time.time > enemy.slow.activationTime + enemy.slow.duration) {
                    enemy.slow = null;
                }
            }
            
            /*
                The below separation method causes jitter in big pools of enemies because center enemies are bouncing back and forth
                Todo: Make the separation logic start from the center of a crowd and work its way out to prevent this jitter
            */

            const float targetSeparationDist = 0.15f;
            Vector2 separation = Vector2.zero;
            foreach (Enemy avoidEnemy in enemies) {
                if (avoidEnemy == enemy) continue;
                
                Vector2 diff = enemy.position - avoidEnemy.position;
                float dist = diff.magnitude;

                if (dist < targetSeparationDist)
                    separation += diff.normalized / dist; // Stronger repulsion if closer
            }

            Vector2 targetPos = usingPath ? pathData.abPath.vectorPath[pathData.waypointIndex] : player.position;
            Vector2 dirToTarget = (targetPos - enemy.position.ToVector2()).normalized;
            Vector2 finalDirection = (dirToTarget + separation.normalized * 0.5f).normalized;
            enemy.rigidbody.linearVelocity = finalDirection * (speed * Time.fixedDeltaTime);

            enemy.spriteRenderer.flipX = player.position.x < enemy.position.x;
        }
    }

    private void InitExitPortal() {
        exitPortalTimer.SetTime(Random.Range(1f, 2f));
        // exitPortalTimer.SetTime(Random.Range(35f, 45f));
        
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
            SpawnLevelEntity<Entity>(exitPortalPrefab, exitPortalParent.position, Quaternion.identity, exitPortalParent);
            exitPortalStatusText.text = $"Exit Portal: { exitPortalParent.name }";
        };
    }
    
    
    public class EnemyWaveManager {
        public float timeInCurWave;
        public int curWaveIndex;
        public EnemyWaves waves;
        public EnemyWaves.WaveData curWaveData;
        
        public const int prefixedSumResolution = 500;
        public float[] prefixedSums = new float[prefixedSumResolution];

        public List<float> spawnTimes = new();
        public int spawnTimeIndex;
    }

    [NonSerialized] private EnemyWaveManager waveManager = new();
    
    private void InitWave(EnemyWaves waves) {
        waveManager.waves = waves;
        waveManager.curWaveIndex = -1;
    }
    
    private void UpdateWave() {
        EnemyWaveManager wm = waveManager;
        if (wm.curWaveIndex >= wm.waves.waves.Count) return;
        
        wm.timeInCurWave += Time.deltaTime;
        float waveDuration = wm.curWaveIndex == -1 ? wm.waves.timeBeforeFirstWave : wm.curWaveData.waveDuration;

        if (wm.timeInCurWave >= waveDuration) {
            wm.curWaveIndex++;
            if (!wm.waves.waves.IndexInRange(wm.curWaveIndex)) return;

            EnemyWaves.WaveData newWaveData = wm.waves.waves[wm.curWaveIndex];
            wm.curWaveData = newWaveData;

            if (newWaveData.enemyCount >= EnemyWaveManager.prefixedSumResolution) {
                Debug.LogError($"Wave cannot have more enemies than {nameof(EnemyWaveManager.prefixedSumResolution)}");
            }
            
            wm.timeInCurWave = 0f;
            wm.spawnTimeIndex = 0;

            float totalWeight = 0f;
            for (int i = 0; i < EnemyWaveManager.prefixedSumResolution; i++) {
                float sliceIndex = i / (float)(EnemyWaveManager.prefixedSumResolution - 1);
                float weight = Mathf.Clamp01(newWaveData.spawnRateCurve.Evaluate(sliceIndex));
                totalWeight += weight;
                wm.prefixedSums[i] = totalWeight;
            }

            wm.spawnTimes.Clear();
            int enemySpawnCount = newWaveData.enemyCount;
            for (int i = 0; i < enemySpawnCount; i++) {
                float targetWeight = (i / (float)(enemySpawnCount - 1)) * totalWeight;

                // Find the corresponding time using linear search
                int weightIndex = 0;
                while (weightIndex < EnemyWaveManager.prefixedSumResolution && wm.prefixedSums[weightIndex] < targetWeight) {
                    weightIndex++;
                }

                float normalizedTime = weightIndex / (float)(EnemyWaveManager.prefixedSumResolution - 1);
                wm.spawnTimes.Add(normalizedTime * newWaveData.spawnDuration);
            }
        }

        if (wm.spawnTimes.Count <= 0) return;
        
        while (wm.spawnTimes.IndexInRange(wm.spawnTimeIndex) && wm.spawnTimes[wm.spawnTimeIndex] <= wm.timeInCurWave) {
            Vector2 randomSpawnPos = player.position + RandomOffset360(3f, 4f);
            NNInfo info = AstarPath.active.graphs[0].GetNearest(randomSpawnPos, NNConstraint.Walkable);
            
            Enemy enemy = SpawnLevelEntity<Enemy>(defaultEnemy.enemyPrefab, info.position, Quaternion.identity);
            enemy.data = defaultEnemy;
            
            enemies.Add(enemy);
            enemyLookup.Add(enemy.gameObject, enemy);
            
            wm.spawnTimeIndex++;
        }

    }

    private Vector3 RandomOffset360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }


    private class Altar : Entity {
        public float soulCompletion;
    }
   
    private List<Altar> activeAltars = new();
    private Dictionary<GameObject, InventorySlot[]> deadBodySlotsLookup = new();

    private void SpawnResources(Transform resourceSpawnParent) {
        List<Transform> spawnPoints = resourceSpawnParent.GetComponentsInChildren<Transform>().ToList();
        spawnPoints.RemoveAt(0); // Remove resourceSpawnParent
        
        int gemRocksToSpawn = Random.Range(6, 10);
        for (int i = 0; i < gemRocksToSpawn; i++) {
            SpawnResource<Entity>(gemRockPrefab, true);
        }
        
        int deadBodiesToSpawn = Random.Range(3, 5);
        for (int i = 0; i < deadBodiesToSpawn; i++) {
            int randomInventorySize = Random.Range(2, 6);
            InventorySlot[] deadBodySlots = new InventorySlot[randomInventorySize];

            for (int j = 0; j < randomInventorySize; j++) {
                Item spawnItem = deadBodyPool.GetItemFromPool();
                InventoryItem lootItem = new() {
                    itemDataUuid = spawnItem.uuid, 
                    count = Random.Range(1, spawnItem.maxStackCount / 3),
                    notDiscovered = true,
                };
                deadBodySlots[j] = new() {
                    item = lootItem,
                    ui = lootInvetoryPtr.slots[j].ui // Use the already instantiated ui of lootInventoryPtr
                };
            }
            
            Entity body = SpawnResource<Entity>(deadBodyPrefab, false);
            deadBodySlotsLookup.Add(body.gameObject, deadBodySlots);
        }
        
        int altarsToSpawn = Random.Range(1, 2);
        for (int i = 0; i < altarsToSpawn; i++) {
            Altar altarEntity = SpawnResource<Altar>(altarPrefab, true);
            activeAltars.Add(altarEntity);
        }

        T SpawnResource<T>(GameObject resourcePrefab, bool cutsNavmesh) where T : Entity, new() {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnTrans = spawnPoints[randomIndex];
            spawnPoints.RemoveAt(randomIndex);
            
            T resource = SpawnLevelEntity<T>(resourcePrefab, spawnTrans.position, spawnTrans.rotation);

            if (cutsNavmesh) {
                AstarPath.active.UpdateGraphs(resource.collider.bounds);
            }

            return resource;
        }
    }

    private void DestroyLevelEntities() {
        for (int i = entities.Count - 1; i >= 0; i--) {
            if (entities[i].lifeTime == EntityLifeTime.Level) {
                DestroyEntityAtIndex(i);    
            }
        }

        deadBodySlotsLookup.Clear();
        activeAltars.Clear();
        enemies.Clear();
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

    private string GetSavePath(Inventory inventory) {
        if (inventory == playerInventory)   return inventorySavePath;
        if (inventory == stashInventory)    return stashSavePath;
        if (inventory == crucibleInventory) return crucibleSavePath;
        return string.Empty;
    }
    
    private void SaveInventory(Inventory inventory) {
        cachedInventoryForSaving.Clear();
        foreach (InventorySlot slot in inventory.slots) {
            cachedInventoryForSaving.Add(slot.item); 
        }
        SaveToFile(GetSavePath(inventory), cachedInventoryForSaving);
    }

    private void LoadInventory(Inventory inventory) {
        List<InventoryItem> items = LoadFromFile<List<InventoryItem>>(GetSavePath(inventory));

        // Items can be null because we save all inventory slots, including empty ones
        foreach (InventoryItem item in items) {
            bool isDemonEye = item != null && item.modifierUuids != null;
            if (isDemonEye) {
                BuildAndRegisterEye(item);
            }
        }
        
        CopyItemsToInventory(items, inventory);
    }

    private void CopyItemsToInventory(List<InventoryItem> items, Inventory toInventory) {
        if (items == null || toInventory == null) return;
        
        for (int i = 0; i < toInventory.slots.Length; i++) {
            if (!toInventory.slots.IndexInRange(i) || !items.IndexInRange(i)) break;
            toInventory.slots[i].item = items[i];
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


    public enum EntityLifeTime { Global, Level }

    public class Entity {
        public Transform trans;
        public Collider2D collider;
        public Rigidbody2D rigidbody;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public int health;
        public float lastDamageTime;
        public EntityLifeTime lifeTime;
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }

        public GameObject gameObject => trans.gameObject;
    }
    
    private T SpawnGlobalEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        return SpawnEntity<T>(prefab, position, rotation, parent, EntityLifeTime.Global);
    }
    
    private T SpawnLevelEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        return SpawnEntity<T>(prefab, position, rotation, parent, EntityLifeTime.Level);
    }
    
    private T SpawnEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, EntityLifeTime lifeTime) where T : Entity, new() {
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        T newEntity = new() {
            trans = obj.transform,
            health = 100,
            lifeTime = lifeTime,
            collider = obj.TryGetComponent(out Collider2D col) ? col : null,
            rigidbody = obj.TryGetComponent(out Rigidbody2D rbody) ? rbody : null,
            spriteRenderer = obj.TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer : null,
            animator = obj.TryGetComponent(out Animator anim) ? anim : null,
        };
        entities.Add(newEntity);
        entityLookup.Add(obj, newEntity);
        return newEntity;
    }

    private void DestroyEntityAtIndex(int entityIndex) {
        Entity entity = entities[entityIndex];
        entityLookup.Remove(entity.gameObject);
        entities.RemoveAt(entityIndex);
        Destroy(entity.gameObject);
    }

    private void DestroyEntity(GameObject gameObj) {
        DestroyEntity(entityLookup[gameObj]);
    }
    
    private void DestroyEntity(Entity entity) {
        entityLookup.Remove(entity.gameObject);
        entities.Remove(entity);
        Destroy(entity.gameObject);
    }


    private void InitHideoutUI() {
        characterTabButton.image.sprite = tabSelectedSprite;
        eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
        traderTabButton.image.sprite = tabNonSelectedSprite;
        
        hideoutHeaderParent.gameObject.SetActive(true);
        hideoutTabsParent.gameObject.SetActive(true);
        playerPanel.gameObject.SetActive(true);
        stashPanel.gameObject.SetActive(true);
        eyeForgePanel.gameObject.SetActive(false);
        traderInventoryPanel.gameObject.SetActive(false);
        traderTransactionPanel.gameObject.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
    }

    private void InitButtonCallbacks() {
        characterTabButton.onClick.AddListener(() => {
            characterTabButton.image.sprite = tabSelectedSprite;
            eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
            traderTabButton.image.sprite = tabNonSelectedSprite;
            
            ToggleSlimPlayerPanel(false);
            playerPanel.gameObject.SetActive(true);
            stashPanel.gameObject.SetActive(true);
            eyeForgePanel.gameObject.SetActive(false);
            traderInventoryPanel.gameObject.SetActive(false);
            traderTransactionPanel.gameObject.SetActive(false);
        });
        
        eyeForgeTabButton.onClick.AddListener(() => {
            characterTabButton.image.sprite = tabNonSelectedSprite;
            eyeForgeTabButton.image.sprite = tabSelectedSprite;
            traderTabButton.image.sprite = tabNonSelectedSprite;
            
            ToggleSlimPlayerPanel(true);
            playerPanel.gameObject.SetActive(true);
            stashPanel.gameObject.SetActive(true);
            eyeForgePanel.gameObject.SetActive(true);
            traderInventoryPanel.gameObject.SetActive(false);
            traderTransactionPanel.gameObject.SetActive(false);
        });
        
        traderTabButton.onClick.AddListener(() => {
            characterTabButton.image.sprite = tabNonSelectedSprite;
            eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
            traderTabButton.image.sprite = tabSelectedSprite;
            
            playerPanel.gameObject.SetActive(false);
            stashPanel.gameObject.SetActive(true);
            eyeForgePanel.gameObject.SetActive(false);
            traderInventoryPanel.gameObject.SetActive(true);
            traderTransactionPanel.gameObject.SetActive(true);
        });
        
        crucibleForgeButton.onClick.AddListener(() => {
            int eyeSlotIndex = 0;
            InventoryItem eyeItem = null;

            for (int i = 0; i < crucibleInventory.slots.Length; i++) {
                InventorySlot slot = crucibleInventory.slots[i];
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Eye) {
                    eyeItem = slot.item;
                    eyeSlotIndex = i;
                }
            }

            if (eyeItem == null) return;

            for (int i = 0; i < crucibleInventory.slots.Length; i++) {
                if (i == eyeSlotIndex) continue;
                if (crucibleInventory.slots[i].item != null) break;
                if (i == crucibleInventory.slots.Length - 1) return;
            }

            InventoryItem newDemonEyeItem = new() {
                modifierUuids = new(),
            };

            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.item == null) continue;
                
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Vein) {
                    newDemonEyeItem.modifierUuids.Add(slot.item.ItemRef.uuid);
                }
                slot.item = null;
            }

            BuildAndRegisterEye(newDemonEyeItem);
            
            crucibleInventory.slots[eyeSlotIndex].item = newDemonEyeItem;
            RefreshInventoryDisplay(crucibleInventory);
        });
        
        traderDealButton.onClick.AddListener(() => {
            InventoryValueType valueType = transactionState == TransactionInvetoryState.Buying ? InventoryValueType.Buy : InventoryValueType.Sell;
            int price = GetInventoryValue(transactionInventory, valueType);
            
            if (transactionState == TransactionInvetoryState.Buying && stashValue >= price) {
                SetStashValue(stashValue - price); 
                for (int i = 0; i < transactionInventory.slots.Length; i++) { 
                    MoveEntireItemStack(transactionInventory, stashInventory, i);
                }
                RefreshInventoryDisplay(transactionInventory);
                RefreshInventoryDisplay(stashInventory);
                transactionState = TransactionInvetoryState.Empty;
            }
            else if (transactionState == TransactionInvetoryState.Selling) {
                SetStashValue(stashValue + price);
                ClearInventory(transactionInventory);
                RefreshInventoryDisplay(transactionInventory);
                transactionState = TransactionInvetoryState.Empty;
            }

            RefreshTransactionUI();
        });
        
        enterNextRaidButton.onClick.AddListener(() => {
            gameStateMachine.SetStateIfNotCurrent(raidState);
        });
    }

    // Its better just to have these as constants because the canvas layout recalculates in LateUpdate
    private const float playerPanelWidth = 500f;
    private const float playerPocketsBackpackWidth = 221.55f;
    
    private void ToggleSlimPlayerPanel(bool toggle) {
        if (toggle) {
            playerPocketsBackpackParent.gameObject.SetActive(false);
            playerPanel.GetComponent<LayoutElement>().preferredWidth = playerPanelWidth - playerPocketsBackpackWidth;
            return;
        }
        
        playerPocketsBackpackParent.gameObject.SetActive(true);
        playerPanel.GetComponent<LayoutElement>().preferredWidth = playerPanelWidth;
    }

    private void RefreshTransactionUI() {
        if (transactionState == TransactionInvetoryState.Empty) {
            traderTransactionInfoText.text = string.Empty;
            return;
        }
        
        if (transactionState == TransactionInvetoryState.Buying) {
            int buyPrice = GetInventoryValue(transactionInventory, InventoryValueType.Buy);
            traderTransactionInfoText.text = $"Purchase for {buyPrice}";
        }
        else if (transactionState == TransactionInvetoryState.Selling) {
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            int xpGain = GetInventoryValue(transactionInventory, InventoryValueType.Xp);
            traderTransactionInfoText.text = $"Sell for {sellPrice}\n Gain {xpGain} trader experience";
        }
    }

    private void SetStashValue(int value) {
        stashValue = value;
        stashValueText.text = stashValue.ToString();
    }

}