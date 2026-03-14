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

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public bool traderOwned;
        [NonSerialized] public int traderSlotIndex;

        public Item ItemRef {
            get {
                if (gameInstance.eyeInstanceFromItemId.ContainsKey(itemOrInstanceUuid)) {
                    return gameInstance.demonEyeItem;
                }
                
                UuidScriptableObject uuidObject = gameInstance.resourceLookup[itemOrInstanceUuid];
                return uuidObject switch {
                    Item item => item,
                    Augment augment => augment.augmentedModifierItem,
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
        
        public bool TryGetUuidObject(out UuidScriptableObject uuidObject) { 
            // Demon Eye instances will not be in the resourceLookup
            return gameInstance.resourceLookup.TryGetValue(itemOrInstanceUuid, out uuidObject);
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
    
    [NonSerialized] public Inventory playerInventory;
    [NonSerialized] public Inventory stashInventory;
    [NonSerialized] private Inventory crucibleInventory;
    [NonSerialized] private Inventory transactionInventory;
    [NonSerialized] private Inventory traderInventory;
    [NonSerialized] private Inventory lootInvetoryPtr;
    [NonSerialized] private InventorySlotUI[] lootInventorySlotUis;
    [NonSerialized] private List<Inventory> allInventories = new();
    
    private const int playerPocketSize = 10;
    private const int playerQuickUseSize = 4;
    private const int playerEquipmentSize = 3;
    private int NakedPlayerInventorySize => playerPocketSize + playerQuickUseSize + playerEquipmentSize;

    private const int traderInventoryColCount = 6;
    private const int traderInventoryRowCount = 5;

    private int stashValue;

    private bool InventoryIsOpen => playerPanel.gameObject.activeInHierarchy;
    private bool LootInventoryIsOpen => lootInventoryPanel.gameObject.activeInHierarchy;

    private bool OnCharacterTab => characterTabButton.image.sprite == tabSelectedSprite;
    private bool OnEyeForgeTab => eyeForgeTabButton.image.sprite == tabSelectedSprite;
    private bool OnTradingTab => traderTabButton.image.sprite == tabSelectedSprite;
    
    private void InitInventories() {
        const int maxBackpackSize = 30;
        SpawnUiSlots(playerPassiveParent, playerQuickUseSize);
        SpawnUiSlots(playerPocketParent, playerPocketSize + maxBackpackSize);
        playerInventory = CreateInventory(playerInventoryParent, NakedPlayerInventorySize);
        LoadInventory(playerInventory);

        InventorySlotUI[] quickUseSlots = playerPassiveParent.GetComponentsInChildren<InventorySlotUI>();
        foreach (InventorySlotUI slotUI in quickUseSlots) {
            slotUI.onlyAcceptedItemType = quickUseType;
        }
        
        int stashInventorySize = 40;
        SpawnUiSlots(stashInventoryParent, stashInventorySize);
        stashInventory = CreateInventory(stashInventoryParent, stashInventorySize);
        LoadInventory(stashInventory);
       
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(lootInventoryParent, cachedLootInventorySize); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventorySize);
        lootInventorySlotUis = lootInventoryParent.GetComponentsInChildren<InventorySlotUI>(true);

        const int traderInventorySize = traderInventoryRowCount * traderInventoryColCount;
        SpawnUiSlots(traderInventoryParent, traderInventorySize);
        traderInventory = CreateInventory(traderInventoryParent, traderInventorySize);
        LoadInventory(traderInventory);
        
        const int transactionInventorySize = 25;
        SpawnUiSlots(traderTransactionInventoryParent, transactionInventorySize);
        transactionInventory = CreateInventory(traderTransactionInventoryParent, transactionInventorySize);

        const int maxCrucibleInventorySize = 6;
        const int startingCrucibleInventorySize = 2;
        SpawnUiSlots(crucibleParent, maxCrucibleInventorySize, eyeForgeSlotPrefab);
        crucibleInventory = CreateInventory(crucibleParent, startingCrucibleInventorySize + player.crucibleLevel);
        SetupEyeCrucibleInventorySlots();
        LoadInventory(crucibleInventory);
    }
    
    private void SetupEyeCrucibleInventorySlots() {
        int inventoryLength = crucibleInventory.slots.Length;
        for (int i = 0; i < inventoryLength; i++) {
            InventorySlotUI slotUI = crucibleInventory.slots[i].ui;
            slotUI.disallowItemStacking = true;
            slotUI.onlyAcceptedItemType = i == 0 ? eyeType : eyeModifierType;

            if (i == 0) {
                slotUI.gameObject.transform.position = crucibleParent.position;
                continue;
            }
            
            float deg = 360f / (inventoryLength - 1) * (i - 1);
            Vector3 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 180f;
            slotUI.gameObject.transform.position = crucibleParent.position + spawnDir;
        }
    }
    
    private void SaveInventory(Inventory inventory) {
        cachedInventoryForSaving.Clear();
        foreach (InventorySlot slot in inventory.slots) {
            cachedInventoryForSaving.Add(slot.itemInstance); 
        }
        SaveToFile(GetInventorySavePath(inventory), cachedInventoryForSaving);
    }

    private void LoadInventory(Inventory inventory) {
        List<ItemInstance> itemInstances = LoadFromFile<List<ItemInstance>>(GetInventorySavePath(inventory));
        if (itemInstances == null) return;

        if (inventory == playerInventory && itemInstances.Count != inventory.slots.Length) {
            ChangeInventorySize(inventory, itemInstances.Count);
        }
        
        // Items can be null because we save all inventory slots, including empty ones
        foreach (ItemInstance itemInstance in itemInstances) {
            if (itemInstance == null) continue;
            if (itemInstance.isDemonEye) {
                BuildAndRegisterEye(itemInstance);
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
        var slots = new InventorySlot[lootInvetoryPtr.slots.Length];
        
        for (int j = 0; j < lootInvetoryPtr.slots.Length; j++) {
            ItemInstance itemInstance = null;
            if (inventoryItems.IndexInRange(j)) {
                itemInstance = inventoryItems[j];
            }
            slots[j] = new() {
                itemInstance = itemInstance,
                ui = lootInventorySlotUis[j],
            };
        }
        
        return slots;
    }

    private void UpdateInventory() {
        CheckForEquipmentChange();
        
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
                HideInventoryItemPopup();
                return;
            }
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
        if (itemDescPopup.gameObject.activeInHierarchy) return;
        
        InventorySlot hoveredSlot = info.inventory.slots[info.slotIndex];
        
        itemDescPopup.gameObject.SetActive(true);
        itemDescPopup.Set(hoveredSlot.itemInstance);
        TweenPopUp(itemDescPopup.rectTransform);
        
        // Fit popup size to text elements
        FitPopupSize(itemDescPopup.rectTransform, itemDescPopup.tagsParent.rect, itemDescPopup.nameText.rectTransform.rect, itemDescPopup.descText.rectTransform.rect);

        // Set popup position
        Vector2 hoveredSlotCenter = hoveredSlot.ui.rectTransform.WorldRect().center;
        float halfPopupWidth = itemDescPopup.rectTransform.rect.width / 2f;
        Vector2 popupOffset = new(45 + halfPopupWidth, 40);
        if (hoveredSlotCenter.x < ScreenCenter.x) {
            itemDescPopup.transform.position = hoveredSlotCenter + popupOffset;
        }
        else {
            itemDescPopup.transform.position = hoveredSlotCenter + new Vector2(-popupOffset.x, popupOffset.y);
        }

        // Add mechanic desctiption if necessary
        if (hoveredSlot.itemInstance.ItemRef.type == eyeModifierType) {
            ModifierItem modifierItem = (ModifierItem)hoveredSlot.itemInstance.ItemRef;
            if (modifierItem.relativeMechanicDesc) {
                mechanicDescPopup.gameObject.SetActive(true);
                mechanicDescPopup.nameText.text = modifierItem.relativeMechanicDesc.displayName;
                mechanicDescPopup.descText.text = modifierItem.relativeMechanicDesc.description;
                mechanicDescPopup.transform.position = itemDescPopup.rectTransform.WorldRect().min;
                
                mechanicDescPopup.nameFitter.ForceRecalculate();
                mechanicDescPopup.descFitter.ForceRecalculate();
                FitPopupSize(mechanicDescPopup.rectTransform, mechanicDescPopup.nameText.rectTransform.rect, mechanicDescPopup.descText.rectTransform.rect);
                
                TweenPopUp(mechanicDescPopup.rectTransform);
            } 
        }
    }

    private void HideInventoryItemPopup() {
        mechanicDescPopup.gameObject.SetActive(false);
        
        itemDescPopup.nameText.text = string.Empty;
        itemDescPopup.descText.text = string.Empty;
        itemDescPopup.gameObject.SetActive(false);
        
        mechanicDescPopup.nameText.text = string.Empty;
        mechanicDescPopup.descText.text = string.Empty;
        mechanicDescPopup.gameObject.SetActive(false);
    }
    
    private void CheckToMoveItem(InventoryHoverInfo invHoverInfo) {
        if (!moveStackInputAction.WasPressedThisFrame()) return;

        Inventory hoveredInventory = invHoverInfo.inventory;
        if (hoveredInventory == null) return;
        
        if (!TryGetItemFromHoverInfo(invHoverInfo, out ItemInstance hoveredItem)) return;
        if (NotAllowedToMoveOrPickupItem(invHoverInfo)) return;
        if (ClickedOnEquipedBackpackWithItems(invHoverInfo.inventory, invHoverInfo.slotIndex)) return;

        MoveItemOption moveOption = MoveItemOption.FullStack;
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
                bool hoveredItemIsDemonEye = hoveredItem.ItemRef.type == demonEyeType;
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
            // if (transactionState == TransactionState.Buying) {
            //     if (hoveredInventory == traderInventory) {
            //         destinationInventory = transactionInventory;
            //         moveOption = MoveItemOption.Single;
            //     }
            //     else if (hoveredInventory == transactionInventory) {
            //         destinationInventory = traderInventory;
            //     }
            // }
            /*else*/ if (transactionState == TransactionState.Selling) {
                if (hoveredInventory == stashInventory) {
                    destinationInventory = transactionInventory;
                }
                else if (hoveredInventory == transactionInventory) {
                    destinationInventory = stashInventory;
                }
            }
            // else {
            //     if (hoveredInventory == traderInventory) {
            //         destinationInventory = transactionInventory;
            //         moveOption = MoveItemOption.Single;
            //     }
            //     else if (hoveredInventory == stashInventory) {
            //         destinationInventory = transactionInventory;
            //     }
            // }
        }

        if (destinationInventory == null) return;
        
        MoveItemBetweenInventories(hoveredInventory, destinationInventory, invHoverInfo.slotIndex, moveOption);
    }

    private void CheckToConsumeItem(InventoryHoverInfo invHoverInfo) {
        if (!useItemInputAction.WasPressedThisFrame()) return;
        if (!TryGetItemFromHoverInfo(invHoverInfo, out ItemInstance hoveredItem)) return;
        if (hoveredItem.ItemRef.type != quickUseType) return;
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
        return hoveredInventory.slots[hoveredSlot].ui.itemUI.IsGrayedOut;
    }
    
    private bool NotAllowedToMoveOrPickupItem(InventoryHoverInfo info) {
        if (IsHoveredItemGrayedOut(info)) {
            return true;
        }
        // Because we wait to reduce the consumed inventory item count until the consuming tween has finished,
        // we can't drag it from its slot, why not just use decrement a reference to InventoryItem? Because when moving it 
        // between inventories the item reference may become stale.
        if (playerConsumingTween.isAlive && info.inventory == consumingInventory && info.slotIndex == consumingSlotIndex) {
            return true;
        }
        if (info.inventory == crucibleInventory && PlayingForgeAnimation) {
            return true;
        }
        return false;
    }
    
    private void SpawnUiSlots(RectTransform parent, int numSlots, GameObject slotPrefab = null) {
        for (int i = 0; i < numSlots; i++) {
            Instantiate(slotPrefab ? slotPrefab : inventorySlotPrefab, Vector3.zero, Quaternion.identity, parent);
        }
    }
    
    private Inventory CreateInventory(RectTransform uiParent, int slotCount) {
        Inventory inventory = new() {
            parent = uiParent,
            slots = new InventorySlot[slotCount],
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

    private bool IsEquipmentSlot(Inventory inventory, int slotIndex) {
        return inventory == playerInventory && slotIndex < playerEquipmentSize;
    }
    
    private bool EquipedBackpackHasItems() {
        int startingIndex = NakedPlayerInventorySize;
        for (int i = startingIndex; i < playerInventory.slots.Length; i++) {
            if (playerInventory.slots[i].itemInstance != null) {
                return true;
            }
        }
        return false;
    }
    
    private bool ClickedOnEquipedBackpackWithItems(Inventory inventory, int slotIndex) {
        if (inventory.slots[slotIndex].itemInstance.ItemRef.type != backpackType) {
            return false;
        }
        return IsEquipmentSlot(inventory, slotIndex) && EquipedBackpackHasItems();
    }
    
    private ItemInstance prevEquippedEyeItemInstance;
    private ItemInstance prevEquippedBackpackItemInstance;
    private ItemInstance prevEquippedTrinketItemInstance;
    
    private void CheckForEquipmentChange() {
        ItemInstance curEyeItemInstance = playerInventory.slots[0].itemInstance;
        ItemInstance curBackpackItemInstance = playerInventory.slots[1].itemInstance;

        if (prevEquippedEyeItemInstance != curEyeItemInstance) {
            prevEquippedEyeItemInstance = curEyeItemInstance;
            equipedEye = curEyeItemInstance == null ? emptyDemonEye : eyeInstanceFromItemId[curEyeItemInstance.itemOrInstanceUuid];
            if (equipedEye != emptyDemonEye) {
                customQuestEvent?.Invoke("FirstDemonEyeEquiped");
            }
        }
        
        if (prevEquippedBackpackItemInstance != curBackpackItemInstance) {
            prevEquippedBackpackItemInstance = curBackpackItemInstance;
            if (curBackpackItemInstance != null) {
                Assert.IsTrue(curBackpackItemInstance.ItemRef is BackpackItem);
                int backpackSize = (curBackpackItemInstance.ItemRef as BackpackItem).additionalStorageSlots;
                ChangeInventorySize(playerInventory, NakedPlayerInventorySize + backpackSize);
            }
            else {
                ChangeInventorySize(playerInventory, NakedPlayerInventorySize);
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
        
        foreach (Inventory inventory in allInventories) {
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
        bool pickupInputUsed = selectItemInputAction.WasPressedThisFrame() || splitStackInputAction.WasPressedThisFrame();
        bool placeInputUsed = selectItemInputAction.WasPressedThisFrame() || placeSingleItemInputAction.WasPressedThisFrame();
        
        if (!pickupInputUsed && !placeInputUsed) {
            return IsDraggingItem;
        }

        // We don't allow trader items to be picked up
        if (hoverInfo.inventory == traderInventory && !IsDraggingItem) {
            if (TryGetItemFromHoverInfo(hoverInfo, out ItemInstance item)) {
                SetTradingItem(item); 
            }
            return IsDraggingItem;
        }

        // If we are putting trader items back, then we also don't want to pick up the items
        if (!IsDraggingItem && hoverInfo.inventory == transactionInventory && transactionState == TransactionState.Buying) {
            MoveItemBetweenInventories(transactionInventory, traderInventory, hoverInfo.slotIndex, MoveItemOption.Single);
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

            bool splittingStack = splitStackInputAction.WasPressedThisFrame() && item.count > 1;
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
            dragAndDropItemUI.gameObject.SetActive(true);
            dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
            TweenItemMove(dragAndDropItemUI);
        }

        bool placingItem = !pickingUpItem;
        if (placingItem && placeInputUsed) {
            bool droppingItemInHideout = hoverInfo.inventory == null && InHideout;
            bool tryingToPlaceItemToSellWhileBuying = hoverInfo.inventory == transactionInventory && transactionState == TransactionState.Buying;
            bool tryingToPlaceInTraderInventory = hoverInfo.inventory == traderInventory;
            
            if (droppingItemInHideout || tryingToPlaceItemToSellWhileBuying || tryingToPlaceInTraderInventory) {
                TryAddItemToInventory(startDragInfo.inventory, dragItemInstance, startDragInfo.slotIndex);
                EndDragAndDropItem();
                return IsDraggingItem;
            }

            bool droppingItemInRaid = hoverInfo.inventory == null && InRaid;
            if (droppingItemInRaid) {
                bool droppingEntireStack = selectItemInputAction.WasPressedThisFrame();
                if (droppingEntireStack) {
                    DropItemFromInventory(dragItemInstance);
                    dragItemInstance.count = 0;
                }
                else {
                    DropItemFromInventory(dragItemInstance, 1);
                    dragItemInstance.count--;
                    dragAndDropItemUI.UpdateCount(dragItemInstance.count);
                }

                if (dragItemInstance.count <= 0) {
                    EndDragAndDropItem();
                }
                return IsDraggingItem;
            }

            bool swappingItems = false;
            if (TryGetItemFromHoverInfo(hoverInfo, out ItemInstance swapItem)) {
                bool itemsCanSwap = swapItem != dragItemInstance || (swapItem.IsFullStack || dragItemInstance.IsFullStack);
                swappingItems = itemsCanSwap && selectItemInputAction.WasPressedThisFrame();
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
                dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
                TweenItemMove(dragAndDropItemUI);

                return IsDraggingItem;
            }

            bool placingSingleItemFromStack = placeSingleItemInputAction.WasPressedThisFrame();
            if (placingSingleItemFromStack) {
                InventoryAddResult result = TryAddItemToInventory(hoverInfo.inventory, dragItemInstance.ItemRef, 1, hoverInfo.slotIndex);

                dragItemInstance.count -= result.addedCount;
                if (dragItemInstance.count <= 0) {
                    EndDragAndDropItem();
                }
                else {
                    dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
                    TweenItemMove(dragAndDropItemUI);
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
                    dragAndDropItemUI.SetItem(dragItemInstance.ItemRef, dragItemInstance.count);
                    TweenItemMove(dragAndDropItemUI);
                }
            }
        }

        return IsDraggingItem;
    }

    private void DropItemFromInventory(ItemInstance itemInstance, int count = -1) {
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dropDir = (mouseWorldPos - player.position.ToVector2()).normalized;
        
        int dropCount = count == -1 ? itemInstance.count : count;
        
        Vector3 endPos = player.position + RandomizeVectorAngle(dropDir, 20f) * 0.2f;
        Entity itemDropEntity = SpawnItemAsEntity(itemInstance.ItemRef, dropCount, player.position, Quaternion.identity);
        
        AddBounceEffect(itemDropEntity, endPos, 0.8f);
    }

    private void UpdateDragAndDropItemToCursor() {
        if (dragItemInstance == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        dragAndDropItemUI.GetComponent<RectTransform>().position = mousePos;
    }
    
    private void EndDragAndDropItem() {
        if (dragItemInstance == null) return;
        dragItemInstance = null;
        dragAndDropItemUI.ClearItem();
        dragAndDropItemUI.gameObject.SetActive(false);
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
        
        bool allowInfiniteStacking = inventory == traderInventory;
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
        if (fromInventory == transactionInventory && toInventory == traderInventory) {
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
        if (inventory == playerInventory) {
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
        int removedCount = RemoveNumberOfItemsFromInventory(stashInventory, item, count);
        if (removedCount != count) {
            int additionalRemoveCount = count - removedCount;
            removedCount += RemoveNumberOfItemsFromInventory(playerInventory, item, additionalRemoveCount);
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
    private bool ReduceItemCountInInventory(Inventory inventory, int slotIndex, int reduction = 1) {
        var item = GetInventoryItem(inventory, slotIndex);
        item.count -= reduction;
        if (item.count <= 0) {
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
        foreach (InventorySlot slot in inventory.slots) {
            slot.ui.ClearItem();
        }

        for (int i = 0; i < inventory.slots.Length; i++) {
            ItemInstance itemInstance = inventory.slots[i].itemInstance;
            if (itemInstance == null || itemInstance.notDiscovered) continue;
            inventory.slots[i].ui.SetItem(itemInstance.ItemRef, itemInstance.count);
        }
    }
    
    private void UpdateGraySlots() {
        if (OnEyeForgeTab) {
            foreach (InventorySlot slot in stashInventory.slots) {
                if (slot.itemInstance == null) continue;
                Item item = slot.itemInstance.ItemRef;
                if (item.type != eyeType && item.type != eyeModifierType) {
                    slot.ui.itemUI.ToggleGray();
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

    public int GetOwnedCountOfItem(Item item) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(stashInventory, item);
        itemCount += GetItemCountInInventory(playerInventory, item);
        return itemCount;
    }

    private bool MeetsSingleUpgradeRequirement(UpgradePath.Requirement req) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(stashInventory, req.item);
        itemCount += GetItemCountInInventory(playerInventory, req.item);
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
                    value += slot.itemInstance.ItemRef.type == demonEyeType ? GetDemonEyeSellPrice(slot.itemInstance) : slot.itemInstance.ItemRef.buyPrice * slot.itemInstance.count;
                    break;
                case InventoryValueType.Sell:
                    value += slot.itemInstance.ItemRef.type == demonEyeType ? GetDemonEyeSellPrice(slot.itemInstance) : slot.itemInstance.ItemRef.GetSellPrice() * slot.itemInstance.count;
                    break;
            }
        }
        return value;
    }

    private void OpenPlayerInventory() {
        playerPanel.gameObject.SetActive(true);
        Cursor.visible = true;
    }

    private void ClosePlayerInventory() {
        playerPanel.gameObject.SetActive(false);
        Cursor.visible = false;
        EndDragAndDropItem();
    }

    private Sequence searchSequence;
    private Tween searchCirclePopInTween;
    
    private Timer discoverLootTimer;
    private int discoverLootIndex;
    
    private void OpenLootInventory() {
        if (lootInventoryPanel.gameObject.activeInHierarchy) return;
        
        discoverLootIndex = -1;
        lootInventoryPanel.gameObject.SetActive(true);
        
        foreach (InventorySlot slot in lootInvetoryPtr.slots) {
            slot.ui.ClearItem();
            slot.ui.MakeSlotActive();
        }

        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = lootInvetoryPtr.slots[i].ui;
            
            if (lootInvetoryPtr.slots[i].itemInstance.notDiscovered) {
                discoverLootIndex = discoverLootIndex == -1 ? i : discoverLootIndex;
            }
            else {
                ItemInstance itemInstance = lootInvetoryPtr.slots[i].itemInstance;
                slotUI.SetItem(itemInstance.ItemRef, itemInstance.count);
            }
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        lootSearchingText.SetActive(true);

        searchSequence = Sequence.Create();
        
        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = lootInvetoryPtr.slots[i].ui;
            
            if (lootInvetoryPtr.slots[i].itemInstance.notDiscovered) {
                searchSequence.Chain(Tween.PunchScale(slotUI.rectTransform, Vector3.one * 2f, 0.1f, 2f, startDelay: 0.01f * i));
                searchSequence.ChainCallback(slotUI, (target) => target.MakeSlotInactive());
            }
        }

        searchSequence.ChainDelay(0.15f);

        searchSequence.ChainCallback(target: this, (target) => {
            InventorySlot slot = target.lootInvetoryPtr.slots[target.discoverLootIndex];
            if (slot.itemInstance != null) {
                target.AnimateSlotSearch(slot.ui);
                target.discoverLootTimer.SetTime(1f);
            }
        });
        
        discoverLootTimer.EndAction ??= () => {
            ItemInstance itemInstance = lootInvetoryPtr.slots[discoverLootIndex].itemInstance;
            itemInstance.notDiscovered = false;
            
            InventorySlotUI slotUI = lootInvetoryPtr.slots[discoverLootIndex].ui;
            slotUI.MakeSlotActive();
            slotUI.StopSlotSearching();
            slotUI.SetItem(itemInstance.ItemRef, itemInstance.count);

            Tween.PunchScale(slotUI.itemUI.image.rectTransform, Vector3.one * 4f, 0.1f, 2f); 
            
            discoverLootIndex++;
            
            if (discoverLootIndex < lootInvetoryPtr.slots.Length && lootInvetoryPtr.slots[discoverLootIndex].itemInstance != null) {
                slotUI = lootInvetoryPtr.slots[discoverLootIndex].ui;
                AnimateSlotSearch(slotUI);
                discoverLootTimer.SetTime(1f);
            }
            else {
                lootSearchingText.SetActive(false);
            }
        };
    }

    private void AnimateSlotSearch(InventorySlotUI slotUI) {
        slotUI.MakeSlotSearching();
        searchCirclePopInTween = Tween.Scale(slotUI.searchingCircle.transform, Vector3.one * 0.2f, Vector3.one * 1f, 0.25f, Ease.OutElastic); 
    }

    private void CloseLootInventory() {
        lootSearchingText.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
        discoverLootTimer.Stop();
        searchSequence.Stop();
        searchCirclePopInTween.Stop();
        
        // Reset all tweening properties because the animations might have stopped while playing 
        foreach (InventorySlot slot in lootInvetoryPtr.slots) {
            slot.ui.rectTransform.localScale = Vector3.one;
            slot.ui.StopSlotSearching();
        }
    }
    
}
