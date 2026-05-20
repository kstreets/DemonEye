using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Game {
    
    public struct InteractionData {
        public float timeSpentSummoningPortal;
        
        public Sequence discoverSlotsSequence;
        public Tween searchCirclePopInTween;
        public Timer discoverItemTimer;
        public int discoverItemIndex;
    }
    
    private void CancelPortalSummoning() {
        curRaid.temp.interactionData.timeSpentSummoningPortal = 0f;
    }
    
    private bool InteractingWithPortal() {
        return curRaid.temp.interactionData.timeSpentSummoningPortal > Mathf.Epsilon;
    }
    
    private void CheckForInteractions() { 
        DisableInteractionPrompt();
        
        Vector2 checkCenter = player.position + new Vector3(0f, 0.05f, 0f);
        List<Collider2D> cols = Physics.OverlapCircle(checkCenter, 0.1f, Masks.ItemMask);
        
        foreach (Collider2D col in cols) {
            if (col.CompareTag(Tags.Pickup)) {
                ItemDrop itemDrop = col.GetComponent<ItemDrop>();
                Item dropItemRef = itemDrop.ItemInstance.ItemRef;
                
                Color itemColor = Styles.instance.GetColorForRarity(dropItemRef.GetRarity());
                string details = ColorText($"{dropItemRef.displayName} x{itemDrop.ItemInstance.count}", itemColor);
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), details);
                
                if (input.interact.WasPressedThisFrame()) {
                    InventoryAddResult result = TryAddItemToInventory(inventories.player, itemDrop.ItemInstance);
                    if (result.type == InventoryAddResult.ResultType.Success) {
                        Entity droppedEntity = entities.lookup[itemDrop.gameObject];
                        PickupDroppedItem(droppedEntity); 
                        itemDrop.circleCollider.enabled = false;
                    }
                    else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
                        itemDrop.ItemInstance.count -= result.addedCount;
                    }
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Body");
                if (input.interact.WasPressedThisFrame()) {
                    inventories.lootPtr.slots = curRaid.deadBodySlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }
            
            if (col.CompareTag(Tags.Bush)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Bush");
                if (input.interact.WasPressedThisFrame()) {
                    inventories.lootPtr.slots = curRaid.bushSlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.Altar)) {
                int soulsPrice = curRaid.map.altarSoulPrice;
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), $"{soulsPrice} Souls");
                if (input.interact.WasPressedThisFrame() && player.soulCurrency >= soulsPrice) {
                    player.soulCurrency -= soulsPrice;
                    Item dropItem = GetItemFromDropPool(dropPools.eyeUpgrades);
                    Entity item = SpawnItemAsEntity(dropItem, 1, OffsetY(col.transform.position, 0.2f), Quaternion.identity);
                    item.spriteRenderer.sortingOrder = 1;
                    col.enabled = false;
                }
            }
            
            if (col.CompareTag(Tags.Chest)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Open Chest");
                if (input.interact.WasPressedThisFrame()) {
                    Item dropItem = GetItemFromDropPool(dropPools.chests);
                    Entity item = SpawnItemAsEntity(dropItem, 1, OffsetY(col.transform.position, 0.1f), Quaternion.identity);
                    Vector3 endPos = item.position + RotationVector(Random.Range(0f, 360f), 0.18f, 0.25f);
                    AddBounceEffect(item, endPos, 0.6f);
                    col.enabled = false;
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                ExitPortal portal = GetExitPortalFromTransform(col.transform);
                ref float timeSpentSummoningPortal = ref curRaid.temp.interactionData.timeSpentSummoningPortal;
                
                if (!portal.hasBeenSummoned && timeSpentSummoningPortal < config.gameplay.portalSummonTime) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Summon Exit Portal");
                    if (input.interact.IsPressed()) {
                        timeSpentSummoningPortal += Time.deltaTime;
                        if (timeSpentSummoningPortal >= config.gameplay.portalSummonTime) {
                            StartSummoningExitPortal(col.transform);
                            timeSpentSummoningPortal = 0f;
                        }
                    }
                    else {
                        timeSpentSummoningPortal = 0f;
                    }
                }
                
                if (portal.canTake) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Take Exit Portal");
                    if (input.interact.WasPressedThisFrame()) {
                        exitPortalTakenByPlayer = portal;
                        exitPortalTakenByPlayer.closingCountdownSequence.Stop();
                        states.gameStateMachine.SetStateIfNotCurrent(
                            curRaid.state == RaidState.PostFinalWave ? 
                            states.winExit : states.earlyExit
                        );
                        customQuestEvent?.Invoke("FirstExtract");
                    }
                }
            }
        }
    }

    private void PickupDroppedItem(Entity droppedEntity) {
        Vector3 playerPickupTarget = new(0f, 0.07f, 0f);
        
        droppedEntity.GetEffect(Entity.EffectsIndicies.Bounce).Stop();
        droppedEntity.trans.SetParent(player.trans, true);
        
        TweenSettings horizontalSettings = new() {
            duration = 0.15f,
            ease = Ease.InQuart,
        };
        
        TweenSettings verticalSettings = new() {
            duration = 0.09f,
            ease = Ease.InQuart,
        };
        
        TweenSettings itemScaleSettings = new() {
            startDelay = 0.03f,
            duration = 0.15f,
            ease = Ease.InCubic,
        };
        
        ShakeSettings playerScaleSettings = new() {
            startDelay = 0.1f,
            duration = 0.08f,
            strength = Vector2.one * 0.15f,
            frequency = 5f,
        };
        
        Tween.LocalPositionX(droppedEntity.trans, playerPickupTarget.x, horizontalSettings)
        .Group(Tween.LocalPositionY(droppedEntity.trans, playerPickupTarget.y, verticalSettings))
        .Group(Tween.Scale(droppedEntity.trans, 0f,itemScaleSettings))
        .Group(Tween.PunchScale(player.trans, playerScaleSettings))
        .OnComplete(() => DestroyEntity(droppedEntity));
    }
    
    private float DiscoverSlotTime => config.gameplay.discoverSlotTime * GetAbsoluteStat(PlayerStat.LootingSpeed);
    private float DiscoverItemTime => config.gameplay.discoverItemTime * GetAbsoluteStat(PlayerStat.LootingSpeed);
    
    private void OpenLootInventory() {
        if (LootInventoryIsOpen) return;
        
        ref int discoverItemIndex = ref curRaid.temp.interactionData.discoverItemIndex;
        ref Sequence discoverSlotsSequence = ref curRaid.temp.interactionData.discoverSlotsSequence;
        ref Timer discoverItemTimer = ref curRaid.temp.interactionData.discoverItemTimer;
        
        discoverItemIndex = -1;
        ui.lootInventoryPanel.gameObject.SetActive(true);
        
        foreach (InventorySlot slot in inventories.lootPtr.slots) {
            slot.ui.ClearItem();
            slot.ui.MakeSlotActive();
        }

        for (int i = 0; i < inventories.lootPtr.slots.Length; i++) {
            if (inventories.lootPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = inventories.lootPtr.slots[i].ui;
            
            if (inventories.lootPtr.slots[i].itemInstance.notDiscovered) {
                discoverItemIndex = discoverItemIndex == -1 ? i : discoverItemIndex;
            }
            else {
                ItemInstance itemInstance = inventories.lootPtr.slots[i].itemInstance;
                slotUI.SetItem(itemInstance.ItemRef, itemInstance.count);
            }
        }

        bool alreadyDiscoveredAll = discoverItemIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        ui.lootSearchingText.SetActive(true);

        discoverSlotsSequence = Sequence.Create();
        
        for (int i = 0; i < inventories.lootPtr.slots.Length; i++) {
            if (inventories.lootPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = inventories.lootPtr.slots[i].ui;
            
            if (inventories.lootPtr.slots[i].itemInstance.notDiscovered) {
                discoverSlotsSequence.Chain(Tween.PunchScale(slotUI.rectTransform, Vector3.one * 2f, 0.1f, 2f, startDelay: DiscoverSlotTime * i));
                discoverSlotsSequence.ChainCallback(slotUI, (target) => target.MakeSlotInactive());
            }
        }

        discoverSlotsSequence.ChainDelay(0.15f);

        discoverSlotsSequence.ChainCallback(target: this, static (target) => {
            ref int discoverItemIndex = ref target.curRaid.temp.interactionData.discoverItemIndex;
            ref Timer discoverItemTimer = ref target.curRaid.temp.interactionData.discoverItemTimer;
            
            InventorySlot slot = target.inventories.lootPtr.slots[discoverItemIndex];
            if (slot.itemInstance != null) {
                target.AnimateSlotSearch(slot.ui);
                discoverItemTimer.SetTime(target.DiscoverItemTime);
            }
        });
        
        discoverItemTimer.EndAction ??= static () => {
            Inventory lootInventoryPtr = gameInstance.inventories.lootPtr;
            ref Timer discoverItemTimer = ref gameInstance.curRaid.temp.interactionData.discoverItemTimer;
            ref int discoverItemIndex = ref gameInstance.curRaid.temp.interactionData.discoverItemIndex; 
            
            ItemInstance itemInstance = lootInventoryPtr.slots[discoverItemIndex].itemInstance;
            itemInstance.notDiscovered = false;
            
            InventorySlotUI slotUI = lootInventoryPtr.slots[discoverItemIndex].ui;
            slotUI.MakeSlotActive();
            slotUI.StopSlotSearching();
            slotUI.SetItem(itemInstance.ItemRef, itemInstance.count);

            Tween.PunchScale(slotUI.itemUI.image.rectTransform, Vector3.one * 4f, 0.1f, 2f); 
            
            discoverItemIndex++;
            
            if (discoverItemIndex < lootInventoryPtr.slots.Length && lootInventoryPtr.slots[discoverItemIndex].itemInstance != null) {
                slotUI = lootInventoryPtr.slots[discoverItemIndex].ui;
                gameInstance.AnimateSlotSearch(slotUI);
                discoverItemTimer.SetTime(gameInstance.DiscoverItemTime);
            }
            else {
                gameInstance.ui.lootSearchingText.SetActive(false);
            }
        };
    }

    private void AnimateSlotSearch(InventorySlotUI slotUI) {
        slotUI.MakeSlotSearching();
        curRaid.temp.interactionData.searchCirclePopInTween = Tween.Scale(slotUI.searchingCircle.transform, Vector3.one * 0.2f, Vector3.one * 1f, 0.25f, Ease.OutElastic); 
    }

    private void CloseLootInventory() {
        ui.lootSearchingText.SetActive(false);
        ui.lootInventoryPanel.gameObject.SetActive(false);
        curRaid.temp.interactionData.discoverItemTimer.Stop();
        curRaid.temp.interactionData.discoverSlotsSequence.Stop();
        curRaid.temp.interactionData.searchCirclePopInTween.Stop();
        
        // Reset all tweening properties because the animations might have stopped while playing 
        foreach (InventorySlot slot in inventories.lootPtr.slots) {
            slot.ui.rectTransform.localScale = Vector3.one;
            slot.ui.StopSlotSearching();
        }
    }
    
    private void CheckForHotBarInteractions() {
        Item itemToConsume = null;
        int playerInventorySlotIndex = playerEquipmentSize;
        
        foreach (InputAction action in hotBar.quickUseActions) {
            if (action.WasPressedThisFrame()) {
                itemToConsume = inventories.player.slots[playerInventorySlotIndex].itemInstance?.ItemRef;
                break;
            }
            playerInventorySlotIndex++;
        }

        if (itemToConsume) {
            HavePlayerConsumeItem(inventories.player, playerInventorySlotIndex);
        }
    }
    
}
