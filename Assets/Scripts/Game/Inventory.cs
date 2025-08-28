using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameManager {
    
    [Serializable]
    public class InventoryItem {
        public int itemDataUuid;
        public List<int> modifierUuids;
        public int count = 1;

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public Item _itemRef; // Used for items created at runtime, like demon eyes

        public Item ItemRef => _itemRef ? _itemRef : itemLookup[itemDataUuid];
        public bool IsFullStack => count == ItemRef.MaxStackCount;

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
                foreach (int modifierUuid in modifierUuids) {
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
    [NonSerialized] private Inventory crucibleInventory;
    [NonSerialized] private Inventory traderInventory;
    [NonSerialized] private Inventory transactionInventory;
    [NonSerialized] private Inventory lootInvetoryPtr;
    [NonSerialized] private List<Inventory> allInventories = new();
    
    private const int playerPocketSize = 6;
    private const int playerEquipmentSize = 3;
    private int DefaultPlayerInventorySize => playerPocketSize + playerEquipmentSize;

    private const int stashUpgradeSlotIncrease = 4;
    
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
        SpawnUiSlots(playerPocketParent, playerPocketSize);
        SpawnUiSlots(playerBackpackParent, 20);
        playerInventory = CreateInventory(playerInventoryParent, DefaultPlayerInventorySize); 
        
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(lootInventoryParent, cachedLootInventorySize); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventorySize);

        int stashInventorySize = 12 + hideoutStateData.stashLevel * stashUpgradeSlotIncrease;
        SpawnUiSlots(stashInventoryParent, 40);
        stashInventory = CreateInventory(stashInventoryParent, stashInventorySize);
        
        const int traderInventorySize = 15;
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

            InventorySlotUI centerSlotUi = centerSlot.GetComponent<InventorySlotUI>();
            centerSlotUi.disallowItemStacking = true;
            centerSlotUi.acceptsAllTypes = false;
            centerSlotUi.onlyAcceptedItemType = Item.ItemType.Eye;
            
            for (int i = 0; i < crucibleVeinSize; i++) {
                float deg = 360f / crucibleVeinSize * i;
                Vector2 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 150f;
                GameObject slot = Instantiate(inventorySlotPrefab, crucibleCenter + spawnDir, Quaternion.identity, crucibleParent);
                InventorySlotUI veinSlot = slot.GetComponent<InventorySlotUI>();
                
                if (i != 0 && i > hideoutStateData.crucibleLevel) {
                    veinSlot.MakeSlotInactive();
                }
                
                veinSlot.disallowItemStacking = true;
                veinSlot.acceptsAllTypes = false;
                veinSlot.onlyAcceptedItemType = Item.ItemType.Soulcard;
            }
        }
        crucibleInventory = CreateInventory(crucibleParent, crucibleInventorySize);
    }
    
    private void UpdateInventory() {
        if (InRaid) {
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
        UpdateItemtooltip(invHoverInfo);
        HandleItemClicked(invHoverInfo);
        CheckForEquipmentChange();
    }
    
    private void UpdateItemtooltip(InventoryHoverInfo invHoverInfo) {
        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem _)) {
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

    private void HandleItemClicked(InventoryHoverInfo invHoverInfo) {
        if (!selectItemInputAction.WasPressedThisFrame() && !splitStackInputAction.WasPressedThisFrame()) return;

        Inventory hoveredInventory = invHoverInfo.hoveredInventory;
        if (hoveredInventory == null) return;

        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem hoveredItem)) return;

        bool clickedOnEquipedBackpack = hoveredInventory == playerInventory && hoveredItem.ItemRef.type == Item.ItemType.Backpack;
        if (clickedOnEquipedBackpack && EquipedBackpackHasItems()) {
            return;
        }
        
        Inventory destinationInventory = null;

        if (InRaid) {
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
                bool hoveredItemIsDemonEye = hoveredItem.ItemRef.type == Item.ItemType.DemonEye;
                destinationInventory = hoveredItemIsDemonEye ? playerInventory : crucibleInventory;
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

    private bool TryGetItemFromHoverInfo(InventoryHoverInfo invHoverInfo, out InventoryItem hoveredItem) {
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
    
    private void SpawnUiSlots(RectTransform parent, int numSlots) {
        for (int i = 0; i < numSlots; i++) {
            Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity, parent);
        }
    }
    
    private Inventory CreateInventory(RectTransform uiParent, int slotCount) {
        Inventory inventory = new() {
            parent = uiParent,
            slots = new InventorySlot[slotCount]
        };
        inventory.slots.InitalizeWithDefault();
        LinkInventoryWithUiSlots(inventory);
        allInventories.Add(inventory);
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

    private bool EquipedBackpackHasItems() {
        int startingIndex = DefaultPlayerInventorySize;
        for (int i = startingIndex; i < playerInventory.slots.Length; i++) {
            if (playerInventory.slots[i].item != null) {
                return true;
            }
        }
        return false;
    }

    
    private InventoryItem prevEquippedEyeItem;
    private InventoryItem prevEquippedBackpackItem;
    
    private void CheckForEquipmentChange() {
        InventoryItem curEyeItem = playerInventory.slots[0].item;
        InventoryItem curBackpackItem = playerInventory.slots[2].item;

        if (prevEquippedEyeItem != curEyeItem) {
            prevEquippedEyeItem = curEyeItem;
            if (curEyeItem == null) {
                equipedEye = new() { coreAttack = defaultAttack };
            }
            else {
                equipedEye = eyeInstanceFromItemId[curEyeItem.itemDataUuid];
            }
        }
        
        if (prevEquippedBackpackItem != curBackpackItem) {
            prevEquippedBackpackItem = curBackpackItem;
            if (curBackpackItem != null) {
                ChangeInventorySize(playerInventory, DefaultPlayerInventorySize + 9);
            }
            else {
                ChangeInventorySize(playerInventory, DefaultPlayerInventorySize);
            }
            RefreshInventoryDisplay(playerInventory);
        }

    }

    private void AddItemsToTraderInventory(int traderLevel) {
        ItemPool itemPool = traderLevelPools[traderLevel];
        for (int i = 0; i < 5; i++) {
            Item traderItem = itemPool.GetItemFromPool();
            TryAddItemToInventory(traderInventory, traderItem, traderItem.MaxStackCount);
            RefreshInventoryDisplay(traderInventory);
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

            int overflowAmount = (count + slot.item.count) - slot.item.ItemRef.MaxStackCount;
            if (overflowAmount > 0) {
                int addCount = slot.item.ItemRef.MaxStackCount - slot.item.count;
                
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
            if (slot.item != null || slot.ui.SlotIsInactive) continue;
            
            bool slotCanAcceptItemType = slot.ui.acceptsAllTypes || slot.ui.onlyAcceptedItemType == item.ItemRef.type;
            if (!slotCanAcceptItemType) continue;

            int addCount = slot.ui.disallowItemStacking ? 1 : Mathf.Clamp(count, 0, item.ItemRef.MaxStackCount);
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
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += modInstance.GetDescriptionForEye() + "\n";
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

    // Returns the count of items we removed
    private int RemoveNumberOfItemsFromInventory(Inventory inventory, Item item, int count) {
        int removedCount = 0;
        
        for (int i = 0; i < inventory.slots.Length; i++) {
            InventorySlot slot = inventory.slots[i];
            if (slot.item == null || slot.item.itemDataUuid != item.uuid) continue;
            
            if (slot.item.count >= count) {
                removedCount += count;
                AdjustItemCountInInventory(inventory, i, slot.item.count - count);
                return removedCount;
            }
            
            removedCount += slot.item.count;
            count -= slot.item.count;
            RemoveItemFromInventory(inventory, i);
        }
        
        return removedCount;
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
            slot.ui.ClearItem();
        }

        for (int i = 0; i < inventory.slots.Length; i++) {
            InventoryItem item = inventory.slots[i].item;
            if (item == null || item.notDiscovered) continue;
            inventory.slots[i].ui.SetItem(item.ItemRef, item.count);
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

    private int GetItemCountInInventory(Inventory inventory, Item item) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            if (slot.item.ItemRef.uuid == item.uuid) {
                count += slot.item.count;
            }
        }
        return count;
    }

    private int GetInventoryWeight(Inventory inventory) {
        int weight = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            weight += slot.item.ItemRef.Weight * slot.item.count;
        }
        return weight;
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
            child.GetComponentInChildren<InventorySlotUI>()?.ClearItem();
        }
        
        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].item == null) continue;
            if (lootInvetoryPtr.slots[i].item.notDiscovered) {
                discoverLootIndex = i;
                break;
            }
            InventoryItem item = lootInvetoryPtr.slots[i].item;
            lootInventoryParent.GetChild(i).GetComponentInChildren<InventorySlotUI>().SetItem(item.ItemRef, item.count);
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        discoverLootTimer.SetTime(1f);
        discoverLootTimer.EndAction ??= () => {
            InventoryItem item = lootInvetoryPtr.slots[discoverLootIndex].item;
            
            item.notDiscovered = false;
            lootInventoryParent.GetChild(discoverLootIndex).GetComponentInChildren<InventorySlotUI>().SetItem(item.ItemRef, item.count);
            
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
    
    private void SetStashValue(int value) {
        stashValue = value;
        stashValueText.text = stashValue.ToString();
    }
    
}
