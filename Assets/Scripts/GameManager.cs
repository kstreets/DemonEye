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

public partial class GameManager : MonoBehaviour {

    public List<Item> allItems;

    public Animator playerAnim;
    public Camera mainCamera;
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

    public AnimationCurve enemySeparationFalloffCurve;
    
    [Header("Spawn Positions")]
    public Vector3 hideoutSpawnPosition;
    public Vector3 hellSpawnPosition;

    [Header("UI")]
    public RectTransform playerInventoryParent;
    public RectTransform lootInventoryParent;
    public RectTransform stashInventoryParent;
    public RectTransform crucibleParent;
    public RectTransform traderInventoryParent;
    public GameObject inventorySlotPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject interactPrompt;
    public TextMeshProUGUI exitPortalStatusText;
    public Button crucibleForgeButton;
    public GameObject itemDescPopup;
    
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
        Cursor.visible = false;
        
        foreach (Item itemData in allItems) {
            if (itemData is EyeModifier mod) {
                eyeModifierLookup.Add(mod.uuid, mod);
            }
            else if (itemData is CoreAttack core) {
                baseAttackLookup.Add(core.uuid, core);
            }
            itemDataLookup.Add(itemData.uuid, itemData);
        }

        InitInventory();
        BuildSavePaths();
        LoadInventory(playerInventory);
        LoadInventory(stashInventory);

        equipedEye = new() { coreAttack = defaultAttack };
        
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
        RefreshInventoryDisplay(stashInventory);
    }

    private void OnHideoutStateExit() {
    }

    private void OnHideoutStateUpdate() {
    }

    private void OnRaidStateEndter() {
        smallMapParent.gameObject.SetActive(true);

        Map map = smallMapParent.GetComponent<Map>();
        
        player.transform.position = hellSpawnPosition;
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


    private Transform player;
    private const float playerSpeed = .75f;
    private Limitter attackLimiter;
    private List<Collider2D> playerContacts = new(10);
    
    private void UpdatePlayer() {
        if (InventoryIsOpen) {
            return;
        }
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (playerSpeed * Time.deltaTime);

        if (moveInput.x < 0) {
            player.GetComponent<SpriteRenderer>().flipX = true;
        }
        else if (moveInput.x > 0) {
            player.GetComponent<SpriteRenderer>().flipX = false;
        }
        
        if (moveInput != Vector2.zero) {
            playerAnim.Play("PlayerRun");
        }
        else {
            playerAnim.Play("PlayerIdle");
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
        
        Collider2D playerCol = player.GetComponent<Collider2D>();
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
                    lootInvetoryPtr.slots = deadBodySlotsLookup[col.gameObject];
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
    
    private void UpdateCrucible() {
        if (ButtonIsPressed(crucibleForgeButton)) {
            int eyeSlotIndex = 0;
            InventoryItem eyeItem = null;
            InventoryItem coreItem = null;

            for (int i = 0; i < crucibleInventory.slots.Length; i++) {
                InventorySlot slot = crucibleInventory.slots[i];
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Eye) {
                    eyeItem = slot.item;
                    eyeSlotIndex = i;
                }

                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Core) {
                    coreItem = slot.item;
                }
            }

            if (eyeItem == null || coreItem == null) return;

            InventoryItem newDemonEyeItem = new() {
                modifierUuids = new(),
            };

            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.item == null) continue;
                
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Vein) {
                    newDemonEyeItem.modifierUuids.Add(slot.item.ItemRef.uuid);
                }
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Core) {
                    newDemonEyeItem.baseAttackUuid = slot.item.ItemRef.uuid;
                }
                slot.item = null;
            }

            BuildAndRegisterEye(newDemonEyeItem);
            
            crucibleInventory.slots[eyeSlotIndex].item = newDemonEyeItem;
            RefreshInventoryDisplay(crucibleInventory);
        }
    }

    
    [Serializable]
    public class InventoryItem {
        public string itemDataUuid;
        public string baseAttackUuid;
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
                baseAttackUuid = baseAttackUuid,
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
    [NonSerialized] public Inventory lootInvetoryPtr;
    [NonSerialized] public List<Inventory> allInventories = new();

    private Timer discoverLootTimer;
    private int discoverLootIndex;
    
    private bool InventoryIsOpen => playerInventoryParent.gameObject.activeSelf;
    private bool StashIsOpen => stashInventoryParent.gameObject.activeSelf;
    private bool CrucibleIsOpen => crucibleParent.gameObject.activeSelf;
    private bool LootInventoryIsOpen => lootInventoryParent.gameObject.activeSelf;
    
    private void InitInventory() {
        const int inventorySlotSizeWithPadding = 150;
        
        const int playerInventoryWidth = 3;
        const int playerInventoryHeight = 4;
        int playerEquipmentSlotCount = playerInventoryParent.childCount;
        SpawnUiSlots(playerInventoryParent, playerInventoryWidth, playerInventoryHeight);
        playerInventory = CreateInventory(playerInventoryParent, playerInventoryWidth * playerInventoryHeight + playerEquipmentSlotCount); 
        
        const int cachedLootInventoryWidth = 3;
        const int cachedLootInventoryHeight = 4;
        SpawnUiSlots(lootInventoryParent, cachedLootInventoryWidth, cachedLootInventoryHeight); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventoryWidth * cachedLootInventoryHeight);
        
        const int stashInventoryWidth = 4;
        const int stashInventoryHeight = 6;
        SpawnUiSlots(stashInventoryParent, stashInventoryWidth, stashInventoryHeight);
        stashInventory = CreateInventory(stashInventoryParent, stashInventoryWidth * stashInventoryHeight);
        
        SpawnUiSlots(traderInventoryParent, 5, 4);

        const int crucibleInventorySize = 9;
        // Spawn crucible slots
        { 
            const int crucibleVeinSize = crucibleInventorySize - 1;
            Vector2 crucibleCenter = crucibleParent.anchoredPosition;
            GameObject centerSlot = Instantiate(inventorySlotPrefab, crucibleCenter, Quaternion.identity, crucibleParent);
            Instantiate(inventoryItemPrefab, crucibleCenter, Quaternion.identity, centerSlot.transform);
            for (int i = 0; i < crucibleVeinSize; i++) {
                float deg = 360f / crucibleVeinSize * i;
                Vector2 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 150f;
                GameObject slot = Instantiate(inventorySlotPrefab, crucibleCenter + spawnDir, Quaternion.identity, crucibleParent);
                Instantiate(inventoryItemPrefab, crucibleCenter + spawnDir, Quaternion.identity, slot.transform);
            }
        }
        crucibleInventory = CreateInventory(crucibleParent, crucibleInventorySize);
        
        void SpawnUiSlots(RectTransform parent, int width, int height) {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    Vector3 pos = new(parent.position.x, parent.position.y, 0f);
                    Vector3 offset = new(inventorySlotSizeWithPadding * i, -(inventorySlotSizeWithPadding * j), 0f);
                    GameObject slot = Instantiate(inventorySlotPrefab, pos + offset, Quaternion.identity, parent);
                    Instantiate(inventoryItemPrefab, pos + offset, Quaternion.identity, slot.transform);
                }
            }
        }
        
        Inventory CreateInventory(RectTransform uiParent, int slotCount) {
            Inventory inventory = new() {
                parent = uiParent,
                slots = new InventorySlot[slotCount]
            };
            inventory.slots.InitalizeWithDefault();
            
            for (int i = 0; i < inventory.slots.Length; i++) {
                inventory.slots[i].ui = inventory.parent.GetChild(i).GetComponent<InventorySlotUI>();
            }
            
            allInventories.Add(inventory);
            return inventory;
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

        if (!InventoryIsOpen && (!StashIsOpen || !LootInventoryIsOpen)) {
            HideItemTooltip();
            return;
        }
        
        InventoryHoverInfo invHoverInfo = UpdateInventoryHover();
        UpdateItemtooltip();
        HandleItemClicked();

        void UpdateItemtooltip() {
            int hoveredSlot = invHoverInfo.hoveredSlotIndex;
            Inventory hoveredInventory = invHoverInfo.hoveredInventory;

            if (hoveredInventory == null) return;
            if (!hoveredInventory.slots.IndexInRange(hoveredSlot)) return;
            if (hoveredInventory.slots[hoveredSlot].item == null) return;
            if (hoveredInventory.slots[hoveredSlot].item.notDiscovered) return;
            
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
            if (selectItemInputAction.WasPressedThisFrame() || splitStackInputAction.WasPressedThisFrame()) {
                Inventory hoveredInventory = invHoverInfo.hoveredInventory;
                if (hoveredInventory == null) return;

                Inventory nonHoveredInventory = null;
                foreach (Inventory inventory in allInventories) {
                    if (inventory.parent.gameObject.activeSelf && inventory != hoveredInventory) {
                        nonHoveredInventory = inventory;
                        break;
                    }
                }

                if (nonHoveredInventory == null) return;

                MoveItemBetweenInventories(hoveredInventory, nonHoveredInventory, invHoverInfo.hoveredSlotIndex);
                RefreshInventoryDisplay(hoveredInventory);
                RefreshInventoryDisplay(nonHoveredInventory);
            }
        }
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
            if (!inventory.parent.gameObject.activeSelf) continue;
            
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
        RectTransform inventoryParent = inventory.parent;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        for (int i = 0; i < inventoryParent.childCount; i++) {
            Transform child = inventoryParent.GetChild(i);
            
            // Inventory parents might have buttons or other UI elements that arn't inventory slots
            if (!child.TryGetComponent(out InventorySlotUI _)) continue; 
            
            RectTransform rectTrans = child.GetComponent<RectTransform>();
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
    
    private void MoveItemBetweenInventories(Inventory fromInventory, Inventory toInventory, int hoveredSlotIndex) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, hoveredSlotIndex);
        if (inventoryItem == null || inventoryItem.notDiscovered) return;

        if (splitStackInputAction.WasPressedThisFrame() && inventoryItem.count > 1) {
            int firstHalf = inventoryItem.count / 2;
            int secondHalf = inventoryItem.count - firstHalf;

            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = secondHalf;
            
            InventoryAddResult splitResult = TryAddItemToInventory(toInventory, newItem);
            if (splitResult.type == InventoryAddResult.ResultType.Success) {
                AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, firstHalf);
            }
            else if (splitResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - splitResult.addedCount;
                AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, keepItemCount);
            }
            return;
        }
        
        InventoryAddResult moveResult = TryAddItemToInventory(toInventory, inventoryItem);
        if (moveResult.type == InventoryAddResult.ResultType.Success) {
            RemoveItemFromInventory(fromInventory, hoveredSlotIndex);
        }
        else if (moveResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
            int keepItemCount = inventoryItem.count - moveResult.addedCount;
            AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, keepItemCount);
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
    }

    public void RefreshInventoryDisplay(Inventory inventory) {
        RectTransform inventoryParent = inventory.parent;
        if (!inventoryParent.gameObject.activeSelf) return;
        
        foreach (Transform child in inventoryParent.transform) {
            child.GetComponentInChildren<InventoryItemUI>()?.Clear();
        }

        for (int i = 0; i < inventory.slots.Length; i++) {
            InventoryItem item = inventory.slots[i].item;
            if (item == null || item.notDiscovered) continue;
            inventoryParent.GetChild(i).GetComponentInChildren<InventoryItemUI>().Set(item.ItemRef, item.count);
        }
    }

    private void OpenPlayerInventory() {
        playerInventoryParent.gameObject.SetActive(true);
        crosshairTrans.gameObject.SetActive(false);
        Cursor.visible = true;
        RefreshInventoryDisplay(playerInventory);
    }

    private void ClosePlayerInventory() {
        playerInventoryParent.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    private void OpenStashInventory() {
        stashInventoryParent.gameObject.SetActive(true);
        RefreshInventoryDisplay(stashInventory);
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
            bool isDemonEye = item != null && item.baseAttackUuid != null;
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

    
    public enum EntityLifeTime { Global, Level }

    public class Entity {
        public Transform trans;
        public Collider2D collider;
        public Rigidbody2D rigidbody;
        public SpriteRenderer spriteRenderer;
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

}