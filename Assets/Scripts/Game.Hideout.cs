using System;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public partial class Game {
    
    private void InitHideout(GameState gameState) {
        hideoutState = gameState?.hideoutState ?? new();
        InitTrader(gameState);
        InitSkillsPanel();
        InitQuestPanel();
        SetPentagramFill(0f);
    }
    
    private void UpdateHideoutNotifiers() {
        bool oneOrMoreQuestsReadyToSubmit = false;
        foreach (QuestPackage questPkg in quests.activePkgs) {
            Quest quest = questPkg.questNode.curQuest;
            bool questIsComplete = QuestIsComplete(quest);
            oneOrMoreQuestsReadyToSubmit = questIsComplete || oneOrMoreQuestsReadyToSubmit;
            bool showingQuest = quests.presentingPkg == questPkg;
            questPkg.questToggleButton.notifier.SetActive(questIsComplete && !showingQuest);
        }
        hideoutTabs.questsButton.notifier.SetActive(oneOrMoreQuestsReadyToSubmit && !OnQuestsTab);
    }
    
    // ************************
    // Trader
    // ************************

    private void InitTrader(GameState gameState) {
        config.trader.state = gameState?.traderState ?? new();
        CheckForTraderRestock();
        MarkTraderItemsAsTraderOwned();
        CalculateAndSetTraderRepBars();
    }

    private void TraderOnExitRaid() {
        config.trader.state.raidsUntilRestock--;
        CheckForTraderRestock();
    }

    private void CheckForTraderRestock(bool forceRestock = false) {
        ref int raidsUntilRestock = ref config.trader.state.raidsUntilRestock;

        if (forceRestock) {
            raidsUntilRestock = 0;
        }
        
        if (raidsUntilRestock <= 0) {
            SetTradingSlot(null, tweenSize: false);
            FillTraderInventoryWithItems();
            raidsUntilRestock = config.gameplay.raidsPerTraderRestock;
        }

        if (raidsUntilRestock == 1) {
            traderPanel.itemRefreshTimeText.text = "Items restock after next raid";
            return;
        }
        traderPanel.itemRefreshTimeText.text = $"Items restock in {raidsUntilRestock} more raids";
    }

    private void IncreaseTraderRep(int repGain) {
        if (config.trader.ReachedMaxLevel()) return;

        int wouldGainLevels = config.trader.LevelsGainedFromXp(repGain);
        config.trader.state.reputation += repGain;
        
        bool increasedLevel = wouldGainLevels > 0;
        if (increasedLevel) {
            CheckForTraderRestock(forceRestock: true);
            ui.levelUpNotification.Show($"Reached Trader Level {config.trader.GetLevel()}");
        }
        CalculateAndSetTraderRepBars();
    }

    private void CalculateAndSetTraderRepBars() {
        int levelIndex = config.trader.GetLevel();
        
        if (config.trader.ReachedMaxLevel()) {
            SetTraderRepBarViewAsMaxLevel(traderPanel.repBar, levelIndex);
            SetTraderRepBarViewAsMaxLevel(questsPanel.traderRepBar, levelIndex);
            return;
        }

        float fill = config.trader.CurrentLevelCompletion();
        int repLeftToGo = config.trader.XpUntilNextLevel();
        SetTraderRepBarView(traderPanel.repBar, fill, repLeftToGo, levelIndex);
        SetTraderRepBarView(questsPanel.traderRepBar, fill, repLeftToGo, levelIndex);
    }
    
    private void SetTraderRepBarView(TraderRepBar bar, float fill, int repLeftToGo, int level) {
        bar.xpLevelFill.fillAmount = fill;
        bar.remainingXpText.text = $"{repLeftToGo} Rep Left";
        bar.levelText.text = $"Level {level}";
    }
    
    private void SetTraderRepBarViewAsMaxLevel(TraderRepBar bar, int level) {
        bar.xpLevelFill.fillAmount = 1f;
        bar.remainingXpText.text = string.Empty;
        bar.levelText.text = $"Level {level} (Max)";
    }

    public void FillTraderInventoryWithItems() {
        ClearInventory(inventories.trader);
        int curTraderLevel = config.trader.GetLevel();
        
        float raritySkew = curTraderLevel switch { 
            0 => 0.13f, 
            1 => 0.25f, 
            2 => 0.40f,
            3 => 0.50f,
            _ => 0.60f,
        };
        
        float stockCountSkew = curTraderLevel switch { 
            0 => 0f, 
            1 => 0.12f, 
            2 => 0.20f,
            3 => 0.40f,
            4 => 0.60f,
            _ => 0.80f,
        };
        
        using var _ = ListPool<Item>.Get(out var items);
        GetUniqueItemsFromDropPool(dropPools.trader, traderInventoryColCount * traderInventoryRowCount, ref items);
        items = items.OrderBy(x => TraderTypeOrder(x.type)).ThenBy(x => x.type.name).ThenBy(x => x.GetRarity()).ThenBy(x => x.buyPrice).ToList();
        
        foreach (Item item in items) {
            if (item.traderSpawning.levelRequired > curTraderLevel) continue;
            
            int lowerRange = item.traderSpawning.stockRange.x;
            int maxUpperRange = item.traderSpawning.stockRange.y;
            int weightedUpperRange = lowerRange + ((maxUpperRange - lowerRange) / 2);
            while (weightedUpperRange < maxUpperRange && RollProbability(stockCountSkew)) {
                weightedUpperRange++;
            }
            int stackCount = Random.Range(lowerRange, weightedUpperRange); 
            TryAddItemToInventory(inventories.trader, item, stackCount);
        }
        
        MarkTraderItemsAsTraderOwned();
    }
    
    // Any type not listed here gets placed after these, ordered by type name
    private int TraderTypeOrder(ItemType type) {
        ItemType[] typeOrder = { itemTypes.quickUse, itemTypes.backpack, itemTypes.wearableModifier, itemTypes.eyeUpgrade };
        for (int i = 0; i < typeOrder.Length; i++) {
            if (type == typeOrder[i]) return i;
            foreach (ItemType derivative in typeOrder[i].derivativeItemTypes) {
                if (derivative == type) return i;
            }
        }
        return typeOrder.Length;
    }

    private void MarkTraderItemsAsTraderOwned() {
        for (int i = 0; i < inventories.trader.slots.Length; i++) {
            InventorySlot slot = inventories.trader.slots[i];
            if (slot.itemInstance == null) continue;
            slot.itemInstance.traderOwned = true;
            slot.itemInstance.traderSlotIndex = i;
        }
    }

    private void ClearItemsAsTraderOwned(Inventory inventory) {
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.itemInstance == null) continue;
            slot.itemInstance.traderOwned = false;
            slot.itemInstance.traderSlotIndex = -1;
        }
    }
    
    private InventorySlot curTradingInventorySlot;
    
    private void SetTradingSlot(InventorySlot traderInventorySlot, bool tweenSize) {
        traderInventorySlot ??= inventories.trader.slots[0];
        
        curTradingInventorySlot?.ui.ClearSelectionUnderlay();
        curTradingInventorySlot = traderInventorySlot;
        curTradingInventorySlot?.ui.SetSelectionUnderlay();
        
        if (tweenSize && curTradingInventorySlot != null) {
            ItemUI itemUI = curTradingInventorySlot.ui.itemUI;
            Tween.PunchScale(itemUI.rectTransform, Vector3.one * 0.3f, 0.12f, 5f);
            PlayAudioClip(audio.itemSelectClip);
        }

        ItemInstance tradingItemInstance = curTradingInventorySlot?.itemInstance;
        transactionPanel.transaction.UpdateBuyItem(tradingItemInstance);
        if (tradingItemInstance != null && transactionState == TransactionState.Selling) {
            transactionPanel.transaction.toggleGroup.ManualyToggle(transactionPanel.transaction.buyToggle);
        }
    }
    
    private void ReduceTradingItemStock() {
        int slotIndex = curTradingInventorySlot.itemInstance.traderSlotIndex;
        ReduceItemCountInInventory(inventories.trader, slotIndex, keepOnEmpty: true);
    }

    private enum TransactionState { Selling, Buying }
    private TransactionState transactionState;
    
    private void OnBuyTogglePressed() {
        transactionState = TransactionState.Buying;
        transactionPanel.inventoryParent.gameObject.SetActive(false);

        SetTradingSlot(curTradingInventorySlot, tweenSize: false);
        if (usingController) {
            SetControllerSelection(curTradingInventorySlot.ui.rectTransform);
        }

        // Move any selling items back to stash
        foreach (InventorySlot slot in inventories.transaction.slots) {
            if (slot.itemInstance == null) continue;
            TryAddItemToInventory(inventories.stash, slot.itemInstance);
        }
        ClearInventory(inventories.transaction);
    }
    
    private void OnSellTogglePressed() {
        transactionState = TransactionState.Selling;
        transactionPanel.inventoryParent.gameObject.SetActive(true);
        curTradingInventorySlot?.ui.ClearSelectionUnderlay();
        
        // Move the controller pointer over to the stash from the traders inventory
        if (usingController && lastInventoryHoverInfo.inventory == inventories.trader) {
            SelectDefaultNavTarget();
        }
    }

    private void OnSellButtonPressed() {
        if (transactionState == TransactionState.Selling && GetInventoryItemCount(inventories.transaction) <= 0) return;
        int sellPrice = GetInventoryValue(inventories.transaction, InventoryValueType.Sell);
        player.state.coinCurrency += sellPrice;
        ClearInventory(inventories.transaction);
        PlayAudioClip(audio.cashRegisterClip);
        thisFrame.flags |= FrameFlags.SoldToTrader;
    }
    
    private void OnMoneyPurchaseButtonPressed() {
        if (transactionState == TransactionState.Buying && curTradingInventorySlot == null) return;
        Item curTradingItem = curTradingInventorySlot.itemInstance.ItemRef;
        int buyPrice = curTradingItem.buyPrice;
        if (player.state.coinCurrency >= buyPrice) {
            player.state.coinCurrency -= buyPrice;
            TryAddItemToInventory(inventories.stash, curTradingItem, 1);
            ReduceTradingItemStock();
            // After buying items we just make sure all items in stash are no longer trader owned
            ClearItemsAsTraderOwned(inventories.stash);
            TriggerTraderShopDialogue(TraderShopDialogueType.Purchase);
            PlayAudioClip(audio.purchaseClip);
        }
    }
    
    private void OnBarterPurchaseButtonPressed() {
        if (curTradingInventorySlot == null) return;

        Item curTradingItem = curTradingInventorySlot.itemInstance.ItemRef;
        if (!OwnsAllItemsOfCounts(curTradingItem.traderSpawning.barterRequirements)) return;
            
        RemoveOwnedItemsFromInventories(curTradingItem.traderSpawning.barterRequirements);
        TryAddItemToInventory(inventories.stash, curTradingItem, 1);
        ReduceTradingItemStock();
        TriggerTraderShopDialogue(TraderShopDialogueType.Purchase);
        PlayAudioClip(audio.purchaseClip);
    }
    
    private void UpdateTransactionUI() {
        if (!OnTradingTab) return;
        
        if (transactionState == TransactionState.Buying) {
            transactionPanel.transaction.UpdateBuyItem(curTradingInventorySlot?.itemInstance);
            transactionPanel.transaction.toggleGroup.ManualyToggleCosmetically(transactionPanel.transaction.buyToggle);
            transactionPanel.transaction.ClearSellPrice();
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(inventories.transaction, InventoryValueType.Sell);
            if (thisFrame.flags.HasFlag(FrameFlags.SoldToTrader)) {
                transactionPanel.transaction.ClearSellPrice();
            }
            else {
                transactionPanel.transaction.UpdateSellPrice(sellPrice);
            }
            transactionPanel.transaction.toggleGroup.ManualyToggleCosmetically(transactionPanel.transaction.sellToggle);
            transactionPanel.transaction.sellButton.SetClickableState(GetInventoryItemCount(inventories.transaction) > 0);
        }
    }
    
    private enum TraderShopDialogueType { Greeting, Purchase }

    private Tween traderDialogueTween;
    private string prevTraderDialogue;
    private int raidCountOnLastDialogueTrigger = -1;

    private void TriggerTraderShopDialogue(TraderShopDialogueType dialogueType) {
        bool canPlayNewDialogue = raidCountOnLastDialogueTrigger != curSession.raidsEntered;
        if (!canPlayNewDialogue) return;
        
        raidCountOnLastDialogueTrigger = curSession.raidsEntered;
        
        traderDialogueTween.Stop();
        traderPanel.shopTextTypewriter.gameObject.SetActive(false);

        // The delay just looks nice 
        traderDialogueTween = Tween.Delay(0.1f, () => {
            var typewriter = gameInstance.traderPanel.shopTextTypewriter;
            if (dialogueType == TraderShopDialogueType.Greeting) {
                string dialogue = traderGreetings.GetRandom(prevTraderDialogue);
                prevTraderDialogue = dialogue;
                typewriter.ShowText("{ffade}{shake}" + dialogue);
            }
            typewriter.gameObject.SetActive(true);
        });
    }
    
    // ************************
    // Eye Forge 
    // ************************
    
    private enum ForgeMode { Empty, Forging, UpgradingDemonEye, PostForgeOrUpgrade }
    private ForgeMode forgeMode;
    
    private enum ForgeError { Nothing, ForgingButJustEye, ForgingButWithoutEye, ForgingButMissingUpgrades, PentagramLevelTooLow, NeedsToOwnMoreEyeUpgrades }
    private ForgeError forgeError;
    
    private bool ForgeIsOnCrafting => eyeForgePanel.forgingParent.activeInHierarchy;
    private bool ForgeIsOnLevelUp => eyeForgePanel.levelUpParent.activeInHierarchy;

    private void UpdateForgeState() {
        bool showingForge = eyeForgePanel.panel.gameObject.activeInHierarchy;
        if (!showingForge) return;

        int crucibleItemCount = GetInventoryItemCount(inventories.eyeForge);
        ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[0].itemInstance;
        
        if (forgeMode == ForgeMode.PostForgeOrUpgrade && crucibleItemCount == 1) {
            forgeError = ForgeError.Nothing;
        }
        else if (crucibleItemCount <= 0) {
            forgeMode = ForgeMode.Empty;
            forgeError = ForgeError.Nothing;
        }
        else if (eyeSlotItemInstance != null && eyeSlotItemInstance.isDemonEye) {
            forgeMode = ForgeMode.UpgradingDemonEye;
            if (eyeSlotItemInstance.DemonEyeLevel > hideoutState.pentagramLevelIndex) {
                forgeError = ForgeError.PentagramLevelTooLow;
            }
            else if (!OwnsEyeUpgradesRequiredToUpgradeDemonEye(eyeSlotItemInstance)) {
                forgeError = ForgeError.NeedsToOwnMoreEyeUpgrades;
            }
            else {
                forgeError = ForgeError.Nothing;
            }
        }
        else {
            forgeMode = ForgeMode.Forging;
            if (eyeSlotItemInstance != null && crucibleItemCount == 1) {
                forgeError = ForgeError.ForgingButJustEye;
            }
            else if (eyeSlotItemInstance == null) {
                forgeError = ForgeError.ForgingButWithoutEye;
            }
            else if (crucibleItemCount < inventories.eyeForge.slots.Length) {
                forgeError = ForgeError.ForgingButMissingUpgrades;
            }
            else {
                forgeError = ForgeError.Nothing;
            }
        }
        
        if (InTutorialFirstForge) return;
        bool shouldShowPlayerPanel = (forgeMode is ForgeMode.Empty or ForgeMode.PostForgeOrUpgrade) || forgeError is ForgeError.ForgingButJustEye;
        
        if (shouldShowPlayerPanel) {
            if (!ShowingPlayerPanel) {
                ToggleHideoutPanels(playerPanel.panel, eyeForgePanel.panel, stashPanel.panel);
                ToggleSlimPlayerPanel(true);
            }
        }
        else if (ShowingPlayerPanel) {
            ToggleHideoutPanels(eyeForgeDetailsPanel.panel, eyeForgePanel.panel, stashPanel.panel);
        }
    }
    
    private void UpdateForgePanel() {
        bool showingForge = eyeForgePanel.panel.gameObject.activeInHierarchy;
        if (!showingForge) return;
        
        int curPentagramLevel = hideoutState.pentagramLevelIndex + 1;
        eyeForgePanel.panelNumeral.sprite = config.styles.RomanNumeralSprite(curPentagramLevel);
        eyeForgePanel.panelNumeral.enabled = !InTutorialFirstForge;
        
        int curLevelDisplayedInLevelUpTab = (int)char.GetNumericValue(eyeForgePanel.subHeaderTextMesh.text[^1]);
        int nextPentagramLevel = curPentagramLevel + 1;
        if (nextPentagramLevel != curLevelDisplayedInLevelUpTab) {
            eyeForgePanel.subHeaderTextMesh.text = $"Unlocks Demon Eye Level {nextPentagramLevel}";
        }
        
        eyeForgePanel.forgeHintTextMesh.text = string.Empty;
        
        if (ForgeIsOnCrafting && !PlayingForgeAnimation) {
            ButtonFeel forgeButton = eyeForgePanel.forgeButton;
            
            if (forgeMode is ForgeMode.PostForgeOrUpgrade) {
                if (InTutorialFirstForge) {
                    forgeButton.text.text = "Craft";
                    forgeButton.SetClickableState(false);
                }
                else {
                    forgeButton.text.text = "Continue";
                    forgeButton.SetClickableState(true);
                }
                return;
            }
            
            forgeButton.text.text = forgeMode is ForgeMode.UpgradingDemonEye ? "Upgrade" : "Craft";
            
            if (forgeMode is ForgeMode.UpgradingDemonEye) {
                ItemInstance eyeItemInstance = inventories.eyeForge.slots[0].itemInstance;
                
                if (forgeError is ForgeError.PentagramLevelTooLow) {
                    int pentagramLevelNeeded = eyeItemInstance.DemonEyeLevel + 1;
                    eyeForgePanel.forgeHintTextMesh.text = $"Requires Pentagram level {pentagramLevelNeeded} to upgrade";
                }
                
                // We want to remove any previously placed eye upgrades because the upgrade placehoder items will hide them.
                // Note we try to move the items to the Stash, but if not enough space, then the Player inventory, but if they are full then
                // we just do nothing because we don't have a safe way of moving the item, should probably dynamically add a slot in the case but eh.
                for (int i = 1; i < inventories.eyeForge.slots.Length; i++) {
                    if (inventories.eyeForge.slots[i].itemInstance != null) {
                        MoveItemBetweenInventories(inventories.eyeForge, inventories.stash, i, MoveItemOption.FullStack);
                    }
                    if (inventories.eyeForge.slots[i].itemInstance != null) {
                        MoveItemBetweenInventories(inventories.eyeForge, inventories.player, i, MoveItemOption.FullStack);
                    }
                }
                
                var upgrades = ListPool<EyeUpgrade>.Get();
                GetDemonEyeCoreUpgrades(eyeItemInstance, ref upgrades);
                
                var upgradeCountLookup = DictionaryPool<EyeUpgrade, int>.Get();
                foreach (EyeUpgrade upgrade in upgrades) {
                    upgradeCountLookup.TryAdd(upgrade, 0);
                    upgradeCountLookup[upgrade]++;
                }
                
                for (int i = 0; i < upgrades.Count; i++) {
                    InventorySlot slot = inventories.eyeForge.slots[i + 1];
                    EyeUpgrade upgrade = upgrades[i];
                    slot.ui.SetPlaceHolderItemImage(upgrade);
                    slot.ui.itemUI.UpdateOwnedVsRequiredCount(GetOwnedCountOfItem(upgrade), upgradeCountLookup[upgrade]);
                }
                
                ListPool<EyeUpgrade>.Release(upgrades);
                DictionaryPool<EyeUpgrade, int>.Release(upgradeCountLookup);
            }
            
            bool canForge = false;
            if (forgeMode == ForgeMode.Forging) {
                canForge = EverySlotHasAnItem(inventories.eyeForge);
            }
            if (forgeMode == ForgeMode.UpgradingDemonEye) {
                ItemInstance eyeItemInstance = inventories.eyeForge.slots[0].itemInstance;
                canForge = forgeError != ForgeError.PentagramLevelTooLow && OwnsEyeUpgradesRequiredToUpgradeDemonEye(eyeItemInstance);
            }
            
            if (canForge && forgeButton.isDisabled) {
                forgeButton.Enable();
            }
            else if (!canForge && !forgeButton.isDisabled) {
                forgeButton.Disable();
            }
        }
        else if (ForgeIsOnLevelUp) {
            int upgradeIndex = hideoutState.pentagramLevelIndex;
            bool levelUpExists = config.eyeForgeUpgradePath.pathUpgrades.IndexInRange(upgradeIndex);
            
            if (levelUpExists) {
                List<ItemWithCount> itemRequirements = config.eyeForgeUpgradePath.pathUpgrades[upgradeIndex].requirements;
                eyeForgePanel.levelUpRequirementList.Show(itemRequirements);
                eyeForgePanel.levelUpButton.SetClickableState(OwnsAllItemsOfCounts(itemRequirements));
                eyeForgePanel.maxLevelReachedNotifier.SetActive(false);
            }
            else {
                eyeForgePanel.levelUpRequirementList.gameObject.SetActive(false);
                eyeForgePanel.levelUpButton.SetClickableState(false);
                eyeForgePanel.maxLevelReachedNotifier.SetActive(true);
            }
        }
    }
    
    private bool OwnsEyeUpgradesRequiredToUpgradeDemonEye(ItemInstance eyeItemInstance) {
        using var _ = ListPool<EyeUpgrade>.Get(out var upgrades);
        using var __ = ListPool<ItemWithCount>.Get(out var itemsWithCount);
        GetDemonEyeCoreUpgrades(eyeItemInstance, ref upgrades);
        ItemListToUniqueItemsWithCount(upgrades, ref itemsWithCount);
        return OwnsAllItemsOfCounts(itemsWithCount);
    }

    private void UpdateForgeInfoPanel() {
        if (!OnEyeForgeTab || !ShowingForgeDetailsPanel || PlayingForgeAnimation) return;
        
        TextMeshProUGUI panelText = eyeForgeDetailsPanel.panelHeaderText;
        DemonEyeDescList demonEyeDesc = eyeForgeDetailsPanel.demonEyeDesc;
        ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[0].itemInstance;
        
        panelText.text = "Details";
        eyeForgeDetailsPanel.upgradeHeader.SetActive(false);
        
        if (forgeMode == ForgeMode.UpgradingDemonEye) {
            panelText.text = "Upgrading Demon Eye";
            eyeForgeDetailsPanel.upgradeHeader.SetActive(true);
            eyeForgeDetailsPanel.upgradeFromNumeral.sprite = config.styles.RomanNumeralSprite(eyeSlotItemInstance.DemonEyeLevel);
            eyeForgeDetailsPanel.upgradeToNumeral.sprite = config.styles.RomanNumeralSprite(eyeSlotItemInstance.DemonEyeLevel + 1);
        }
        else {
            int eyeForgeInventoryCount = GetInventoryItemCount(inventories.eyeForge);
            int eyeUpgradeCount = forgeError == ForgeError.ForgingButWithoutEye ? eyeForgeInventoryCount : eyeForgeInventoryCount - 1;
            Color textColor = eyeUpgradeCount == Config.demonEyeCoreUpgradeCount ? config.styles.increaseDescColor : config.styles.decreaseDescColor;
            panelText.text = $"Previewing Upgrades {ColorText(eyeUpgradeCount.ToString(), textColor)}/{Config.demonEyeCoreUpgradeCount}";
        }
        
        using var _ = ListPool<int>.Get(out var uuids);
        
        if (forgeMode == ForgeMode.UpgradingDemonEye) {
            int nextUpgradeCount = (eyeSlotItemInstance.DemonEyeLevel + 1) * Config.demonEyeCoreUpgradeCount;
            for (int i = 0; i < nextUpgradeCount; i++) {
                int index = i % Config.demonEyeCoreUpgradeCount;
                uuids.Add(eyeSlotItemInstance.nestedUuids[index]);
            }
        }
        
        if (forgeMode == ForgeMode.Forging) {
            foreach (InventorySlot slot in inventories.eyeForge.slots) {
                if (slot.itemInstance == null || slot.itemInstance.ItemRef.type != itemTypes.eyeUpgrade) continue;
                uuids.Add(slot.itemInstance.itemOrInstanceUuid);
            }
        }
        
        bool showCountsAsIncrease = forgeMode == ForgeMode.UpgradingDemonEye;
        demonEyeDesc.UpdateDisplay(EyeUpgradeSetFromIds(uuids), showCountsAsIncrease);
    }
    
    private void OnForgeButtonPressed() {
        if (PlayingForgeAnimation) return;
        
        HideHint();
        
        if (forgeMode is ForgeMode.PostForgeOrUpgrade) {
            forgeMode = ForgeMode.Empty;
            return;
        }

        ItemInstance eyeItemInstance = inventories.eyeForge.slots[0].itemInstance;
        if (eyeItemInstance == null) return;
        
        ButtonFeel forgeButton = eyeForgePanel.forgeButton;
        forgeButton.KeepPressed();
        forgeButton.text.text = forgeMode is ForgeMode.Forging ? "Crafting..." : "Upgrading...";
        
        PlayAudioClip(audio.startForgingClip);
        
        if (forgeMode is ForgeMode.Forging) {
            DoEyeForgeAnimation(OnEyeForgeAnimationFinished);
            return;
        }
        
        using var _ = ListPool<EyeUpgrade>.Get(out var upgrades);
        GetDemonEyeCoreUpgrades(eyeItemInstance, ref upgrades);
        
        const float startFillingUpgradesDelay = 0.2f;
        const float perUpgradeFillDelay = 0.08f;
        for (int i = 0; i < upgrades.Count; i++) {
            inventories.eyeForge.slots[i + 1].ui.ClearItem();
            Tween.Delay(upgrades[i], startFillingUpgradesDelay + (i * perUpgradeFillDelay), static (eyeUpgrade) => {
                gameInstance.ReduceOwnedCountOfItem(eyeUpgrade);
                Inventory eyeForgeInventory = gameInstance.inventories.eyeForge;
                InventorySlot slot = FindFirstEmptyInventorySlot(eyeForgeInventory);
                slot.itemInstance = new(eyeUpgrade);
                gameInstance.TweenItemMove(slot.ui.itemUI);
            });
        }
        
        const float startForgeDelay = 0.2f;
        float totalStartForgeDelay = startForgeDelay + ((upgrades.Count * perUpgradeFillDelay));
        Tween.Delay(totalStartForgeDelay, static () => gameInstance.DoEyeForgeAnimation(gameInstance.OnEyeForgeAnimationFinished));
    }
    
    private void OnEyeForgeAnimationFinished() {
        const int eyeSlotIndex = 0;
        
        ButtonFeel forgeButton = eyeForgePanel.forgeButton;
        forgeButton.StopKeepPressed();

        ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[eyeSlotIndex].itemInstance;
        using var _ = ListPool<ItemInstance>.Get(out var eyeUpgradeItemInstances);

        foreach (InventorySlot slot in inventories.eyeForge.slots) {
            slot.ui.itemUI.rectTransform.anchoredPosition = Vector2.zero;
            slot.ui.itemUI.rectTransform.localScale = Vector3.one;
            slot.ui.itemUI.forgeEffect.SetMaterialFill(1f);
                
            if (slot.itemInstance == null) continue;
                
            if (slot.ui.AcceptsItemType(itemTypes.eyeUpgrade)) {
                eyeUpgradeItemInstances.Add(slot.itemInstance);
            }
            slot.itemInstance = null;
        }
            
        if (eyeSlotItemInstance.isDemonEye) {
            UpgradeDemonEye(eyeSlotItemInstance, eyeUpgradeItemInstances);
            inventories.eyeForge.slots[eyeSlotIndex].itemInstance = eyeSlotItemInstance;
        }
        else {
            string demonEyeName = randomDemonEyeNames.GetRandom();
            ItemInstance newDemonEye = CreateNewDemonEyeItemInstance(demonEyeName, eyeUpgradeItemInstances);
            inventories.eyeForge.slots[eyeSlotIndex].itemInstance = newDemonEye;
        }
        
        forgeMode = ForgeMode.PostForgeOrUpgrade;
    }
    
    private Sequence eyeForgeSequence;
    private bool PlayingForgeAnimation => eyeForgeSequence.isAlive || eyeForgePanel.forgeButton.beingKeptPressed;
    
    private void DoEyeForgeAnimation(Action onAnimationEndCallback) {
        PlayAudioClip(audio.forgingClip);
        
        const float fillDuration = 5.5f;
        const float perUpgradeExplosionDelay = 0.2f;
        const float perUpgradeDissolveDelay = 0.6f;
        const float popOutDuration = 0.1f;

        float upgradeExplosionsDuration = perUpgradeExplosionDelay * (GetInventoryItemCount(inventories.eyeForge) - 1);
        float totalAnimationDuration = fillDuration + upgradeExplosionsDuration + popOutDuration;

        InventorySlot[] slots = inventories.eyeForge.slots;
        
        bool upgradingDemonEye = slots[0].itemInstance.isDemonEye;
        if (upgradingDemonEye) {
            slots[0].ui.itemUI.forgeEffect.SetIntoSprite(config.demonEyeLevels.levelSprites[1]);
        } else {
            slots[0].ui.itemUI.forgeEffect.SetIntoSprite(config.demonEyeLevels.levelSprites[0]);
        }
        slots[0].ui.itemUI.forgeEffect.UseSmoothing(false);
        
        Tween.Custom(this, 0f, 1f, fillDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.curves.pentagramFill.Evaluate(val));
        });
        
        Tween.Custom(this, 1f, 0f, fillDuration * 0.3f, startDelay: totalAnimationDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.curves.pentagramFill.Evaluate(val));
        });

        {
            Ease ease = Ease.Linear;
            float duration = 2.5f;
            
            Tween.Custom(slots[0], 1f, 0f, fillDuration * 0.85f, ease: Ease.OutCubic, startDelay: perUpgradeDissolveDelay, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.forgeEffect.SetMaterialFill(val);
            });
            
            Tween.Custom(slots[1], 1f, 0f, duration, ease: ease, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.forgeEffect.SetMaterialFill(val);
            });
            
            Tween.Custom(slots[2], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 2, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.forgeEffect.SetMaterialFill(val);
            });
            Tween.Custom(slots[5], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 2, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.forgeEffect.SetMaterialFill(val);
            });
            
            Tween.Custom(slots[3], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 4, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.forgeEffect.SetMaterialFill(val);
            });
            Tween.Custom(slots[4], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 4, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.forgeEffect.SetMaterialFill(val);
            });
        }
        
        for (int i = 0; i < slots.Length; i++) {
            InventorySlot slot = slots[i];
            if (slot.itemInstance == null) continue;

            RectTransform rectTransform = slot.ui.itemUI.rectTransform;

            // Use our own shake because primetween shake's curve does not work
            rectTransform.DoTweenShake(10f, 3.3f, totalAnimationDuration, curves.pentagramItemShake);

            Sequence sequence = Sequence.Create();

            bool isEyeSlot = i == 0;
            if (isEyeSlot) {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 1.42f, new() {
                    duration = fillDuration,
                    ease = Ease.InCubic,
                }));
                
                sequence.ChainDelay(perUpgradeExplosionDelay);
                
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 1.35f, Vector3.one, new() {
                    duration = popOutDuration,
                    ease = Ease.InOutBounce,
                }));
                
                sequence.Group(Tween.Delay(popOutDuration, () => {
                    Entity forgeExplosion = SpawnEntity(entityPools.forgeExplosion, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel.panel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel.panel, Vector3.one * 0.035f, 1f, 12f);
                }));
                
                eyeForgeSequence = sequence;
                eyeForgeSequence.OnComplete(onAnimationEndCallback);
            }
            else {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 0.87f, new() {
                    duration = fillDuration,
                    ease = Ease.InCubic,
                }));
                
                sequence.ChainDelay(perUpgradeExplosionDelay + popOutDuration + 0.025f);
                
                sequence.ChainCallback(() => {
                    Entity dust = SpawnEntity(entityPools.forgeDust, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel.panel);
                    DestroyEntity(dust, CurrentClipLength(dust.animator));
                    Entity fractureParticles = SpawnEntity(entityPools.upgradeFractureParticles, slot.ui.rectTransform.position, Quaternion.identity, ui.mainCanvasRectTransform);
                    DestroyEntity(fractureParticles, 1.1f);
                });
            }
        }
    }
    
    private static int completionPropertyId = Shader.PropertyToID("_Completion");
    private Sequence levelUpPentagramSequence;
    
    private void OnLevelUpPentagramPressed() {
        PlayAudioClip(audio.burnClip);
        
        levelUpPentagramSequence.Complete();
        levelUpPentagramSequence = Sequence.Create();
        
        eyeForgePanel.burnEffectImage.material.SetFloat(completionPropertyId, 0f);
        levelUpPentagramSequence.Group(
            Tween.Custom(eyeForgePanel.burnEffectImage, 0f, 1f, 1.5f, static (image, comp) => {
                image.material.SetFloat(completionPropertyId, comp);
            })
        );
        
        levelUpPentagramSequence.Group(
            Tween.Delay(0.25f, static () => {
                List<ItemWithCount> reqs = gameInstance.config.eyeForgeUpgradePath.pathUpgrades[gameInstance.hideoutState.pentagramLevelIndex].requirements;
                gameInstance.RemoveOwnedItemsFromInventories(reqs);
                gameInstance.hideoutState.pentagramLevelIndex++;
                gameInstance.SaveGameState();
            })
        );
        
        ui.levelUpNotification.Show($"Pentagram Level {hideoutState.pentagramLevelIndex + 2}", delay: 0.62f);
    }
    
    private void OnPentagramForgeTogglePressed() {
        eyeForgePanel.levelUpParent.SetActive(false);
        eyeForgePanel.forgingParent.SetActive(true);
    }
    
    private void OnPentagramLevelUpTogglePressed() {
        eyeForgePanel.levelUpParent.SetActive(true);
        eyeForgePanel.forgingParent.SetActive(false);
    }
    
    private int fillParamProperty = Shader.PropertyToID("_Fill");
    
    private void SetPentagramFill(float value) {
        eyeForgePanel.pentagramFillImage.material.SetFloat(fillParamProperty, value);
    }
    
    // ************************
    // Quests 
    // ************************
    
    private static int scortchedOpacityId = Shader.PropertyToID("_Opacity");
    private static int scortchedAspectId = Shader.PropertyToID("_AspectRatio");
    
    public class QuestPackage {
        public QuestGraphRuntime.Node questNode;
        public QuestUI questUI;
        public ToggleButton questToggleButton;
    }
    
    private void InitQuestPanel() {
        questsPanel.scortchedOverlayImage.material.SetFloat(scortchedOpacityId, 0f);
        
        const int questUiPoolSize = 6;
        for (int i = 0; i < questUiPoolSize; i++) {
            ReleaseQuestPackage(CreateQuestPackage());
        }
        
        HashSet<QuestGraphRuntime.Node> initialQuestNodes = new();
        foreach (QuestGraphRuntime.Node node in quests.graph.rootNode.nextNodes) {
            FindStartingQuestNodes(initialQuestNodes, node);
        }
        
        foreach (QuestGraphRuntime.Node questNode in initialQuestNodes) {
            AddQuestToDisplay(questNode); 
        }
        RefreshQuestDisplays();
    }
    
    private void FindStartingQuestNodes(HashSet<QuestGraphRuntime.Node> nodes, QuestGraphRuntime.Node curNode) {
        bool questHasBeenSubmitted = quests.stateLookupFromUuid[curNode.curQuest.uuid].submitted;
        
        if (!questHasBeenSubmitted) {
            nodes.Add(curNode);
            return;
        }
        
        foreach (QuestGraphRuntime.Node nextNode in curNode.nextNodes) {
            FindStartingQuestNodes(nodes, nextNode);
        }
    }

    public void RefreshQuestDisplays() {
        if (quests.activePkgs.Count <= 0) return;

        if (quests.presentingPkg == null || quests.presentingPkg.questNode == null) {
            quests.presentingPkg = quests.activePkgs[0];
            questsPanel.toggleButtonGroup.ManualyToggle(quests.presentingPkg.questToggleButton);
        }
        
        foreach (QuestPackage questPackage in quests.activePkgs) {
            questPackage.questUI.gameObject.SetActive(false);
        }
        
        quests.presentingPkg.questUI.gameObject.SetActive(true);
        quests.presentingPkg.questUI.Display(quests.presentingPkg.questNode.curQuest);
        questsPanel.toggleButtonGroup.ManualyToggle(quests.presentingPkg.questToggleButton);
    }

    private void AddQuestToDisplay(QuestGraphRuntime.Node questNode) {
        QuestPackage questPackage = GetQuestPackage();
        questPackage.questNode = questNode;
        questPackage.questToggleButton.gameObject.SetActive(true);
        questPackage.questToggleButton.text.text = questNode.curQuest.title;
        quests.activePkgs.Add(questPackage);
    }

    private void RemoveQuestFromDisplay(QuestPackage questPackage) {
        quests.activePkgs.Remove(questPackage);
        ReleaseQuestPackage(questPackage);
    }

    private QuestPackage GetQuestPackage() {
        return quests.reservedPkgs.TryDequeue(out QuestPackage reserved) ? reserved : CreateQuestPackage();
    }
    
    private void ReleaseQuestPackage(QuestPackage package) {
        package.questUI.gameObject.SetActive(false);
        package.questToggleButton.gameObject.SetActive(false);
        package.questUI.completeButton.StopKeepPressed();
        package.questNode = null;
        quests.reservedPkgs.Enqueue(package);
    }
    
    private QuestPackage CreateQuestPackage() {
        QuestUI questUI = Instantiate(prefabs.quest, questsPanel.questsParent).GetComponent<QuestUI>();
        questUI.Init();
        
        ToggleButton toggle = Instantiate(prefabs.questSelectionToggle, questsPanel.questSelectionParent).GetComponent<ToggleButton>();
        questsPanel.toggleButtonGroup.Add(toggle);

        QuestPackage questPackage = new() {
            questNode = null,
            questUI = questUI,
            questToggleButton = toggle,
        };
            
        toggle.button.onClick.AddListener(() => OnQuestToggleClicked(questPackage));
        questUI.completeButton.AddListener(() => OnQuestCompleteClicked(questPackage));
        
        return questPackage;
    }
    
    private void OnQuestToggleClicked(QuestPackage questPackage) {
        quests.presentingPkg = questPackage;
        RefreshQuestDisplays();
    }

    private void OnQuestCompleteClicked(QuestPackage questPackage) {
        PlayAudioClip(audio.burnClip);
        
        QuestGraphRuntime.Node compQuestNode = questPackage.questNode;
        IncreaseTraderRep(compQuestNode.curQuest.traderReputationReward);
        compQuestNode.curQuest.state.submitted = true;
        
        foreach (ObjectiveData obj in questPackage.questNode.curQuest.objectives) {
            bool isFetch = obj.type is QuestObjectiveTypes.FetchByItem or QuestObjectiveTypes.FetchByType;
            if (isFetch && !obj.keepFetchedItems) {
                RemoveNumberOfOwnedItems(obj.targetItem, obj.targetValue);
            }
        }
        
        // Set to null so that we can show the newly unlocked quest, or keep it null
        // if there is none because RefreshQuestDisplays() will handle the null for us
        quests.presentingPkg = null;
        
        if (questPackage.questNode.nextNodes != null) {
            foreach (QuestGraphRuntime.Node nextQuestNode in compQuestNode.nextNodes) {
                Quest nextQuest = nextQuestNode.curQuest;
                if (nextQuest.state.submitted || QuestIsActive(nextQuest)) continue;
                AddQuestToDisplay(nextQuestNode);
                quests.presentingPkg ??= quests.activePkgs[^1];
            }
        }
        
        // Remove from active list so RefreshQuestDisplays() doesn't choose this one.
        quests.activePkgs.Remove(questPackage);
        
        RefreshQuestDisplays();
        SaveGameState();
        
        questPackage.questToggleButton.gameObject.SetActive(false);
        questPackage.questUI.completeButton.KeepPressed();
        
        const float burnTime = 1.4f;
        const float scortchFadeTime = 1.6f;
        const float fadeScortchDelay = 0.6f;

        // Animate the black scortched overlay
        float aspect = questsPanel.scortchedOverlayImage.rectTransform.AspectRatio();
        questsPanel.scortchedOverlayImage.material.SetFloat(scortchedAspectId, aspect);
        questsPanel.scortchedOverlayImage.material.SetFloat(scortchedOpacityId, 1f);
        questsPanel.scortchedOverlayImage.rectTransform.SetAsLastSibling();
        Tween.Custom(1f, 0f, scortchFadeTime, startDelay: fadeScortchDelay, onValueChange: static (comp) => {
            gameInstance.questsPanel.scortchedOverlayImage.material.SetFloat(scortchedOpacityId, comp);
        });
            
        // Burn the quest body
        questPackage.questUI.transform.SetAsLastSibling();
        questPackage.questUI.Burn(burnTime, curves.questBurn, curves.questBurnEmbers);
        
        // When done burning, release the quest package. The shader finishes a little early so we modify the duration.
        Tween.Delay(questPackage, burnTime, static (burningQuestPkg) => {
            gameInstance.RemoveQuestFromDisplay(burningQuestPkg); 
        });
    }
    
    private bool QuestIsActive(Quest quest) {
        foreach (QuestPackage activeQuestPackage in quests.activePkgs) {
            if (quest == activeQuestPackage.questNode.curQuest) {
                return true;
            }
        } 
        return false;
    }
    
    // ************************
    // Leveling Skills
    // ************************

    private void InitSkillsPanel() {
        skillsPanel.panel.hasteSkillRow.Init(skillUpgradePaths.haste.MaxLevel, 
            $"{DisplayProbIncrease(config.gameplay.movementSpeedIncPerLevel)} Movement Speed\n" +
            $"{DisplayProbIncrease(config.gameplay.lootingSpeedIncPerLevel)} Looting Speed\n" +
            $"{DisplayProbIncrease(config.gameplay.firerateIncPerLevel)} Rate of Fire"
        );
        skillsPanel.panel.intellectSkillRow.Init(skillUpgradePaths.intellect.MaxLevel, 
            $"{DisplayProbIncrease(config.gameplay.critChanceIncPerLevel)} Critical Strike Chance\n" +
            $"{DisplayMultiplierIncrease(config.gameplay.critMultiplierIncPerLevel)} Critical Strike Multiplier\n" +
            $"{DisplayIncrease(config.gameplay.projectileCountIncPerLevel)} Projectile Count"
        );
        skillsPanel.panel.lifeBloodSkillRow.Init(skillUpgradePaths.lifeBlood.MaxLevel, 
            $"{DisplayIncrease(config.gameplay.healthIncPerLevel)} Max Health\n" +
            $"{DisplayIncrease(config.gameplay.healingIncOnRaidExitPerLevel)} Healing on Extraction\n" +
            $"{DisplayProbIncrease(config.gameplay.healingSpeedIncPerLevel)} Healing Speed"
        );
        skillsPanel.panel.strengthSkillRow.Init(skillUpgradePaths.strength.MaxLevel, 
            $"{DisplayProbIncrease(config.gameplay.bleedResistIncPerLevel)} Bleed Resist\n" +
            $"{DisplayIncrease(config.gameplay.carryCapacityIncPerLevel)} Carry Capacity\n" +
            $"{DisplayMultiplierIncrease(config.gameplay.damageMultiplierIncPerLevel)} Damage"
        );
    }

    private void OnSkillLevelUpButtonPressed(SkillLevelUpRow skillRow, SkillUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeSkill(upgradePath, playerStatLevel);
        if (result == UpgradeStatResult.CantAfford || result == UpgradeStatResult.AtMaxLevel) return;
        
        thisFrame.flags |= FrameFlags.SkillUpgraded;
        
        skillRow.levelUpButton.KeepPressed();
        string restoreText = skillRow.levelUpButton.text.text;
        skillRow.levelUpButton.text.text = "Leveling...";
        
        const float burnAnimationTime = 1.5f;
        skillRow.Burn(burnAnimationTime, curves.skillBurn, curves.skillBurnEmbers);
        PlayAudioClip(audio.burnClip);
        
        const float delayBeforeUpgradeHappens = burnAnimationTime * 0.23f;
        Tween.Delay(delayBeforeUpgradeHappens, () => 
        {
            player.state.soulCurrency -= upgradePath.soulsNeededPerLevel[playerStatLevel];
            
            if (upgradePath == skillUpgradePaths.haste) {
                player.state.hasteSkillLevel++;
            }
            else if (upgradePath == skillUpgradePaths.intellect) {
                player.state.intellectSkillLevel++;
            }
            else if (upgradePath == skillUpgradePaths.lifeBlood) {
                int prevFullPlayerHealth = FullPlayerHealth();
                player.state.lifeBloodSkillLevel++;
                int newFullPlayerHealth = FullPlayerHealth();
                player.health += newFullPlayerHealth - prevFullPlayerHealth;
            }
            else if (upgradePath == skillUpgradePaths.strength) {
                player.state.strengthSkillLevel++;
            }
        
            SaveGameState();
            RefreshSkillsPanel();
        });
        
        const float delayBetweenRapidUpgrades = burnAnimationTime * 0.65f;
        Tween.Delay(delayBetweenRapidUpgrades, () => 
        {
            skillRow.levelUpButton.StopKeepPressed();
            skillRow.levelUpButton.text.text = restoreText;
        });
    }
    
    private void RefreshSkillsPanel() {
        PlayerStatsPanel pStats = skillsPanel.playerStatsPanel;
        SkillsPanel skills = skillsPanel.panel;
        
        pStats.carryCapacityRow.statValueText.text = ((int)GetPlayerStat(PlayerStat.CarryCapacity)).ToString();
        pStats.critChanceRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.CritChance));
        pStats.critMultiRow.statValueText.text = DisplayMultiplierNoColor(GetPlayerStat(PlayerStat.CritMulti));
        pStats.damageRow.statValueText.text = DisplayMultiplierNoColor(GetPlayerStat(PlayerStat.DamageMulti));
        pStats.firerateRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.FireratePercentage));
        pStats.healthRow.statValueText.text = ((int)(GetPlayerStat(PlayerStat.Health))).ToString();
        pStats.healingOnRaidExitRow.statValueText.text = ((int)(GetPlayerStat(PlayerStat.HealingOnRaidExit))).ToString();
        pStats.healingSpeedRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.HealingSpeed));
        pStats.lootingSpeedRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.LootingSpeed));
        pStats.movementSpeedRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.MovementSpeedPercentage));
        pStats.projectileCountRow.statValueText.text = DisplayNumberNoColor(GetPlayerStat(PlayerStat.ProjectileCount));
        
        RefreshSkillRow(skills.hasteSkillRow, skillUpgradePaths.haste, player.state.hasteSkillLevel);
        RefreshSkillRow(skills.intellectSkillRow, skillUpgradePaths.intellect, player.state.intellectSkillLevel);
        RefreshSkillRow(skills.lifeBloodSkillRow, skillUpgradePaths.lifeBlood, player.state.lifeBloodSkillLevel);
        RefreshSkillRow(skills.strengthSkillRow, skillUpgradePaths.strength, player.state.strengthSkillLevel);
    }

    private void RefreshSkillRow(SkillLevelUpRow skillLevelRow, SkillUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeSkill(upgradePath, playerStatLevel);
        if (result == UpgradeStatResult.AtMaxLevel) return;
        
        int soulsRequired = upgradePath.soulsNeededPerLevel[playerStatLevel];
        bool enableButton = result == UpgradeStatResult.Affordable;
        skillLevelRow.Refresh(playerStatLevel, upgradePath.MaxLevel, soulsRequired, enableButton);
    }

    private enum UpgradeStatResult { CantAfford, Affordable, AtMaxLevel }
    
    private UpgradeStatResult CanUpgradeSkill(SkillUpgradePath upgradePath, int playerSkillLevel) {
        if (!upgradePath.soulsNeededPerLevel.IndexInRange(playerSkillLevel)) {
            return UpgradeStatResult.AtMaxLevel;
        }
        if (player.state.soulCurrency >= upgradePath.soulsNeededPerLevel[playerSkillLevel]) {
            return UpgradeStatResult.Affordable;    
        }
        return UpgradeStatResult.CantAfford;
    }
    
    public static string[] traderGreetings = {
        "Can you actually buy something this time?",
        "Can I interest you in a pyramid scheme?",
        "Nice to see you haven't given up yet.",
        "Are you finished with those quests yet?",
        "You're becoming my favorite client. Only 174% markup for you!",
        "I have made investments that might ruin me financially.",
        "Chop chop! I got a nap scheduled in 10 minutes.",
    };
    
    public static string[] postTutorialTraderGreetings = {
        "If you need more money you can always load up on loot at the Starting Grounds and sell it all.",
        "Have you read my book yet? My other clients say they highly recommend it.",
    };
    
}