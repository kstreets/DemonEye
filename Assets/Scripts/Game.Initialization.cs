using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public partial class Game {
    
    private void InitGame() {
        SettingsState settingsState = LoadSettings();
        InitSettings(settingsState);

        GameState gameState = LoadGameState();
        persistentFlags = gameState?.persistentFlags ?? default;
        camera.defaultPPU = camera.pixelPerfect.assetsPPU;
        
        InitInput();
        InitInputDevice();
        InitResources();
        InitAudio();
        InitMusic();
        DemonEyeTween.Init();
        InitDemonEye();
        InitButtonCallbacks();
        InitPauseMenu();
        InitEntityPools();
        InitGameStates();
        InitMenuNavigation();
        InitInputIcons();
        InitHotBar();
        
        InitInventories(gameState);
        InitEntities(gameState);
        InitMaps(gameState);
        InitQuests(gameState);
        InitHideout(gameState);
        InitTutorial(gameState);
        
        InitUI();

        bool createInitialSaveFile = gameState == null;
        if (createInitialSaveFile) {
            SaveGameState();
        }
    }
    
    private void InitEntities(GameState gameState) { 
        entities.player = MakePlayer();
        InitPlayerState(player, gameState);
    }
    
    private void InitInput() {
        input.move = InputSystem.actions.FindAction("Move");
        input.interact = InputSystem.actions.FindAction("Interact");
        input.advanceDialogue = InputSystem.actions.FindAction("AdvanceDialogue");
        input.inventory = InputSystem.actions.FindAction("Inventory");
        input.selectItem = InputSystem.actions.FindAction("SelectItem");
        input.placeSingleItem = InputSystem.actions.FindAction("PlaceSingleItem");
        input.splitStack = InputSystem.actions.FindAction("SplitStack");
        input.moveStack = InputSystem.actions.FindAction("MoveStack");
        input.useItem = InputSystem.actions.FindAction("UseItem");
        input.escape = InputSystem.actions.FindAction("Escape");
        input.pause = InputSystem.actions.FindAction("Pause");
        input.quickUse1 = InputSystem.actions.FindAction("QuickUse1");
        input.quickUse2 = InputSystem.actions.FindAction("QuickUse2");
        input.quickUse3 = InputSystem.actions.FindAction("QuickUse3");
        input.quickUse4 = InputSystem.actions.FindAction("QuickUse4");
    }

    private void InitEntityPools() {
        entityPools.itemDrop = CreateEntityPool<Entity>(prefabs.itemDrop, 20, null);
        entityPools.bloodDrop = CreateEntityPool<Entity>(prefabs.bloodDrop, 10, null);
        entityPools.projectile = CreateEntityPool<Projectile>(prefabs.baseProjectile, 20, OnSpawnProjectile);
        entityPools.boneShatterProjectile = CreateEntityPool<Projectile>(prefabs.boneShatterProjectile, 20, OnSpawnProjectile);
        entityPools.gooProjectile = CreateEntityPool<Projectile>(prefabs.gooProjectile, 20, OnSpawnProjectile);
        entityPools.piercingShotProjectile = CreateEntityPool<Projectile>(prefabs.piercingProjectile, 20, OnSpawnProjectile);
        entityPools.soulProjectile = CreateEntityPool<Projectile>(prefabs.soulProjectile, 40, OnSpawnProjectile);
        entityPools.poisonDebuff = CreateEntityPool<Entity>(prefabs.poisonDebuff, 10, null);
        entityPools.explosion = CreateEntityPool<Entity>(prefabs.explosion, 5, null);
        entityPools.projectileImpact = CreateEntityPool<Entity>(prefabs.projectileImpact, 20, null);
        entityPools.teleportIn = CreateEntityPool<Entity>(prefabs.teleportIn, 20, null);
        entityPools.teleportOut = CreateEntityPool<Entity>(prefabs.teleportOut, 20, null);
        entityPools.expressExitPortal = CreateEntityPool<Entity>(prefabs.expressExitPortal, 1, null);
        entityPools.bloodSplatter = CreateEntityPool<Entity>(prefabs.bloodSplatter, 20, null);
        entityPools.runSmoke = CreateEntityPool<Entity>(prefabs.runSmoke, 5, null);
        entityPools.damageNumber = CreateEntityPool<Entity>(prefabs.damageNumber, 20, null);
        entityPools.forgeExplosion = CreateEntityPool<Entity>(prefabs.forgeExplosion, 1, null);
        entityPools.forgeDust = CreateEntityPool<Entity>(prefabs.forgeDust, 5, null);
        entityPools.upgradeFractureParticles = CreateEntityPool<Entity>(prefabs.upgradeFractureParticles, 5, null);
        entityPools.blast = CreateEntityPool<Entity>(prefabs.blast, 5, null);
        entityPools.lootReveal = CreateEntityPool<Entity>(prefabs.lootReveal, 1, null);
        entityPools.notification = CreateEntityPool<Entity>(prefabs.notification, 2, null);
        entityPools.bloodBubble = CreateEntityPool<Entity>(prefabs.bloodBubble, 10, null);
        entityPools.eyeUpgradeReveal = CreateEntityPool<Entity>(prefabs.eyeUpgradeReveal, 1, null);
        entityPools.altarSoulSwirl = CreateEntityPool<Entity>(prefabs.altarSoulSwirl, 1, null);
    }

    private void InitGameStates() {
        states.gameStateMachine = new();
        var gameStateMachine = states.gameStateMachine;
        
        states.mainMenu = gameStateMachine.CreateState(enter: OnMainMenuStateEnter, exit: OnMainMenuStateExit);
        states.settingsMenu = gameStateMachine.CreateState(enter: ShowSettingsMenuUI, exit: CloseSettingsMenuUI);
        states.hideout = gameStateMachine.CreateState(update: OnHideoutStateUpdate, lateUpdate: OnHideoutStateLateUpdate, enter: OnHideoutStateEnter, exit: OnHideoutStateExit);
        states.mapSelection = gameStateMachine.CreateState(update: OnMapSelectionUpdate, lateUpdate: OnMapSelectionLateUpdate, enter: OnMapSelectionEnter, exit: OnMapSelectionExit);
        states.raid = gameStateMachine.CreateState(update: OnRaidStateUpdate, fixedUpdate: OnRaidStateFixedUpdate, lateUpdate: OnRaidStateLateUpdate, enter: OnRaidStateEnter, exit: OnRaidStateExit);
        states.gameOver = gameStateMachine.CreateState(enter: OnGameOverEnter, exit: OnGameOverExit);
        states.earlyExit = gameStateMachine.CreateState(enter: OnEarlyExitEnter, exit: OnEarlyExitExit);
        states.winExit = gameStateMachine.CreateState(enter: OnWinExitEnter, exit: OnWinExitExit);
        
        states.raid.To(states.gameOver).When(() => player.health <= 0);
    }
    
    private void InitButtonCallbacks() {
        mainMenu.playButton.AddListener(() => {
            if (InTutorialSlaughterMap) {
                LoadMapAsync(config.tutorialSlaughterMap);
                return;
            }
            State destination = InTutorialFirstForge ? states.hideout : states.mapSelection;
            states.gameStateMachine.SetStateIfNotCurrent(destination);
        });
        
        mainMenu.hideoutButton.AddListener(() => {
            states.gameStateMachine.SetStateIfNotCurrent(states.hideout);
        });
        
        mainMenu.settingsButton.AddListener(() => {
            states.gameStateMachine.SetStateIfNotCurrent(states.settingsMenu);
        });
        
        mainMenu.exitButton.AddListener(() => {
            ui.messagePopup.Show("Are you sure you would like to exit?", "Yes", "No", onYes: QuitGame);
        });
        
        settings.displayToggle.AddListener(() => {
            settings.displayParent.gameObject.SetActive(true);
            settings.audioParent.gameObject.SetActive(false);
        });
        
        settings.audioToggle.AddListener(() => {
            settings.displayParent.gameObject.SetActive(false);
            settings.audioParent.gameObject.SetActive(true);
        });
        
        ui.menuBackButton.AddListener(() => {
            OnEscapePressed(new());
        });
        
        settings.applyChangesButton.AddListener(ApplySettings);
        
        hideoutTabs.characterButton.AddListener(() => {
            ToggleHideoutPanels(playerPanel.panel, stashPanel.panel);
            ToggleSlimPlayerPanel(false);
        });
        
        hideoutTabs.eyeForgeButton.AddListener(() => {
            ToggleHideoutPanels(playerPanel.panel, eyeForgePanel.panel, stashPanel.panel);
            ToggleSlimPlayerPanel(true);
        });
        
        hideoutTabs.traderButton.AddListener(() => {
            ToggleHideoutPanels(traderPanel.panel, transactionPanel.panel, stashPanel.panel);
            if (!InTutorial) { // The trader is already talking to us, don't make it talk over itself
                TriggerTraderShopDialogue(TraderShopDialogueType.Greeting); 
            }
        });
        
        hideoutTabs.questsButton.AddListener(() => {
            ToggleHideoutPanels(questsPanel.panel);
            RefreshQuestDisplays();
        });
        
        hideoutTabs.skillsButton.AddListener(() => {
            ToggleHideoutPanels(skillsPanel.panel.rectTransform, skillsPanel.playerStatsPanel.rectTransform);
        });

        SkillLevelUpRow hasteRow = skillsPanel.panel.hasteSkillRow;
        hasteRow.levelUpButton.AddListener(() => OnSkillLevelUpButtonPressed(hasteRow, skillUpgradePaths.haste, player.state.hasteSkillLevel));
        
        SkillLevelUpRow intellectRow = skillsPanel.panel.intellectSkillRow;
        intellectRow.levelUpButton.AddListener(() => OnSkillLevelUpButtonPressed(intellectRow, skillUpgradePaths.intellect, player.state.intellectSkillLevel));
        
        SkillLevelUpRow lifeBloodRow = skillsPanel.panel.lifeBloodSkillRow;
        lifeBloodRow.levelUpButton.AddListener(() => OnSkillLevelUpButtonPressed(lifeBloodRow, skillUpgradePaths.lifeBlood, player.state.lifeBloodSkillLevel));
        
        SkillLevelUpRow strength = skillsPanel.panel.strengthSkillRow;
        strength.levelUpButton.AddListener(() => OnSkillLevelUpButtonPressed(strength, skillUpgradePaths.strength, player.state.strengthSkillLevel));
        
        eyeForgePanel.forgeToggle.AddListener(OnPentagramForgeTogglePressed);
        eyeForgePanel.levelUpToggle.AddListener(OnPentagramLevelUpTogglePressed);
        eyeForgePanel.forgeButton.AddListener(OnForgeButtonPressed);
        eyeForgePanel.levelUpButton.AddListener(OnLevelUpPentagramPressed);
        
        transactionPanel.transaction.buyToggle.AddListener(OnBuyTogglePressed);
        transactionPanel.transaction.sellToggle.AddListener(OnSellTogglePressed);
        transactionPanel.transaction.sellButton.AddListener(OnSellButtonPressed);
        transactionPanel.transaction.moneyPurchaseButton.AddListener(OnMoneyPurchaseButtonPressed);
        transactionPanel.transaction.barterPurchaseButton.AddListener(OnBarterPurchaseButtonPressed);

        for (int i = 0; i < mapPanels.mapSelectionPanel.selectors.Length; i++) {
            ButtonFeel mapSelectionButton = mapPanels.mapSelectionPanel.selectors[i].selectionButton;
            MapData map = config.maps[i];
            mapSelectionButton.AddListener(() => ShowMapConfirmationUI(map));
        }
        
        mapPanels.confirmationPanel.teleportButton.AddListener(() => {
            LoadMapAsync(mapPanels.confirmationPanel.selectedMap);
        });
    }
    
    private void InitHotBar() {
        hotBar.quickUseActions = new() {
            input.quickUse1, 
            input.quickUse2, 
            input.quickUse3, 
            input.quickUse4,
        };
        hotBar.slotUIs = ui.hotBarParent.GetComponentsInChildren<InventorySlotUI>();
        Assert.IsTrue(hotBar.slotUIs.Length == playerQuickUseSize, "Make sure to match hot bar inventory UIs count with quick use count");
    }
    
}
