using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public partial class Game {
    
    private bool OnCharacterTab => hideoutTabs.characterButton.image.sprite == hideoutTabs.selectedSprite;
    private bool OnEyeForgeTab => hideoutTabs.eyeForgeButton.image.sprite == hideoutTabs.selectedSprite;
    private bool OnTradingTab => hideoutTabs.traderButton.image.sprite == hideoutTabs.selectedSprite;
    
    private void InitHideout(GameState gameState) {
        InitTrader(gameState);
        InitSkillsPanel();
        InitQuestPanel();
        SetPentagramFill(0f);
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
            traderPanel.itemRefreshTimeText.text = "Items Restock After Next Raid";
            return;
        }
        traderPanel.itemRefreshTimeText.text = $"Items Restock In {raidsUntilRestock} More Raids";
    }

    private void IncreaseTraderRep(int repGain) {
        if (config.trader.ReachedMaxLevel()) return;

        int wouldGainLevels = config.trader.LevelsGainedFromXp(repGain);
        config.trader.state.reputation += repGain;
        
        bool increasedLevel = wouldGainLevels > 0;
        if (increasedLevel) {
            CheckForTraderRestock(forceRestock: true);
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
    
    private InventorySlot curTradingInventorySlot;
    
    private void SetTradingSlot(InventorySlot traderInventorySlot, bool tweenSize) {
        traderInventorySlot ??= inventories.trader.slots[0];
        
        curTradingInventorySlot?.ui.ClearSelectionUnderlay();
        curTradingInventorySlot = traderInventorySlot;
        curTradingInventorySlot?.ui.SetSelectionUnderlay();
        
        if (tweenSize && curTradingInventorySlot != null) {
            ItemUI itemUI = curTradingInventorySlot.ui.itemUI;
            Tween.PunchScale(itemUI.rectTransform, Vector3.one * 0.3f, 0.12f, 5f);
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
    }
    
    private void OnSellButtonPressed() {
        if (transactionState == TransactionState.Selling && GetInventoryItemCount(inventories.transaction) <= 0) return;
        int sellPrice = GetInventoryValue(inventories.transaction, InventoryValueType.Sell);
        player.state.coinCurrency += sellPrice;
        ClearInventory(inventories.transaction);
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
        }
    }
    
    private void OnBarterPurchaseButtonPressed() {
        if (curTradingInventorySlot == null) return;

        Item curTradingItem = curTradingInventorySlot.itemInstance.ItemRef;
        foreach (ItemWithCount barterReq in curTradingItem.traderSpawning.barterRequirements) {
            if (GetOwnedCountOfItem(barterReq.item) < barterReq.count) return;
        }
            
        foreach (ItemWithCount barterReq in curTradingItem.traderSpawning.barterRequirements) {
            int removedCount = RemoveNumberOfItemsFromInventory(inventories.stash, barterReq.item, barterReq.count);
            if (removedCount != barterReq.count) {
                int additionalRemoveCount = barterReq.count - removedCount;
                RemoveNumberOfItemsFromInventory(inventories.player, barterReq.item, additionalRemoveCount);
            }
        }

        TryAddItemToInventory(inventories.stash, curTradingItem, 1);
        ReduceTradingItemStock();
        TriggerTraderShopDialogue(TraderShopDialogueType.Purchase);
    }
    
    private void UpdateTransactionUI() {
        if (!OnTradingTab) return;
        
        if (transactionState == TransactionState.Buying) {
            transactionPanel.transaction.UpdateBuyItem(curTradingInventorySlot?.itemInstance);
            transactionPanel.transaction.toggleGroup.ManualyToggleCosmetically(transactionPanel.transaction.buyToggle);
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(inventories.transaction, InventoryValueType.Sell);
            transactionPanel.transaction.UpdateSellPrice(sellPrice);
            transactionPanel.transaction.toggleGroup.ManualyToggleCosmetically(transactionPanel.transaction.sellToggle);
        }
    }
    
    private enum TraderShopDialogueType { Greeting, Purchase }

    private Tween traderDialogueTween;

    private void TriggerTraderShopDialogue(TraderShopDialogueType dialogueType) {
        traderDialogueTween.Stop();
        traderPanel.shopTextTypewriter.gameObject.SetActive(false);

        // The delay just looks nice 
        traderDialogueTween = Tween.Delay(0.1f, () => {
            var typewriter = gameInstance.traderPanel.shopTextTypewriter;
            if (dialogueType == TraderShopDialogueType.Greeting) {
                typewriter.ShowText("{ffade}{shake}Can you actually buy something this time?");
            }
            if (dialogueType == TraderShopDialogueType.Purchase) {
                typewriter.ShowText("{ffade}{shake}Pleasure doing business. Keep it real.\n{ffade}{shake}That one was a 34% mark up.");
            }
            typewriter.gameObject.SetActive(true);
        });
    }
    
    // ************************
    // Eye Forge 
    // ************************
    
    private enum CrucibleMode { Empty, Forging, ForgingButJustEye, ForgingButWithoutEye, DemonEyeTooLowToUpgrade, }
    private CrucibleMode crucibleMode;

    private void UpdateForgeState() {
        if (!OnEyeForgeTab) return;

        int crucibleItemCount = GetInventoryItemCount(inventories.eyeForge);
        ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[0].itemInstance;

        if (crucibleItemCount <= 0) {
            crucibleMode = CrucibleMode.Empty;
        }
        else if (eyeSlotItemInstance != null && eyeSlotItemInstance.isDemonEye && eyeSlotItemInstance.demonEyeUpgradesAvailable <= 0) {
            crucibleMode = CrucibleMode.DemonEyeTooLowToUpgrade;
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
        ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[0].itemInstance;
        
        if (crucibleMode == CrucibleMode.Empty) {
            detailsText.text = "Place an eyeball in the center to start the Demon Eye forging process";
            demonEyeDesc.HideAllElements();
        }
        else if (crucibleMode == CrucibleMode.ForgingButJustEye && !eyeSlotItemInstance.isDemonEye) {
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
                detailsText.text = $"Previewing Upgrades {ColorText(eyeUpgradeCount.ToString(), config.styles.timeDescColor)}/{totalUpgradeCount}";
            }
            
            using var _ = ListPool<int>.Get(out var uuids);
            
            if (eyeSlotItemInstance.isDemonEye) {
                uuids.AddRange(eyeSlotItemInstance.nestedUuids);
            }
            
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
            if (slot.ui.AcceptsItemType(itemTypes.eye)) {
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
        
        string demonEyeName = randomDemonEyeNames.GetRandom();
        
        DoEyeForgeAnimation(demonEyeName, onAnimationEndCallback: () => {
            forgeButton.StopKeepPressed();
            forgeButton.text.text = prevButtonText;
            
            ItemInstance eyeSlotItemInstance = inventories.eyeForge.slots[eyeSlotIndex].itemInstance;
            using var _ = ListPool<ItemInstance>.Get(out var eyeUpgradeItemInstances);

            foreach (InventorySlot slot in inventories.eyeForge.slots) {
                slot.ui.itemUI.rectTransform.anchoredPosition = Vector2.zero;
                slot.ui.itemUI.rectTransform.localScale = Vector3.one;
                slot.ui.itemUI.pixelFillManager.SetMaterialFill(1f);
                
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
                ItemInstance newDemonEye = CreateNewDemonEyeItemInstance(demonEyeName, eyeUpgradeItemInstances);
                inventories.eyeForge.slots[eyeSlotIndex].itemInstance = newDemonEye;
            }
        });
    }
    
    private Sequence eyeForgeSequence;
    private bool PlayingForgeAnimation => eyeForgeSequence.isAlive;
    
    private void DoEyeForgeAnimation(string demonEyeName, Action onAnimationEndCallback) {
        const float fillDuration = 5.5f;
        const float perUpgradeExplosionDelay = 0.2f;
        const float perUpgradeDissolveDelay = 0.6f;
        const float popOutDuration = 0.1f;

        float upgradeExplosionsDuration = perUpgradeExplosionDelay * (GetInventoryItemCount(inventories.eyeForge) - 1);
        float totalAnimationDuration = fillDuration + upgradeExplosionsDuration + popOutDuration;
        
        InventorySlot[] slots = inventories.eyeForge.slots;
        
        bool upgradingDemonEye = slots[0].itemInstance.isDemonEye;
        if (upgradingDemonEye) {
            slots[0].ui.itemUI.pixelFillManager.SetIntoSprite(config.demonEyeLevels.levelSprites[1]);
        } else {
            slots[0].ui.itemUI.pixelFillManager.SetIntoSprite(config.demonEyeLevels.levelSprites[0]);
        }
        slots[0].ui.itemUI.pixelFillManager.UseSmoothing(false);
        
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
                targetSlot.ui.itemUI.pixelFillManager.SetMaterialFill(val);
            });
            
            Tween.Custom(slots[1], 1f, 0f, duration, ease: ease, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.pixelFillManager.SetMaterialFill(val);
            });
            
            Tween.Custom(slots[2], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 2, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.pixelFillManager.SetMaterialFill(val);
            });
            Tween.Custom(slots[5], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 2, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.pixelFillManager.SetMaterialFill(val);
            });
            
            Tween.Custom(slots[3], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 4, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.pixelFillManager.SetMaterialFill(val);
            });
            Tween.Custom(slots[4], 1f, 0f, duration, ease: ease, startDelay: perUpgradeDissolveDelay * 4, onValueChange: (targetSlot, val) => {
                targetSlot.ui.itemUI.pixelFillManager.SetMaterialFill(val);
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
        
        const float burnTime = 2f;
        const float scortchFadeTime = 1.8f;
        const float fadeScortchDelay = 0.3f;

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
        questPackage.questUI.Burn(burnTime, curves.questBurn);
        
        // Play the ember particles
        questsPanel.emberParticles.Play();
        questsPanel.emberParticles.transform.parent.SetAsLastSibling();
        
        // When done burning, release the quest package. The shader finishes a little early so we modify the duration.
        Tween.Delay(questPackage, burnTime * 0.85f, static (burningQuestPkg) => {
            gameInstance.RemoveQuestFromDisplay(burningQuestPkg); 
        });
    }
    
    private bool QuestIsActive(Quest quest) {
        foreach (QuestPackage activeQuestPackage in quests.activePkgs) {
            if (quest == activeQuestPackage.questNode.curQuest && !QuestIsComplete(quest)) {
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
        
        thisFrame.flags |= FrameFlags.SkillUpgraded;
        
        SaveGameState();
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
    
}