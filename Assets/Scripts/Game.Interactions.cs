using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

public partial class Game {
    
    public struct InteractionData {
        public Sequence discoverSlotsSequence;
        public Tween searchCirclePopInTween;
        public Timer discoverItemTimer;
        public int discoverItemIndex;
        public LootInventoryOrigin curLootOrigin;
        public AudioClipHandle activeSearchingLoopClip;
    }
    
    private void CheckForInteractions() { 
        HideInteractionPopup();
        
        Vector2 checkCenter = player.position + new Vector3(0f, 0.05f, 0f);
        List<Collider2D> cols = Physics.OverlapCircle(checkCenter, 0.1f, Masks.ItemMask);
        
        // Only the closest thing gets interacted with. Otherwise one press would pick up every overlapping item,
        // or loot a body while the prompt was showing an item.
        Collider2D col = ClosestInteractable(cols, checkCenter);
        if (col == null) return;
        
        if (col.CompareTag(Tags.Pickup)) {
            CheckForItemDropInteraction(col.GetComponent<ItemDrop>());
        }

        if (col.CompareTag(Tags.DeadBody)) {
            EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Body");
            if (input.interact.WasPressedThisFrame()) {
                thisFrame.flags |= GameData.FrameFlags.SearchingBody;
                inventories.lootPtr.slots = curRaid.deadBodySlotsLookup[col.gameObject];
                OpenPlayerInventory();
                OpenLootInventory(LootInventoryOrigin.Body);
            }
        }
        
        if (col.CompareTag(Tags.Bush)) {
            EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Bush");
            if (input.interact.WasPressedThisFrame()) {
                thisFrame.flags |= GameData.FrameFlags.SearchingBush;
                inventories.lootPtr.slots = curRaid.bushSlotsLookup[col.gameObject];
                OpenPlayerInventory();
                OpenLootInventory(LootInventoryOrigin.Bush);
            }
        }

        if (col.CompareTag(Tags.Altar)) {
            Altar altar = col.GetComponent<Altar>();
            if (altar.used) {
                // Only chosen when its summoned item is out of reach, so the altar stands in for it
                CheckForItemDropInteraction(altar.summonedItemDrop);
                return;
            }
            
            int soulsPrice = curRaid.map.altarSoulPrice;
            Color soulsTextColor = player.state.soulCurrency >= soulsPrice ? config.styles.soulCurrencyColor : config.styles.outOfStockCountColor;
            string details = $"Summon Eye Upgrade: <sprite=1>{ColorText(soulsPrice.ToString("N0"), soulsTextColor)}";
            EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), details);
            if (input.interact.WasPressedThisFrame() && player.state.soulCurrency >= soulsPrice) {
                thisFrame.flags |= GameData.FrameFlags.SummonedUpgrade;
                SummonEyeUpgradeFromAltar(col);
                player.state.soulCurrency -= soulsPrice;
                altar.used = true;
            }
        }
        
        if (col.CompareTag(Tags.Chest)) {
            EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Open Chest");
            if (input.interact.WasPressedThisFrame()) {
                inventories.lootPtr.slots = curRaid.chestSlotsLookup[col.gameObject];
                OpenPlayerInventory();
                OpenLootInventory(LootInventoryOrigin.Chest);
            }
        }

        if (col.CompareTag(Tags.ExitPortal)) {
            Portal portal = GetExitPortalFromTransform(col.transform);
            
            if (portal.state == Portal.State.Inactive) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.28f), "Summon Extraction Portal");
                if (input.interact.IsPressed()) {
                    portal.StartOpenCloseSequence(config.gameplay.portalPostSummonDelay, config.gameplay.portalActiveDuration);
                }
            }
            
            if (portal.state == Portal.State.Open) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.14f), "Take Extraction Portal");
                if (input.interact.WasPressedThisFrame()) {
                    portal.OnPlayerTook();
                    bool winExit = curRaid.state == RaidState.PostFinalWave;
                    states.gameStateMachine.SetStateIfNotCurrent(winExit ? states.winExit : states.earlyExit);
                    thisFrame.flags |= winExit ? GameData.FrameFlags.ExitTaken : GameData.FrameFlags.EarlyExitTaken;
                }
            }
        }
        
        if (col.CompareTag(Tags.ExpressExitPortal)) {
            EnableInteractionPrompt(OffsetY(col.transform.position, 0.18f), "Take Extraction Portal");
            if (input.interact.WasPressedThisFrame()) {
                col.transform.GetComponent<SummonedPortal>().Close(activeStateOnComplete: false);
                bool winExit = curRaid.state == RaidState.PostFinalWave;
                states.gameStateMachine.SetStateIfNotCurrent(winExit ? states.winExit : states.earlyExit);
                thisFrame.flags |= winExit ? GameData.FrameFlags.ExitTaken : GameData.FrameFlags.EarlyExitTaken;
            }
        }
    }

    private Collider2D ClosestInteractable(List<Collider2D> cols, Vector2 checkCenter) {
        Collider2D closest = null;
        float closestSqrDist = float.MaxValue;
        foreach (Collider2D col in cols) {
            if (!IsInteractable(col, cols)) continue;
            float sqrDist = ((Vector2)col.bounds.center - checkCenter).sqrMagnitude;
            if (sqrDist >= closestSqrDist) continue;
            closestSqrDist = sqrDist;
            closest = col;
        }
        return closest;
    }
    
    // Things in reach that have nothing to do right now shouldn't block whatever is behind them
    private bool IsInteractable(Collider2D col, List<Collider2D> cols) {
        if (col.CompareTag(Tags.Pickup) || col.CompareTag(Tags.DeadBody) || col.CompareTag(Tags.Bush) || 
            col.CompareTag(Tags.Chest) || col.CompareTag(Tags.ExpressExitPortal)) {
            return true;
        }
        if (col.CompareTag(Tags.Altar)) {
            Altar altar = col.GetComponent<Altar>();
            if (!altar.used) return true;
            ItemDrop summonedItemDrop = altar.summonedItemDrop;
            return summonedItemDrop != null && !cols.Contains(summonedItemDrop.circleCollider);
        }
        if (col.CompareTag(Tags.ExitPortal)) {
            Portal portal = GetExitPortalFromTransform(col.transform);
            return portal.state is Portal.State.Inactive or Portal.State.Open;
        }
        return false;
    }
    
    private void CheckForItemDropInteraction(ItemDrop itemDrop) {
        ui.itemDescPopupPickup.Show(itemDrop.ItemInstance);
        
        Item dropItemRef = itemDrop.ItemInstance.ItemRef;
        Color itemColor = config.styles.GetTextColorForRarity(itemDrop.ItemInstance.GetRarity());
        string details = ColorText($"{dropItemRef.displayName} x{itemDrop.ItemInstance.count}", itemColor);
        EnableInteractionPrompt(OffsetY(itemDrop.transform.position, 0.1f), details);
        
        if (!input.interact.WasPressedThisFrame()) return;
        
        InventoryAddResult result = TryAddItemToInventory(inventories.player, itemDrop.ItemInstance);
        if (result.type != InventoryAddResult.ResultType.Failure) {
            thisFrame.flags |= GameData.FrameFlags.PickedUpLoot;
            // The slot's tween plays this when the inventory is showing, but it skips hidden slots, so pickups would be silent
            if (!PlayerInventoryIsOpen) {
                PlayAudioClip(audio.itemMoveClip);
            }
        }
        
        if (result.type == InventoryAddResult.ResultType.Success) {
            Entity droppedEntity = entities.lookup[itemDrop.gameObject];
            PickupDroppedItem(droppedEntity); 
            itemDrop.circleCollider.enabled = false;
            
            if (itemDrop.summoningAltar != null) {
                itemDrop.summoningAltar.summonedItemDrop = null;
                itemDrop.summoningAltar.GetComponent<Collider2D>().enabled = false;
                itemDrop.summoningAltar = null;
            }
        }
        else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
            itemDrop.ItemInstance.count -= result.addedCount;
        }
    }
    
    private void HideInteractionPopup() {
        ui.itemDescPopupPickup.Hide();
        DisableInteractionPrompt();
    }

    private void PickupDroppedItem(Entity droppedEntity) {
        Vector3 playerPickupTarget = new(0f, 0.07f, 0f);
        
        droppedEntity.GetEffect(Entity.EffectsIndicies.Bounce).Stop();
        droppedEntity.trans.SetParent(player.trans, true);
        
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
        
        // Moves straight towards the player with a small hop on top. Tweening x and y separately only looked right
        // for items beside or below the player, items above would drop straight down and then slide across.
        const float hopHeight = 0.05f;
        Vector3 startLocalPos = droppedEntity.trans.localPosition;
        Tween.Custom(droppedEntity.trans, 0f, 1f, 0.15f, ease: Ease.InQuad, onValueChange: (trans, t) => {
            Vector3 pos = Vector3.Lerp(startLocalPos, playerPickupTarget, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
            trans.localPosition = pos;
        })
        .Group(Tween.Scale(droppedEntity.trans, 0f, itemScaleSettings))
        .Group(Tween.PunchScale(player.trans, playerScaleSettings))
        .OnComplete(() => DestroyEntity(droppedEntity));
    }
    
    // Looting speed goes up as it improves, so it divides the time instead of multiplying it
    private float DiscoverSlotTime => config.gameplay.discoverSlotTime / GetAbsoluteStat(PlayerStat.LootingSpeed);
    private float DiscoverItemTime => config.gameplay.discoverItemTime / GetAbsoluteStat(PlayerStat.LootingSpeed);
    
    public enum LootInventoryOrigin { Nothing, Body, Bush, Chest }
    
    private void OpenLootInventory(LootInventoryOrigin origin) {
        if (LootInventoryIsOpen) return;
        
        thisFrame.flags |= GameData.FrameFlags.InventoryOpened;
        IgnoreHeldNavigationInput();
        curRaid.data.interactions.curLootOrigin = origin;
        
        if (origin is LootInventoryOrigin.Body or LootInventoryOrigin.Chest) {
            PlayAudioClip(audio.lootingBodyClip, player.position);
        }
        else if (origin == LootInventoryOrigin.Bush) {
            PlayAudioClip(audio.lootingBushClip, player.position);
        }
        
        ui.lootInventoryPanel.gameObject.SetActive(true);
        
        ref int discoverItemIndex = ref curRaid.data.interactions.discoverItemIndex;
        ref Sequence discoverSlotsSequence = ref curRaid.data.interactions.discoverSlotsSequence;
        ref Timer discoverItemTimer = ref curRaid.data.interactions.discoverItemTimer;
        discoverItemIndex = -1;
        
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
                slotUI.SetItem(itemInstance);
            }
        }

        bool alreadyDiscoveredAll = discoverItemIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        StartSearchingSoundLoop();
        ui.lootSearchingText.SetActive(true);

        discoverSlotsSequence = Sequence.Create();
        // Add a small delay before revealing slots 
        discoverSlotsSequence.ChainDelay(0.05f);
        
        int lootCount = GetInventoryItemCount(inventories.lootPtr);
        for (int i = 0; i < inventories.lootPtr.slots.Length; i++) {
            if (inventories.lootPtr.slots[i].itemInstance == null) continue;
            
            InventorySlotUI slotUI = inventories.lootPtr.slots[i].ui;
            
            if (inventories.lootPtr.slots[i].itemInstance.notDiscovered) {
                float curveComp = (i + 1) / (float)lootCount;
                float delay = gameInstance.curves.discoverSlotTimingCurve.Evaluate(curveComp) * DiscoverSlotTime;
                discoverSlotsSequence.Chain(Tween.PunchScale(slotUI.rectTransform, Vector3.one * 2f, 0.1f, 2f, startDelay: delay));
                discoverSlotsSequence.ChainCallback(slotUI, static (target) => {
                    target.MakeSlotInactive();
                    gameInstance.PlayAudioClip(gameInstance.audio.slotRevealClip, player.position);
                });
            }
        }

        // Add a small delay before revealing items
        discoverSlotsSequence.ChainDelay(0.05f);

        discoverSlotsSequence.ChainCallback(target: this, static (target) => {
            ref int discoverItemIndex = ref target.curRaid.data.interactions.discoverItemIndex;
            ref Timer discoverItemTimer = ref target.curRaid.data.interactions.discoverItemTimer;

            InventorySlot slot = target.inventories.lootPtr.slots[discoverItemIndex];
            if (slot.itemInstance != null) {
                target.AnimateSlotSearch(slot.ui);
                discoverItemTimer.SetTime(target.DiscoverItemTime);
            }
        });
        
        discoverItemTimer.EndAction ??= static () => {
            Inventory lootInventoryPtr = gameInstance.inventories.lootPtr;
            ref Timer discoverItemTimer = ref gameInstance.curRaid.data.interactions.discoverItemTimer;
            ref int discoverItemIndex = ref gameInstance.curRaid.data.interactions.discoverItemIndex;

            ItemInstance itemInstance = lootInventoryPtr.slots[discoverItemIndex].itemInstance;
            Item itemRef = itemInstance.ItemRef;
            itemInstance.notDiscovered = false;
            
            gameInstance.thisFrame.data.foundSearchItem = itemInstance;
            
            InventorySlotUI slotUI = lootInventoryPtr.slots[discoverItemIndex].ui;
            slotUI.MakeSlotActive();
            slotUI.StopSlotSearching();
            slotUI.SetItem(itemInstance);
            gameInstance.PlayItemReveal(slotUI, itemInstance, player.position);

            discoverItemIndex++;
            
            if (discoverItemIndex < lootInventoryPtr.slots.Length && lootInventoryPtr.slots[discoverItemIndex].itemInstance != null) {
                slotUI = lootInventoryPtr.slots[discoverItemIndex].ui;
                gameInstance.AnimateSlotSearch(slotUI);
                discoverItemTimer.SetTime(gameInstance.DiscoverItemTime);
            }
            else {
                gameInstance.ui.lootSearchingText.SetActive(false);
                gameInstance.StopSearchingSoundLoop();
            }
        };
    }

    // The pop, rarity colored flash and sounds for an item being revealed in a slot
    private void PlayItemReveal(InventorySlotUI slotUI, ItemInstance itemInstance, Vector2 soundPosition) {
        Tween.Scale(slotUI.itemUI.image.rectTransform, Vector3.one * 3.5f, Vector3.one, new TweenSettings(0.2f, Ease.OutBack));

        Item.Rarity itemRarity = itemInstance.GetRarity();
        Entity reveal = SpawnEntityOneShot(entityPools.lootReveal, Vector3.zero, Quaternion.identity, slotUI.rectTransform);
        reveal.trans.localPosition = Vector3.zero;
        reveal.image.color = config.styles.GetColorForRarity(itemRarity);

        GetRarityVolumeAndPitch(itemRarity, out float rarityVolume, out float rarityPitch);
        PlayAudioClip(audio.rarityRevealClip, soundPosition, rarityVolume, rarityPitch);
        PlayAudioClip(audio.lootRevealClip, soundPosition);
    }

    private void AnimateSlotSearch(InventorySlotUI slotUI) {
        slotUI.MakeSlotSearching();
        curRaid.data.interactions.searchCirclePopInTween = Tween.Scale(slotUI.searchingCircle.transform, Vector3.one * 0.2f, Vector3.one * 1f, 0.25f, Ease.OutElastic); 
    }

    private void CloseLootInventory() {
        ui.lootSearchingText.SetActive(false);
        ui.lootInventoryPanel.gameObject.SetActive(false);
        curRaid.data.interactions.discoverItemTimer.Stop();
        curRaid.data.interactions.discoverSlotsSequence.Stop();
        curRaid.data.interactions.searchCirclePopInTween.Stop();
        
        // Reset all tweening properties because the animations might have stopped while playing 
        foreach (InventorySlot slot in inventories.lootPtr.slots) {
            slot.ui.rectTransform.localScale = Vector3.one;
            slot.ui.StopSlotSearching();
        }
        
        StopSearchingSoundLoop();
    }
    
    private void StartSearchingSoundLoop() {
        LootInventoryOrigin origin = curRaid.data.interactions.curLootOrigin;
        DynamicClip searchingLoop = origin switch {
            LootInventoryOrigin.Nothing => null,
            LootInventoryOrigin.Body => audio.lootingBodyLoop,
            LootInventoryOrigin.Bush => audio.lootingBushLoop,
            LootInventoryOrigin.Chest => audio.lootingBodyLoop, // We don't have a looting chest
            _ => throw new ArgumentOutOfRangeException(),
        };
        if (searchingLoop != null) {
            curRaid.data.interactions.activeSearchingLoopClip = PlayAudioClip(searchingLoop, player.position, loop: true);
        }
    }
    
    private void StopSearchingSoundLoop() {
        StopAudioClip(curRaid.data.interactions.activeSearchingLoopClip);
    }
    
    private void SummonEyeUpgradeFromAltar(Collider2D altarCol) {
        Altar altar = altarCol.GetComponent<Altar>();
        
        SpawnEntityOneShot(entityPools.altarSoulSwirl, altar.soulSwirlSpawnPoint.position, Quaternion.identity);
        PlayAudioClip(audio.altarSoulsClip, altar.soulSwirlSpawnPoint.position);
        
        using var _ = ListPool<Transform>.Get(out var bubbleSpawns);
        int curSpawnIndex = int.MaxValue;
        const int bubbleCount = 30;
        
        for (int i = 0; i < bubbleCount; i++) {
            if (!altar.bloodBubbleSpawns.IndexInRange(curSpawnIndex)) {
                curSpawnIndex = 0;
                altar.bloodBubbleSpawns.Shuffle();
            }
            bubbleSpawns.Add(altar.bloodBubbleSpawns[curSpawnIndex]);
            curSpawnIndex++;
        }
        
        const float startBubblesDelay = 0.25f;
        const float bubbleInterval = 0.35f;
        float curBubbleSpawnTime = startBubblesDelay;
        
        for (int i = 0; i < bubbleCount; i++) {
            float bubbleAcc = curves.altarBubbleRate.Evaluate(i / (float)bubbleCount);
            float spawnDelay = curBubbleSpawnTime + bubbleInterval * (1f - bubbleAcc);
            Tween.Delay(bubbleSpawns[i], spawnDelay, static (spawnTrans) => { 
                gameInstance.SpawnEntityOneShot(gameInstance.entityPools.bloodBubble, spawnTrans.position, spawnTrans.rotation);
                gameInstance.PlayAudioClip(gameInstance.audio.altarBubbleClip, spawnTrans.position);
            });
            curBubbleSpawnTime = spawnDelay;
        }
        
        Tween.Delay(altar, curBubbleSpawnTime * 0.97f, static (altar) => {
            altar.bloodPoolAnimator.Play(Altar.bloodDrainAnimHash);
        });
        
        Tween.Delay(altar, curBubbleSpawnTime, static (altar) => {
            altar.bloodExplosionParticles.Play();
            gameInstance.PlayAudioClip(gameInstance.audio.altarBloodExplosionClip, altar.bloodExplosionParticles.transform.position);
            gameInstance.camera.cameraShake.Shake(5f, 0.1f, 0.5f, altar.transform.position, falloffStartRange: 0.5f, falloffDistance: 1.5f, CameraShake.Falloff.Linear);
            
            altar.summoningItem = gameInstance.GetItemFromDropPool(gameInstance.dropPools.eyeUpgrades);
            Entity item = gameInstance.SpawnItemAsEntity(altar.summoningItem, 1, altar.transform.position, Quaternion.identity);
            item.spriteRenderer.sortingOrder = 1;
            
            ItemDrop itemDrop = item.gameObject.GetComponent<ItemDrop>();
            itemDrop.summoningAltar = altar;
            altar.summonedItemDrop = itemDrop;
            
            Vector3 endPos = altar.transform.position.Offset(y: 0.25f);
            Tween.Position(item.trans, endPos, 0.12f, Ease.OutBack);
            Tween.Scale(item.trans, 0f, 1f, 0.16f, Ease.OutBack);
            
            Tween.Delay(altar, 0.12f, static (altar) => {
                Entity reveal = gameInstance.SpawnEntityOneShot(gameInstance.entityPools.eyeUpgradeReveal, altar.transform.position.Offset(y: 0.25f), Quaternion.identity);
                reveal.spriteRenderer.color = gameInstance.config.styles.GetColorForRarity(altar.summoningItem.GetRarity());
                GetRarityVolumeAndPitch(altar.summoningItem.GetRarity(), out float rarityVolume, out float rarityPitch);
                gameInstance.PlayAudioClip(gameInstance.audio.rarityRevealClip, player.position, rarityVolume, rarityPitch);
            });
        });
        
    }
    
    // On controller south also selects items in the inventory and confirms in menus, so it only uses the hotbar while playing.
    // Resuming from the pause menu is checked too, since pressing resume with south would otherwise also use an item that frame.
    private bool CanUseControllerHotBar => InRaid && !PlayerInventoryIsOpen && !LootInventoryIsOpen && pauseMenu.resumedOnFrame != Time.frameCount;

    private void CheckForHotBarInteractions() {
        int quickUseIndex = -1;
        for (int i = 0; i < hotBar.quickUseActions.Count; i++) {
            if (hotBar.quickUseActions[i].WasPressedThisFrame()) {
                quickUseIndex = i;
                break;
            }
        }

        if (CanUseControllerHotBar) {
            if (input.quickUsePrevious.WasPressedThisFrame()) {
                MoveHotBarSelection(-1);
            }
            if (input.quickUseNext.WasPressedThisFrame()) {
                MoveHotBarSelection(1);
            }
            if (input.quickUseSelected.WasPressedThisFrame()) {
                quickUseIndex = hotBar.selectedIndex;
            }
        }

        if (quickUseIndex == -1) return;

        int playerInventorySlotIndex = playerEquipmentSize + quickUseIndex;
        if (inventories.player.slots[playerInventorySlotIndex].itemInstance == null) return;
        HavePlayerConsumeItem(inventories.player, playerInventorySlotIndex);
    }

    // Wraps around, so going left from the first slot goes to the last
    private void MoveHotBarSelection(int direction) {
        int slotCount = hotBar.quickUseActions.Count;
        hotBar.selectedIndex = (hotBar.selectedIndex + direction + slotCount) % slotCount;
    }
    
}
