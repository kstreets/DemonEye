using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public partial class Game {
    
    private void InitUI() {
        Cursor.visible = true;
        Cursor.SetCursor(config.styles.cursorTexture, Vector2.zero, CursorMode.Auto);
        
        CloseHideoutUI();
        CloseRaidUI();
        ShowMainMenuUI();
        ui.menuBackButton.gameObject.SetActive(false);
        ui.largeRaidTextTypewriter.gameObject.SetActive(false);
    }

    private Sequence mainMenuSequence;
    
    private void AnimateInMainMenu() {
        if (mainMenuSequence.isAlive) return;
        
        float halfScreenHeight = Screen.height / 2f;
        var logo = mainMenu.logo;
        var playButton = mainMenu.playButton;
        var hideoutButton = mainMenu.hideoutButton;
        var settingsButton = mainMenu.settingsButton;
        var exitButton = mainMenu.exitButton;
        
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
        ui.hideoutParent.gameObject.SetActive(true);
        ui.animatedBgImage.gameObject.SetActive(true);
        mainMenu.parent.gameObject.SetActive(true);
        AnimateInMainMenu();
    }

    private void CloseMainMenuUI() {
        ui.animatedBgImage.gameObject.SetActive(false);
        mainMenu.parent.gameObject.SetActive(false);
    }

    private void ShowMapSelectionUI() {
        ShowHideoutUI();
        hideoutTabs.parent.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        ToggleHideoutPanels(playerPanel.panel, mapSelectionPanel.panel);
    }

    private void CloseMapSelectionUI() {
        CloseHideoutUI();
    }
    
    private void ShowHideoutUI() {
        ToggleHideoutTab(hideoutTabs.characterButton, hideoutTabs.characterText);
        ToggleHideoutPanels(playerPanel.panel, stashPanel.panel);
        ui.menuBackButton.gameObject.SetActive(true);
        playerInfo.coinsCurrencyParent.gameObject.SetActive(true);
        playerInfo.soulsCurrencyParent.gameObject.SetActive(true);
        playerInfo.healthBarParent.gameObject.SetActive(false);
        playerInfo.weightBarParent.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(true);
        ui.animatedBgImage.gameObject.SetActive(true);
        hideoutTabs.parent.gameObject.SetActive(true);
    }

    private void CloseHideoutUI() {
        ToggleHideoutPanels();
        HideInventoryItemPopup(); 
        HideUIElementPopup();
        ui.menuBackButton.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        ui.animatedBgImage.gameObject.SetActive(false);
        hideoutTabs.parent.gameObject.SetActive(false);
    }

    private void ShowRaidUI() {
        playerInfo.healthBarParent.gameObject.SetActive(true);
        playerInfo.weightBarParent.gameObject.SetActive(true);
        playerInfo.coinsCurrencyParent.gameObject.SetActive(false);
        playerInfo.soulsCurrencyParent.gameObject.SetActive(true);
        playerInfo.parent.gameObject.SetActive(true);
        raidInfo.parent.SetActive(true);
        ui.hotBarParent.gameObject.SetActive(true);

        // Initialize minimap for this raid (Tilemap GameObject must be active)
        {
            Tilemap tilemap = curRaid.mapInstance.mainTilemapRenderer.GetComponent<Tilemap>();
            tilemap.CompressBounds();
    
            Vector3 worldCenter = tilemap.LocalToWorld(tilemap.localBounds.center); 
            Vector3 worldSize = (Vector3)tilemap.cellBounds.size * tilemap.cellSize.x; 
    
            ui.minimap.Init(curRaid.map.minimapTexture, worldCenter, worldSize);
            ui.minimap.gameObject.SetActive(true);
        }
    }

    private void CloseRaidUI() {
        HideInventoryItemPopup(); 
        HideUIElementPopup();
        ui.interactPrompt.gameObject.SetActive(false);
        ui.interactDetails.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        raidInfo.parent.SetActive(false);
        ui.portalArrow.gameObject.SetActive(false);
        ui.hotBarParent.gameObject.SetActive(false);
        ui.minimap.gameObject.SetActive(false);
    }

    private void ToggleHideoutTab(Button button, TextMeshProUGUI text) {
        Sprite tabSelectedSprite = hideoutTabs.selectedSprite;
        Sprite tabNonSelectedSprite = hideoutTabs.nonSelectedSprite;
        
        hideoutTabs.characterButton.image.sprite = tabNonSelectedSprite;
        hideoutTabs.eyeForgeButton.image.sprite = tabNonSelectedSprite;
        hideoutTabs.traderButton.image.sprite = tabNonSelectedSprite;
        hideoutTabs.questsButton.image.sprite = tabNonSelectedSprite;
        hideoutTabs.skillsButton.image.sprite = tabNonSelectedSprite;
        
        hideoutTabs.characterText.margin = config.styles.nonSelectedHideoutTabMargin;
        hideoutTabs.eyeForgeText.margin = config.styles.nonSelectedHideoutTabMargin;
        hideoutTabs.traderText.margin = config.styles.nonSelectedHideoutTabMargin;
        hideoutTabs.questsText.margin = config.styles.nonSelectedHideoutTabMargin;
        hideoutTabs.skillsText.margin = config.styles.nonSelectedHideoutTabMargin;
        
        button.image.sprite = tabSelectedSprite;
        text.margin = config.styles.selectedHideoutTabMargin;
    }

    private void ToggleHideoutPanels(params RectTransform[] panels) {
        playerPanel.panel.gameObject.SetActive(false);
        stashPanel.panel.gameObject.SetActive(false);
        eyeForgePanel.panel.gameObject.SetActive(false);
        eyeForgeDetailsPanel.panel.gameObject.SetActive(false);
        ui.lootInventoryPanel.gameObject.SetActive(false);
        traderPanel.panel.gameObject.SetActive(false);
        transactionPanel.panel.gameObject.SetActive(false);
        questsPanel.panel.gameObject.SetActive(false);
        skillsPanel.panel.gameObject.SetActive(false);
        skillsPanel.playerStatsPanel.gameObject.SetActive(false);
        mapSelectionPanel.panel.gameObject.SetActive(false);
        
        foreach (RectTransform rect in panels) {
            rect.gameObject.SetActive(true);
        }
    }
    
    // Here just so that we don't allocate strings every frame
    private int prevSoulCurrency = int.MinValue;
    private int prevCoinCurrency = int.MinValue;
    
    private void UpdateCurrencyNumbers() {
        if (prevSoulCurrency != player.state.soulCurrency) {
            playerInfo.soulsCurrencyText.text = player.state.soulCurrency.ToString("N0");
        }
        if (prevCoinCurrency != player.state.coinCurrency) {
            playerInfo.coinCurrencyText.text = player.state.coinCurrency.ToString("N0");
        }
        prevSoulCurrency = player.state.soulCurrency;
        prevCoinCurrency = player.state.coinCurrency;
    }
    
    private void UpdateInRaidUI() {
        ui.minimap.UpdateMinimap(player.position);
        
        playerInfo.healthBarFillImage.fillAmount = player.health / (float)FullPlayerHealth();
        playerInfo.bleedDebuffIcon.gameObject.SetActive(player.bleeding);
        
        GetEncumberingWeightRange(out int startingEncumberingWeight, out _);
        int inventoryWeight = GetInventoryWeight(inventories.player);
        playerInfo.weightBarFillImage.fillAmount = Mathf.Clamp01(inventoryWeight / (float)startingEncumberingWeight);
        
        float overweightComp = GetOverweightCompletion();
        if (overweightComp > 0f) {
            playerInfo.weightBarFillImage.color = Color.Lerp(config.styles.startingOverWeightColor, config.styles.endingOverWeightColor, overweightComp);
        }
        else {
            playerInfo.weightBarFillImage.color = config.styles.underWeightColor;
        }
        
        if (curRaid.stateSwitchedThisFrame) {
            if (curRaid.state == RaidState.InitialWaves) {
                raidInfo.waveText.gameObject.SetActive(true); 
            }
            else if (curRaid.state == RaidState.FinalWave) {
                raidInfo.waveText.text = "Final Wave";
                Tween.Scale(raidInfo.waveText.transform, 0f, 1f, 0.5f, Ease.OutBack);
                AnimateSmallRaidText(ColorText("Final Wave", config.styles.decreaseDescColor));
            }
            else if (curRaid.state == RaidState.PostFinalWave) {
                raidInfo.waveText.gameObject.SetActive(false);
            }
        }
        
        if (raidInfo.waveText.gameObject.activeInHierarchy) {
            raidInfo.waveText.text = $"Wave {spawnManager.CurWaveNumber}/{spawnManager.TotalWaveCount}";
        }
    }
    
    private void UpdatePlayerPanelUI() {
        if (!PlayerInventoryIsOpen) return;
            
        playerPanel.healthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/{FullPlayerHealth()}";

        int inventoryWeight = GetInventoryWeight(inventories.player);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanel.weightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
        
        Color boostedColor = config.styles.increaseDescColor;
        EquipedStatsPanel equipedStatsPanel = playerPanel.equipedStatsPanel;
        
        equipedStatsPanel.bleedResistText.text = Boosted(PlayerStat.BleedResist) ? 
            DisplayProb(GetAbsoluteStat(PlayerStat.BleedResist), boostedColor) : 
            DisplayProbNoColor(GetAbsoluteStat(PlayerStat.BleedResist));
        
        equipedStatsPanel.critChanceText.text = Boosted(PlayerStat.CritChance) ? 
            DisplayProb(GetAbsoluteStat(PlayerStat.CritChance), boostedColor) :
            DisplayProbNoColor(GetAbsoluteStat(PlayerStat.CritChance));
        
        equipedStatsPanel.critMultiText.text = Boosted(PlayerStat.CritMulti) ? 
            DisplayMultiplier(GetAbsoluteStat(PlayerStat.CritMulti), boostedColor) :
            DisplayMultiplierNoColor(GetAbsoluteStat(PlayerStat.CritMulti));
        
        equipedStatsPanel.damageText.text = Boosted(PlayerStat.DamageMulti) ? 
            DisplayMultiplier(GetAbsoluteStat(PlayerStat.DamageMulti), boostedColor) :
            DisplayMultiplierNoColor(GetAbsoluteStat(PlayerStat.DamageMulti));
        
        equipedStatsPanel.firerateText.text = Boosted(PlayerStat.FireratePercentage) ? 
            DisplayProb(GetAbsoluteStat(PlayerStat.FireratePercentage), boostedColor) :
            DisplayProbNoColor(GetAbsoluteStat(PlayerStat.FireratePercentage));
        
        equipedStatsPanel.projectileCountText.text = Boosted(PlayerStat.ProjectileCount) ? 
            DisplayNumber(GetAbsoluteStat(PlayerStat.ProjectileCount), boostedColor) :
            DisplayNumberNoColor(GetAbsoluteStat(PlayerStat.ProjectileCount));
        
        equipedStatsPanel.rangeText.text = Boosted(PlayerStat.RangePercentage) ? 
            DisplayProb(GetAbsoluteStat(PlayerStat.RangePercentage), boostedColor) :
            DisplayProbNoColor(GetAbsoluteStat(PlayerStat.RangePercentage));
        
        bool Boosted(PlayerStat stat) => GetEquipmentStatAdjustment(stat) > 0f; 
    }
    
    private void UpdateHotBarUI() {
        if (!ui.hotBarParent.gameObject.activeInHierarchy) return;

        for (int i = 0; i < playerQuickUseSize; i++) {
            int itemIndex = i + playerEquipmentSize;
            hotBar.slotUIs[i].ClearItem();

            ItemInstance itemInstance = inventories.player.slots[itemIndex].itemInstance;
            if (itemInstance != null) {
                hotBar.slotUIs[i].SetItem(itemInstance.ItemRef, itemInstance.count);
            } 
        }
    }

    private void AnimateLargeRaidText(string text, float typewriterSpeed) {
        ui.largeRaidText.gameObject.SetActive(true);
        ui.largeRaidText.characterSpacing = 0;
        
        ui.largeRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{wave}}{{#fade}}{{#wave}}{text}");
        ui.largeRaidTextTypewriter.SetTypewriterSpeed(typewriterSpeed);
        ui.largeRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.Chain(Tween.Custom(0, 30, 0.5f, startDelay: 0.3f, ease: Ease.OutBack, onValueChange: static (val) => {
                gameInstance.ui.largeRaidText.characterSpacing = val;
            }));
            sequence.ChainDelay(0.35f);
            sequence.ChainCallback(static () => gameInstance.ui.largeRaidTextTypewriter.StartDisappearingText());
        }
    }

    private void AnimateSmallRaidText(string text) {
        ui.smallRaidText.gameObject.SetActive(true);
        ui.smallRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{smallwave}}{{#fade}}{{#smallwave}}{text}");
        ui.smallRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.ChainDelay(0.8f);
            sequence.ChainCallback(static () => gameInstance.ui.smallRaidTextTypewriter.StartDisappearingText());
        }
    }
    
    // *******************************
    // Gameplay Text Pop Ups
    // *******************************
    
    private enum DamageColor { Normal, Crit, Blood, Hemorrhage, Poison }

    private void SpawnPlayerDamageNumber(int damage) {
        var healthBar = playerInfo.healthBarFillImage;
        var healthBarRect = healthBar.rectTransform;
        Vector3 spawnPosAlongHealthBar = healthBarRect.position.Offset(x: healthBarRect.rect.width * healthBar.fillAmount);
        
        Vector3 startSize = Vector3.one * 0.8f;
        Vector3 endSize = Vector3.one;
        
        float normalizedScaleFromDamage = Mathf.Clamp01(damage / 30f);
        
        float xOffset = healthBarRect.rect.width * 0.35f;
        float yOffset = Mathf.Lerp(-50f, -200f, normalizedScaleFromDamage);
        Vector2 endDamageNumPos = spawnPosAlongHealthBar.Offset(x: xOffset, y: yOffset);

        Entity damageNumber = SpawnEntity(entityPools.damageNumber, spawnPosAlongHealthBar, Quaternion.identity, playerInfo.damageNumberSpawnPos);
        damageNumber.textMesh.text = $"-{damage}";
        damageNumber.textMesh.color = config.styles.playerDamageColor;
        
        float playerDamageFontSize = Mathf.Lerp(40f, 55f, normalizedScaleFromDamage);
        damageNumber.textMesh.fontSize = playerDamageFontSize;
        
        const float moveDuration = 0.6f;
        const float scaleUpDuration = 0.35f;
        const float popOutDuration = 0.4f;

        Tween.Position(damageNumber.trans, endDamageNumPos, moveDuration, Ease.OutBack)
            .Group(Tween.Scale(damageNumber.trans, startSize, endSize, scaleUpDuration, Ease.InOutBack))
            .Chain(Tween.Scale(damageNumber.trans, 0f, popOutDuration, Ease.InBack));
        DestroyEntity(damageNumber, moveDuration + popOutDuration);
    }

    private void SpawnDamageNumber(Vector3 spawnPos, int damage, DamageColor damageColor) {
        Vector3 startSize = Vector3.one * 0.8f;
        Vector3 endSize = Vector3.one * damageColor switch {
            DamageColor.Normal     => 1.0f,
            DamageColor.Crit       => 1.25f,
            DamageColor.Blood      => 0.8f,
            DamageColor.Hemorrhage => 1.25f,
            DamageColor.Poison     => 0.8f,
            _                      => 1f,
        };
        
        float xOffset = Random.Range(-0.08f, 0.08f);
        float yOffset = Random.Range(0.05f, 0.1f);
        Vector2 endDamageNumPos;
        
        if (damageColor == DamageColor.Blood || damageColor == DamageColor.Hemorrhage) {
            spawnPos = OffsetY(spawnPos, 0.05f);
            endDamageNumPos = OffsetY(spawnPos, yOffset * 2.3f);
        }
        else {
            endDamageNumPos = OffsetY(OffsetX(spawnPos, xOffset), yOffset);
        }
        
        Entity damageNumber = SpawnEntity(entityPools.damageNumber, spawnPos, Quaternion.identity, ui.damageNumbersParent);
        damageNumber.textMesh.text = damage.ToString();
        
        const float worldDamageNumberFontSize = 0.11f;
        damageNumber.textMesh.fontSize = worldDamageNumberFontSize;
        
        const float alpha = 0.68f;
        switch (damageColor) {
            case DamageColor.Normal:
                damageNumber.textMesh.color = config.styles.normalDamageColor.Alpha(alpha);
                break;
            case DamageColor.Crit:
                damageNumber.textMesh.color = config.styles.critDamageColor.Alpha(alpha);
                break;
            case DamageColor.Blood:
                damageNumber.textMesh.color = config.styles.bleedDamageColor.Alpha(alpha);
                break;
            case DamageColor.Hemorrhage:
                damageNumber.textMesh.color = config.styles.hemorrhageDamageColor.Alpha(alpha);
                break;
            case DamageColor.Poison:
                damageNumber.textMesh.color = config.styles.poisonDamageColor.Alpha(alpha);
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
        Entity textEntity = SpawnEntity(entityPools.damageNumber, spawnPos, Quaternion.identity, ui.damageNumbersParent);
        textEntity.textMesh.text = text; 
        textEntity.textMesh.color = config.styles.popInTextColor;
        
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
        if (ui.uiElementPopup.gameObject.activeInHierarchy) return;
        
        ui.uiElementPopup.gameObject.SetActive(true);
        TweenPopUp(ui.uiElementPopup.rectTransform);
        
        ui.uiElementPopup.descFitter.ForceRecalculate();
        FitPopupSize(ui.uiElementPopup.rectTransform, ui.uiElementPopup.descText.rectTransform.rect);
        
        // Set popup position
        Vector2 hoveredCenter = hoverInfo.hoveringTransform.WorldRect().center;
        Vector2 popupOffset = new(0f, hoverInfo.hoveringTransform.rect.height);
        ui.uiElementPopup.transform.position = hoveredCenter + popupOffset;
    }

    private void HideUIElementPopup() {
        ui.uiElementPopup.gameObject.SetActive(false);
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
        ui.interactDetails.gameObject.SetActive(true);
        ui.interactDetails.text = detailsString;
        
        ui.interactPrompt.gameObject.SetActive(true);
        ui.interactPrompt.text = $"<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(config.styles.inputIconTint)}>";
        ui.interactPrompt.transform.position = camera.main.WorldToScreenPoint(position);
    }
    
    private void DisableInteractionPrompt() {
        ui.interactPrompt.gameObject.SetActive(false);
        ui.interactDetails.gameObject.SetActive(false);
        
    }

}
