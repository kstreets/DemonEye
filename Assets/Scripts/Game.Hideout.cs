using System;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    private bool OnCharacterTab => hideoutTabs.characterButton.image.sprite == hideoutTabs.selectedSprite;
    private bool OnEyeForgeTab => hideoutTabs.eyeForgeButton.image.sprite == hideoutTabs.selectedSprite;
    private bool OnTradingTab => hideoutTabs.traderButton.image.sprite == hideoutTabs.selectedSprite;
    
    private void InitHideout() {
        InitTrader();
        InitQuests();
        InitSkillsPanel();
        SetPentagramFill(0f);
    }
    
    // ************************
    // Trader
    // ************************
    
    private const float traderItemRefreshTime = 480f;
    
    [Serializable]
    public class TradersSaveData {
        public int traderRep;
        public float refreshItemsTime;
    }
    
    private TradersSaveData traderSaveData;

    private void SaveTrader() {
        SaveToFile(savePaths.trader, traderSaveData);
        SaveInventory(inventories.trader);
    }

    private void InitTrader() {
        traderSaveData = LoadFromFileOrCreateNew<TradersSaveData>(savePaths.trader);
        MarkTraderItemsAsTraderOwned();
        CalculateAndSetTraderRepBars();
    }

    private void UpdateTrader() {
        traderSaveData.refreshItemsTime -= Time.deltaTime;
        
        if (OnTradingTab) {
            traderPanel.itemRefreshTimeText.text = $"Items Refresh In: {GetCountdownText(traderSaveData.refreshItemsTime)}";
        }
        
        if (traderSaveData.refreshItemsTime <= 0f) {
            SetTradingItem(null);
            FillTraderInventoryWithItems();
            traderSaveData.refreshItemsTime = traderItemRefreshTime;
            SaveTrader();
        }
    }
    
    private void IncreaseTraderRep(int repGain) {
        if (ReachedTraderMaxRep()) return;

        int prevLevel = GetTraderRepLevel();
        traderSaveData.traderRep += repGain;
        SaveTrader();
        int repLevel = GetTraderRepLevel();
        bool increasedLevel = prevLevel < repLevel;
        
        if (increasedLevel) {
            FillTraderInventoryWithItems();
        }
        CalculateAndSetTraderRepBars();
    }

    private void CalculateAndSetTraderRepBars() {
        int levelIndex = GetTraderRepLevel();
        
        if (ReachedTraderMaxRep()) {
            SetTraderRepBarViewAsMaxLevel(traderPanel.repBar, levelIndex);
            SetTraderRepBarViewAsMaxLevel(questsPanel.traderRepBar, levelIndex);
            return;
        }
        
        int prefixedSumAtCurLevel = config.traderLevels.prefixedSumRepForLevel[levelIndex];
        int prefixedSumAtPrevLevel = config.traderLevels.prefixedSumRepForLevel[levelIndex - 1];
        int repNeededForThisLevel = prefixedSumAtCurLevel - prefixedSumAtPrevLevel;

        int traderRep = traderSaveData.traderRep;
        int repCompletedAtCurLevel = traderRep - prefixedSumAtPrevLevel;
        int repLeftToGo = prefixedSumAtCurLevel - traderRep;
        float fill = repCompletedAtCurLevel / (float)repNeededForThisLevel;
        
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

    private int GetTraderRepLevel() {
        int rep = traderSaveData.traderRep;
        for (int i = 0; i < config.traderLevels.prefixedSumRepForLevel.Length; i++) {
            if (rep < config.traderLevels.prefixedSumRepForLevel[i]) {
                return i;
            }
        }
        return config.traderLevels.prefixedSumRepForLevel.Length;
    }

    private void FillTraderInventoryWithItems() {
        ClearInventory(inventories.trader);
        int curTraderLevel = GetTraderRepLevel();
        
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
        
        using var _ = ListPool<Item>.Get(out List<Item> items);
        GetUniqueItemsFromDropPool(dropPools.trader, traderInventoryColCount * traderInventoryRowCount, ref items);
        items = items.OrderBy(x => x.type.name).ThenBy(x => x.GetRarity()).ThenBy(x => x.buyPrice).ToList();
        
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
    
    private bool ReachedTraderMaxRep() {
        int rep = traderSaveData.traderRep;
        return rep >= config.traderLevels.prefixedSumRepForLevel[^1];
    }
    
    private ItemInstance curTradingItemInstance;
    
    private void SetTradingItem(ItemInstance itemInstance) {
        curTradingItemInstance = itemInstance;
        transactionPanel.transaction.UpdateBuyItem(itemInstance);
        if (curTradingItemInstance != null && transactionState == TransactionState.Selling) {
            transactionPanel.transaction.toggleGroup.ManualyToggle(transactionPanel.transaction.buyToggle);
        }
    }

    private void ReduceTradingItemStock() {
        bool reducedToNothing = ReduceItemCountInInventory(inventories.trader, curTradingItemInstance.traderSlotIndex);
        if (reducedToNothing) {
            SetTradingItem(null);
        }
    }

    private enum TransactionState { Selling, Buying }
    private TransactionState transactionState;
    
    private void OnBuyTogglePressed() {
        transactionState = TransactionState.Buying;
        transactionPanel.inventoryParent.gameObject.SetActive(false);
        
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
        SetTradingItem(null);
    }
    
    private void OnSellButtonPressed() {
        if (transactionState == TransactionState.Selling && GetInventoryItemCount(inventories.transaction) <= 0) return;
        int sellPrice = GetInventoryValue(inventories.transaction, InventoryValueType.Sell);
        // Before selling items we pass the transaction inventory to callbacks that want to know what we sold
        onSoldItemsToTrader?.Invoke(inventories.transaction.slots); 
        player.coinCurrency += sellPrice;
        ClearInventory(inventories.transaction);
    }
    
    private void OnMoneyPurchaseButtonPressed() {
        if (transactionState == TransactionState.Buying && curTradingItemInstance == null) return;
        int buyPrice = curTradingItemInstance.ItemRef.buyPrice;
        if (player.coinCurrency >= buyPrice) {
            player.coinCurrency -= buyPrice;
            TryAddItemToInventory(inventories.stash, curTradingItemInstance.ItemRef, 1);
            ReduceTradingItemStock();
            // After buying items we just make sure all items in stash are no longer trader owned
            ClearItemsAsTraderOwned(inventories.stash);
        }
    }
    
    private void OnBarterPurchaseButtonPressed() {
        if (curTradingItemInstance == null) return;

        foreach (ItemWithCount barterReq in curTradingItemInstance.ItemRef.traderSpawning.barterRequirements) {
            if (GetOwnedCountOfItem(barterReq.item) < barterReq.count) return;
        }
            
        foreach (ItemWithCount barterReq in curTradingItemInstance.ItemRef.traderSpawning.barterRequirements) {
            int removedCount = RemoveNumberOfItemsFromInventory(inventories.stash, barterReq.item, barterReq.count);
            if (removedCount != barterReq.count) {
                int additionalRemoveCount = barterReq.count - removedCount;
                RemoveNumberOfItemsFromInventory(inventories.player, barterReq.item, additionalRemoveCount);
            }
        }

        TryAddItemToInventory(inventories.stash, curTradingItemInstance.ItemRef, 1);
        ReduceTradingItemStock();
    }
    
    private void RefreshTransactionUI() {
        if (!OnTradingTab) return;
        
        if (transactionState == TransactionState.Buying) {
            transactionPanel.transaction.UpdateBuyItem(curTradingItemInstance);
            transactionPanel.transaction.toggleGroup.ManualyToggleCosmetically(transactionPanel.transaction.buyToggle);
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(inventories.transaction, InventoryValueType.Sell);
            transactionPanel.transaction.UpdateSellPrice(sellPrice);
            transactionPanel.transaction.toggleGroup.ManualyToggleCosmetically(transactionPanel.transaction.sellToggle);
        }
    }
    
    // ************************
    // Eye Forge 
    // ************************
    
    private enum CrucibleMode { Empty, Forging, ForgingButJustEye, ForgingButWithoutEye, NeedToRemoveDemonEye }
    private CrucibleMode crucibleMode;

    private void UpdateForgeState() {
        if (!OnEyeForgeTab) return;

        int crucibleItemCount = GetInventoryItemCount(inventories.eyeForge);
        ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[0].itemInstance;

        if (crucibleItemCount <= 0) {
            crucibleMode = CrucibleMode.Empty;
        }
        else if (eyeSlotItemInstance != null && eyeSlotItemInstance.ItemRef.type == itemTypes.demonEye) {
            crucibleMode = CrucibleMode.NeedToRemoveDemonEye;
        }
        else if (eyeSlotItemInstance != null && crucibleItemCount == 1) {
            crucibleMode = CrucibleMode.ForgingButJustEye;
        }
        else if (eyeSlotItemInstance == null) {
            crucibleMode = CrucibleMode.ForgingButWithoutEye;
        }
        else {
            crucibleMode = CrucibleMode.Forging;
        }
    }
    
    private void UpdateForgePanel() {
        if (!OnEyeForgeTab) return;
        
        ButtonFeel forgeButton = eyeForgePanel.forgeButton;
        bool forging = crucibleMode is CrucibleMode.Forging;
        if (forging && forgeButton.isDisabled) {
            forgeButton.Enable();
        }
        else if (!forging && !forgeButton.isDisabled) {
            forgeButton.Disable();
        }
    }

    private void UpdateForgeInfoPanel() {
        if (!OnEyeForgeTab || PlayingForgeAnimation) return;
        
        TextMeshProUGUI detailsText = eyeForgeDetailsPanel.text;
        DemonEyeDescList demonEyeDesc = eyeForgeDetailsPanel.demonEyeDesc;
        
        if (crucibleMode == CrucibleMode.Empty) {
            detailsText.text = "Place an eyeball in the center to start the Demon Eye forging process";
            demonEyeDesc.HideAllElements();
        }
        else if (crucibleMode == CrucibleMode.ForgingButJustEye) {
            detailsText.text = $"Requires at least {DisplayNumber(1)} eye upgrade to forge a Demon Eye";
            demonEyeDesc.HideAllElements();
        }
        else {
            if (crucibleMode == CrucibleMode.ForgingButWithoutEye) {
                detailsText.text = "Missing eyeball in the center";
            }
            else {
                int eyeUpgradeCount = GetInventoryItemCount(inventories.eyeForge) - 1;
                int totalUpgradeCount = inventories.eyeForge.slots.Length - 1;
                detailsText.text = $"Previewing Upgrades {ColorText(eyeUpgradeCount.ToString(), Styles.instance.timeDescColor)}/{totalUpgradeCount}";
            }
            
            using var _ = ListPool<int>.Get(out var uuids);
            foreach (InventorySlot slot in inventories.eyeForge.slots) {
                if (slot.itemInstance == null || slot.itemInstance.ItemRef.type != itemTypes.eyeUpgrade) continue;
                uuids.Add(slot.itemInstance.itemOrInstanceUuid);
            }
            demonEyeDesc.UpdateDisplay(EyeUpgradeSetFromIds(uuids));
        }
    }
    
    private void OnForgeButtonPressed() {
        if (PlayingForgeAnimation) return;
        
        int eyeSlotIndex = 0;
        ItemInstance eyeItemInstance = null;

        for (int i = 0; i < inventories.eyeForge.slots.Length; i++) {
            InventorySlot slot = inventories.eyeForge.slots[i];
            if (slot.ui.OnlyAcceptsType(itemTypes.eye)) {
                eyeItemInstance = slot.itemInstance;
                eyeSlotIndex = i;
            }
        }

        if (eyeItemInstance == null) return;

        for (int i = 0; i < inventories.eyeForge.slots.Length; i++) {
            if (i == eyeSlotIndex) continue;
            if (inventories.eyeForge.slots[i].itemInstance != null) break;
            if (i == inventories.eyeForge.slots.Length - 1) return;
        }

        ButtonFeel forgeButton = eyeForgePanel.forgeButton;
        toggledOffHoverableUIElement = forgeButton.rectTransform;
        
        forgeButton.KeepPressed();
        string prevButtonText = forgeButton.text.text;
        forgeButton.text.text = "Forging...";
        
        DoEyeForgeAnimation(onAnimationEndCallback: () => {
            forgeButton.StopKeepPressed();
            forgeButton.text.text = prevButtonText;
            
            using var _ = ListPool<ItemInstance>.Get(out var eyeUpgradeItemInstances);

            foreach (InventorySlot slot in inventories.eyeForge.slots) {
                slot.ui.itemUI.rectTransform.anchoredPosition = Vector2.zero;
                slot.ui.itemUI.rectTransform.localScale = Vector3.one;
                
                if (slot.itemInstance == null) continue;
                
                if (slot.ui.OnlyAcceptsType(itemTypes.eyeUpgrade)) {
                    eyeUpgradeItemInstances.Add(slot.itemInstance);
                }
                slot.itemInstance = null;
            }
            
            ItemInstance newDemonEye = CreateNewDemonEyeItemInstance(eyeUpgradeItemInstances);
            inventories.eyeForge.slots[eyeSlotIndex].itemInstance = newDemonEye;
        });
    }
    
    private Sequence eyeForgeSequence;
    private bool PlayingForgeAnimation => eyeForgeSequence.isAlive;
    
    private void DoEyeForgeAnimation(Action onAnimationEndCallback) {
        const float fillDuration = 4.5f;
        const float perUpgradeExplosionDelay = 0.15f;
        const float popOutDuration = 0.15f;

        float upgradeExplosionsDuration = perUpgradeExplosionDelay * (GetInventoryItemCount(inventories.eyeForge) - 1);
        float totalAnimationDuration = fillDuration + upgradeExplosionsDuration + popOutDuration;

        Tween.Custom(this, 0f, 1f, fillDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.curves.pentagramFill.Evaluate(val));
        });
        
        Tween.Custom(this, 1f, 0f, fillDuration * 0.3f, startDelay: totalAnimationDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.curves.pentagramFill.Evaluate(val));
        });

        for (int i = 0; i < inventories.eyeForge.slots.Length; i++) {
            InventorySlot slot = inventories.eyeForge.slots[i];
            if (slot.itemInstance == null) continue;

            RectTransform rectTransform = slot.ui.itemUI.rectTransform;

            // Use our own shake because prime tween shake's curve does not work
            rectTransform.DoTweenShake(10f, 3.3f, totalAnimationDuration, curves.pentagramItemShake);

            Sequence sequence = Sequence.Create();

            if (slot.itemInstance.ItemRef.type == itemTypes.eye) {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 1.25f, new() {
                    duration = fillDuration,
                    ease = Ease.InCubic,
                }));
                
                sequence.ChainDelay(upgradeExplosionsDuration + perUpgradeExplosionDelay);
                
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 1.35f, Vector3.one, new() {
                    duration = popOutDuration,
                    ease = Ease.InOutBounce,
                }));
                
                sequence.Group(Tween.Delay(0f, () => {
                    Entity forgeExplosion = SpawnEntity(entityPools.forgeExplosion, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel.panel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel.panel, Vector3.one * 0.05f, 1f, 15f);
                }));
                
                eyeForgeSequence = sequence;
                eyeForgeSequence.OnComplete(onAnimationEndCallback);
            }
            else {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 0.87f, new() {
                    duration = fillDuration,
                    ease = Ease.InCubic,
                }));
                
                sequence.ChainDelay(perUpgradeExplosionDelay * i);
                
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 0.87f, Vector3.zero, new() {
                    duration = popOutDuration,
                    ease = Ease.InBounce,
                }));
                
                sequence.Group(Tween.Delay(popOutDuration / 2f, () => {
                    Entity forgeExplosion = SpawnEntity(entityPools.forgeExplosion, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel.panel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel.panel, Vector3.one * 0.04f, 0.15f, 15f);
                }));
            }
        }
    }
    
    private int fillParamProperty = Shader.PropertyToID("_Fill");
    
    private void SetPentagramFill(float value) {
        eyeForgePanel.pentagramFillImage.material.SetFloat(fillParamProperty, value);
    }
    
    // ************************
    // Quests 
    // ************************
    
    private class QuestPackage {
        public QuestGraphRuntime.Node questNode;
        public QuestUI questUI;
        public ToggleButton questToggleButton;
    }

    private Queue<QuestPackage> reservedQuestPackages = new();
    private List<QuestPackage> activeQuestPackages = new();
    private QuestPackage presentingQuestPackage;

    [Serializable]
    private class QuestSaveData {
        public Quest.ProgressSave[] progressSaves;
        public bool[] submissionStates;
    }
    
    private QuestSaveData questSaveData;
    
    private void SaveActiveQuestProgresses() {
        foreach (QuestPackage questPackage in activeQuestPackages) {
            QuestGraphRuntime.Node node = questPackage.questNode;
            questSaveData.progressSaves[node.saveIndex] = node.curQuest.GetProgressSave();
        }
        SaveToFile(savePaths.quest, questSaveData);
    }

    private void SaveAndMarkQuestAsSubmitted(QuestGraphRuntime.Node questNode) {
        questSaveData.submissionStates[questNode.saveIndex] = true;
        questSaveData.progressSaves[questNode.saveIndex] = questNode.curQuest.GetProgressSave();
        SaveToFile(savePaths.quest, questSaveData);
    }

    private void InitQuests() {
        questSaveData = LoadFromFile<QuestSaveData>(savePaths.quest);
        
        if (questSaveData == null) {
            questSaveData = new() {
                progressSaves = new Quest.ProgressSave[quests.graph.questCount],
                submissionStates = new bool[quests.graph.questCount],
            };
            questSaveData.progressSaves.InitalizeWithDefault();
            SaveToFile(savePaths.quest, questSaveData);
        }
        
        HashSet<QuestGraphRuntime.Node> initialQuestNodes = new();
        foreach (QuestGraphRuntime.Node node in quests.graph.rootNode.nextNodes) {
            FindStartingQuestNodes(initialQuestNodes, node);
        }
        
        const int questUiPoolSize = 6;
        for (int i = 0; i < questUiPoolSize; i++) {
            ReleaseQuestPackage(CreateQuestPackage());
        }
        
        foreach (QuestGraphRuntime.Node questNode in initialQuestNodes) {
            Quest.ProgressSave progressSave = questSaveData.progressSaves[questNode.saveIndex];
            questNode.curQuest.LoadProgressSave(progressSave);
            ActivateQuest(questNode); 
        }
        
        RefreshQuestDisplays();
    }

    private void UpdateQuests() {
        foreach (QuestPackage questPackage in activeQuestPackages) {
            questPackage.questNode.curQuest.Update();
        }
    }
    
    private void FindStartingQuestNodes(HashSet<QuestGraphRuntime.Node> nodes, QuestGraphRuntime.Node curNode) {
        bool questHasBeenSubmitted = questSaveData.submissionStates[curNode.saveIndex];
        
        if (!questHasBeenSubmitted) {
            nodes.Add(curNode);
            return;
        }
        
        foreach (QuestGraphRuntime.Node nextNode in curNode.nextNodes) {
            FindStartingQuestNodes(nodes, nextNode);
        }
    }

    public void RefreshQuestDisplays() {
        if (activeQuestPackages.Count <= 0) return;

        if (presentingQuestPackage == null || presentingQuestPackage.questNode == null) {
            presentingQuestPackage = activeQuestPackages[0];
            questsPanel.toggleButtonGroup.ManualyToggle(presentingQuestPackage.questToggleButton);
        }
        
        foreach (QuestPackage questPackage in activeQuestPackages) {
            questPackage.questUI.gameObject.SetActive(false);
        }
        
        presentingQuestPackage.questUI.gameObject.SetActive(true);
        presentingQuestPackage.questUI.Display(presentingQuestPackage.questNode.curQuest);
    }

    private void ActivateQuest(QuestGraphRuntime.Node questNode) {
        questNode.curQuest.Init();
        QuestPackage questPackage = GetQuestPackage();
        questPackage.questNode = questNode;
        questPackage.questToggleButton.gameObject.SetActive(true);
        questPackage.questToggleButton.text.text = questNode.curQuest.title;
        activeQuestPackages.Add(questPackage);
    }

    private void DeactivateQuest(QuestPackage questPackage) {
        questPackage.questNode.curQuest.Deinit();
        activeQuestPackages.Remove(questPackage);
        ReleaseQuestPackage(questPackage);
    }

    private QuestPackage GetQuestPackage() {
        return reservedQuestPackages.TryDequeue(out QuestPackage reserved) ? reserved : CreateQuestPackage();
    }
    
    private void ReleaseQuestPackage(QuestPackage package) {
        package.questUI.gameObject.SetActive(false);
        package.questToggleButton.gameObject.SetActive(false);
        package.questNode = null;
        reservedQuestPackages.Enqueue(package);
    }
    
    private QuestPackage CreateQuestPackage() {
        QuestUI ui = Instantiate(prefabs.quest, questsPanel.questsParent).GetComponent<QuestUI>();
        
        ToggleButton toggle = Instantiate(prefabs.questSelectionToggle, questsPanel.questSelectionParent).GetComponent<ToggleButton>();
        questsPanel.toggleButtonGroup.Add(toggle);
        
        QuestPackage questPackage = new() {
            questNode = null,
            questUI = ui,
            questToggleButton = toggle,
        };
            
        toggle.button.onClick.AddListener(() => OnQuestToggleClicked(questPackage));
        ui.completeButton.AddListener(() => OnQuestCompleteClicked(questPackage));
        
        return questPackage;
    }
    
    private void OnQuestToggleClicked(QuestPackage questPackage) {
        presentingQuestPackage = questPackage;
        RefreshQuestDisplays();
    }

    private void OnQuestCompleteClicked(QuestPackage questPackage) {
        QuestGraphRuntime.Node compQuestNode = questPackage.questNode;
        IncreaseTraderRep(compQuestNode.curQuest.traderReputationReward);
        SaveAndMarkQuestAsSubmitted(compQuestNode);
        
        foreach (Quest.Objective objective in questPackage.questNode.curQuest.objectives) {
            if (objective.type == Quest.Objective.Type.Fetch && !objective.keepFetchedItems) {
                RemoveNumberOfOwnedItems(objective.targetItem, objective.targetValue);
                SaveInventory(inventories.player);
                SaveInventory(inventories.stash);
            }    
        }
        
        if (questPackage.questNode.nextNodes != null) {
            foreach (QuestGraphRuntime.Node nextQuestNode in compQuestNode.nextNodes) {
                bool questHasBeenSubmitted = questSaveData.submissionStates[nextQuestNode.saveIndex];
                if (questHasBeenSubmitted || QuestIsActive(nextQuestNode.curQuest)) continue;
                ActivateQuest(nextQuestNode);
            }
        }
        
        DeactivateQuest(questPackage); 
        RefreshQuestDisplays();
    }
    
    private bool QuestIsActive(Quest quest) {
        foreach (QuestPackage activeQuestPackage in activeQuestPackages) {
            if (quest == activeQuestPackage.questNode.curQuest && !quest.IsComplete()) {
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
            $"{DisplayProbIncrease(config.gameplay.firerateIncPerLevel)} Firerate"
        );
        skillsPanel.panel.intellectSkillRow.Init(skillUpgradePaths.intellect.MaxLevel, 
            $"{DisplayProbIncrease(config.gameplay.critChanceIncPerLevel)} Critical Strike Chance\n" +
            $"{DisplayMultiplierIncrease(config.gameplay.critMultiplierIncPerLevel)} Critical Strike Multiplier\n" +
            $"{DisplayIncrease(config.gameplay.projectileCountIncPerLevel)} Projectile Count"
        );
        skillsPanel.panel.lifeBloodSkillRow.Init(skillUpgradePaths.lifeBlood.MaxLevel, 
            $"{DisplayIncrease(config.gameplay.healthIncPerLevel)} Health\n" +
            $"{DisplayProbIncrease(config.gameplay.healingSpeedIncPerLevel)} Healing Speed\n" +
            $"{DisplayProbIncrease(config.gameplay.healingIncPerLevel)} Healing Amount"
        );
        skillsPanel.panel.strengthSkillRow.Init(skillUpgradePaths.strength.MaxLevel, 
            $"{DisplayProbIncrease(config.gameplay.bleedResistIncPerLevel)} Bleed Resist\n" +
            $"{DisplayIncrease(config.gameplay.carryCapacityIncPerLevel)} Carry Capacity\n" +
            $"{DisplayMultiplierIncrease(config.gameplay.damageMultiplierIncPerLevel)} Damage"
        );
    }

    private void OnLevelupButtonPressed(SkillUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeSkill(upgradePath, playerStatLevel);
        if (result == UpgradeStatResult.CantAfford || result == UpgradeStatResult.AtMaxLevel) return;
        
        player.soulCurrency -= upgradePath.soulsNeededPerLevel[playerStatLevel];

        if (upgradePath == skillUpgradePaths.haste) {
            player.hasteSkillLevel++;
        }
        else if (upgradePath == skillUpgradePaths.intellect) {
            player.intellectSkillLevel++;
        }
        else if (upgradePath == skillUpgradePaths.lifeBlood) {
            int prevFullPlayerHealth = FullPlayerHealth();
            player.lifeBloodSkillLevel++;
            int newFullPlayerHealth = FullPlayerHealth();
            player.health += newFullPlayerHealth - prevFullPlayerHealth;
        }
        else if (upgradePath == skillUpgradePaths.strength) {
            player.strengthSkillLevel++;
        }
        
        SavePlayerData();
        RefreshSkillsPanel();
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
        pStats.healingAmountRow.statValueText.text = DisplayIncrease(GetPlayerStatAdjustment(PlayerStat.HealingAmount));
        pStats.healingSpeedRow.statValueText.text = DisplayProbIncrease(GetPlayerStatAdjustment(PlayerStat.HealingSpeed));
        pStats.lootingSpeedRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.LootingSpeed));
        pStats.movementSpeedRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(PlayerStat.MovementSpeedPercentage));
        pStats.projectileCountRow.statValueText.text = DisplayNumberNoColor(GetPlayerStat(PlayerStat.ProjectileCount));
        
        RefreshSkillRow(skills.hasteSkillRow, skillUpgradePaths.haste, player.hasteSkillLevel);
        RefreshSkillRow(skills.intellectSkillRow, skillUpgradePaths.intellect, player.intellectSkillLevel);
        RefreshSkillRow(skills.lifeBloodSkillRow, skillUpgradePaths.lifeBlood, player.lifeBloodSkillLevel);
        RefreshSkillRow(skills.strengthSkillRow, skillUpgradePaths.strength, player.strengthSkillLevel);
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
        if (player.soulCurrency >= upgradePath.soulsNeededPerLevel[playerSkillLevel]) {
            return UpgradeStatResult.Affordable;    
        }
        return UpgradeStatResult.CantAfford;
    }
    
}