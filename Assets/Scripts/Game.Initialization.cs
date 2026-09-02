using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class Game {
    
    private void InitGame() {
        GameState gameState = LoadGameState();
        persistentFlags = gameState?.persistentFlags ?? default;
        camera.defaultPPU = camera.pixelPerfect.assetsPPU;
        
        InitInput();
        InitResources();
        InitAudio();
        DemonEyeTween.Init();
        InitDemonEye();
        InitButtonCallbacks();
        InitEntityPools();
        InitGameStates();
        InitMenuNavigation();
        InitHotBar();
        InitUI();
        
        InitEntities(gameState);
        InitMaps(gameState);
        InitInventories(gameState);
        InitQuests(gameState);
        InitHideout(gameState);

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
        input.inventory = InputSystem.actions.FindAction("Inventory");
        input.selectItem = InputSystem.actions.FindAction("SelectItem");
        input.placeSingleItem = InputSystem.actions.FindAction("PlaceSingleItem");
        input.splitStack = InputSystem.actions.FindAction("SplitStack");
        input.moveStack = InputSystem.actions.FindAction("MoveStack");
        input.useItem = InputSystem.actions.FindAction("UseItem");
        input.escape = InputSystem.actions.FindAction("Escape");
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
        entityPools.bloodSplatter = CreateEntityPool<Entity>(prefabs.bloodSplatter, 20, null);
        entityPools.runSmoke = CreateEntityPool<Entity>(prefabs.runSmoke, 5, null);
        entityPools.damageNumber = CreateEntityPool<Entity>(prefabs.damageNumber, 20, null);
        entityPools.forgeExplosion = CreateEntityPool<Entity>(prefabs.forgeExplosion, 1, null);
        entityPools.forgeDust = CreateEntityPool<Entity>(prefabs.forgeDust, 5, null);
        entityPools.upgradeFractureParticles = CreateEntityPool<Entity>(prefabs.upgradeFractureParticles, 5, null);
        entityPools.blast = CreateEntityPool<Entity>(prefabs.blast, 5, null);
        entityPools.lootReveal = CreateEntityPool<Entity>(prefabs.lootReveal, 1, null);
        entityPools.notification = CreateEntityPool<Entity>(prefabs.notification, 2, null);
    }

    private void InitGameStates() {
        states.gameStateMachine = new();
        var gameStateMachine = states.gameStateMachine;
        
        states.mainMenu = gameStateMachine.CreateState(enter: OnMainMenuStateEnter, exit: OnMainMenuStateExit);
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
            states.gameStateMachine.SetStateIfNotCurrent(states.mapSelection);
        });
        
        mainMenu.hideoutButton.AddListener(() => {
            states.gameStateMachine.SetStateIfNotCurrent(states.hideout);
        });
        
        ui.menuBackButton.AddListener(() => {
            OnEscapePressed(new());
        });
        
        hideoutTabs.characterButton.onClick.AddListener(() => {
            ToggleHideoutTab(hideoutTabs.characterButton, hideoutTabs.characterText);
            ToggleHideoutPanels(playerPanel.panel, stashPanel.panel);
        });
        
        hideoutTabs.eyeForgeButton.onClick.AddListener(() => {
            ToggleHideoutTab(hideoutTabs.eyeForgeButton, hideoutTabs.eyeForgeText);
            ToggleHideoutPanels(eyeForgeDetailsPanel.panel, eyeForgePanel.panel, stashPanel.panel);
        });
        
        hideoutTabs.traderButton.onClick.AddListener(() => {
            ToggleHideoutTab(hideoutTabs.traderButton, hideoutTabs.traderText);
            ToggleHideoutPanels(traderPanel.panel, transactionPanel.panel, stashPanel.panel);
            TriggerTraderShopDialogue(TraderShopDialogueType.Greeting); 
        });
        
        hideoutTabs.questsButton.onClick.AddListener(() => {
            ToggleHideoutTab(hideoutTabs.questsButton, hideoutTabs.questsText);
            ToggleHideoutPanels(questsPanel.panel);
            RefreshQuestDisplays();
        });
        
        hideoutTabs.skillsButton.onClick.AddListener(() => {
            ToggleHideoutTab(hideoutTabs.skillsButton, hideoutTabs.skillsText);
            ToggleHideoutPanels(skillsPanel.panel.rectTransform, skillsPanel.playerStatsPanel.rectTransform);
        });

        skillsPanel.panel.hasteSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(skillUpgradePaths.haste, player.state.hasteSkillLevel));
        skillsPanel.panel.intellectSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(skillUpgradePaths.intellect, player.state.intellectSkillLevel));
        skillsPanel.panel.lifeBloodSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(skillUpgradePaths.lifeBlood, player.state.lifeBloodSkillLevel));
        skillsPanel.panel.strengthSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(skillUpgradePaths.strength, player.state.strengthSkillLevel));
        
        eyeForgePanel.forgeButton.AddListener(OnForgeButtonPressed);
        
        transactionPanel.transaction.buyToggle.AddListener(OnBuyTogglePressed);
        transactionPanel.transaction.sellToggle.AddListener(OnSellTogglePressed);
        transactionPanel.transaction.sellButton.AddListener(OnSellButtonPressed);
        transactionPanel.transaction.moneyPurchaseButton.AddListener(OnMoneyPurchaseButtonPressed);
        transactionPanel.transaction.barterPurchaseButton.AddListener(OnBarterPurchaseButtonPressed);

        for (int i = 0; i < mapSelectionPanel.buttons.Length; i++) {
            Button mapSelectionButton = mapSelectionPanel.buttons[i];
            MapData map = config.maps[i];
            mapSelectionButton.onClick.AddListener(() => LoadMapAsync(map));
        }
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
