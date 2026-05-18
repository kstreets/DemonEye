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
        gameData.input.moveInputAction = InputSystem.actions.FindAction("Move");
        gameData.input.interactInputAction = InputSystem.actions.FindAction("Interact");
        gameData.input.inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        gameData.input.selectItemInputAction = InputSystem.actions.FindAction("SelectItem");
        gameData.input.placeSingleItemInputAction = InputSystem.actions.FindAction("PlaceSingleItem");
        gameData.input.splitStackInputAction = InputSystem.actions.FindAction("SplitStack");
        gameData.input.moveStackInputAction = InputSystem.actions.FindAction("MoveStack");
        gameData.input.useItemInputAction = InputSystem.actions.FindAction("UseItem");
        gameData.input.escapeInputAction = InputSystem.actions.FindAction("Escape");
        gameData.input.quickUse1Action = InputSystem.actions.FindAction("QuickUse1");
        gameData.input.quickUse2Action = InputSystem.actions.FindAction("QuickUse2");
        gameData.input.quickUse3Action = InputSystem.actions.FindAction("QuickUse3");
        gameData.input.quickUse4Action = InputSystem.actions.FindAction("QuickUse4");
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
        mainMenuPlayButton.AddListener(() => {
            gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.mapSelection);
        });
        
        mainMenuHideoutButton.AddListener(() => {
            gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.hideout);
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
                    gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.raid);
                });
            });
        }
    }
    
    private void InitHotBar() {
        gameData.hotBar.quickUseActions = new() {
            gameData.input.quickUse1Action, 
            gameData.input.quickUse2Action, 
            gameData.input.quickUse3Action, 
            gameData.input.quickUse4Action,
        };
        gameData.hotBar.slotUIs = hotBarParent.GetComponentsInChildren<InventorySlotUI>();
        Assert.IsTrue(gameData.hotBar.slotUIs.Length == playerQuickUseSize, "Make sure to match hot bar inventory UIs count with quick use count");
    }
    
}
