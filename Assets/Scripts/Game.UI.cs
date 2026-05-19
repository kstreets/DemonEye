using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class Game {
    
    private void InitUI() {
        Cursor.visible = true;
        CloseHideoutUI();
        CloseRaidUI();
        ShowMainMenuUI();
        gameData.ui.menuBackButton.gameObject.SetActive(false);
        gameData.ui.largeRaidTextTypewriter.gameObject.SetActive(false);
    }

    private Sequence mainMenuSequence;
    
    private void AnimateInMainMenu() {
        if (mainMenuSequence.isAlive) return;
        
        float halfScreenHeight = Screen.height / 2f;
        var logo = gameData.mainMenu.logo;
        var playButton = gameData.mainMenu.playButton;
        var hideoutButton = gameData.mainMenu.hideoutButton;
        var settingsButton = gameData.mainMenu.settingsButton;
        var exitButton = gameData.mainMenu.exitButton;
        
        mainMenuSequence = Sequence.Create();
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(logo, halfScreenHeight, logo.anchoredPosition.y, 0.8f, Ease.OutExpo));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(playButton.rectTransform, -halfScreenHeight, playButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(hideoutButton.rectTransform, -halfScreenHeight, hideoutButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo, startDelay: 0.1f));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(settingsButton.rectTransform, -halfScreenHeight, settingsButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo, startDelay: 0.2f));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(exitButton.rectTransform, -halfScreenHeight, exitButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo, startDelay: 0.3f));

        playButton.rectTransform.anchoredPosition = new(playButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        hideoutButton.rectTransform.anchoredPosition = new(hideoutButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        settingsButton.rectTransform.anchoredPosition = new(settingsButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        exitButton.rectTransform.anchoredPosition = new(exitButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
    }

    private void ShowMainMenuUI() {
        gameData.ui.hideoutParent.gameObject.SetActive(true);
        gameData.ui.animatedBgImage.gameObject.SetActive(true);
        gameData.mainMenu.parent.gameObject.SetActive(true);
        AnimateInMainMenu();
    }

    private void CloseMainMenuUI() {
        gameData.ui.animatedBgImage.gameObject.SetActive(false);
        gameData.mainMenu.parent.gameObject.SetActive(false);
    }

    private void ShowMapSelectionUI() {
        ShowHideoutUI();
        gameData.hideoutTabs.parent.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(false);
        ToggleHideoutPanels(playerPanel, mapSelectionPanel);
    }

    private void CloseMapSelectionUI() {
        CloseHideoutUI();
    }
    
    private void ShowHideoutUI() {
        ToggleHideoutTab(gameData.hideoutTabs.characterButton, gameData.hideoutTabs.characterText);
        ToggleHideoutPanels(playerPanel, stashPanel);
        gameData.ui.menuBackButton.gameObject.SetActive(true);
        coinsCurrencyParent.gameObject.SetActive(true);
        soulsCurrencyParent.gameObject.SetActive(true);
        healthBarParent.gameObject.SetActive(false);
        weightBarParent.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(true);
        gameData.ui.animatedBgImage.gameObject.SetActive(true);
        gameData.hideoutTabs.parent.gameObject.SetActive(true);
    }

    private void CloseHideoutUI() {
        ToggleHideoutPanels();
        HideInventoryItemPopup(); 
        HideUIElementPopup();
        gameData.ui.menuBackButton.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(false);
        gameData.ui.animatedBgImage.gameObject.SetActive(false);
        gameData.hideoutTabs.parent.gameObject.SetActive(false);
    }

    private void ShowRaidUI() {
        healthBarParent.gameObject.SetActive(true);
        weightBarParent.gameObject.SetActive(true);
        coinsCurrencyParent.gameObject.SetActive(false);
        soulsCurrencyParent.gameObject.SetActive(true);
        playerInfoParent.gameObject.SetActive(true);
        raidInfoPanelParent.SetActive(true);
        gameData.ui.hotBarParent.gameObject.SetActive(true);
    }

    private void CloseRaidUI() {
        HideInventoryItemPopup(); 
        HideUIElementPopup();
        interactPrompt.gameObject.SetActive(false);
        interactionDetails.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(false);
        raidInfoPanelParent.SetActive(false);
        portalArrow.gameObject.SetActive(false);
        gameData.ui.hotBarParent.gameObject.SetActive(false);
    }

    private void ToggleHideoutTab(Button button, TextMeshProUGUI text) {
        Sprite tabSelectedSprite = gameData.hideoutTabs.selectedSprite;
        Sprite tabNonSelectedSprite = gameData.hideoutTabs.nonSelectedSprite;
        
        gameData.hideoutTabs.characterButton.image.sprite = tabNonSelectedSprite;
        gameData.hideoutTabs.eyeForgeButton.image.sprite = tabNonSelectedSprite;
        gameData.hideoutTabs.traderButton.image.sprite = tabNonSelectedSprite;
        gameData.hideoutTabs.questsButton.image.sprite = tabNonSelectedSprite;
        gameData.hideoutTabs.skillsButton.image.sprite = tabNonSelectedSprite;
        
        gameData.hideoutTabs.characterText.margin = Styles.instance.nonSelectedHideoutTabMargin;
        gameData.hideoutTabs.eyeForgeText.margin = Styles.instance.nonSelectedHideoutTabMargin;
        gameData.hideoutTabs.traderText.margin = Styles.instance.nonSelectedHideoutTabMargin;
        gameData.hideoutTabs.questsText.margin = Styles.instance.nonSelectedHideoutTabMargin;
        gameData.hideoutTabs.skillsText.margin = Styles.instance.nonSelectedHideoutTabMargin;
        
        button.image.sprite = tabSelectedSprite;
        text.margin = Styles.instance.selectedHideoutTabMargin;
    }

    private void ToggleHideoutPanels(params RectTransform[] panels) {
        playerPanel.gameObject.SetActive(false);
        stashPanel.gameObject.SetActive(false);
        eyeForgePanel.gameObject.SetActive(false);
        forgeDetailsPanel.gameObject.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
        traderInventoryPanel.gameObject.SetActive(false);
        traderTransactionPanel.gameObject.SetActive(false);
        questsPanel.gameObject.SetActive(false);
        skillsPanel.gameObject.SetActive(false);
        playerStatsPanel.gameObject.SetActive(false);
        mapSelectionPanel.gameObject.SetActive(false);
        
        foreach (RectTransform rect in panels) {
            rect.gameObject.SetActive(true);
        }
    }

    // Here just so that we don't allocate strings every frame
    private int prevSoulCurrency = int.MinValue;
    private int prevCoinCurrency = int.MinValue;
    
    private void UpdateCurrencyNumbers() {
        if (prevSoulCurrency != player.soulCurrency) {
            soulsCurrencyText.text = player.soulCurrency.ToString("N0");
        }
        if (prevCoinCurrency != player.coinCurrency) {
            coinCurrencyText.text = player.coinCurrency.ToString("N0");
        }
        prevSoulCurrency = player.soulCurrency;
        prevCoinCurrency = player.coinCurrency;
    }
    
    private void UpdateInRaidUI() {
        healthBarFillImage.fillAmount = player.health / (float)FullPlayerHealth;
        bleedDebuffIcon.gameObject.SetActive(player.bleeding);
        
        GetEncumberingWeightRange(out int startingEncumberingWeight, out _);
        int inventoryWeight = GetInventoryWeight(gameData.inventories.player);
        weightBarFillImage.fillAmount = Mathf.Clamp01(inventoryWeight / (float)startingEncumberingWeight);
        
        float overweightComp = GetOverweightCompletion();
        if (overweightComp > 0f) {
            weightBarFillImage.color = Color.Lerp(Styles.instance.startingOverWeightColor, Styles.instance.endingOverWeightColor, overweightComp);
        }
        else {
            weightBarFillImage.color = Styles.instance.underWeightColor;
        }
        
        if (gameData.curRaid.stateSwitchedThisFrame) {
            if (gameData.curRaid.state == RaidState.InitialWaves) {
                finalWaveCountdownParent.SetActive(true); 
                exitPortalCountdownParent.SetActive(true);
                finalWaveActiveNotifier.SetActive(false);
                exitPortalActiveNotifier.SetActive(false);
                finalExitPortalNotifier.SetActive(false);
            }
            else if (gameData.curRaid.state == RaidState.FinalWave) {
                finalWaveCountdownParent.SetActive(false); 
                exitPortalActiveNotifier.SetActive(false);
                finalWaveActiveNotifier.SetActive(true);
                Tween.Scale(finalWaveActiveNotifier.transform, 0f, 1f, 0.5f, Ease.OutBack);
                AnimateSmallRaidText(ColorText("Final Wave", Styles.instance.decreaseDescColor));
            }
            else if (gameData.curRaid.state == RaidState.PostFinalWave) {
                finalWaveActiveNotifier.SetActive(false);
                finalExitPortalNotifier.SetActive(true);
                Tween.Scale(finalExitPortalNotifier.transform, 0f, 1f, 0.5f, Ease.OutBack);
            }
        }
        
        if (exitPortalCountdownText.gameObject.activeInHierarchy) {
            // exitPortalCountdownText.text = GetCountdownText(exitPortalTween.duration - exitPortalTween.elapsedTime);
        }
        if (finalWaveCountdownText.gameObject.activeInHierarchy) {
            finalWaveCountdownText.text = GetCountdownText(spawnManager.timeUntilFinalPhase);
        }
    }
    
    private void UpdateHotBarUI() {
        if (!gameData.ui.hotBarParent.gameObject.activeInHierarchy) return;

        for (int i = 0; i < playerQuickUseSize; i++) {
            int itemIndex = i + playerEquipmentSize;
            gameData.hotBar.slotUIs[i].ClearItem();

            ItemInstance itemInstance = gameData.inventories.player.slots[itemIndex].itemInstance;
            if (itemInstance != null) {
                gameData.hotBar.slotUIs[i].SetItem(itemInstance.ItemRef, itemInstance.count);
            } 
        }
    }

    private void AnimateLargeRaidText(string text, float typewriterSpeed) {
        gameData.ui.largeRaidText.characterSpacing = 0;
        gameData.ui.largeRaidText.gameObject.SetActive(true);
        gameData.ui.largeRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{wave}}{{#fade}}{{#wave}}{text}");
        gameData.ui.largeRaidTextTypewriter.SetTypewriterSpeed(typewriterSpeed);
        
        gameData.ui.largeRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.Chain(Tween.Custom(0, 30, 0.5f, startDelay: 0.3f, ease: Ease.OutBack, onValueChange: static (val) => {
                gameInstance.gameData.ui.largeRaidText.characterSpacing = val;
            }));
            sequence.ChainDelay(0.35f);
            sequence.ChainCallback(static () => gameInstance.gameData.ui.largeRaidTextTypewriter.StartDisappearingText());
        }
    }

    private void AnimateSmallRaidText(string text) {
        gameData.ui.smallRaidText.gameObject.SetActive(true);
        gameData.ui.smallRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{smallwave}}{{#fade}}{{#smallwave}}{text}");
        
        gameData.ui.smallRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.ChainDelay(0.8f);
            sequence.ChainCallback(static () => gameInstance.gameData.ui.smallRaidTextTypewriter.StartDisappearingText());
        }
    }
    
    // *******************************
    // Gameplay Text Pop Ups
    // *******************************
    
    private enum DamageColor { Normal, Crit, Blood, Poison }

    private void SpawnDamageNumber(Vector3 spawnPos, int damage, DamageColor damageColor) {
        Vector3 startSize = Vector3.one * 0.8f;
        Vector3 endSize = Vector3.one * damageColor switch {
            DamageColor.Normal => 1.0f,
            DamageColor.Crit   => 1.25f,
            DamageColor.Blood  => 0.8f,
            DamageColor.Poison => 0.8f,
            _                  => 1f,
        };
        
        float xOffset = Random.Range(-0.08f, 0.08f);
        float yOffset = Random.Range(0.05f, 0.1f);
        Vector2 endDamageNumPos;
        
        if (damageColor == DamageColor.Blood) {
            spawnPos = OffsetY(spawnPos, 0.05f);
            endDamageNumPos = OffsetY(spawnPos, yOffset * 2.3f);
        }
        else {
            endDamageNumPos = OffsetY(OffsetX(spawnPos, xOffset), yOffset);
        }
        
        Entity damageNumber = SpawnEntity(gameData.entityPools.damageNumber, spawnPos, Quaternion.identity, damageNumbersParent);
        damageNumber.textMesh.text = damage.ToString();
        
        const float alpha = 0.68f;
        switch (damageColor) {
            case DamageColor.Normal:
                damageNumber.textMesh.color = Styles.instance.normalDamageColor.Alpha(alpha);
                break;
            case DamageColor.Crit:
                damageNumber.textMesh.color = Styles.instance.critDamageColor.Alpha(alpha);
                break;
            case DamageColor.Blood:
                damageNumber.textMesh.color = Styles.instance.bleedDamageColor.Alpha(alpha);
                break;
            case DamageColor.Poison:
                damageNumber.textMesh.color = Styles.instance.poisonDamageColor.Alpha(alpha);
                break;
        }

        if (damageColor == DamageColor.Blood) {
            const float bloodMoveDuration = 0.65f;
            const float bloodScaleUpDuration = 0.25f;
            const float bloodPopOutDuration = 0.3f;
            Tween.Position(damageNumber.trans, endDamageNumPos, bloodMoveDuration, Ease.OutCubic)
            .Group(Tween.Scale(damageNumber.trans, startSize, endSize, bloodScaleUpDuration, Ease.InOutBack))
            .Chain(Tween.Scale(damageNumber.trans, 0f, bloodPopOutDuration, Ease.InBack));
            DestroyEntity(damageNumber, bloodMoveDuration + bloodPopOutDuration);
            return;
        }

        float moveDuration = damageColor == DamageColor.Crit ? Random.Range(0.37f, 0.4f) : Random.Range(0.3f, 0.35f);
        const float scaleUpDuration = 0.25f;
        const float popOutDuration = 0.3f;

        Tween.Position(damageNumber.trans, endDamageNumPos, moveDuration, Ease.OutBack)
        .Group(Tween.Scale(damageNumber.trans, startSize, endSize, scaleUpDuration, Ease.InOutBack))
        .Chain(Tween.Scale(damageNumber.trans, 0f, popOutDuration, Ease.InBack));
        DestroyEntity(damageNumber, moveDuration + popOutDuration);
    }

    private Vector3 EnemyDamageNumberSpawnPos(Entity entity) {
        return OffsetY(entity.position, 0.28f);
    }
    
    private void SpawnTrinketActivationText(string text) {
        SpawnTextPopIn(OffsetY(player.position, -0.1f), text);
    }
    
    private void SpawnTextPopIn(Vector3 spawnPos, string text, Vector3? endPos = default) {
        Entity textEntity = SpawnEntity(gameData.entityPools.damageNumber, spawnPos, Quaternion.identity, damageNumbersParent);
        textEntity.textMesh.text = text; 
        textEntity.textMesh.color = Styles.instance.popInTextColor;
        
        float moveDuration = Random.Range(0.37f, 0.4f);
        const float scaleUpDuration = 0.25f;
        const float popOutDuration = 0.3f;
        const float startSize = 0.8f;
        const float endSize = 1f;
        Vector3 endTextPos = endPos ?? OffsetY(player.position, -0.1f);

        Tween.Position(textEntity.trans, endTextPos, moveDuration, Ease.OutBack)
        .Group(Tween.Scale(textEntity.trans, startSize, endSize, scaleUpDuration, Ease.InOutBack))
        .Chain(Tween.Scale(textEntity.trans, 0f, popOutDuration, Ease.InBack));
        DestroyEntity(textEntity, moveDuration + popOutDuration);
    }
    
    // *******************************
    // Pop Ups 
    // *******************************
    
    public static void FitPopupSize(RectTransform popupRect, params Rect[] rects) {
        float height = 0f;
        foreach (Rect rect in rects) {
            height += rect.height;
        }
        
        const int minHeight = 80;
        Rect newPopupRect = popupRect.rect;
        newPopupRect.height = Mathf.Clamp(height, minHeight, Mathf.Infinity);
        popupRect.sizeDelta = new(newPopupRect.width, newPopupRect.height);
    }
    
    public static void FitPopupSize(RectTransform popupRect, params ILayoutElement[] layouts) {
        float height = 0f;
        foreach (ILayoutElement layoutElm in layouts) {
            height += layoutElm.preferredHeight;
        }
        
        const int minHeight = 80;
        Rect newPopupRect = popupRect.rect;
        newPopupRect.height = Mathf.Clamp(height, minHeight, Mathf.Infinity);
        popupRect.sizeDelta = new(newPopupRect.width, newPopupRect.height);
    }
    
    public static void TweenPopUp(RectTransform popupRectTransform) {
        TweenSettings settings = new() {
            duration = 0.065f,
            ease = Ease.OutQuad,
        };
        Tween.Scale(popupRectTransform, Vector3.one * 0.75f, Vector3.one, settings);
    }

    private void UpdateUIElementPopup() {
        UIHoverInfo hoverInfo = UpdateUIHover();
        
        if (!hoverInfo.hoveringTransform || hoverInfo.shouldNotShow) {
            HideUIElementPopup();
            return;
        }
        
        const float hoverTimeUntilTooltip = 0.32f;
        bool spentEnoughTimeHovering = hoverInfo.timeSpentHovering >= hoverTimeUntilTooltip;
        
        if (spentEnoughTimeHovering) {
            ShowUIElementPopup(hoverInfo);
        }
        else {
            HideUIElementPopup();
        }
    }

    private void ShowUIElementPopup(UIHoverInfo hoverInfo) {
        if (gameData.ui.uiElementPopup.gameObject.activeInHierarchy) return;
        
        gameData.ui.uiElementPopup.gameObject.SetActive(true);
        TweenPopUp(gameData.ui.uiElementPopup.rectTransform);
        
        gameData.ui.uiElementPopup.descFitter.ForceRecalculate();
        FitPopupSize(gameData.ui.uiElementPopup.rectTransform, gameData.ui.uiElementPopup.descText.rectTransform.rect);
        
        // Set popup position
        Vector2 hoveredCenter = hoverInfo.hoveringTransform.WorldRect().center;
        Vector2 popupOffset = new(0f, hoverInfo.hoveringTransform.rect.height);
        gameData.ui.uiElementPopup.transform.position = hoveredCenter + popupOffset;
    }

    private void HideUIElementPopup() {
        gameData.ui.uiElementPopup.gameObject.SetActive(false);
    }
    
    private List<RectTransform> hoverableUIElements = new();
    private RectTransform toggledOffHoverableUIElement;
    
    public struct UIHoverInfo {
        public RectTransform hoveringTransform;
        public float timeSpentHovering;
        public bool shouldNotShow;
    }
    
    private UIHoverInfo lastUIHoverInfo;

    private UIHoverInfo UpdateUIHover() {
        UIHoverInfo info = new();
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        foreach (RectTransform element in hoverableUIElements) {
            if (!element.gameObject.activeInHierarchy) continue;
            
            Vector2 localMousePos = element.InverseTransformPoint(mousePos);
            Bounds localUiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(element);
            if (!localUiBounds.Contains(localMousePos)) continue;
            
            info.hoveringTransform = element;
            
            bool hoveringOverPrevElement = info.hoveringTransform == lastUIHoverInfo.hoveringTransform;
            if (hoveringOverPrevElement) {
                info.timeSpentHovering = lastUIHoverInfo.timeSpentHovering + Time.deltaTime;
            }
            else {
                info.timeSpentHovering = 0f;
            }
            
            break;
        }

        if (info.hoveringTransform == toggledOffHoverableUIElement) {
            info.shouldNotShow = true;
        }
        else {
            info.shouldNotShow = false;
            toggledOffHoverableUIElement = null;
        }
        
        lastUIHoverInfo = info;
        return info;
    }

    private void EnableInteractionPrompt(Vector3 position, string detailsString) {
        interactionDetails.gameObject.SetActive(true);
        interactionDetails.text = detailsString;
        
        interactPrompt.gameObject.SetActive(true);
        interactPrompt.text = $"<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(Styles.instance.inputIconTint)}>";
        interactPrompt.transform.position = gameData.camera.main.WorldToScreenPoint(position);
    }
    
    private void DisableInteractionPrompt() {
        interactPrompt.gameObject.SetActive(false);
        interactionDetails.gameObject.SetActive(false);
        
    }

}
