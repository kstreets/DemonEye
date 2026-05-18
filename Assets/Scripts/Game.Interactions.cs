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
        gameData.curRaid.temp.interactionData.timeSpentSummoningPortal = 0f;
    }
    
    private bool InteractingWithPortal() {
        return gameData.curRaid.temp.interactionData.timeSpentSummoningPortal > Mathf.Epsilon;
    }
    
    private void CheckForInteractions() { 
        DisableInteractionPrompt();
        
        Vector2 checkCenter = player.position + new Vector3(0f, 0.05f, 0f);
        List<Collider2D> cols = Physics.OverlapCircle(checkCenter, 0.1f, Masks.ItemMask);
        
        foreach (Collider2D col in cols) {
            if (col.CompareTag(Tags.Pickup)) {
                ItemDrop itemDrop = col.GetComponent<ItemDrop>();
                Item dropItemRef = itemDrop.ItemInstance.ItemRef;
                
                Color itemColor = styles.GetColorForRarity(dropItemRef.GetRarity());
                string details = ColorText($"{dropItemRef.displayName} x{itemDrop.ItemInstance.count}", itemColor);
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), details);
                
                if (gameData.input.interactInputAction.WasPressedThisFrame()) {
                    InventoryAddResult result = TryAddItemToInventory(gameData.inventories.player, itemDrop.ItemInstance);
                    if (result.type == InventoryAddResult.ResultType.Success) {
                        Entity droppedEntity = gameData.entities.lookup[itemDrop.gameObject];
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
                if (gameData.input.interactInputAction.WasPressedThisFrame()) {
                    gameData.inventories.lootPtr.slots = gameData.curRaid.deadBodySlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }
            
            if (col.CompareTag(Tags.Bush)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Bush");
                if (gameData.input.interactInputAction.WasPressedThisFrame()) {
                    gameData.inventories.lootPtr.slots = gameData.curRaid.bushSlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.Altar)) {
                int soulsPrice = gameData.curRaid.map.altarSoulPrice;
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), $"{soulsPrice} Souls");
                if (gameData.input.interactInputAction.WasPressedThisFrame() && player.soulCurrency >= soulsPrice) {
                    player.soulCurrency -= soulsPrice;
                    Item dropItem = GetItemFromDropPool(eyeUpgradesDropPool);
                    Entity item = SpawnItemAsEntity(dropItem, 1, OffsetY(col.transform.position, 0.2f), Quaternion.identity);
                    item.spriteRenderer.sortingOrder = 1;
                    col.enabled = false;
                }
            }
            
            if (col.CompareTag(Tags.Chest)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Open Chest");
                if (gameData.input.interactInputAction.WasPressedThisFrame()) {
                    Item dropItem = GetItemFromDropPool(chestsDropPool);
                    Entity item = SpawnItemAsEntity(dropItem, 1, OffsetY(col.transform.position, 0.1f), Quaternion.identity);
                    Vector3 endPos = item.position + RotationVector(Random.Range(0f, 360f), 0.18f, 0.25f);
                    AddBounceEffect(item, endPos, 0.6f);
                    col.enabled = false;
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                ExitPortal portal = GetExitPortalFromTransform(col.transform);
                ref float timeSpentSummoningPortal = ref gameData.curRaid.temp.interactionData.timeSpentSummoningPortal;
                
                if (!portal.hasBeenSummoned && timeSpentSummoningPortal < gameplayConfig.portalSummonTime) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Summon Exit Portal");
                    if (gameData.input.interactInputAction.IsPressed()) {
                        timeSpentSummoningPortal += Time.deltaTime;
                        if (timeSpentSummoningPortal >= gameplayConfig.portalSummonTime) {
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
                    if (gameData.input.interactInputAction.WasPressedThisFrame()) {
                        exitPortalTakenByPlayer = portal;
                        exitPortalTakenByPlayer.closingCountdownSequence.Stop();
                        gameData.states.gameStateMachine.SetStateIfNotCurrent(
                            gameData.curRaid.state == RaidState.PostFinalWave ? 
                            gameData.states.winExit : gameData.states.earlyExit
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
    
    private float DiscoverSlotTime => gameplayConfig.discoverSlotTime * GetAbsoluteStat(Player.Stat.LootingSpeed);
    private float DiscoverItemTime => gameplayConfig.discoverItemTime * GetAbsoluteStat(Player.Stat.LootingSpeed);
    
    private void OpenLootInventory() {
        if (LootInventoryIsOpen) return;
        
        ref int discoverItemIndex = ref gameData.curRaid.temp.interactionData.discoverItemIndex;
        ref Sequence discoverSlotsSequence = ref gameData.curRaid.temp.interactionData.discoverSlotsSequence;
        ref Timer discoverItemTimer = ref gameData.curRaid.temp.interactionData.discoverItemTimer;
        
        discoverItemIndex = -1;
        lootInventoryPanel.gameObject.SetActive(true);
        
        foreach (InventorySlot slot in gameData.inventories.lootPtr.slots) {
            slot.ui.ClearItem();
            slot.ui.MakeSlotActive();
        }

        for (int i = 0; i < gameData.inventories.lootPtr.slots.Length; i++) {
            if (gameData.inventories.lootPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = gameData.inventories.lootPtr.slots[i].ui;
            
            if (gameData.inventories.lootPtr.slots[i].itemInstance.notDiscovered) {
                discoverItemIndex = discoverItemIndex == -1 ? i : discoverItemIndex;
            }
            else {
                ItemInstance itemInstance = gameData.inventories.lootPtr.slots[i].itemInstance;
                slotUI.SetItem(itemInstance.ItemRef, itemInstance.count);
            }
        }

        bool alreadyDiscoveredAll = discoverItemIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        lootSearchingText.SetActive(true);

        discoverSlotsSequence = Sequence.Create();
        
        for (int i = 0; i < gameData.inventories.lootPtr.slots.Length; i++) {
            if (gameData.inventories.lootPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = gameData.inventories.lootPtr.slots[i].ui;
            
            if (gameData.inventories.lootPtr.slots[i].itemInstance.notDiscovered) {
                discoverSlotsSequence.Chain(Tween.PunchScale(slotUI.rectTransform, Vector3.one * 2f, 0.1f, 2f, startDelay: DiscoverSlotTime * i));
                discoverSlotsSequence.ChainCallback(slotUI, (target) => target.MakeSlotInactive());
            }
        }

        discoverSlotsSequence.ChainDelay(0.15f);

        discoverSlotsSequence.ChainCallback(target: this, static (target) => {
            ref int discoverItemIndex = ref target.gameData.curRaid.temp.interactionData.discoverItemIndex;
            ref Timer discoverItemTimer = ref target.gameData.curRaid.temp.interactionData.discoverItemTimer;
            
            InventorySlot slot = target.gameData.inventories.lootPtr.slots[discoverItemIndex];
            if (slot.itemInstance != null) {
                target.AnimateSlotSearch(slot.ui);
                discoverItemTimer.SetTime(target.DiscoverItemTime);
            }
        });
        
        discoverItemTimer.EndAction ??= static () => {
            Inventory lootInventoryPtr = gameInstance.gameData.inventories.lootPtr;
            ref Timer discoverItemTimer = ref gameInstance.gameData.curRaid.temp.interactionData.discoverItemTimer;
            ref int discoverItemIndex = ref gameInstance.gameData.curRaid.temp.interactionData.discoverItemIndex; 
            
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
                gameInstance.lootSearchingText.SetActive(false);
            }
        };
    }

    private void AnimateSlotSearch(InventorySlotUI slotUI) {
        slotUI.MakeSlotSearching();
        gameData.curRaid.temp.interactionData.searchCirclePopInTween = Tween.Scale(slotUI.searchingCircle.transform, Vector3.one * 0.2f, Vector3.one * 1f, 0.25f, Ease.OutElastic); 
    }

    private void CloseLootInventory() {
        lootSearchingText.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
        gameData.curRaid.temp.interactionData.discoverItemTimer.Stop();
        gameData.curRaid.temp.interactionData.discoverSlotsSequence.Stop();
        gameData.curRaid.temp.interactionData.searchCirclePopInTween.Stop();
        
        // Reset all tweening properties because the animations might have stopped while playing 
        foreach (InventorySlot slot in gameData.inventories.lootPtr.slots) {
            slot.ui.rectTransform.localScale = Vector3.one;
            slot.ui.StopSlotSearching();
        }
    }
    
    private void CheckForHotBarInteractions() {
        Item itemToConsume = null;
        int playerInventorySlotIndex = playerEquipmentSize;
        
        foreach (InputAction action in gameData.hotBar.quickUseActions) {
            if (action.WasPressedThisFrame()) {
                itemToConsume = gameData.inventories.player.slots[playerInventorySlotIndex].itemInstance?.ItemRef;
                break;
            }
            playerInventorySlotIndex++;
        }

        if (itemToConsume) {
            HavePlayerConsumeItem(gameData.inventories.player, playerInventorySlotIndex);
        }
    }
    
}
