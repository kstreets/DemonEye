using System;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    private bool OnCharacterTab => characterTabButton.image.sprite == tabSelectedSprite;
    private bool OnEyeForgeTab => eyeForgeTabButton.image.sprite == tabSelectedSprite;
    private bool OnTradingTab => traderTabButton.image.sprite == tabSelectedSprite;
    
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
        SaveToFile(traderSavePath, traderSaveData);
        SaveInventory(traderInventory);
    }

    private void InitTrader() {
        traderSaveData = LoadFromFileOrCreateNew<TradersSaveData>(traderSavePath);
        MarkTraderItemsAsTraderOwned();
        SetTraderRepBar();
    }

    private void UpdateTrader() {
        traderSaveData.refreshItemsTime -= Time.deltaTime;
        
        if (OnTradingTab) {
            traderItemRefreshTimeText.text = $"Items Refresh In: {GetCountdownText(traderSaveData.refreshItemsTime)}";
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
        SetTraderRepBar();
    }

    private void SetTraderRepBar() {
        int levelIndex = GetTraderRepLevel();
        
        if (ReachedTraderMaxRep()) {
            traderXpLevelFill.fillAmount = 1f;
            traderRemainingXpText.text = string.Empty;
            traderLevelText.text = $"Level {levelIndex} (Max)";
            return;
        }
        
        int prefixedSumAtCurLevel = traderLevels.prefixedSumRepForLevel[levelIndex];
        int prefixedSumAtPrevLevel = traderLevels.prefixedSumRepForLevel[levelIndex - 1];
        int repNeededForThisLevel = prefixedSumAtCurLevel - prefixedSumAtPrevLevel;

        int traderRep = traderSaveData.traderRep;
        int repCompletedAtCurLevel = traderRep - prefixedSumAtPrevLevel;
        int repLeftToGo = prefixedSumAtCurLevel - traderRep;
        
        traderXpLevelFill.fillAmount = repCompletedAtCurLevel / (float)repNeededForThisLevel;
        traderRemainingXpText.text = $"{repLeftToGo} Rep Left";
        traderLevelText.text = $"Level {levelIndex}";
    }

    private int GetTraderRepLevel() {
        int rep = traderSaveData.traderRep;
        for (int i = 0; i < traderLevels.prefixedSumRepForLevel.Length; i++) {
            if (rep < traderLevels.prefixedSumRepForLevel[i]) {
                return i;
            }
        }
        return traderLevels.prefixedSumRepForLevel.Length;
    }

    private void FillTraderInventoryWithItems() {
        ClearInventory(traderInventory);
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
        GetUniqueItemsFromDropPool(traderDropPool, traderInventoryColCount * traderInventoryRowCount, items, raritySkew);
        items = items.OrderBy(x => x.type.name).ThenBy(x => x.GetRarity()).ThenBy(x => x.buyPrice).ToList();
        
        foreach (Item item in items) {
            if (item.traderLevelRequired > curTraderLevel) continue;
            
            int lowerRange = item.traderStockRange.x;
            int maxUpperRange = item.traderStockRange.y;
            int weightedUpperRange = lowerRange + ((maxUpperRange - lowerRange) / 2);
            while (weightedUpperRange < maxUpperRange && RollProbability(stockCountSkew)) {
                weightedUpperRange++;
            }
            int stackCount = Random.Range(lowerRange, weightedUpperRange); 
            TryAddItemToInventory(traderInventory, item, stackCount);
        }
        
        MarkTraderItemsAsTraderOwned();
    }
    
    private void MarkTraderItemsAsTraderOwned() {
        for (int i = 0; i < traderInventory.slots.Length; i++) {
            InventorySlot slot = traderInventory.slots[i];
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
        return rep >= traderLevels.prefixedSumRepForLevel[^1];
    }
    
    private ItemInstance curTradingItemInstance;
    
    private void SetTradingItem(ItemInstance itemInstance) {
        curTradingItemInstance = itemInstance;
        transactionPanel.UpdateBuyItem(itemInstance);
        if (curTradingItemInstance != null && transactionState == TransactionState.Selling) {
            transactionPanel.toggleGroup.ManualyToggle(transactionPanel.buyToggle);
        }
    }

    private void ReduceTradingItemStock() {
        bool reducedToNothing = ReduceItemCountInInventory(traderInventory, curTradingItemInstance.traderSlotIndex);
        if (reducedToNothing) {
            SetTradingItem(null);
        }
    }

    private enum TransactionState { Selling, Buying }
    private TransactionState transactionState;
    
    private void OnBuyTogglePressed() {
        transactionState = TransactionState.Buying;
        traderTransactionInventoryParent.gameObject.SetActive(false);
        // Move any selling items back to stash
        foreach (InventorySlot slot in transactionInventory.slots) {
            if (slot.itemInstance == null) continue;
            TryAddItemToInventory(stashInventory, slot.itemInstance);
        }
        ClearInventory(transactionInventory);
    }
    
    private void OnSellTogglePressed() {
        transactionState = TransactionState.Selling;
        traderTransactionInventoryParent.gameObject.SetActive(true);
        SetTradingItem(null);
    }
    
    private void OnSellButtonPressed() {
        if (transactionState == TransactionState.Selling && GetInventoryItemCount(transactionInventory) <= 0) return;
        int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
        // Before selling items we pass the transaction inventory to callbacks that want to know what we sold
        onSoldItemsToTrader?.Invoke(transactionInventory.slots); 
        player.coinCurrency += sellPrice;
        ClearInventory(transactionInventory);
    }
    
    private void OnMoneyPurchaseButtonPressed() {
        if (transactionState == TransactionState.Buying && curTradingItemInstance == null) return;
        int buyPrice = curTradingItemInstance.ItemRef.buyPrice;
        if (player.coinCurrency >= buyPrice) {
            player.coinCurrency -= buyPrice;
            TryAddItemToInventory(stashInventory, curTradingItemInstance.ItemRef, 1);
            ReduceTradingItemStock();
            // After buying items we just make sure all items in stash are no longer trader owned
            ClearItemsAsTraderOwned(stashInventory);
        }
    }
    
    private void OnBarterPurchaseButtonPressed() {
        if (curTradingItemInstance == null) return;

        foreach (ItemWithCount barterReq in curTradingItemInstance.ItemRef.barterRequirements) {
            if (GetOwnedCountOfItem(barterReq.item) < barterReq.count) return;
        }
            
        foreach (ItemWithCount barterReq in curTradingItemInstance.ItemRef.barterRequirements) {
            int removedCount = RemoveNumberOfItemsFromInventory(stashInventory, barterReq.item, barterReq.count);
            if (removedCount != barterReq.count) {
                int additionalRemoveCount = barterReq.count - removedCount;
                RemoveNumberOfItemsFromInventory(playerInventory, barterReq.item, additionalRemoveCount);
            }
        }

        TryAddItemToInventory(stashInventory, curTradingItemInstance.ItemRef, 1);
        ReduceTradingItemStock();
    }
    
    private void RefreshTransactionUI() {
        if (!OnTradingTab) return;
        
        if (transactionState == TransactionState.Buying) {
            transactionPanel.UpdateBuyItem(curTradingItemInstance);
            transactionPanel.toggleGroup.ManualyToggleCosmetically(transactionPanel.buyToggle);
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            transactionPanel.UpdateSellPrice(sellPrice);
            transactionPanel.toggleGroup.ManualyToggleCosmetically(transactionPanel.sellToggle);
        }
    }
    
    // ************************
    // Eye Forge 
    // ************************
    
    private enum CrucibleMode { Empty, Forging, ForgingButJustEye, ForgingButWithoutEye, NeedToRemoveDemonEye }
    private CrucibleMode crucibleMode;

    private void UpdateForgeState() {
        if (!OnEyeForgeTab) return;

        if (playingForgeUpgradeAnimation) {
            if (!forgeEyeButton.isDisabled) {
                forgeEyeButton.Disable();
            }
            return;
        }

        int crucibleItemCount = GetInventoryItemCount(crucibleInventory);
        ItemInstance eyeSlotItemInstance = crucibleInventory.slots[0].itemInstance;

        if (crucibleItemCount <= 0) {
            crucibleMode = CrucibleMode.Empty;
        }
        else if (eyeSlotItemInstance != null && eyeSlotItemInstance.ItemRef.type == demonEyeType) {
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

    private void UpdateForgeInfoPanel() {
        if (!OnEyeForgeTab) return;
        
        bool forging = crucibleMode is CrucibleMode.Forging;
        if (forging && forgeEyeButton.isDisabled) {
            forgeEyeButton.Enable();
        }
        else if (!forging && !forgeEyeButton.isDisabled) {
            forgeEyeButton.Disable();
        }

        if (PlayingForgeAnimation) return;

        bool showUpgradeScreen = crucibleMode is CrucibleMode.Empty or CrucibleMode.NeedToRemoveDemonEye;
        forgeDetailsUpgradeScreen.gameObject.SetActive(showUpgradeScreen);
        forgeDetailsForgeScreen.gameObject.SetActive(!showUpgradeScreen);
        
        if (showUpgradeScreen) {
            bool meetsAllUpgradeRequirements = true;
            List<UpgradePath.Requirement> curUpgradeReqs = crucibleUpgradePath.pathUpgrades[player.crucibleLevel].requirements;
            
            for (int i = 0; i < forgeDetailsResourceRequirements.Count; i++) {
                if (!curUpgradeReqs.IndexInRange(i)) {
                    forgeDetailsResourceRequirements[i].gameObject.SetActive(false);
                    continue;
                }
                forgeDetailsResourceRequirements[i].gameObject.SetActive(true);
                
                UpgradePath.Requirement req = curUpgradeReqs[i];
                int ownedCount = GetOwnedCountOfItem(req.item);
                forgeDetailsResourceRequirements[i].Set(req.item, req.count, ownedCount);

                if (ownedCount < req.count) {
                    meetsAllUpgradeRequirements = false;
                }
            }

            if (upgradeForgeButton.isDisabled && meetsAllUpgradeRequirements) {
                upgradeForgeButton.Enable();
            }
            else if (!upgradeForgeButton.isDisabled && !meetsAllUpgradeRequirements) {
                upgradeForgeButton.Disable();
            }
            
            return;
        }
        
        if (crucibleMode == CrucibleMode.Empty) {
            forgeDetailsForgeText.text = "Place an eyeball in the center to start the Demon Eye forging process";
        }
        else if (crucibleMode == CrucibleMode.ForgingButJustEye) {
            forgeDetailsForgeText.text = $"Requires at least {DisplayNumber(1)} eye upgrade to forge a Demon Eye";
        }
        else if (crucibleMode == CrucibleMode.ForgingButWithoutEye) {
            forgeDetailsForgeText.text = $"Missing eyeball in the center";
        }
        else {
            int eyeUpgradeCount = GetInventoryItemCount(crucibleInventory) - 1;
            int totalUpgradeCount = crucibleInventory.slots.Length - 1;
            forgeDetailsForgeText.text = $"<size=90%>Previewing Upgrades {ColorText(eyeUpgradeCount.ToString(), styles.timeDescColor)}/{totalUpgradeCount}</size><line-height=150%>\n";
            
            List<int> uuids = new(); // TODO: Performance
            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.itemInstance == null || slot.itemInstance.ItemRef.type != eyeModifierType) continue;
                uuids.Add(slot.itemInstance.itemOrInstanceUuid);
            }
            
            ModifierSet modifierSet = ConstructModifierSet(uuids);
            
            string eyeDescription = "";
            foreach (ModifierSet.Element modSetElm in modifierSet.elements) {
                eyeDescription += GetDemonEyeModDescription(modSetElm.modifierItem, modSetElm.modifierCount, modSetElm.uniqueAugments);
            }
            forgeDetailsForgeText.text += eyeDescription;
        }
    }
    
    private void OnForgeButtonPressed() {
        if (PlayingForgeAnimation) return;
        
        int eyeSlotIndex = 0;
        ItemInstance eyeItemInstance = null;

        for (int i = 0; i < crucibleInventory.slots.Length; i++) {
            InventorySlot slot = crucibleInventory.slots[i];
            if (slot.ui.OnlyAcceptsType(eyeType)) {
                eyeItemInstance = slot.itemInstance;
                eyeSlotIndex = i;
            }
        }

        if (eyeItemInstance == null) return;

        for (int i = 0; i < crucibleInventory.slots.Length; i++) {
            if (i == eyeSlotIndex) continue;
            if (crucibleInventory.slots[i].itemInstance != null) break;
            if (i == crucibleInventory.slots.Length - 1) return;
        }

        toggledOffHoverableUIElement = forgeEyeButton.rectTransform;
        
        forgeEyeButton.KeepPressed();
        string prevButtonText = forgeEyeButton.text.text;
        forgeEyeButton.text.text = "Forging...";
        
        DoEyeForgeAnimation(onAnimationEndCallback: () => {
            forgeEyeButton.StopKeepPressed();
            forgeEyeButton.text.text = prevButtonText;
            
            ItemInstance newDemonEyeItemInstance = new() {
                nestedUuids = new(),
                isDemonEye = true,
            };

            foreach (InventorySlot slot in crucibleInventory.slots) {
                slot.ui.itemUI.rectTransform.anchoredPosition = Vector2.zero;
                slot.ui.itemUI.rectTransform.localScale = Vector3.one;
                
                if (slot.itemInstance == null) continue;
                
                if (slot.ui.OnlyAcceptsType(eyeModifierType)) {
                    newDemonEyeItemInstance.nestedUuids.Add(slot.itemInstance.itemOrInstanceUuid);
                }
                slot.itemInstance = null;
            }
            
            BuildAndRegisterEye(newDemonEyeItemInstance);
            crucibleInventory.slots[eyeSlotIndex].itemInstance = newDemonEyeItemInstance;
        });
    }
    
    private Sequence eyeForgeSequence;
    private bool PlayingForgeAnimation => eyeForgeSequence.isAlive;
    
    private void DoEyeForgeAnimation(Action onAnimationEndCallback) {
        const float fillDuration = 4.5f;
        const float perUpgradeExplosionDelay = 0.15f;
        const float popOutDuration = 0.15f;

        float upgradeExplosionsDuration = perUpgradeExplosionDelay * (GetInventoryItemCount(crucibleInventory) - 1);
        float totalAnimationDuration = fillDuration + upgradeExplosionsDuration + popOutDuration;

        Tween.Custom(this, 0f, 1f, fillDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.pentagramFillCurve.Evaluate(val));
        });
        
        Tween.Custom(this, 1f, 0f, fillDuration * 0.3f, startDelay: totalAnimationDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.pentagramFillCurve.Evaluate(val));
        });

        for (int i = 0; i < crucibleInventory.slots.Length; i++) {
            InventorySlot slot = crucibleInventory.slots[i];
            if (slot.itemInstance == null) continue;

            RectTransform rectTransform = slot.ui.itemUI.rectTransform;

            // Use our own shake because prime tween shake's curve does not work
            rectTransform.DoTweenShake(10f, 3.3f, totalAnimationDuration, itemShakeCurve);

            Sequence sequence = Sequence.Create();

            if (slot.itemInstance.ItemRef.type == eyeType) {
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
                    Entity forgeExplosion = SpawnEntity(forgeExplosionPool, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel, Vector3.one * 0.05f, 1f, 15f);
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
                    Entity forgeExplosion = SpawnEntity(forgeExplosionPool, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel, Vector3.one * 0.04f, 0.15f, 15f);
                }));
            }
        }
    }
    
    private int fillParamProperty = Shader.PropertyToID("_Fill");
    
    private void SetPentagramFill(float value) {
        pentagramFillImage.material.SetFloat(fillParamProperty, value);
    }
    
    private void OnUpgradeForgePressed() {
        toggledOffHoverableUIElement = upgradeForgeButton.rectTransform;
            
        UpgradePath.UpgradeRequirements requirements = crucibleUpgradePath.pathUpgrades[player.crucibleLevel];
            
        bool canUpgrade = true;
        foreach (UpgradePath.Requirement requirement in requirements.requirements) {
            if (MeetsSingleUpgradeRequirement(requirement)) continue;
            canUpgrade = false;
            break;
        }
        
        if (!canUpgrade) return;
            
        foreach (UpgradePath.Requirement requirement in requirements.requirements) {
            int stashRemoveCount = RemoveNumberOfItemsFromInventory(stashInventory, requirement.item, requirement.count);
            if (stashRemoveCount == requirement.count) continue;
            RemoveNumberOfItemsFromInventory(playerInventory, requirement.item, requirement.count - stashRemoveCount);
        }
            
        player.crucibleLevel++;
        SavePlayerData();
            
        upgradeForgeButton.KeepPressed();
        string prevButtonText = upgradeForgeButton.text.text;
        upgradeForgeButton.text.text = "Upgrading...";
            
        DoForgeUpgradeAnimation(() => {
            upgradeForgeButton.StopKeepPressed();
            upgradeForgeButton.text.text = prevButtonText;
        });
    }
    
    private bool playingForgeUpgradeAnimation;
    
    private void DoForgeUpgradeAnimation(Action onAnimationEndCallback) {
        playingForgeUpgradeAnimation = true;
        
        const float explosionDelay = 0.1f;
        
        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        
        for (int i = 1; i < crucibleInventory.slots.Length; i++) {
            RectTransform slotTransform = crucibleInventory.slots[i].ui.rectTransform;
            sequence.Group(Tween.Scale(slotTransform, Vector3.one, Vector3.zero, 0.15f, Ease.InOutBounce, startDelay: explosionDelay * i));
            sequence.Group(Tween.PunchScale(eyeForgePanel, Vector3.one * 0.05f, 0.1f, 15f, startDelay: explosionDelay * i));
            sequence.Group(Tween.Delay(0.1f * i, () => {
                Entity forgeExplosion = SpawnEntity(forgeDustExplosionPool, OffsetY(slotTransform.position, 10f), Quaternion.identity, eyeForgePanel);
                DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
            }));
        }
        
        sequence.ChainDelay(0.25f);
        
        sequence.ChainCallback(() => {
            ChangeInventorySize(crucibleInventory, crucibleInventory.slots.Length + 1);
            SetupEyeCrucibleInventorySlots();
            crucibleInventory.slots[^1].ui.rectTransform.localScale = Vector3.zero;
            
            for (int i = 1; i < crucibleInventory.slots.Length; i++) {
                RectTransform slotTransform = crucibleInventory.slots[i].ui.rectTransform;
                Tween.Scale(slotTransform, Vector3.zero, Vector3.one, 0.15f, Ease.InOutBounce, startDelay: explosionDelay * i);
                Tween.PunchScale(eyeForgePanel, Vector3.one * 0.05f, 0.1f, 15f, startDelay: explosionDelay * i);
                Tween.Delay(explosionDelay * i, () => {
                    Entity forgeExplosion = SpawnEntity(forgeDustExplosionPool, OffsetY(slotTransform.position, 10f), Quaternion.identity, eyeForgePanel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                });
                
                if (i == crucibleInventory.slots.Length - 1) {
                    const float additionalCompletionDelay = 0.15f;
                    Tween.Delay(this, explosionDelay * i + additionalCompletionDelay, (inst) => {
                        inst.playingForgeUpgradeAnimation = false;
                        onAnimationEndCallback?.Invoke();
                    });
                }
            }
        });
    }

    // ************************
    // Quests 
    // ************************
    
    private class QuestPackage {
        public QuestGraphRuntime.Node questNode;
        public QuestUI questUI;
        public ToggleButton questToggleButton;

        public void RefreshDisplay() => questUI.Display(questNode.curQuest);
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
        SaveToFile(questSavePath, questSaveData);
    }

    private void SaveAndMarkQuestAsSubmitted(QuestGraphRuntime.Node questNode) {
        questSaveData.submissionStates[questNode.saveIndex] = true;
        questSaveData.progressSaves[questNode.saveIndex] = questNode.curQuest.GetProgressSave();
        SaveToFile(questSavePath, questSaveData);
    }

    private void InitQuests() {
        questSaveData = LoadFromFile<QuestSaveData>(questSavePath);
        
        if (questSaveData == null) {
            questSaveData = new() {
                progressSaves = new Quest.ProgressSave[questGraph.questCount],
                submissionStates = new bool[questGraph.questCount],
            };
            questSaveData.progressSaves.InitalizeWithDefault();
            SaveToFile(questSavePath, questSaveData);
        }
        
        HashSet<QuestGraphRuntime.Node> initialQuestNodes = new();
        foreach (QuestGraphRuntime.Node node in questGraph.rootNode.nextNodes) {
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

    private void RefreshQuestDisplays() {
        if (activeQuestPackages.Count <= 0) return;

        if (presentingQuestPackage == null || presentingQuestPackage.questNode == null) {
            presentingQuestPackage = activeQuestPackages[0];
            questToggleButtonGroup.ManualyToggle(presentingQuestPackage.questToggleButton);
        }
        
        foreach (QuestPackage questPackage in activeQuestPackages) {
            questPackage.questUI.gameObject.SetActive(false);
        }
        
        presentingQuestPackage.questUI.gameObject.SetActive(true);
        presentingQuestPackage.RefreshDisplay();
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
        QuestUI ui = Instantiate(questPrefab, questsParent).GetComponent<QuestUI>();
        
        ToggleButton toggle = Instantiate(questSelectionTogglePrefab, questSelectionParent).GetComponent<ToggleButton>();
        questToggleButtonGroup.Add(toggle);
        
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
                SaveInventory(playerInventory);
                SaveInventory(stashInventory);
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
        skillsPanel.hasteSkillRow.Init(hasteUpgradePath.MaxLevel, 
            $"{DisplayProbIncrease(gameplayConfig.movementSpeedIncPerLevel)} Movement Speed\n" +
            $"{DisplayProbIncrease(gameplayConfig.lootingSpeedIncPerLevel)} Looting Speed\n" +
            $"{DisplayProbIncrease(gameplayConfig.firerateIncPerLevel)} Firerate"
        );
        skillsPanel.intellectSkillRow.Init(intellectUpgradePath.MaxLevel, 
            $"{DisplayProbIncrease(gameplayConfig.critChanceIncPerLevel)} Critical Strike Chance\n" +
            $"{DisplayMultiplierIncrease(gameplayConfig.critMultiplierIncPerLevel)} Critical Strike Multiplier\n" +
            $"{DisplayIncrease(gameplayConfig.projectileCountIncPerLevel)} Projectile Count"
        );
        skillsPanel.lifeBloodSkillRow.Init(lifeBloodUpgradePath.MaxLevel, 
            $"{DisplayIncrease(gameplayConfig.healthIncPerLevel)} Health\n" +
            $"{DisplayProbIncrease(gameplayConfig.healingSpeedIncPerLevel)} Healing Speed\n" +
            $"{DisplayProbIncrease(gameplayConfig.healingIncPerLevel)} Healing Amount"
        );
        skillsPanel.strengthSkillRow.Init(strengthUpgradePath.MaxLevel, 
            $"{DisplayProbIncrease(gameplayConfig.bleedResistIncPerLevel)} Bleed Resist\n" +
            $"{DisplayIncrease(gameplayConfig.carryCapacityIncPerLevel)} Carry Capacity\n" +
            $"{DisplayMultiplierIncrease(gameplayConfig.damageMultiplierIncPerLevel)} Damage"
        );
    }

    private void OnLevelupButtonPressed(SkillUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeSkill(upgradePath, playerStatLevel);
        if (result == UpgradeStatResult.CantAfford || result == UpgradeStatResult.AtMaxLevel) return;
        
        player.soulCurrency -= upgradePath.soulsNeededPerLevel[playerStatLevel];

        if (upgradePath == hasteUpgradePath) {
            player.hasteSkillLevel++;
        }
        else if (upgradePath == intellectUpgradePath) {
            player.intellectSkillLevel++;
        }
        else if (upgradePath == lifeBloodUpgradePath) {
            int prevFullPlayerHealth = FullPlayerHealth;
            player.lifeBloodSkillLevel++;
            int newFullPlayerHealth = FullPlayerHealth;
            player.health += newFullPlayerHealth - prevFullPlayerHealth;
        }
        else if (upgradePath == strengthUpgradePath) {
            player.strengthSkillLevel++;
        }
        
        SavePlayerData();
        RefreshSkillsPanel();
    }
    
    private void RefreshSkillsPanel() {
        playerStatsPanel.carryCapacityRow.statValueText.text = ((int)GetPlayerStat(Player.Stat.CarryCapacity)).ToString();
        playerStatsPanel.critChanceRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(Player.Stat.CritChance));
        playerStatsPanel.critMultiRow.statValueText.text = DisplayMultiplierNoColor(GetPlayerStat(Player.Stat.CritMulti));
        playerStatsPanel.damageRow.statValueText.text = DisplayMultiplierNoColor(GetPlayerStat(Player.Stat.DamageMulti));
        playerStatsPanel.firerateRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(Player.Stat.FireratePercentage));
        playerStatsPanel.healthRow.statValueText.text = ((int)(GetPlayerStat(Player.Stat.Health))).ToString();
        playerStatsPanel.healingAmountRow.statValueText.text = DisplayIncrease(GetPlayerStatAdjustment(Player.Stat.HealingAmount));
        playerStatsPanel.healingSpeedRow.statValueText.text = DisplayProbIncrease(GetPlayerStatAdjustment(Player.Stat.HealingSpeed));
        playerStatsPanel.lootingSpeedRow.statValueText.text = DisplayProbIncrease(GetPlayerStatAdjustment(Player.Stat.LootingSpeed));
        playerStatsPanel.movementSpeedRow.statValueText.text = DisplayProbNoColor(GetPlayerStat(Player.Stat.MovementSpeedPercentage));
        playerStatsPanel.projectileCountRow.statValueText.text = DisplayNumberNoColor(GetPlayerStat(Player.Stat.ProjectileCount));
        
        RefreshSkillRow(skillsPanel.hasteSkillRow, hasteUpgradePath, player.hasteSkillLevel);
        RefreshSkillRow(skillsPanel.intellectSkillRow, intellectUpgradePath, player.intellectSkillLevel);
        RefreshSkillRow(skillsPanel.lifeBloodSkillRow, lifeBloodUpgradePath, player.lifeBloodSkillLevel);
        RefreshSkillRow(skillsPanel.strengthSkillRow, strengthUpgradePath, player.strengthSkillLevel);
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