using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class Game {
    
    private void InitGame() {
        BuildSavePaths();
        InitInput();
        InitResources();
        InitEntities();
        InitAudio();
        InitMapSaves();
        DemonEyeTween.Init();
        InitDemonEye();
        InitInventories();
        InitButtonCallbacks();
        InitEntityPools();
        InitGameStates();
        InitMenuNavigation();
        InitHotBar();
        InitUI();
        InitHideout();
    }
    
    private void InitEntities() {
        gameData.entities.player = MakePlayer();
    }
    
    private void InitInput() {
        gameData.input.move = InputSystem.actions.FindAction("Move");
        gameData.input.interact = InputSystem.actions.FindAction("Interact");
        gameData.input.inventory = InputSystem.actions.FindAction("Inventory");
        gameData.input.selectItem = InputSystem.actions.FindAction("SelectItem");
        gameData.input.placeSingleItem = InputSystem.actions.FindAction("PlaceSingleItem");
        gameData.input.splitStack = InputSystem.actions.FindAction("SplitStack");
        gameData.input.moveStack = InputSystem.actions.FindAction("MoveStack");
        gameData.input.useItem = InputSystem.actions.FindAction("UseItem");
        gameData.input.escape = InputSystem.actions.FindAction("Escape");
        gameData.input.quickUse1 = InputSystem.actions.FindAction("QuickUse1");
        gameData.input.quickUse2 = InputSystem.actions.FindAction("QuickUse2");
        gameData.input.quickUse3 = InputSystem.actions.FindAction("QuickUse3");
        gameData.input.quickUse4 = InputSystem.actions.FindAction("QuickUse4");
    }

    private void InitEntityPools() {
        gameData.entityPools.itemDrop = CreateEntityPool<Entity>(gameData.prefabs.itemDrop, 20, null);
        gameData.entityPools.bloodDrop = CreateEntityPool<Entity>(gameData.prefabs.bloodDrop, 10, null);
        gameData.entityPools.projectile = CreateEntityPool<Projectile>(gameData.prefabs.baseProjectile, 20, OnSpawnProjectile);
        gameData.entityPools.boneShatterProjectile = CreateEntityPool<Projectile>(gameData.prefabs.boneShatterProjectile, 20, OnSpawnProjectile);
        gameData.entityPools.gooProjectile = CreateEntityPool<Projectile>(gameData.prefabs.gooProjectile, 20, OnSpawnProjectile);
        gameData.entityPools.piercingShotProjectile = CreateEntityPool<Projectile>(gameData.prefabs.piercingProjectile, 20, OnSpawnProjectile);
        gameData.entityPools.poisonDebuff = CreateEntityPool<Entity>(gameData.prefabs.poisonDebuff, 10, null);
        gameData.entityPools.explosion = CreateEntityPool<Entity>(gameData.prefabs.explosion, 5, null);
        gameData.entityPools.projectileImpact = CreateEntityPool<Entity>(gameData.prefabs.projectileImpact, 20, null);
        gameData.entityPools.teleportIn = CreateEntityPool<Entity>(gameData.prefabs.teleportIn, 20, null);
        gameData.entityPools.teleportOut = CreateEntityPool<Entity>(gameData.prefabs.teleportOut, 20, null);
        gameData.entityPools.bloodSplatter = CreateEntityPool<Entity>(gameData.prefabs.bloodSplatter, 20, null);
        gameData.entityPools.runSmoke = CreateEntityPool<Entity>(gameData.prefabs.runSmoke, 5, null);
        gameData.entityPools.damageNumber = CreateEntityPool<Entity>(gameData.prefabs.damageNumber, 20, null);
        gameData.entityPools.forgeExplosion = CreateEntityPool<Entity>(gameData.prefabs.forgeExplosion, 10, null);
        gameData.entityPools.blast = CreateEntityPool<Entity>(gameData.prefabs.blast, 5, null);
    }

    private void InitGameStates() {
        gameData.states.gameStateMachine = new();
        var gameStateMachine = gameData.states.gameStateMachine;
        
        gameData.states.mainMenu = gameStateMachine.CreateState(enter: OnMainMenuStateEnter, exit: OnMainMenuStateExit);
        gameData.states.hideout = gameStateMachine.CreateState(update: OnHideoutStateUpdate, lateUpdate: OnHideoutStateLateUpdate, enter: OnHideoutStateEnter, exit: OnHideoutStateExit);
        gameData.states.mapSelection = gameStateMachine.CreateState(update: OnMapSelectionUpdate, lateUpdate: OnMapSelectionLateUpdate, enter: OnMapSelectionEnter, exit: OnMapSelectionExit);
        gameData.states.raid = gameStateMachine.CreateState(update: OnRaidStateUpdate, fixedUpdate: OnRaidStateFixedUpdate, lateUpdate: OnRaidStateLateUpdate, enter: OnRaidStateEnter, exit: OnRaidStateExit);
        gameData.states.gameOver = gameStateMachine.CreateState(enter: OnGameOverEnter, exit: OnGameOverExit);
        gameData.states.earlyExit = gameStateMachine.CreateState(enter: OnEarlyExitEnter, exit: OnEarlyExitExit);
        gameData.states.winExit = gameStateMachine.CreateState(enter: OnWinExitEnter, exit: OnWinExitExit);
        
        gameData.states.raid.To(gameData.states.gameOver).When(() => player.health <= 0);
    }
    
    private void InitButtonCallbacks() {
        gameData.mainMenu.playButton.AddListener(() => {
            gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.mapSelection);
        });
        
        gameData.mainMenu.hideoutButton.AddListener(() => {
            gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.hideout);
        });
        
        gameData.ui.menuBackButton.AddListener(() => {
            OnEscapePressed(new());
        });
        
        gameData.hideoutTabs.characterButton.onClick.AddListener(() => {
            ToggleHideoutTab(gameData.hideoutTabs.characterButton, gameData.hideoutTabs.characterText);
            ToggleHideoutPanels(playerPanel, stashPanel);
        });
        
        gameData.hideoutTabs.eyeForgeButton.onClick.AddListener(() => {
            ToggleHideoutTab(gameData.hideoutTabs.eyeForgeButton, gameData.hideoutTabs.eyeForgeText);
            ToggleHideoutPanels(forgeDetailsPanel, eyeForgePanel, stashPanel);
        });
        
        gameData.hideoutTabs.traderButton.onClick.AddListener(() => {
            ToggleHideoutTab(gameData.hideoutTabs.traderButton, gameData.hideoutTabs.traderText);
            ToggleHideoutPanels(traderInventoryPanel, traderTransactionPanel, stashPanel);
        });
        
        gameData.hideoutTabs.questsButton.onClick.AddListener(() => {
            ToggleHideoutTab(gameData.hideoutTabs.questsButton, gameData.hideoutTabs.questsText);
            ToggleHideoutPanels(questsPanel);
            RefreshQuestDisplays();
        });
        
        gameData.hideoutTabs.skillsButton.onClick.AddListener(() => {
            ToggleHideoutTab(gameData.hideoutTabs.skillsButton, gameData.hideoutTabs.skillsText);
            ToggleHideoutPanels(skillsPanel.rectTransform, playerStatsPanel.rectTransform);
        });

        skillsPanel.hasteSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(gameData.skillUpgradePaths.haste, player.hasteSkillLevel));
        skillsPanel.intellectSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(gameData.skillUpgradePaths.intellect, player.intellectSkillLevel));
        skillsPanel.lifeBloodSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(gameData.skillUpgradePaths.lifeBlood, player.lifeBloodSkillLevel));
        skillsPanel.strengthSkillRow.levelUpButton.AddListener(() => OnLevelupButtonPressed(gameData.skillUpgradePaths.strength, player.strengthSkillLevel));
        
        forgeEyeButton.AddListener(OnForgeButtonPressed);
        
        transactionPanel.buyToggle.AddListener(OnBuyTogglePressed);
        transactionPanel.sellToggle.AddListener(OnSellTogglePressed);
        transactionPanel.sellButton.AddListener(OnSellButtonPressed);
        transactionPanel.moneyPurchaseButton.AddListener(OnMoneyPurchaseButtonPressed);
        transactionPanel.barterPurchaseButton.AddListener(OnBarterPurchaseButtonPressed);

        for (int i = 0; i < mapSelectionButtons.Length; i++) {
            Button mapSelectionButton = mapSelectionButtons[i];
            MapData map = gameData.config.maps[i];
            mapSelectionButton.onClick.AddListener(() => LoadMapAsync(map));
        }
    }
    
    private void InitHotBar() {
        gameData.hotBar.quickUseActions = new() {
            gameData.input.quickUse1, 
            gameData.input.quickUse2, 
            gameData.input.quickUse3, 
            gameData.input.quickUse4,
        };
        gameData.hotBar.slotUIs = gameData.ui.hotBarParent.GetComponentsInChildren<InventorySlotUI>();
        Assert.IsTrue(gameData.hotBar.slotUIs.Length == playerQuickUseSize, "Make sure to match hot bar inventory UIs count with quick use count");
    }
    
}
