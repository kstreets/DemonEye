using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public partial class Game {
    
    private void OnGameStartInitUI() {
        CloseHideoutUI();
        CloseRaidUI();
        ShowMainMenuUI();
        
        InitSkillsPanel();
        
        menuBackButton.gameObject.SetActive(false);
        largeRaidTextTypewriter.gameObject.SetActive(false);

        SetPentagramFill(0f);
    }

    private Sequence mainMenuSequence;
    
    private void AnimateInMainMenu() {
        if (mainMenuSequence.isAlive) return;
        
        float halfScreenHeight = Screen.height / 2f;
        
        mainMenuSequence = Sequence.Create();
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(mainMenuLogo, halfScreenHeight, mainMenuLogo.anchoredPosition.y, 0.8f, Ease.OutExpo));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(mainMenuPlayButton.rectTransform, -halfScreenHeight, mainMenuPlayButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(mainMenuHideoutButton.rectTransform, -halfScreenHeight, mainMenuHideoutButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo, startDelay: 0.1f));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(mainMenuSettingsButton.rectTransform, -halfScreenHeight, mainMenuSettingsButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo, startDelay: 0.2f));
        mainMenuSequence.Group(Tween.UIAnchoredPositionY(mainMenuExitButton.rectTransform, -halfScreenHeight, mainMenuExitButton.rectTransform.anchoredPosition.y, 0.8f, Ease.OutExpo, startDelay: 0.3f));

        mainMenuPlayButton.rectTransform.anchoredPosition = new(mainMenuPlayButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        mainMenuHideoutButton.rectTransform.anchoredPosition = new(mainMenuHideoutButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        mainMenuSettingsButton.rectTransform.anchoredPosition = new(mainMenuSettingsButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
        mainMenuExitButton.rectTransform.anchoredPosition = new(mainMenuExitButton.rectTransform.anchoredPosition.x, -halfScreenHeight);
    }

    private void ShowMainMenuUI() {
        hideoutParent.gameObject.SetActive(true);
        menuBackgroundImage.gameObject.SetActive(true);
        mainMenuParent.gameObject.SetActive(true);
        AnimateInMainMenu();
    }

    private void CloseMainMenuUI() {
        menuBackgroundImage.gameObject.SetActive(false);
        mainMenuParent.gameObject.SetActive(false);
    }

    private void ShowMapSelectionUI() {
        ShowHideoutUI();
        hideoutTabsParent.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(false);
        ToggleHideoutPanels(playerPanel, mapSelectionPanel);
    }

    private void CloseMapSelectionUI() {
        CloseHideoutUI();
    }
    
    private void ShowHideoutUI() {
        ToggleHideoutTab(characterTabButton, characterTabText);
        ToggleHideoutPanels(playerPanel, stashPanel);
        menuBackButton.gameObject.SetActive(true);
        coinsCurrencyParent.gameObject.SetActive(true);
        soulsCurrencyParent.gameObject.SetActive(true);
        healthBarParent.gameObject.SetActive(false);
        weightBarParent.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(true);
        menuBackgroundImage.gameObject.SetActive(true);
        hideoutTabsParent.gameObject.SetActive(true);
    }

    private void CloseHideoutUI() {
        ToggleHideoutPanels();
        HideInventoryItemPopup(); 
        HideUIElementPopup();
        menuBackButton.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(false);
        menuBackgroundImage.gameObject.SetActive(false);
        hideoutTabsParent.gameObject.SetActive(false);
    }

    private void ShowRaidUI() {
        healthBarParent.gameObject.SetActive(true);
        weightBarParent.gameObject.SetActive(true);
        coinsCurrencyParent.gameObject.SetActive(false);
        soulsCurrencyParent.gameObject.SetActive(true);
        playerInfoParent.gameObject.SetActive(true);
        raidInfoPanelParent.SetActive(true);
        hotBarParent.gameObject.SetActive(true);
    }

    private void CloseRaidUI() {
        HideInventoryItemPopup(); 
        HideUIElementPopup();
        interactPrompt.gameObject.SetActive(false);
        interactionDetails.gameObject.SetActive(false);
        playerInfoParent.gameObject.SetActive(false);
        raidInfoPanelParent.SetActive(false);
        portalArrow.gameObject.SetActive(false);
        hotBarParent.gameObject.SetActive(false);
    }

    private void ToggleHideoutTab(Button button, TextMeshProUGUI text) {
        characterTabButton.image.sprite = tabNonSelectedSprite;
        eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
        traderTabButton.image.sprite = tabNonSelectedSprite;
        questsTabButton.image.sprite = tabNonSelectedSprite;
        skillsTabButton.image.sprite = tabNonSelectedSprite;
        
        characterTabText.margin = styles.nonSelectedHideoutTabMargin;
        eyeForgeTabText.margin = styles.nonSelectedHideoutTabMargin;
        traderTabText.margin = styles.nonSelectedHideoutTabMargin;
        questsTabText.margin = styles.nonSelectedHideoutTabMargin;
        skillsTabText.margin = styles.nonSelectedHideoutTabMargin;
        
        button.image.sprite = tabSelectedSprite;
        text.margin = styles.selectedHideoutTabMargin;
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

    private void OnEscapePressed(InputAction.CallbackContext context) {
        if (InMapSelection || InHideout) {
            gameStateMachine.SetState(mainMenuState);
        }
        if (InRaid && InventoryIsOpen) {
            ClosePlayerInventory();
            CloseLootInventory(); 
        }
    }
    
    private void InitButtonCallbacks() {
        mainMenuPlayButton.AddListener(() => {
            gameStateMachine.SetStateIfNotCurrent(mapSelectionState);
        });
        
        mainMenuHideoutButton.AddListener(() => {
            gameStateMachine.SetStateIfNotCurrent(hideoutState);
        });
        
        menuBackButton.AddListener(() => {
            OnEscapePressed(new());
        });
        
        characterTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(characterTabButton, characterTabText);
            ToggleHideoutPanels(playerPanel, stashPanel);
        });
        
        eyeForgeTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(eyeForgeTabButton, eyeForgeTabText);
            ToggleHideoutPanels(forgeDetailsPanel, eyeForgePanel, stashPanel);
        });
        
        traderTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(traderTabButton, traderTabText);
            ToggleHideoutPanels(traderInventoryPanel, traderTransactionPanel, stashPanel);
        });
        
        questsTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(questsTabButton, questsTabText);
            ToggleHideoutPanels(questsPanel);
            RefreshQuestDisplays();
        });
        
        skillsTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(skillsTabButton, skillsTabText);
            ToggleHideoutPanels(skillsPanel.rectTransform, playerStatsPanel.rectTransform);
        });

        skillsPanel.hasteSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(hasteUpgradePath, player.hasteSkillLevel));
        skillsPanel.intellectSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(intellectUpgradePath, player.intellectSkillLevel));
        skillsPanel.lifeBloodSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(lifeBloodUpgradePath, player.lifeBloodSkillLevel));
        skillsPanel.strengthSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(strengthUpgradePath, player.strengthSkillLevel));
        
        forgeEyeButton.AddListener(OnForgeButtonPressed);
        upgradeForgeButton.AddListener(OnUpgradeForgePressed);
        
        transactionPanel.buyToggle.AddListener(OnBuyTogglePressed);
        transactionPanel.sellToggle.AddListener(OnSellTogglePressed);
        transactionPanel.sellButton.AddListener(OnSellButtonPressed);
        transactionPanel.moneyPurchaseButton.AddListener(OnMoneyPurchaseButtonPressed);
        transactionPanel.barterPurchaseButton.AddListener(OnBarterPurchaseButtonPressed);

        for (int i = 0; i < mapSelectionButtons.Length; i++) {
            Button mapSelectionButton = mapSelectionButtons[i];
            MapData map = maps[i];
            mapSelectionButton.onClick.AddListener(() => {
                LoadMapAsync(map, () => {
                    CreateDropPoolsForMap(map);
                    gameStateMachine.SetStateIfNotCurrent(raidState);
                });
            });
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
        
        GetEncumberingWeightRange(out int startingEncumberingWeight, out _);
        int inventoryWeight = GetInventoryWeight(playerInventory);
        weightBarFillImage.fillAmount = Mathf.Clamp01(inventoryWeight / (float)startingEncumberingWeight);
        
        float overweightComp = GetOverweightCompletion();
        if (overweightComp > 0f) {
            weightBarFillImage.color = Color.Lerp(styles.startingOverWeightColor, styles.endingOverWeightColor, overweightComp);
        }
        else {
            weightBarFillImage.color = styles.underWeightColor;
        }
        
        if (raidStateSwitchedThisFrame) {
            if (curRaidState == RaidState.InitialWaves) {
                finalWaveCountdownParent.SetActive(true); 
                exitPortalCountdownParent.SetActive(true);
                finalWaveActiveNotifier.SetActive(false);
                exitPortalActiveNotifier.SetActive(false);
                finalExitPortalNotifier.SetActive(false);
            }
            else if (curRaidState == RaidState.FinalWave) {
                finalWaveCountdownParent.SetActive(false); 
                exitPortalActiveNotifier.SetActive(false);
                finalWaveActiveNotifier.SetActive(true);
                Tween.Scale(finalWaveActiveNotifier.transform, 0f, 1f, 0.5f, Ease.OutBack);
                AnimateSmallRaidText(ColorText("Final Wave", styles.decreaseDescColor));
            }
            else if (curRaidState == RaidState.PostFinalWave) {
                finalWaveActiveNotifier.SetActive(false);
                finalExitPortalNotifier.SetActive(true);
                Tween.Scale(finalExitPortalNotifier.transform, 0f, 1f, 0.5f, Ease.OutBack);
            }
        }
        
        if (exitPortalCountdownText.gameObject.activeInHierarchy) {
            // exitPortalCountdownText.text = GetCountdownText(exitPortalTween.duration - exitPortalTween.elapsedTime);
        }
        if (finalWaveCountdownText.gameObject.activeInHierarchy) {
            finalWaveCountdownText.text = GetCountdownText(spawnManager.timeUntilFinalWave);
        }
    }

    private void AnimateLargeRaidText(string text, float typewriterSpeed) {
        largeRaidText.characterSpacing = 0;
        largeRaidText.gameObject.SetActive(true);
        largeRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{wave}}{{#fade}}{{#wave}}{text}");
        largeRaidTextTypewriter.SetTypewriterSpeed(typewriterSpeed);
        
        largeRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.Chain(Tween.Custom(0, 30, 0.5f, startDelay: 0.3f, ease: Ease.OutBack, onValueChange: static (val) => {
                gameInstance.largeRaidText.characterSpacing = val;
            }));
            sequence.ChainDelay(0.35f);
            sequence.ChainCallback(static () => gameInstance.largeRaidTextTypewriter.StartDisappearingText());
        }
    }

    private void AnimateSmallRaidText(string text) {
        smallRaidText.gameObject.SetActive(true);
        smallRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{smallwave}}{{#fade}}{{#smallwave}}{text}");
        
        smallRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.ChainDelay(0.8f);
            sequence.ChainCallback(static () => gameInstance.smallRaidTextTypewriter.StartDisappearingText());
        }
    }
    
    // *******************************
    // Animation Sequences
    // *******************************

    private void AnimateGameOverSequence(Action onCompleteCallback) {
        Tween.StopAll();
        
        foreach (Entity entity in entities) {
            if (entity.rigidbody) {
                entity.rigidbody.linearVelocity = Vector2.zero;
            }
            if (entity.animator) {
                entity.animator.enabled = false;
            }
        }
        
        player.spriteRenderer.sortingLayerName = "DeathWipe";
        
        player.GetEffect(EffectsIndicies.HitFlash).Complete();
        player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
        player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 1f);
        player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 0f;
        deathBackgroundImage.color = deathBackgroundImage.color.Alpha(1f);

        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        sequence.Chain(Tween.UIFillAmount(deathBackgroundImage, 1f, 1f, Ease.InOutQuad));
        sequence.ChainCallback(() => {
            player.animator.enabled = true;
            player.animator.Play(playerDeathAnim);
        });
        
        sequence.Group(Tween.Custom(1f, 0f, 0.5f, val => {
            player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
            player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, val);
            player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        }, Ease.OutExpo));
        
        int initialPPU = pixelPerfectCamera.assetsPPU;
        
        sequence.Group(Tween.Custom(pixelPerfectCamera.assetsPPU, 80, 0.8f, val => {
            pixelPerfectCamera.assetsPPU = (int)val;
        }, Ease.InOutQuad));

        sequence.Group(Tween.Delay(0.25f, () => AnimateLargeRaidText(ColorText("YOU DIED", styles.decreaseDescColor), 1f)));
        
        sequence.ChainDelay(1f);

        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Chain(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.5f));

        sequence.Group(Tween.Scale(player.trans, Vector3.zero, 1.5f, Ease.InOutQuint, startDelay: 0.35f));
        
        sequence.OnComplete(() => {
            player.spriteRenderer.sortingLayerName = "Entity";
            player.trans.localScale = Vector3.one;
            pixelPerfectCamera.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateGameWinSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(teleportOutPool, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = pixelPerfectCamera.assetsPPU;
        sequence.Chain(Tween.Custom(pixelPerfectCamera.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            pixelPerfectCamera.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.15f);
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(deathBackgroundImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(0.15f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            pixelPerfectCamera.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateEarlyExitSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(teleportOutPool, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = pixelPerfectCamera.assetsPPU;
        sequence.Chain(Tween.Custom(pixelPerfectCamera.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            pixelPerfectCamera.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.05f);
        sequence.Chain(Tween.Scale(exitPortalTakenByPlayer.transform, Vector3.zero, 0.25f, Ease.InOutBounce));
        
        sequence.ChainDelay(0.15f);
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(deathBackgroundImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        sequence.Group(Tween.Delay(0.35f, () => AnimateLargeRaidText(ColorText("EARLY EXIT TAKEN", styles.increaseDescColor), 3.8f)));
        
        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(1.6f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            pixelPerfectCamera.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }

    // *******************************
    // Pop Ups 
    // *******************************
    
    public void FitPopupSize(RectTransform popupRect, params Rect[] rects) {
        float height = 0f;
        foreach (Rect rect in rects) {
            height += rect.height;
        }
        
        const int minHeight = 80;
        Rect newPopupRect = popupRect.rect;
        newPopupRect.height = Mathf.Clamp(height, minHeight, Mathf.Infinity);
        popupRect.sizeDelta = new(newPopupRect.width, newPopupRect.height);
    }

    private void TweenPopUp(RectTransform popupRectTransform) {
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
        if (uiElementPopup.gameObject.activeInHierarchy) return;
        
        uiElementPopup.gameObject.SetActive(true);
        TweenPopUp(uiElementPopup.rectTransform);
        
        if (hoverInfo.hoveringTransform == upgradeForgeButton.rectTransform) {
            uiElementPopup.descText.text = "Add an additional slot to the pentagram!\nCosts:";
            List<UpgradePath.Requirement> requirements = crucibleUpgradePath.pathUpgrades[player.crucibleLevel].requirements;
            foreach (UpgradePath.Requirement req in requirements) {
                bool meetsSingleReq = MeetsSingleUpgradeRequirement(req); 
                Color textColor = meetsSingleReq ? styles.increaseDescColor : styles.decreaseDescColor; 
                uiElementPopup.descText.text += ColorText($"\n{req.item.displayName} x{req.count}", textColor);
            }
        }
        
        uiElementPopup.descFitter.ForceRecalculate();
        FitPopupSize(uiElementPopup.rectTransform, uiElementPopup.descText.rectTransform.rect);
        
        // Set popup position
        Vector2 hoveredCenter = hoverInfo.hoveringTransform.WorldRect().center;
        Vector2 popupOffset = new(0f, hoverInfo.hoveringTransform.rect.height);
        uiElementPopup.transform.position = hoveredCenter + popupOffset;
    }

    private void HideUIElementPopup() {
        uiElementPopup.gameObject.SetActive(false);
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
        interactPrompt.text = $"<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}>";
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position);
    }
    
    private void DisableInteractionPrompt() {
        interactPrompt.gameObject.SetActive(false);
        interactionDetails.gameObject.SetActive(false);
        
    }

}
