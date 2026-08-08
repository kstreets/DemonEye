using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

public partial class Game {
    
    [Serializable]
    public class ItemInstance {
        public int itemOrInstanceUuid;
        public List<int> nestedUuids;
        public int count = 1;
        public bool isDemonEye;
        public string demonEyeName;
        public int demonEyeLevel;

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public bool traderOwned;
        [NonSerialized] public int traderSlotIndex;

        public Item ItemRef {
            get {
                if (gameInstance.demonEye.instanceFromItemId.ContainsKey(itemOrInstanceUuid)) {
                    return gameInstance.itemRefs.demonEye;
                }
                
                UuidScriptableObject uuidObject = gameInstance.res.lookup[itemOrInstanceUuid];
                return uuidObject switch {
                    Item item => item,
                    Augment augment => augment.augmentedEyeUpgrade,
                    _ => null, 
                };
            }
        }
        
        public bool IsFullStack => count == ItemRef.MaxStackCount;

        public ItemInstance(UuidScriptableObject uuidObject = null, int count = 1) {
            if (uuidObject == null) return;
            this.itemOrInstanceUuid = uuidObject.uuid;
            this.count = count;
        }
        
        public ItemInstance Clone() {
            ItemInstance clonedItemInstance = new() {
                itemOrInstanceUuid = itemOrInstanceUuid,
                count = count,
                isDemonEye = isDemonEye,
                demonEyeName = demonEyeName,
                demonEyeLevel = demonEyeLevel,
                notDiscovered = notDiscovered,
                traderOwned = traderOwned,
                traderSlotIndex = traderSlotIndex,
            };

            if (nestedUuids != null) {
                foreach (int uuid in nestedUuids) {
                    clonedItemInstance.nestedUuids ??= new();     
                    clonedItemInstance.nestedUuids.Add(uuid);
                }
            }

            return clonedItemInstance;
        }
        
    }
    
    public class InventorySlot {
        public ItemInstance itemInstance;
        public InventorySlotUI ui;
    }

    public class Inventory {
        public InventorySlot[] slots;
        public RectTransform parent;
    }
    
    public const int playerPocketSize = 10;
    public const int playerQuickUseSize = 4;
    public const int playerEquipmentSize = 3;
    public const int traderInventoryColCount = 6;
    public const int traderInventoryRowCount = 5;
    
    private int NakedPlayerInventorySize => playerPocketSize + playerQuickUseSize + playerEquipmentSize;
    private bool PlayerInventoryIsOpen => playerPanel.panel.gameObject.activeInHierarchy;
    private bool LootInventoryIsOpen => ui.lootInventoryPanel.gameObject.activeInHierarchy;

    private void InitInventories(GameState gameState) {
        const int maxBackpackSize = 30;
        SpawnUiSlots(playerPanel.quickUseParent, playerQuickUseSize);
        SpawnUiSlots(playerPanel.pocketParent, playerPocketSize + maxBackpackSize);
        inventories.player = CreateInventory(playerPanel.inventoryParent, NakedPlayerInventorySize);

        InventorySlotUI[] quickUseSlots = playerPanel.quickUseParent.GetComponentsInChildren<InventorySlotUI>();
        foreach (InventorySlotUI slotUI in quickUseSlots) {
            slotUI.onlyAcceptedItemType = itemTypes.quickUse;
        }
        
        const int stashInventorySize = 40;
        SpawnUiSlots(stashPanel.inventoryParent, stashInventorySize);
        inventories.stash = CreateInventory(stashPanel.inventoryParent, stashInventorySize);
       
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(ui.lootInventoryParent, cachedLootInventorySize); 
        inventories.lootPtr = CreateInventory(ui.lootInventoryParent, cachedLootInventorySize);
        inventories.lootSlotUis = ui.lootInventoryParent.GetComponentsInChildren<InventorySlotUI>(true);

        const int traderInventorySize = traderInventoryRowCount * traderInventoryColCount;
        SpawnUiSlots(traderPanel.inventoryParent, traderInventorySize);
        inventories.trader = CreateInventory(traderPanel.inventoryParent, traderInventorySize);
        
        const int transactionInventorySize = 25;
        SpawnUiSlots(transactionPanel.inventoryParent, transactionInventorySize);
        inventories.transaction = CreateInventory(transactionPanel.inventoryParent, transactionInventorySize);

        const int crucibleInventorySize = 6;
        SpawnUiSlots(eyeForgePanel.pentagramParent, crucibleInventorySize, prefabs.eyeForgeSlot);
        inventories.eyeForge = CreateInventory(eyeForgePanel.pentagramParent, crucibleInventorySize);
        SetupEyeForgeInventorySlots();

        if (gameState != null) {
            InitInventoryItems(gameState.playerInventoryItems, inventories.player);
            InitInventoryItems(gameState.stashInventoryItems, inventories.stash);
            InitInventoryItems(gameState.traderInventoryItems, inventories.trader);
            InitInventoryItems(gameState.forgeInventoryItems, inventories.eyeForge);
        }
    }
    
    private void SetupEyeForgeInventorySlots() {
        int inventoryLength = inventories.eyeForge.slots.Length;
        for (int i = 0; i < inventoryLength; i++) {
            bool isCenterSlot = i == 0;
            InventorySlotUI slotUI = inventories.eyeForge.slots[i].ui;
            slotUI.disallowItemStacking = true;
            slotUI.onlyAcceptedItemType = isCenterSlot ? itemTypes.eye : itemTypes.eyeUpgrade;
            slotUI.itemUI.pixelFillManager.Init(isCenterSlot ? PixelFillManager.FillDirection.None : PixelFillManager.FillDirection.Up);

            if (isCenterSlot) {
                slotUI.gameObject.transform.position = eyeForgePanel.pentagramParent.position;
                continue;
            }
            
            float deg = 360f / (inventoryLength - 1) * (i - 1);
            Vector3 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 180f;
            slotUI.gameObject.transform.position = eyeForgePanel.pentagramParent.position + spawnDir;
        }
    }
    
    private void InitInventoryItems(List<ItemInstance> itemInstances, Inventory inventory) {
        if (itemInstances == null) return;

        if (inventory == inventories.player && itemInstances.Count != inventory.slots.Length) {
            ChangeInventorySize(inventory, itemInstances.Count);
        }
        
        // Items can be null because we save all inventory slots, including empty ones
        foreach (ItemInstance itemInstance in itemInstances) {
            if (itemInstance == null) continue;
            if (itemInstance.isDemonEye) {
                BuildAndRegisterDemonEye(itemInstance);
            }
        }
        
        CopyItemsToInventory(itemInstances, inventory);
    }
    
    private void CopyItemsToInventory(List<ItemInstance> items, Inventory toInventory) {
        if (items == null || toInventory == null) return;
        
        for (int i = 0; i < toInventory.slots.Length; i++) {
            if (!toInventory.slots.IndexInRange(i) || !items.IndexInRange(i)) break;
            toInventory.slots[i].itemInstance = items[i];
        }
    }
    
    private InventorySlot[] CreateLootInventoryInstance(List<ItemInstance> inventoryItems) {
        var slots = new InventorySlot[inventories.lootPtr.slots.Length];
        
        for (int i = 0; i < inventories.lootPtr.slots.Length; i++) {
            ItemInstance itemInstance = null;
            if (inventoryItems.IndexInRange(i)) {
                itemInstance = inventoryItems[i];
            }
            slots[i] = new() {
                itemInstance = itemInstance,
                ui = inventories.lootSlotUis[i],
            };
        }
        
        return slots;
    }
    
    private InventorySlot[] CreateLootInventoryFromItems(List<Item> items, DropPool dropPool, float stackTaperRate) {
        using var _ = ListPool<ItemInstance>.Get(out var itemInstances);
            
        foreach (Item item in items) {
            int stackCount = 1;
            
            float taperingChance = Mathf.Lerp(GetDropChanceOfItem(item, dropPool, curRaid.map), 0f, stackTaperRate);
            while (RollProbability(taperingChance)) {
                stackCount++;
                taperingChance = Mathf.Lerp(taperingChance, 0f, stackTaperRate);
            }
            itemInstances.Add(new(item, stackCount) { notDiscovered = true });
        }
        
        return CreateLootInventoryInstance(itemInstances);
    }
    
    private bool AppendToLootInventory(InventorySlot[] lootSlots, Item item, int stackCount) {
        foreach (InventorySlot slot in lootSlots) {
            if (slot.itemInstance != null) continue;
            slot.itemInstance = new(item, stackCount) { notDiscovered = true };
            return true;
        }
        return false;
    }
    
    private void UpdateInventory() {
        CheckForEquipmentChange();
        HandleInventoryVisibility();
        
        if (NoOpenInventories()) {
            HideInventoryItemPopup();
            return;
        }
        
        InventoryHoverInfo invHoverInfo = UpdateInventoryHover();
        CheckToMoveItem(invHoverInfo);

        bool draggingItem = UpdateInventoryDragAndDrop(invHoverInfo);
        if (draggingItem) {
            HideInventoryItemPopup();
        }
        else {
            UpdateInventoryItemPopup(invHoverInfo);
            CheckToConsumeItem(invHoverInfo);
        }
    }
    
    private void HandleInventoryVisibility() {
        if (!InRaid || !input.inventory.WasPressedThisFrame()) return;
        if (PlayerInventoryIsOpen) {
            ClosePlayerInventory(); 
        }
        else {
            OpenPlayerInventory();
        }
        if (LootInventoryIsOpen) {
            CloseLootInventory();
        }
    }
    
    private bool NoOpenInventories() {
        foreach (Inventory inventory in inventories.all) {
            if (inventory.parent.gameObject.activeInHierarchy) {
                return false;
            }
        }
        return true;
    }

    private void UpdateInventoryItemPopup(InventoryHoverInfo invHoverInfo) {
        bool hoveringOverItem = TryGetItemFromHoverInfo(invHoverInfo, out ItemInstance _);
        
        const float hoverTimeUntilTooltip = 0.32f;
        bool spentEnoughTimeHovering = invHoverInfo.timeSpentHovering >= hoverTimeUntilTooltip;
        
        if (hoveringOverItem && spentEnoughTimeHovering) {
            ShowInventoryItemPopup(invHoverInfo);
        }
        else {
            HideInventoryItemPopup();
        }
    }
    
    private void ShowInventoryItemPopup(InventoryHoverInfo info) {
        if (ui.itemDescPopupInv.gameObject.activeInHierarchy) return;
        
        InventorySlot hoveredSlot = info.inventory.slots[info.slotIndex];
        
        // Set popup position
        Vector2 popupPosition = Vector2.zero;
        Vector2 hoveredSlotCenter = hoveredSlot.ui.rectTransform.WorldRect().center;
        float halfPopupWidth = ui.itemDescPopupInv.rectTransform.rect.width / 2f;
        Vector2 popupOffset = new(45 + halfPopupWidth, 40);
        
        if (hoveredSlotCenter.x < ScreenCenter.x) {
            popupPosition = hoveredSlotCenter + popupOffset;
        }
        else {
            popupPosition = hoveredSlotCenter + new Vector2(-popupOffset.x, popupOffset.y);
        }
            
        ui.itemDescPopupInv.Show(hoveredSlot.itemInstance, popupPosition);
        
        // Add mechanic desctiption if necessary
        if (hoveredSlot.itemInstance.ItemRef.type == itemTypes.eyeUpgrade) {
            EyeUpgrade eyeUpgrade = (EyeUpgrade)hoveredSlot.itemInstance.ItemRef;
            if (eyeUpgrade.relativeMechanicDesc) {
                ui.mechanicDescPopup.gameObject.SetActive(true);
                ui.mechanicDescPopup.nameText.text = eyeUpgrade.relativeMechanicDesc.displayName;
                ui.mechanicDescPopup.descText.text = eyeUpgrade.relativeMechanicDesc.description;
                ui.mechanicDescPopup.transform.position = ui.itemDescPopupInv.rectTransform.WorldRect().min;
                
                ui.mechanicDescPopup.nameFitter.ForceRecalculate();
                ui.mechanicDescPopup.descFitter.ForceRecalculate();
                FitPopupSize(
                    ui.mechanicDescPopup.rectTransform,
                    ui.mechanicDescPopup.nameText.rectTransform.rect, 
                    ui.mechanicDescPopup.descText.rectTransform.rect
                );
                TweenPopUp(ui.mechanicDescPopup.rectTransform);
            } 
        }
    }

    private void HideInventoryItemPopup() {
        ui.itemDescPopupInv.Hide();
        ui.mechanicDescPopup.nameText.text = string.Empty;
        ui.mechanicDescPopup.descText.text = string.Empty;
        ui.mechanicDescPopup.gameObject.SetActive(false);
    }
    
    private void CheckToMoveItem(InventoryHoverInfo invHoverInfo) {
        if (!input.moveStack.WasPressedThisFrame()) return;

        Inventory hoveredInventory = invHoverInfo.inventory;
        if (hoveredInventory == null) return;
        
        if (!TryGetItemFromHoverInfo(invHoverInfo, out ItemInstance hoveredItem)) return;
        if (NotAllowedToMoveOrPickupItem(invHoverInfo)) return;
        if (ClickedOnEquipedBackpackWithItems(invHoverInfo.inventory, invHoverInfo.slotIndex)) return;

        MoveItemOption moveOption = MoveItemOption.FullStack;
        Inventory destinationInventory = null;
        
        if (InRaid) {
            if (hoveredInventory == inventories.player && LootInventoryIsOpen) {
                destinationInventory = inventories.lootPtr;
            }
            else if (hoveredInventory == inventories.lootPtr) {
                destinationInventory = inventories.player;
            }
        }
        else if (OnCharacterTab) {
            if (hoveredInventory == inventories.player) {
                destinationInventory = inventories.stash;
            }
            else if (hoveredInventory == inventories.stash) {
                destinationInventory = inventories.player;
            }
        }
        else if (OnEyeForgeTab) {
            if (hoveredInventory == inventories.stash) {
                destinationInventory = inventories.eyeForge;
            }
            else if (hoveredInventory == inventories.eyeForge) {
                destinationInventory = inventories.stash;
            }
        }
        else if (OnTradingTab) {
            if (transactionState == TransactionState.Selling) {
                if (hoveredInventory == inventories.stash) {
                    destinationInventory = inventories.transaction;
                }
                else if (hoveredInventory == inventories.transaction) {
                    destinationInventory = inventories.stash;
                }
            }
        }

        if (destinationInventory == null) return;
        MoveItemBetweenInventories(hoveredInventory, destinationInventory, invHoverInfo.slotIndex, moveOption);
    }

    private void CheckToConsumeItem(InventoryHoverInfo invHoverInfo) {
        if (!input.useItem.WasPressedThisFrame()) return;
        if (!TryGetItemFromHoverInfo(invHoverInfo, out ItemInstance hoveredItem)) return;
        if (hoveredItem.ItemRef.type != itemTypes.quickUse) return;
        HavePlayerConsumeItem(invHoverInfo.inventory, invHoverInfo.slotIndex);
    }

    private bool TryGetItemFromHoverInfo(InventoryHoverInfo invHoverInfo, out ItemInstance hoveredItemInstance) {
        hoveredItemInstance = null;
        
        int hoveredSlot = invHoverInfo.slotIndex;
        Inventory hoveredInventory = invHoverInfo.inventory;
        
        if (hoveredInventory == null) return false;
        if (!hoveredInventory.slots.IndexInRange(hoveredSlot)) return false;
        if (hoveredInventory.slots[hoveredSlot].itemInstance == null) return false;
        if (hoveredInventory.slots[hoveredSlot].itemInstance.notDiscovered) return false;
        
        hoveredItemInstance = hoveredInventory.slots[hoveredSlot].itemInstance;
        return true;
    }

    private bool IsHoveredItemGrayedOut(InventoryHoverInfo invHoverInfo) {
        Assert.IsTrue(TryGetItemFromHoverInfo(invHoverInfo, out _), 
            $"Method requires that you're hovering over an item, call {nameof(TryGetItemFromHoverInfo)} before to make sure.");
        
        int hoveredSlot = invHoverInfo.slotIndex;
        Inventory hoveredInventory = invHoverInfo.inventory;
        return hoveredInventory.slots[hoveredSlot].ui.IsGrayedOut;
    }
    
    private bool NotAllowedToMoveOrPickupItem(InventoryHoverInfo info) {
        if (IsHoveredItemGrayedOut(info)) {
            return true;
        }
        // Because we wait to reduce the consumed inventory item count until the consuming tween has finished,
        // we can't drag it from its slot, why not just use decrement a reference to InventoryItem? Because when moving it 
        // between inventories the item reference may become stale.
        if (player.isConsumingItem && info.inventory == player.consumption.inventory && info.slotIndex == player.consumption.slotIndex) {
            return true;
        }
        if (info.inventory == inventories.eyeForge && PlayingForgeAnimation) {
            return true;
        }
        return false;
    }
    
    private void SpawnUiSlots(RectTransform parent, int numSlots, GameObject slotPrefab = null) {
        for (int i = 0; i < numSlots; i++) {
            Instantiate(slotPrefab ? slotPrefab : prefabs.inventorySlot, Vector3.zero, Quaternion.identity, parent);
        }
    }
    
    private Inventory CreateInventory(RectTransform uiParent, int slotCount) {
        Inventory inventory = new() {
            parent = uiParent,
            slots = new InventorySlot[slotCount],
        };
        inventory.slots.InitalizeWithDefault();
        LinkInventoryWithUiSlots(inventory);
        inventories.all.Add(inventory);
        return inventory;
    }

    private void LinkInventoryWithUiSlots(Inventory inventory) {
        InventorySlotUI[] slotUis = inventory.parent.GetComponentsInChildren<InventorySlotUI>(true);
        foreach (InventorySlotUI slotUi in slotUis) {
            slotUi.gameObject.SetActive(false);
        }
        
        for (int i = 0; i < inventory.slots.Length; i++) {
            inventory.slots[i].ui = slotUis[i];
            inventory.slots[i].ui.gameObject.SetActive(true);
        }
    }

    private void ChangeInventorySize(Inventory inventory, int newSlotCount) {
        bool expanding = newSlotCount > inventory.slots.Length;
        
        InventorySlot[] oldSlots = inventory.slots;
        inventory.slots = new InventorySlot[newSlotCount];
        inventory.slots.InitalizeWithDefault();

        LinkInventoryWithUiSlots(inventory);
        int copyLength = expanding ? oldSlots.Length : inventory.slots.Length;
        for (int i = 0; i < copyLength; i++) {
            inventory.slots[i] = oldSlots[i];
        }
    }

    private bool IsEquipmentSlot(Inventory inventory, int slotIndex) {
        return inventory == inventories.player && slotIndex < playerEquipmentSize;
    }
    
    private bool EquipedBackpackHasItems() {
        int startingIndex = NakedPlayerInventorySize;
        for (int i = startingIndex; i < inventories.player.slots.Length; i++) {
            if (inventories.player.slots[i].itemInstance != null) {
                return true;
            }
        }
        return false;
    }
    
    private bool ClickedOnEquipedBackpackWithItems(Inventory inventory, int slotIndex) {
        if (inventory.slots[slotIndex].itemInstance.ItemRef.type != itemTypes.backpack) {
            return false;
        }
        return IsEquipmentSlot(inventory, slotIndex) && EquipedBackpackHasItems();
    }
    
    private ItemInstance prevEquippedEyeItemInstance;
    private ItemInstance prevEquippedBackpackItemInstance;
    private ItemInstance prevEquippedTrinketItemInstance;
    
    private void CheckForEquipmentChange() {
        ItemInstance curEyeItemInstance = inventories.player.slots[0].itemInstance;
        ItemInstance curBackpackItemInstance = inventories.player.slots[1].itemInstance;
        ItemInstance curTrinketItemInstance = inventories.player.slots[2].itemInstance;

        if (prevEquippedEyeItemInstance != curEyeItemInstance) {
            prevEquippedEyeItemInstance = curEyeItemInstance;
            DemonEyeInstance newDemonEye = curEyeItemInstance == null ? demonEye.empty : demonEye.instanceFromItemId[curEyeItemInstance.itemOrInstanceUuid];
            OnEquipDemonEye(newDemonEye);
        }
        
        if (prevEquippedBackpackItemInstance != curBackpackItemInstance) {
            prevEquippedBackpackItemInstance = curBackpackItemInstance;
            if (curBackpackItemInstance != null) {
                Assert.IsTrue(curBackpackItemInstance.ItemRef is BackpackItem);
                int backpackSize = (curBackpackItemInstance.ItemRef as BackpackItem)!.additionalStorageSlots;
                ChangeInventorySize(inventories.player, NakedPlayerInventorySize + backpackSize);
            }
            else {
                ChangeInventorySize(inventories.player, NakedPlayerInventorySize);
            }
        }
        
        if (prevEquippedTrinketItemInstance != curTrinketItemInstance) {
            prevEquippedTrinketItemInstance = curTrinketItemInstance;
            if (curTrinketItemInstance != null) {
                Assert.IsTrue(curTrinketItemInstance.ItemRef is Trinket);
                Trinket trinket = curTrinketItemInstance.ItemRef as Trinket;
                OnEquipTrinket(trinket);
            }
        }
    }

    public struct InventoryHoverInfo {
        public Inventory inventory;
        public int slotIndex;
        public float timeSpentHovering;
    }

    private InventoryHoverInfo lastInventoryHoverInfo;
    
    private InventoryHoverInfo UpdateInventoryHover() {
        InventoryHoverInfo info = new();
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        foreach (Inventory inventory in inventories.all) {
            if (!inventory.parent.gameObject.activeInHierarchy) continue;
            
            Vector2 localMousePos = inventory.parent.InverseTransformPoint(mousePos);
            Bounds localUiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(inventory.parent);
            if (!localUiBounds.Contains(localMousePos)) continue;
            
            info.inventory = inventory;
            info.slotIndex = GetHoveredInventorySlot(inventory);

            bool hoveringOverPrevSlot = info.inventory == lastInventoryHoverInfo.inventory && info.slotIndex == lastInventoryHoverInfo.slotIndex;
            if (hoveringOverPrevSlot && !IsDraggingItem) {
                info.timeSpentHovering = lastInventoryHoverInfo.timeSpentHovering + Time.deltaTime;
            }
            else {
                info.timeSpentHovering = 0f;
            }
            
            break;
        }

        lastInventoryHoverInfo = info;
        return info;
    }
    
    private int GetHoveredInventorySlot(Inventory inventory) {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        for (int i = 0; i < inventory.slots.Length; i++) {
            RectTransform rectTrans = inventory.slots[i].ui.rectTransform;
            bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
            if (mouseInRect) {
                return i;
            }
        }
        return -1;
    }

    private ItemInstance dragItemInstance;
    private InventoryHoverInfo startDragInfo;
    
    private bool IsDraggingItem => dragItemInstance != null;

    private bool UpdateInventoryDragAndDrop(InventoryHoverInfo hoverInfo) {
        bool pickupInputUsed = input.selectItem.WasPressedThisFrame() || input.splitStack.WasPressedThisFrame();
        bool placeInputUsed = input.selectItem.WasPressedThisFrame() || input.placeSingleItem.WasPressedThisFrame();
        
        if (!pickupInputUsed && !placeInputUsed) {
            return IsDraggingItem;
        }

        // We don't allow trader items to be picked up
        if (hoverInfo.inventory == inventories.trader && !IsDraggingItem) {
            if (TryGetItemFromHoverInfo(hoverInfo, out _)) {
                SetTradingSlot(hoverInfo.inventory.slots[hoverInfo.slotIndex], tweenSize: true); 
            }
            return IsDraggingItem;
        }

        // If we are putting trader items back, then we also don't want to pick up the items
        if (!IsDraggingItem && hoverInfo.inventory == inventories.transaction && transactionState == TransactionState.Buying) {
            MoveItemBetweenInventories(inventories.transaction, inventories.trader, hoverInfo.slotIndex, MoveItemOption.Single);
            return IsDraggingItem;
        }
        
        bool pickingUpItem = dragItemInstance == null;
        if (pickingUpItem && pickupInputUsed) {
            if (!TryGetItemFromHoverInfo(hoverInfo, out ItemInstance item)) {
                return IsDraggingItem;
            }

            if (NotAllowedToMoveOrPickupItem(hoverInfo)) {
                return IsDraggingItem;
            }
            
            if (ClickedOnEquipedBackpackWithItems(hoverInfo.inventory, hoverInfo.slotIndex)) {
                return IsDraggingItem;
            }

            bool splittingStack = input.splitStack.WasPressedThisFrame() && item.count > 1;
            if (splittingStack) {
                int firstHalf = item.count / 2;
                int secondHalf = item.count - firstHalf;
                
                dragItemInstance = item.Clone();
                dragItemInstance.count = secondHalf;
                
                AdjustItemCountInInventory(hoverInfo.inventory, hoverInfo.slotIndex, firstHalf);
            }
            else {
                dragItemInstance = item;
                RemoveItemFromInventory(hoverInfo.inventory, hoverInfo.slotIndex);
            }

            startDragInfo = hoverInfo;
            ui.dragAndDropItemUI.gameObject.SetActive(true);
            ui.dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
            TweenItemMove(ui.dragAndDropItemUI);
        }

        bool placingItem = !pickingUpItem;
        if (placingItem && placeInputUsed) {
            bool droppingItemInHideout = hoverInfo.inventory == null && InHideout;
            bool tryingToPlaceItemToSellWhileBuying = hoverInfo.inventory == inventories.transaction && transactionState == TransactionState.Buying;
            bool tryingToPlaceInTraderInventory = hoverInfo.inventory == inventories.trader;
            
            if (droppingItemInHideout || tryingToPlaceItemToSellWhileBuying || tryingToPlaceInTraderInventory) {
                TryAddItemToInventory(startDragInfo.inventory, dragItemInstance, startDragInfo.slotIndex);
                EndDragAndDropItem();
                return IsDraggingItem;
            }

            bool droppingItemInRaid = hoverInfo.inventory == null && InRaid;
            if (droppingItemInRaid) {
                bool droppingEntireStack = input.selectItem.WasPressedThisFrame();
                if (droppingEntireStack) {
                    DropItemFromInventory(dragItemInstance);
                    dragItemInstance.count = 0;
                }
                else {
                    DropItemFromInventory(dragItemInstance, 1);
                    dragItemInstance.count--;
                    ui.dragAndDropItemUI.UpdateCount(dragItemInstance.count);
                }

                if (dragItemInstance.count <= 0) {
                    EndDragAndDropItem();
                }
                return IsDraggingItem;
            }

            bool swappingItems = false;
            if (TryGetItemFromHoverInfo(hoverInfo, out ItemInstance swapItem)) {
                bool itemsCanSwap = swapItem != dragItemInstance || (swapItem.IsFullStack || dragItemInstance.IsFullStack);
                swappingItems = itemsCanSwap && input.selectItem.WasPressedThisFrame();
            }
            
            if (swappingItems && IsHoveredItemGrayedOut(hoverInfo)) {
                return IsDraggingItem;
            }
            
            if (swappingItems) {
                InventorySlot targetSlot = hoverInfo.inventory.slots[hoverInfo.slotIndex];
                if (targetSlot.ui.disallowItemStacking && dragItemInstance.count > 1) {
                    return IsDraggingItem;
                }
                if (!targetSlot.ui.AcceptsAllTypes && targetSlot.ui.onlyAcceptedItemType != dragItemInstance.ItemRef.type) {
                    return IsDraggingItem;
                }

                targetSlot.itemInstance = dragItemInstance;
                dragItemInstance = swapItem;
                ui.dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
                TweenItemMove(ui.dragAndDropItemUI);

                return IsDraggingItem;
            }

            bool placingSingleItemFromStack = input.placeSingleItem.WasPressedThisFrame();
            if (placingSingleItemFromStack) {
                InventoryAddResult result = TryAddItemToInventory(hoverInfo.inventory, dragItemInstance.ItemRef, 1, hoverInfo.slotIndex);

                dragItemInstance.count -= result.addedCount;
                if (dragItemInstance.count <= 0) {
                    EndDragAndDropItem();
                }
                else {
                    ui.dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
                    TweenItemMove(ui.dragAndDropItemUI);
                }
            }

            bool placingEntireStack = !placingSingleItemFromStack;
            if (placingEntireStack) {
                InventoryAddResult result = TryAddItemToInventory(hoverInfo.inventory, dragItemInstance, hoverInfo.slotIndex);

                if (result.type == InventoryAddResult.ResultType.Success) {
                    EndDragAndDropItem();
                }
                else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
                    dragItemInstance.count -= result.addedCount;
                    ui.dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
                    TweenItemMove(ui.dragAndDropItemUI);
                }
            }
        }

        return IsDraggingItem;
    }

    private void DropItemFromInventory(ItemInstance itemInstance, int count = -1) {
        Vector2 mouseWorldPos = camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dropDir = (mouseWorldPos - (Vector2)player.position).normalized;
        
        int dropCount = count == -1 ? itemInstance.count : count;
        
        Vector3 endPos = player.position + RandomizeVectorAngle(dropDir, 20f) * 0.2f;
        Entity itemDropEntity = SpawnItemAsEntity(itemInstance.ItemRef, dropCount, player.position, Quaternion.identity);
        
        AddBounceEffect(itemDropEntity, endPos, 0.8f);
    }

    private void UpdateDragAndDropItemToCursor() {
        if (dragItemInstance == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        ui.dragAndDropItemUI.GetComponent<RectTransform>().position = mousePos;
    }
    
    private void EndDragAndDropItem() {
        if (dragItemInstance == null) return;
        dragItemInstance = null;
        ui.dragAndDropItemUI.ClearItem();
        ui.dragAndDropItemUI.gameObject.SetActive(false);
    }

    public struct InventoryAddResult {
        public enum ResultType { Success, Failure, FailureToAddAll };
        public ResultType type;
        public int addedCount;
    }
    
    public InventoryAddResult TryAddItemToInventory(Inventory inventory, Item item, int count, int slotIndex = -1) {
        ItemInstance newItemInstance = new(item, count);
        return TryAddItemToInventory(inventory, newItemInstance, slotIndex);
    }

    public InventoryAddResult TryAddItemToInventory(Inventory inventory, ItemInstance itemInstance, int slotIndex = -1) {
        InventoryAddResult result = new() { type = InventoryAddResult.ResultType.Failure };
        
        bool allowInfiniteStacking = inventory == inventories.trader;
        bool droppingItemInSpecificSlot = slotIndex != -1;

        using var _ = ListPool<InventorySlot>.Get(out List<InventorySlot> availableSlots);
        
        if (droppingItemInSpecificSlot) {
            availableSlots.Add(inventory.slots[slotIndex]);
        }
        else {
            availableSlots.AddRange(inventory.slots[..]);
        }

        int remainingItemCount = itemInstance.count;

        // If we can stack the item then we just do that
        foreach (InventorySlot slot in availableSlots) {
            if (slot.itemInstance == null || slot.itemInstance.itemOrInstanceUuid != itemInstance.itemOrInstanceUuid) continue;
            if (slot.ui.disallowItemStacking || (!allowInfiniteStacking && slot.itemInstance.IsFullStack)) continue;

            TweenItemMove(slot.ui.itemUI);
                
            if (allowInfiniteStacking) {
                slot.itemInstance.count += itemInstance.count;
                result.addedCount += itemInstance.count;
                result.type = InventoryAddResult.ResultType.Success;
                return result;
            }
            
            int overflowAmount = (remainingItemCount + slot.itemInstance.count) - slot.itemInstance.ItemRef.MaxStackCount;
            if (overflowAmount > 0) {
                int addCount = slot.itemInstance.ItemRef.MaxStackCount - slot.itemInstance.count;
                
                slot.itemInstance.count += addCount;
                remainingItemCount = overflowAmount;
                
                result.addedCount += addCount;
                result.type = InventoryAddResult.ResultType.FailureToAddAll;

                if (!droppingItemInSpecificSlot) continue;

                return result;
            }
            
            slot.itemInstance.count += remainingItemCount;
            result.addedCount += remainingItemCount;
            result.type = InventoryAddResult.ResultType.Success;
            
            return result;
        }

        // Otherwise add to empty inventory slot
        foreach (InventorySlot slot in availableSlots) {
            if (slot.itemInstance != null || slot.ui.SlotIsInactive) continue;

            if (!slot.ui.AcceptsItem(itemInstance.ItemRef)) continue;

            int newItemCount = allowInfiniteStacking ? remainingItemCount : Mathf.Clamp(remainingItemCount, 0, itemInstance.ItemRef.MaxStackCount);
            newItemCount = slot.ui.disallowItemStacking ? 1 : newItemCount;
            result.addedCount += newItemCount;
            
            ItemInstance newItemInstance = itemInstance.Clone();
            newItemInstance.count = newItemCount;
            slot.itemInstance = newItemInstance;
            
            TweenItemMove(slot.ui.itemUI);
            
            bool movedEntireStack = newItemCount == remainingItemCount;
            result.type = movedEntireStack ? InventoryAddResult.ResultType.Success : InventoryAddResult.ResultType.FailureToAddAll;
            
            return result;
        }
        
        return result;
    }
    
    private void TweenItemMove(ItemUI itemUI) {
        Tween.PunchScale(itemUI.rectTransform, Vector3.one * 0.15f, 0.135f);
    }

    private enum MoveItemOption { FullStack, Single }

    private void MoveItemBetweenInventories(Inventory fromInventory, Inventory toInventory, int slotIndex, MoveItemOption moveOption) {
        ItemInstance itemInstance = GetInventoryItem(fromInventory, slotIndex);
        if (itemInstance == null || itemInstance.notDiscovered) return;

        int specificSlotToMoveTo = -1;
        if (fromInventory == inventories.transaction && toInventory == inventories.trader) {
            specificSlotToMoveTo = itemInstance.traderSlotIndex;
        }
        
        if (moveOption == MoveItemOption.Single) {
            ItemInstance newItemInstance = itemInstance.Clone();
            newItemInstance.count = 1;
            
            InventoryAddResult result = TryAddItemToInventory(toInventory, newItemInstance, specificSlotToMoveTo);
            if (result.type is InventoryAddResult.ResultType.Success or InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = itemInstance.count - result.addedCount;
                AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
            }
            return;
        }

        MoveEntireItemStack(fromInventory, toInventory, slotIndex, specificSlotToMoveTo);
    }

    private void MoveEntireItemStack(Inventory fromInventory, Inventory toInventory, int fromSlotIndex, int toSlotIndex = -1) {
        ItemInstance itemInstance = GetInventoryItem(fromInventory, fromSlotIndex);
        if (itemInstance == null) return;
        
        InventoryAddResult moveResult = TryAddItemToInventory(toInventory, itemInstance, toSlotIndex);
        if (moveResult.type == InventoryAddResult.ResultType.Success) {
            RemoveItemFromInventory(fromInventory, fromSlotIndex);
        }
        else if (moveResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
            int keepItemCount = itemInstance.count - moveResult.addedCount;
            AdjustItemCountInInventory(fromInventory, fromSlotIndex, keepItemCount);
        }
    }
    
    private void ClearInventory(Inventory inventory) {
        for (int i = 0; i < inventory.slots.Length; i++) {
            RemoveItemFromInventory(inventory, i);
        }
        if (inventory == inventories.player) {
            CheckForEquipmentChange();
        }
    }

    private void RemoveItemFromInventory(Inventory inventory, int slotIndex) {
        inventory.slots[slotIndex].itemInstance = null;
        inventory.slots[slotIndex].ui.ClearItem();
    }

    // Returns the count of items we removed
    private int RemoveNumberOfItemsFromInventory(Inventory inventory, Item item, int count) {
        int removedCount = 0;
        
        for (int i = 0; i < inventory.slots.Length; i++) {
            InventorySlot slot = inventory.slots[i];
            if (slot.itemInstance == null || slot.itemInstance.itemOrInstanceUuid != item.uuid) continue;
            
            if (slot.itemInstance.count >= count) {
                removedCount += count;
                AdjustItemCountInInventory(inventory, i, slot.itemInstance.count - count);
                return removedCount;
            }
            
            removedCount += slot.itemInstance.count;
            count -= slot.itemInstance.count;
            RemoveItemFromInventory(inventory, i);
        }
        
        return removedCount;
    }

    private void RemoveNumberOfOwnedItems(Item item, int count) {
        int removedCount = RemoveNumberOfItemsFromInventory(inventories.stash, item, count);
        if (removedCount != count) {
            int additionalRemoveCount = count - removedCount;
            removedCount += RemoveNumberOfItemsFromInventory(inventories.player, item, additionalRemoveCount);
        }
        Assert.IsTrue(removedCount == count, "Did not remove the specified number of item, this is bad");
    }
    
    private ItemInstance GetInventoryItem(Inventory inventory, int slotIndex) {
        if (slotIndex < 0 || slotIndex >= inventory.slots.Length) {
            return null;
        }
        return inventory.slots[slotIndex].itemInstance;
    }

    // Returns true if we reduced the item to nothing
    private bool ReduceItemCountInInventory(Inventory inventory, int slotIndex, int reduction = 1, bool keepOnEmpty = false) {
        var item = GetInventoryItem(inventory, slotIndex);
        item.count -= reduction;
        if (item.count <= 0 && !keepOnEmpty) {
            RemoveItemFromInventory(inventory, slotIndex);
            return true;
        }
        return false;
    }
    
    private void AdjustItemCountInInventory(Inventory inventory, int slotIndex, int newCount) {
        ItemInstance itemInstance = GetInventoryItem(inventory, slotIndex);
        itemInstance.count = newCount;
        if (itemInstance.count <= 0) {
            RemoveItemFromInventory(inventory, slotIndex);
        }
    }

    public void RefreshInventoryDisplay(Inventory inventory) {
        bool notBeingShown = !inventory.parent.gameObject.activeInHierarchy;
        if (notBeingShown) return;
        
        foreach (InventorySlot slot in inventory.slots) {
            slot.ui.ClearItem();
        }

        for (int i = 0; i < inventory.slots.Length; i++) {
            ItemInstance itemInstance = inventory.slots[i].itemInstance;
            if (itemInstance == null || itemInstance.notDiscovered) continue;
            inventory.slots[i].ui.SetItem(itemInstance.ItemRef, itemInstance.count);
        }
    }

    private void RefreshAllInventoryDisplays() {
        foreach (Inventory inventory in inventories.all) {
            RefreshInventoryDisplay(inventory);
        }
    }
    
    private void UpdateGraySlots() {
        if (OnEyeForgeTab) {
            foreach (InventorySlot slot in inventories.stash.slots) {
                if (slot.itemInstance == null) continue;
                Item item = slot.itemInstance.ItemRef;
                if (item.type != itemTypes.eye && item.type != itemTypes.demonEye && item.type != itemTypes.eyeUpgrade) {
                    slot.ui.ToggleGray();
                }
            }
        }
        if (OnTradingTab) {
            foreach (InventorySlot slot in inventories.trader.slots) {
                if (slot.itemInstance == null) continue;
                if (slot.itemInstance.count <= 0) {
                    slot.ui.ToggleOutOfStock();
                }
            }
        }
    }

    public int GetInventoryItemCount(Inventory inventory) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.itemInstance == null) continue;
            count++;
        }
        return count;
    }

    public int GetItemCountInInventory(Inventory inventory, Item item) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.itemInstance == null) continue;
            if (slot.itemInstance.ItemRef.uuid == item.uuid) {
                count += slot.itemInstance.count;
            }
        }
        return count;
    }
    
    public int GetItemCountInInventory(Inventory inventory, ItemType itemType) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.itemInstance == null) continue;
            if (slot.itemInstance.ItemRef.type == itemType) {
                count += slot.itemInstance.count;
            }
        }
        return count;
    }

    public int GetOwnedCountOfItem(Item item) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(inventories.stash, item);
        itemCount += GetItemCountInInventory(inventories.player, item);
        return itemCount;
    }
    
    public int GetOwnedCountOfItem(ItemType itemType) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(inventories.stash, itemType);
        itemCount += GetItemCountInInventory(inventories.player, itemType);
        return itemCount;
    }

    private bool MeetsSingleUpgradeRequirement(UpgradePath.Requirement req) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(inventories.stash, req.item);
        itemCount += GetItemCountInInventory(inventories.player, req.item);
        return itemCount >= req.count; 
    }

    private int GetInventoryWeight(Inventory inventory) {
        int weight = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.itemInstance == null) continue;
            weight += slot.itemInstance.ItemRef.Weight * slot.itemInstance.count;
        }
        return weight;
    }
    
    private enum InventoryValueType { Buy, Sell }

    private int GetInventoryValue(Inventory inventory, InventoryValueType valueType) {
        int value = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.itemInstance == null) continue;
            switch (valueType) {
                case InventoryValueType.Buy:
                    value += slot.itemInstance.ItemRef.type == itemTypes.demonEye ? GetDemonEyeSellPrice(slot.itemInstance) : slot.itemInstance.ItemRef.buyPrice * slot.itemInstance.count;
                    break;
                case InventoryValueType.Sell:
                    value += slot.itemInstance.ItemRef.type == itemTypes.demonEye ? GetDemonEyeSellPrice(slot.itemInstance) : slot.itemInstance.ItemRef.GetSellPrice() * slot.itemInstance.count;
                    break;
            }
        }
        return value;
    }

    private void OpenPlayerInventory() {
        playerPanel.panel.gameObject.SetActive(true);
        Cursor.visible = true;
    }

    private void ClosePlayerInventory() {
        playerPanel.panel.gameObject.SetActive(false);
        Cursor.visible = false;
        EndDragAndDropItem();
    }
    
}
