using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class Game {
    
    private bool OnCharacterTab => hideoutTabs.toggleGroup.IsSelected(hideoutTabs.characterButton);
    private bool OnEyeForgeTab => hideoutTabs.toggleGroup.IsSelected(hideoutTabs.eyeForgeButton);
    private bool OnTradingTab => hideoutTabs.toggleGroup.IsSelected(hideoutTabs.traderButton);
    private bool OnQuestsTab => hideoutTabs.toggleGroup.IsSelected(hideoutTabs.questsButton);
    
    private bool ShowingMainMenu => mainMenu.parent.gameObject.activeInHierarchy;
    private bool ShowingPlayerPanel => playerPanel.panel.gameObject.activeInHierarchy;
    private bool ShowingForgeDetailsPanel => eyeForgeDetailsPanel.panel.gameObject.activeInHierarchy;
    
    private void InitUI() {
        Cursor.visible = !usingController;
        Cursor.SetCursor(config.styles.cursorTexture, Vector2.zero, CursorMode.Auto);

        // Remember where the currencies go in the player info panel, so they can be put back after being moved for small screens
        currencyInfoParent = playerInfo.coinsCurrencyParent.transform.parent;
        coinsCurrencySiblingIndex = playerInfo.coinsCurrencyParent.transform.GetSiblingIndex();
        soulsCurrencySiblingIndex = playerInfo.soulsCurrencyParent.transform.GetSiblingIndex();

        CloseSettingsMenuUI();
        CloseHideoutUI();
        CloseRaidUI();
        ShowMainMenuUI();
        InitCurrencyNumbers();
        InitUIHints();
        
        ui.levelUpNotification.Init();
        ui.menuBackButton.gameObject.SetActive(false);
        ui.messagePopup.Hide();
        ui.largeRaidTextTypewriter.gameObject.SetActive(false);
        ui.openingDialogueTypewriter.gameObject.SetActive(false);
        ui.traderTutorialDialogueBox.SetActive(false);
        mainMenu.hideoutNotifier.SetActive(false);
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

        // Start everything off screen right away, in case the sequence is paused before it first updates
        logo.anchoredPosition = new(logo.anchoredPosition.x, halfScreenHeight);
        playButton.rectTransform.anchoredPosition = new(playButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        hideoutButton.rectTransform.anchoredPosition = new(hideoutButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        settingsButton.rectTransform.anchoredPosition = new(settingsButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        exitButton.rectTransform.anchoredPosition = new(exitButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
    }

    private void ShowMainMenuUI() {
        ui.hideoutParent.gameObject.SetActive(true);
        ui.animatedBgImage.gameObject.SetActive(true);
        mainMenu.parent.gameObject.SetActive(true);
        mainMenu.hideoutNotifier.SetActive(InTutorial && tutorial.stateMachine.PassedThisState(tutorial.inSlaughterMap));
        AnimateInMainMenu();
    }

    private void CloseMainMenuUI() {
        ui.animatedBgImage.gameObject.SetActive(false);
        mainMenu.parent.gameObject.SetActive(false);
        mainMenu.hideoutNotifier.SetActive(false);
    }
    
    private void ShowSettingsMenuUI() {
        ui.settingsParent.gameObject.SetActive(true);
        ui.menuBackButton.gameObject.SetActive(true);
        ui.animatedBgImage.gameObject.SetActive(true);
        settings.toggleGroup.ManualyToggle(settings.audioToggle);
    }
    
    private void CloseSettingsMenuUI() {
        ui.settingsParent.gameObject.SetActive(false);
        ui.menuBackButton.gameObject.SetActive(false);
        ui.animatedBgImage.gameObject.SetActive(false);
        SaveSettings();
    }

    private void ShowMapSelectionUI() {
        ShowHideoutUI();
        hideoutTabs.parent.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        // The player info is hidden here, so keep the currencies in it so they're hidden too
        SetCurrencyDisplaysInHideout(false);
        ToggleHideoutPanels(playerPanel.panel, mapPanels.mapSelectionPanel.rectTransform);
        mapPanels.mapSelectionPanel.UpdateSelectorStates(config.maps);
        ui.teleportingIntoRaidHeader.gameObject.SetActive(true);
    }
    
    private void ShowMapConfirmationUI(MapData map) {
        mapPanels.mapSelectionPanel.gameObject.SetActive(false);
        mapPanels.confirmationPanel.gameObject.SetActive(true);
        mapPanels.confirmationPanel.Display(map);
    }

    private void CloseMapSelectionUI() {
        CloseHideoutUI();
    }
    
    private void ShowHideoutUI() {
        SetCurrencyDisplaysInHideout(true);
        hideoutTabs.toggleGroup.ManualyToggle(hideoutTabs.characterButton);
        ui.menuBackButton.gameObject.SetActive(true);
        inputPrompts.hideoutParent.gameObject.SetActive(!InMapSelection);
        playerInfo.coinsCurrencyParent.gameObject.SetActive(true);
        playerInfo.soulsCurrencyParent.gameObject.SetActive(true);
        playerInfo.healthBarParent.gameObject.SetActive(false);
        playerInfo.weightBarParent.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(true);
        ui.animatedBgImage.gameObject.SetActive(true);
        hideoutTabs.parent.gameObject.SetActive(true);
    }

    private void CloseHideoutUI() {
        SetCurrencyDisplaysInHideout(false);
        ToggleHideoutPanels();
        HideInventoryItemPopup(); 
        HideHint();
        ToggleSlimPlayerPanel(false);
        ui.menuBackButton.gameObject.SetActive(false);
        inputPrompts.hideoutParent.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        ui.animatedBgImage.gameObject.SetActive(false);
        ui.teleportingIntoRaidHeader.gameObject.SetActive(false);
        hideoutTabs.parent.gameObject.SetActive(false);
    }

    private void ShowRaidUI() {
        SetCurrencyDisplaysInHideout(false);
        playerInfo.healthBarParent.gameObject.SetActive(true);
        playerInfo.weightBarParent.gameObject.SetActive(true);
        playerInfo.coinsCurrencyParent.gameObject.SetActive(false);
        playerInfo.soulsCurrencyParent.gameObject.SetActive(true);
        playerInfo.parent.gameObject.SetActive(true);
        raidInfo.parent.SetActive(true);
        ui.hotBarParent.gameObject.SetActive(true);
        
        inputPrompts.inRaidParent.SetActive(true);
        // Transfer hideout prompts to raid to reduce prompt bindings
        foreach (Transform invPrompt in inputPrompts.allInventoryPrompts) {
            invPrompt.parent = inputPrompts.inRaidParent.transform;
            invPrompt.gameObject.SetActive(false);
        }

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
        HideHint();
        ui.interactPrompt.gameObject.SetActive(false);
        ui.interactDetails.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        raidInfo.parent.SetActive(false);
        ui.portalArrow.gameObject.SetActive(false);
        ui.hotBarParent.gameObject.SetActive(false);
        ui.minimap.gameObject.SetActive(false);
        
        inputPrompts.inRaidParent.SetActive(false);
        foreach (Transform invPrompt in inputPrompts.allInventoryPrompts) {
            invPrompt.parent = inputPrompts.hideoutParent.transform;
            invPrompt.gameObject.SetActive(true);
        }
    }

    private void ToggleHideoutPanels(params RectTransform[] panels) {
        // Panels that stay showing aren't turned off and back on, otherwise their slots and buttons lose their hover highlight
        SetHideoutPanelActive(playerPanel.panel, panels);
        SetHideoutPanelActive(stashPanel.panel, panels);
        SetHideoutPanelActive(eyeForgePanel.panel, panels);
        SetHideoutPanelActive(eyeForgeDetailsPanel.panel, panels);
        SetHideoutPanelActive(ui.lootInventoryPanel, panels);
        SetHideoutPanelActive(traderPanel.panel, panels);
        SetHideoutPanelActive(transactionPanel.panel, panels);
        SetHideoutPanelActive(questsPanel.panel, panels);
        SetHideoutPanelActive(skillsPanel.panel.rectTransform, panels);
        SetHideoutPanelActive(skillsPanel.playerStatsPanel.rectTransform, panels);
        SetHideoutPanelActive(mapPanels.mapSelectionPanel.rectTransform, panels);
        SetHideoutPanelActive((RectTransform)mapPanels.confirmationPanel.transform, panels);
        
        void SetHideoutPanelActive(RectTransform panel, RectTransform[] panelsToShow) {
            panel.gameObject.SetActive(Array.IndexOf(panelsToShow, panel) >= 0);
        }
    }

    private Sequence FadeOutAndCollapsePanel(RectTransform panel, float fadeTime = 0.2f, float collapseTime = 0.35f) {
        HorizontalLayoutGroup layout = panel.parent.GetComponentInParent<HorizontalLayoutGroup>();
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        LayoutElement layoutElement = panel.GetComponent<LayoutElement>();
        
        float startWidth = panel.rect.width;
        float startSpacing = layout.spacing;
        float origMinWidth = layoutElement.minWidth;
        float origPreferredWidth = layoutElement.preferredWidth;

        // Stop drags and clicks landing on a panel that's leaving
        canvasGroup.blocksRaycasts = false;
        HidePopupsFromPanel(panel);
        // Otherwise the content's min width stops the panel shrinking
        layoutElement.minWidth = 0f;

        return Sequence.Create()
            .Chain(Tween.Alpha(canvasGroup, 0f, fadeTime, Ease.OutQuad))
            .Chain(Tween.Custom(0f, 1f, collapseTime, ease: Ease.InOutCubic, onValueChange: t => {
                layoutElement.preferredWidth = Mathf.Lerp(startWidth, 0f, t);
                // Shrink the gap too, otherwise the remaining panel snaps sideways when this one is deactivated
                layout.spacing = Mathf.Lerp(startSpacing, 0f, t);
            }))
            .ChainCallback(() => {
                panel.gameObject.SetActive(false);
                // Restore everything so the panel shows normally next time
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                layoutElement.minWidth = origMinWidth;
                layoutElement.preferredWidth = origPreferredWidth;
                layout.spacing = startSpacing;
            });
    }

    // Hides the item and hint popups if they belong to something inside the panel
    private void HidePopupsFromPanel(RectTransform panel) {
        InventoryHoverInfo invHover = lastInventoryHoverInfo;
        if (invHover.inventory != null && invHover.slotIndex >= 0 && invHover.slotIndex < invHover.inventory.slots.Length) {
            if (invHover.inventory.slots[invHover.slotIndex].ui.rectTransform.IsChildOf(panel)) {
                HideInventoryItemPopup();
            }
        }

        RectTransform hintedTransform = uiHints.lastHintHoverInfo.hoveringTransform;
        if (hintedTransform && hintedTransform.IsChildOf(panel)) {
            HideHint();
            uiHints.lastHintHoverInfo = default; // Restarts the hover delay so it doesn't pop straight back up when the panel returns
        }
    }

    // Panels turn off their CanvasGroup's raycasts while fading, so anything inside one shouldn't count as hovered.
    // Otherwise popups that were just hidden would show again before the panel finishes fading.
    private static bool InNonInteractableCanvasGroup(Transform trans) {
        for (Transform cur = trans; cur != null; cur = cur.parent) {
            if (!cur.TryGetComponent(out CanvasGroup group)) continue;
            if (!group.blocksRaycasts) return true;
            if (group.ignoreParentGroups) return false;
        }
        return false;
    }

    private Sequence FadeInAndExpandPanel(RectTransform panel, float expandTime = 0.35f, float fadeTime = 0.2f) {
        HorizontalLayoutGroup layout = panel.parent.GetComponentInParent<HorizontalLayoutGroup>();
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        LayoutElement layoutElement = panel.GetComponent<LayoutElement>();

        float endSpacing = layout.spacing;
        float origMinWidth = layoutElement.minWidth;
        float origPreferredWidth = layoutElement.preferredWidth;

        canvasGroup.alpha = 0f;
        // Stop drags and clicks landing on a panel that's still arriving
        canvasGroup.blocksRaycasts = false;
        panel.gameObject.SetActive(true);
        // Measure before overriding the layout element, so this is the width the layout will give the panel once restored
        float endWidth = LayoutUtility.GetPreferredWidth(panel);

        layoutElement.minWidth = 0f;
        layoutElement.preferredWidth = 0f;
        layout.spacing = 0f;

        return Sequence.Create()
            .Chain(Tween.Custom(0f, 1f, expandTime, ease: Ease.InOutCubic, onValueChange: t => {
                layoutElement.preferredWidth = Mathf.Lerp(0f, endWidth, t);
                layout.spacing = Mathf.Lerp(0f, endSpacing, t);
            }))
            .Chain(Tween.Alpha(canvasGroup, 1f, fadeTime, Ease.OutQuad))
            .ChainCallback(() => {
                canvasGroup.blocksRaycasts = true;
                layoutElement.minWidth = origMinWidth;
                layoutElement.preferredWidth = origPreferredWidth;
                layout.spacing = endSpacing;
            });
    }

    private Sequence FadeInHideout(float riseDistance = 60f, float time = 1.4f) {
        RectTransform hideout = ui.hideoutPanelsParent;
        LayoutGroup layout = hideout.GetComponent<LayoutGroup>();
        if (!hideout.TryGetComponent(out CanvasGroup canvasGroup)) {
            canvasGroup = hideout.gameObject.AddComponent<CanvasGroup>();
        }

        RectOffset padding = layout.padding;
        int endTop = padding.top;
        int endBottom = padding.bottom;

        canvasGroup.alpha = 0f;
        // Stop drags and clicks landing on the hideout while it's still moving
        canvasGroup.blocksRaycasts = false;
        hideout.gameObject.SetActive(true);

        return Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1f, time, Ease.OutQuad))
            .Group(Tween.Custom(riseDistance, 0f, time, ease: Ease.OutCubic, onValueChange: offset => {
                // The content is centered, so shift both sides to move it by the full offset without changing the space it gets
                int pixelOffset = Mathf.RoundToInt(offset);
                padding.top = endTop + pixelOffset;
                padding.bottom = endBottom - pixelOffset;
                // Editing the padding's fields doesn't dirty the layout on its own
                LayoutRebuilder.MarkLayoutForRebuild(hideout);
            }))
            .ChainCallback(() => {
                canvasGroup.blocksRaycasts = true;
                padding.top = endTop;
                padding.bottom = endBottom;
                LayoutRebuilder.MarkLayoutForRebuild(hideout);
            });
    }

    public static Tween FadeIn(CanvasGroup canvasGroup, float time = 0.3f, Ease ease = Ease.OutQuad, float startDelay = 0f) {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.gameObject.SetActive(true);
        return Tween.Alpha(canvasGroup, 1f, time, ease, startDelay: startDelay)
        .OnComplete(canvasGroup, static canvasGroup => canvasGroup.blocksRaycasts = true);
    }

    public static Tween FadeIn(TMP_Text text, float time = 0.3f, Ease ease = Ease.OutQuad, float startDelay = 0f) {
        text.alpha = 0f;
        text.gameObject.SetActive(true);
        return Tween.Alpha(text, 1f, time, ease, startDelay: startDelay);
    }

    // Its better just to have these as constants because the canvas layout recalculates in LateUpdate
    private const float playerPanelWidth = 600f;
    private const float slimPlayerPanelWidth = 440f;
    private bool PlayerInventoryIsSlim => !playerPanel.inventoryParent.gameObject.activeSelf;

    private void ToggleSlimPlayerPanel(bool toggle) {
        Vector2 defaultPlayerHalfAnchorPos = new(0f, playerPanel.playerHalfParent.anchoredPosition.y);
        if (toggle) {
            playerPanel.inventoryParent.gameObject.SetActive(false);
            playerPanel.panelLayoutElement.preferredWidth = slimPlayerPanelWidth;
            float centeringOffset = (slimPlayerPanelWidth - playerPanel.playerHalfParent.rect.width) / 2f;
            playerPanel.playerHalfParent.anchoredPosition = defaultPlayerHalfAnchorPos.Offset(x: centeringOffset);
            return;
        }
        playerPanel.inventoryParent.gameObject.SetActive(true);
        playerPanel.panelLayoutElement.preferredWidth = playerPanelWidth;
        playerPanel.playerHalfParent.anchoredPosition = defaultPlayerHalfAnchorPos;
    }
    
    private int prevSoulCurrency = int.MinValue;
    private int prevCoinCurrency = int.MinValue;
    private Sequence soulCurrencySequence;
    private Sequence coinCurrencySequence;
    
    private void InitCurrencyNumbers() {
        playerInfo.soulsCurrencyText.text = player.state.soulCurrency.ToString("N0");
        playerInfo.coinCurrencyText.text = player.state.coinCurrency.ToString("N0");
        prevSoulCurrency = player.state.soulCurrency;
        prevCoinCurrency = player.state.coinCurrency;
    }
    
    private void UpdateCurrencyNumbers() {
        if (prevSoulCurrency != player.state.soulCurrency) {
            soulCurrencySequence.Complete();
            soulCurrencySequence = AnimateCurrency(playerInfo.soulsCurrencyText, prevSoulCurrency, player.state.soulCurrency);
        }
        if (prevCoinCurrency != player.state.coinCurrency) {
            coinCurrencySequence.Complete();
            coinCurrencySequence = AnimateCurrency(playerInfo.coinCurrencyText, prevCoinCurrency, player.state.coinCurrency);
        }
        prevSoulCurrency = player.state.soulCurrency;
        prevCoinCurrency = player.state.coinCurrency;
    }
    
    public static Sequence AnimateCurrency(TextMeshProUGUI textMesh, int previous, int current) {
        const float duration = 0.25f;
        Sequence seq = Sequence.Create();
        seq.Group(Tween.Custom(textMesh, previous, current, duration, static (textMesh, val) => {
            textMesh.text = val.ToString("N0");
        }));
        seq.Group(Tween.PunchScale(textMesh.transform, Vector3.one * 0.22f, duration * 0.75f));
        return seq;
    }
    
    private void UpdateInRaidUI() {
        ui.minimap.UpdateMinimap(player.position);
        
        playerInfo.healthBarFillImage.fillAmount = CurPlayerHealthPercentage();
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
                waveTextSequence.Stop();
                raidInfo.waveText.transform.localScale = Vector3.one;
                raidInfo.waveText.gameObject.SetActive(true);
                raidInfo.waveText.text = WaveCountText();
                displayedWaveNumber = spawnManager.CurWaveNumber;
            }
            else if (curRaid.state == RaidState.FinalWave) {
                AnimateWaveTextChange("Final Wave");
                AnimateSmallRaidText(ColorText("Final Wave", config.styles.decreaseDescColor));
            }
            else if (curRaid.state == RaidState.PostFinalWave) {
                raidInfo.waveText.gameObject.SetActive(false);
            }
        }

        if (curRaid.state == RaidState.InitialWaves && displayedWaveNumber != spawnManager.CurWaveNumber) {
            displayedWaveNumber = spawnManager.CurWaveNumber;
            AnimateWaveTextChange(WaveCountText());
        }
    }

    private int displayedWaveNumber;
    private Sequence waveTextSequence;

    private string WaveCountText() => $"Wave {spawnManager.CurWaveNumber}/{spawnManager.TotalWaveCount}";

    // Grows the wave text, swaps the text at its largest, then shrinks it back down
    private void AnimateWaveTextChange(string newText) {
        waveTextSequence.Complete();
        TextMeshProUGUI waveText = raidInfo.waveText;
        waveTextSequence = Sequence.Create()
            .Chain(Tween.Scale(waveText.transform, 1.35f, 0.15f, Ease.OutQuad))
            .ChainCallback(() => waveText.text = newText)
            .Chain(Tween.Scale(waveText.transform, 1f, 0.3f, Ease.OutBack));
    }
    
    private void UpdatePlayerPanelUI() {
        if (!PlayerInventoryIsOpen) return;
            
        playerPanel.healthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/{FullPlayerHealth()}";

        int inventoryWeight = GetInventoryWeight(inventories.player);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanel.weightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
        
        Color boostedColor = config.styles.increaseDescColor;
        EquipedStatsPanel equipedStatsPanel = playerPanel.equipedStatsPanel;
        
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
            // Controller only has the selected slot bound, so it's highlighted. Not while the inventory is open, since the hotbar can't be used then.
            bool inventoryOpen = PlayerInventoryIsOpen || LootInventoryIsOpen;
            hotBar.slotUIs[i].SetOutlined(UsingControllerControls && !inventoryOpen && i == hotBar.selectedIndex);

            ItemInstance itemInstance = inventories.player.slots[itemIndex].itemInstance;
            if (itemInstance != null) {
                hotBar.slotUIs[i].SetItem(itemInstance);
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
    
    private void SpawnTrinketActivationText(string text, Color? color = null) {
        SpawnTextPopIn(OffsetY(player.position, -0.1f), text);
    }
    
    private void SpawnTextPopIn(Vector3 spawnPos, string text, Vector3? endPos = default, Color? color = null) {
        Entity textEntity = SpawnEntity(entityPools.damageNumber, spawnPos, Quaternion.identity, ui.damageNumbersParent);
        textEntity.textMesh.text = text; 
        textEntity.textMesh.color = color ?? config.styles.popInTextColor;
        
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
    
    public void PushNotification(string text) {
        Entity notification = SpawnEntity(entityPools.notification, Vector3.zero, Quaternion.identity, ui.notificationParent, EntityLifetime.Global);
        notification.gameObject.GetComponent<NotificationUI>().text.text = text;
        
        const float startWidth = 0f;
        float endWidth = ui.notificationParent.rect.width;
        
        TweenSettings inSettings = new() {
            duration = 0.4f,
            ease = Ease.OutBack,
        };

        Sequence.Create()
            .Chain(
                Tween.Custom(notification, startWidth, endWidth, inSettings, static (notification, width) => {
                    notification.gameObject.GetComponent<NotificationUI>().backgroundParent.ResizeWidth(width);
                })
            )
            .ChainDelay(4f)
            .Chain(AnimateNotificationOut(notification, endWidth));
    }

    private Sequence AnimateNotificationOut(Entity notification, float fromWidth) {
        TweenSettings outSettings = new() {
            duration = 0.25f,
            ease = Ease.Linear,
        };

        return Sequence.Create()
            .Chain(
                Tween.Custom(notification, fromWidth, 0f, outSettings, static (notification, width) => {
                    notification.gameObject.GetComponent<NotificationUI>().backgroundParent.ResizeWidth(width);
                })
            )
            .ChainCallback(notification, static (notification) => gameInstance.DestroyEntity(notification));
    }

    // Stopping all tweens (like on death) freezes notifications partway through, so they'd never go away on their own
    private void DismissNotificationsAfterTweensStopped() {
        foreach (Transform child in ui.notificationParent) {
            if (!child.gameObject.activeSelf) continue;
            if (!entities.lookup.TryGetValue(child.gameObject, out Entity notification)) continue;
            // Shrinks from whatever width it's at, since it might have been stopped partway through animating in
            float curWidth = child.GetComponent<NotificationUI>().backgroundParent.sizeDelta.x;
            AnimateNotificationOut(notification, curWidth);
        }
    }
    
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
    
    
    public class UIHints {
        public readonly List<RectTransform> hoverableRectTransforms = new();
        public readonly List<InventorySlot> hoverableInventorySlots = new();
        public readonly Dictionary<RectTransform, string> descriptionLookup = new();
        public readonly Dictionary<RectTransform, Func<string>> descriptionCallbackLookup = new();
        public HintHoverInfo lastHintHoverInfo;
    }
    private UIHints uiHints = new();
    
    private void InitUIHints() {
        AddHint(inventories.player.slots[0], "Demon Eye Slot");
        AddHint(inventories.player.slots[1], "Backpack Slot");
        AddHint(inventories.player.slots[2], "Trinket Slot");
        
        const string quickUseDesc = "Items placed here are available on the hotbar during raids";
        AddHint(inventories.player.slots[3], quickUseDesc);
        AddHint(inventories.player.slots[4], quickUseDesc);
        AddHint(inventories.player.slots[5], quickUseDesc);
        AddHint(inventories.player.slots[6], quickUseDesc);
        AddHint(ui.quickUseHeaderText, quickUseDesc);
        
        AddHintWithCallback(inventories.eyeForge.slots[0], static () => {
            if (gameInstance.InTutorialFirstForge) {
                return "Place an Eyeball here to craft a Demon Eye";
            }
            return "Place an Eyeball or Demon Eye here to craft or level up a Demon Eye";
        });
        
        AddHintWithCallback(inventories.eyeForge.slots[1],  GetForgeSlotHint);
        AddHintWithCallback(inventories.eyeForge.slots[2],  GetForgeSlotHint);
        AddHintWithCallback(inventories.eyeForge.slots[3],  GetForgeSlotHint);
        AddHintWithCallback(inventories.eyeForge.slots[4],  GetForgeSlotHint);
        AddHintWithCallback(inventories.eyeForge.slots[5],  GetForgeSlotHint);
        
        static string GetForgeSlotHint() {
            if (gameInstance.forgeMode is ForgeMode.UpgradingDemonEye) {
                return "Blood Rune required for Demon Eye upgrade. Autofills on upgrade.";
            }
            return $"Place {DisplayNumber(1)} of {DisplayNumber(5)} Blood Runes here to craft a Demon Eye";
        }
        
        AddHint(ui.stashPanelHeaderText, "A place to keep all your items safe. Stashed items remain even after dying.");
        
        AddHintWithCallback(eyeForgePanel.forgeButton.rectTransform, static () => {
            if (gameInstance.PlayingForgeAnimation) {
                return string.Empty;
            }
            
            ForgeMode forgeMode = gameInstance.forgeMode;
            ForgeError forgeError = gameInstance.forgeError;
            Inventory eyeForgeInventory = gameInstance.inventories.eyeForge;
            
            if (forgeMode == ForgeMode.Empty) {
                return "Place an eyeball in the center to start the Demon Eye crafting process";
            }
            if (forgeError == ForgeError.ForgingButJustEye) {
                return $"Requires {DisplayNumber(5)} Blood Runes to craft a Demon Eye";
            }
            if (forgeError == ForgeError.ForgingButMissingUpgrades) {
                int curEyeUpgradeCount = gameInstance.GetInventoryItemCount(eyeForgeInventory) - 1;
                int eyeUpgradesStillNeeded = GameData.Config.demonEyeCoreUpgradeCount - curEyeUpgradeCount;
                return $"Requires {DisplayNumber(eyeUpgradesStillNeeded)} more Blood Runes to craft a Demon Eye";
            }
            if (forgeError == ForgeError.ForgingButWithoutEye) {
                return "Missing eyeball in the center";
            }
            if (forgeError == ForgeError.PentagramLevelTooLow) {
                int pentegramLevelRequired = eyeForgeInventory.slots[0].itemInstance.DemonEyeLevel + 1;
                return $"Pentagram needs to be level {pentegramLevelRequired} to upgrade this Demon Eye";
            }
            if (forgeError == ForgeError.NeedsToOwnMoreEyeUpgrades) {
                return "Blood Runes requirement have not been met";
            }
            if (forgeMode == ForgeMode.UpgradingDemonEye) {
                return "Upgrade Demon Eye";
            }
            if (forgeMode == ForgeMode.PostForgeOrUpgrade) {
                return "Continue with upgrading the Demon Eye";
            }
            return "Craft Demon Eye";
        });
    }
    
    private void AddHint(InventorySlot slot, string description) {
        RectTransform rectTransform = slot.ui.rectTransform;
        Assert.IsFalse(uiHints.descriptionLookup.ContainsKey(rectTransform), "Hint RectTransform has already been added");
        uiHints.hoverableInventorySlots.Add(slot);
        uiHints.descriptionLookup.Add(rectTransform, description);
    }
        
    private void AddHint(RectTransform rectTransform, string description) {
        Assert.IsFalse(uiHints.descriptionLookup.ContainsKey(rectTransform), "Hint RectTransform has already been added");
        uiHints.hoverableRectTransforms.Add(rectTransform);
        uiHints.descriptionLookup.Add(rectTransform, description);
    }
    
    private void AddHintWithCallback(InventorySlot slot, Func<string> getDescriptionCallback) {
        RectTransform rectTransform = slot.ui.rectTransform;
        Assert.IsFalse(uiHints.descriptionLookup.ContainsKey(rectTransform), "Hint RectTransform has already been added");
        Assert.IsFalse(getDescriptionCallback == null, "Description callback should not be null");
        uiHints.hoverableInventorySlots.Add(slot);
        uiHints.descriptionCallbackLookup.Add(rectTransform, getDescriptionCallback);
    }
    
    private void AddHintWithCallback(RectTransform rectTransform, Func<string> getDescriptionCallback) {
        Assert.IsFalse(uiHints.descriptionLookup.ContainsKey(rectTransform), "Hint RectTransform has already been added");
        Assert.IsFalse(getDescriptionCallback == null, "Description callback should not be null");
        uiHints.hoverableRectTransforms.Add(rectTransform);
        uiHints.descriptionCallbackLookup.Add(rectTransform, getDescriptionCallback);
    }
    
    private void UpdateUIHints() {
        HintHoverInfo hoverInfo = UpdateHintHover();
        
        if (!hoverInfo.hoveringTransform) {
            HideHint();
            return;
        }
        
        const float hoverTimeUntilTooltip = 0.5f;
        bool spentEnoughTimeHovering = hoverInfo.timeSpentHovering >= hoverTimeUntilTooltip;
        
        if (spentEnoughTimeHovering) {
            ShowHint(hoverInfo);
        }
        else {
            HideHint();
        }
    }

    private void ShowHint(HintHoverInfo hoverInfo) {
        if (ui.hintPopup.gameObject.activeInHierarchy) {
            // This makes sure that hints that can change during runtime are always up to date when already showing
            if (uiHints.descriptionCallbackLookup.TryGetValue(hoverInfo.hoveringTransform, out Func<string> callback)) {
                ui.hintPopup.descText.text = callback.Invoke();
            }
            return;
        }
        
        Vector2 hoveredCenter = hoverInfo.hoveringTransform.WorldRect().center;
        Vector2 popupOffset = new(0f, hoverInfo.hoveringTransform.rect.height * 0.7f * CanvasScale);
        
        if (uiHints.descriptionLookup.TryGetValue(hoverInfo.hoveringTransform, out string desc)) {
            ui.hintPopup.Show(hoveredCenter + popupOffset, desc);
        }
        else if (uiHints.descriptionCallbackLookup.TryGetValue(hoverInfo.hoveringTransform, out Func<string> callback)) {
            string description = callback.Invoke();
            if (description == string.Empty) return;
            ui.hintPopup.Show(hoveredCenter + popupOffset, description);
        }
    }

    private void HideHint() {
        ui.hintPopup.Hide();
    }
    
    public struct HintHoverInfo {
        public RectTransform hoveringTransform;
        public float timeSpentHovering;
    }
    
    private HintHoverInfo UpdateHintHover() {
        HintHoverInfo info = new();
        Vector2 mousePos = PointerScreenPos;
        
        foreach (RectTransform element in uiHints.hoverableRectTransforms) {
            if (UpdateHintHoverInfoForRectTransform(element, mousePos, ref info)) break;
        }
        
        foreach (InventorySlot slot in uiHints.hoverableInventorySlots) {
            if (slot.itemInstance != null) continue; // Don't have hints show when slot has an item
            if (UpdateHintHoverInfoForRectTransform(slot.ui.rectTransform, mousePos, ref info)) break;
        }
        
        uiHints.lastHintHoverInfo = info;
        return info;
    }
    
    private bool UpdateHintHoverInfoForRectTransform(RectTransform element, Vector2 mousePos, ref HintHoverInfo info) {
        if (!element.gameObject.activeInHierarchy) return false;
        if (InNonInteractableCanvasGroup(element)) return false;

        Vector2 localMousePos = element.InverseTransformPoint(mousePos);
        Bounds localUiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(element);
        if (!localUiBounds.Contains(localMousePos)) return false;
            
        info.hoveringTransform = element;
            
        bool hoveringOverPrevElement = info.hoveringTransform == uiHints.lastHintHoverInfo.hoveringTransform;
        if (hoveringOverPrevElement) {
            info.timeSpentHovering = uiHints.lastHintHoverInfo.timeSpentHovering + Time.deltaTime;
        }
        else {
            info.timeSpentHovering = 0f;
        }
        
        return true;
    }

    private void EnableInteractionPrompt(Vector3 position, string detailsString) {
        ui.interactDetails.gameObject.SetActive(true);
        ui.interactDetails.text = detailsString;
        
        ui.interactPrompt.gameObject.SetActive(true);
        ui.interactPrompt.text = InputIcon(input.interact);
        ui.interactPrompt.transform.position = camera.main.WorldToScreenPoint(position);
    }
    
    private void DisableInteractionPrompt() {
        ui.interactPrompt.gameObject.SetActive(false);
        ui.interactDetails.gameObject.SetActive(false);
        ui.interactDetails.color = config.styles.interactionsTextColor;
    }
    
    // Multiply canvas units (sizes and offsets authored at 1080p) by this to get screen pixels
    public float CanvasScale => ui.mainCanvasRectTransform.lossyScale.x;

    private void UIOnScreenSizeChanged() {
        ui.mainCanvasScaler.scaleFactor = Screen.height switch {
            >= 2160 => 1.4f,
            >= 1440 => 1.2f,
            >= 1080 => 1f, 
            >= 800 => 0.88f, 
            >= 750 => 0.84f,
            >= 700 => 0.75f,
            >= 600 => 0.5f,
            >= 500 => 0.35f,
            _      => 0.2f,
        };
        PlaceCurrencyDisplays();
        Canvas.ForceUpdateCanvases();
    }

    private Transform currencyInfoParent;
    private int coinsCurrencySiblingIndex;
    private int soulsCurrencySiblingIndex;
    private bool currenciesInHideout;

    private void SetCurrencyDisplaysInHideout(bool inHideout) {
        currenciesInHideout = inHideout;
        PlaceCurrencyDisplays();
    }

    private void PlaceCurrencyDisplays() {
        if (!currencyInfoParent) return; // Not initialized yet

        Transform coins = playerInfo.coinsCurrencyParent.transform;
        Transform souls = playerInfo.soulsCurrencyParent.transform;

        bool useSmallScreenSpot = currenciesInHideout && ui.mainCanvasScaler.scaleFactor < 0.95f;
        if (useSmallScreenSpot) {
            coins.SetParent(ui.currencyForSmallScreensParent, worldPositionStays: false);
            souls.SetParent(ui.currencyForSmallScreensParent, worldPositionStays: false);
            return;
        }

        if (coins.parent == currencyInfoParent) return;
        coins.SetParent(currencyInfoParent, worldPositionStays: false);
        souls.SetParent(currencyInfoParent, worldPositionStays: false);
        // Lowest index first so the first one placed doesn't shift the other's spot
        if (coinsCurrencySiblingIndex < soulsCurrencySiblingIndex) {
            coins.SetSiblingIndex(coinsCurrencySiblingIndex);
            souls.SetSiblingIndex(soulsCurrencySiblingIndex);
        }
        else {
            souls.SetSiblingIndex(soulsCurrencySiblingIndex);
            coins.SetSiblingIndex(coinsCurrencySiblingIndex);
        }
    }
    
    public void PlayTypewritterCharacterShowSound() {
        PlayAudioClip(audio.textCharAppearClip);
    }

}
