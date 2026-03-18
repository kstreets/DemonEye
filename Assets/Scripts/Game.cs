using System;
using System.Collections.Generic;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;
using Assert = UnityEngine.Assertions.Assert;
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public partial class Game : MonoBehaviour {

    public static Game gameInstance;
    
    public StartingItemsConfig startingItems;
    public Styles styles;
    public GameplayConfig gameplayConfig;
    
    [Foldout("Quests")]
    public List<QuestLine> questLines;
    public QuestGraphRuntime questGraph;
    public Quest pickPocketQuest;
    [EndFoldout]

    [Foldout("Traders")]
    public Trader potionManTrader;
    public Trader armsDealerTrader;
    public Trader hatManTrader;
    [EndFoldout]

    [Foldout("Maps")]
    public MapData[] maps;
    [EndFoldout]
    
    [Foldout("Pooling Prefabs")]
    public GameObject itemDropPrefab;
    public GameObject baseProjectilePrefab;
    public GameObject boneShatterProjectilePrefab;
    public GameObject bloodDropPrefab;
    public GameObject poisonDebuffPrefab;
    public GameObject explosionPrefab;
    public GameObject boomonExplosionPrefab;
    public GameObject gooProjectilePrefab;
    public GameObject piercingProjectilePrefab;
    public GameObject projectileImpactPrefab;
    public GameObject teleportInPrefab;
    public GameObject teleportOutPrefab;
    public GameObject bloodSplatterPrefab;
    public GameObject runSmokePrefab;
    public GameObject slamSmokePrefab;
    public GameObject blastPrefab;
    [EndFoldout]
    
    [Foldout("Item Type Refs")]
    public ItemType quickUseType;
    public ItemType backpackType;
    public ItemType eyeType;
    public ItemType demonEyeType;
    public ItemType gemType;
    public ItemType eyeModifierType;
    public ItemType wearableModifierType;
    [EndFoldout]

    [Foldout("Skill Upgrade Paths")]
    public SkillUpgradePath hasteUpgradePath;
    public SkillUpgradePath intellectUpgradePath;
    public SkillUpgradePath lifeBloodUpgradePath;
    public SkillUpgradePath strengthUpgradePath;
    [EndFoldout]
    
    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public PixelPerfectCamera pixelPerfectCamera;

    public GameObject playerPrefab;
    public GameObject gemRockPrefab;
    public GameObject deadBodyPrefab;
    public GameObject altarPrefab;
    public GameObject bushPrefab;

    public Item demonEyeItem;
    
    [Foldout("Effects")]
    public AnimationCurve hitFlashCurve;
    public AnimationCurve bounceCurve;
    public AnimationCurve shakeCurve;
    [EndFoldout]

    [Foldout("UI/Prefabs")]
    public GameObject inventorySlotPrefab;
    public GameObject eyeForgeSlotPrefab;
    public GameObject rockSmokePrefab;
    public GameObject damageNumberPrefab;
    public GameObject forgeExplosionPrefab;
    public GameObject forgeDustExplosionPrefab;
    public GameObject questSelectionTogglePrefab;
    public GameObject questPrefab;
    [EndFoldout]

    [Foldout("UI/MiscRefs")]
    public RectTransform mainCanvasRectTransform;
    public ItemDescPopup itemDescPopup;
    public MechanicDescPopup mechanicDescPopup;
    public UIElementPopup uiElementPopup;
    public RectTransform hideoutParent;
    public RectTransform hotBarParent;
    public ItemUI dragAndDropItemUI;
    public Image menuBackgroundImage;
    public Image deathBackgroundImage;
    public ButtonFeel menuBackButton;
    public TextMeshProUGUI smallRaidText;
    public TypewriterComponent smallRaidTextTypewriter;
    public TextMeshProUGUI largeRaidText;
    public TypewriterComponent largeRaidTextTypewriter;
    [EndFoldout]
    
    [Foldout("UI/Main Menu")]
    public RectTransform mainMenuParent;
    public RectTransform mainMenuLogo;
    public ButtonFeel mainMenuPlayButton;
    public ButtonFeel mainMenuHideoutButton;
    public ButtonFeel mainMenuSettingsButton;
    public ButtonFeel mainMenuExitButton;
    [EndFoldout]
    
    [Foldout("UI/HideoutTabs")]
    public RectTransform hideoutTabsParent;
    public Sprite tabNonSelectedSprite;
    public Sprite tabSelectedSprite;
    public Button characterTabButton;
    public Button eyeForgeTabButton;
    public Button traderTabButton;
    public Button questsTabButton;
    public Button skillsTabButton;
    public TextMeshProUGUI characterTabText;
    public TextMeshProUGUI eyeForgeTabText;
    public TextMeshProUGUI traderTabText;
    public TextMeshProUGUI questsTabText;
    public TextMeshProUGUI skillsTabText;
    [EndFoldout]

    [Foldout("UI/PlayerPanel")]
    public RectTransform playerPanel;
    public RectTransform playerEquipmentParent;
    public RectTransform playerPocketParent;
    // public RectTransform playerBackpackParent;
    public RectTransform playerPocketsBackpackParent;
    public RectTransform playerPassiveParent;
    public RectTransform playerInventoryParent;
    public TextMeshProUGUI playerPanelHealthText;
    public TextMeshProUGUI playerPanelWeightText;
    public TextMeshProUGUI agilityStatValueText;
    public TextMeshProUGUI bleedResStatValueText;
    public TextMeshProUGUI healthStatValueText;
    public TextMeshProUGUI strengthStatValueText;
    public Image playerPreviewImage;
    [EndFoldout]
    
    [Foldout("UI/StashPanel")]
    public RectTransform stashPanel;
    public RectTransform stashInventoryParent;
    [EndFoldout]
    
    [Foldout("UI/EyeForgePanel")]
    public RectTransform eyeForgePanel;
    public RectTransform crucibleParent;
    public Image pentagramFillImage;
    public AnimationCurve pentagramFillCurve;
    public AnimationCurve itemShakeCurve;
    [EndFoldout]
    
    [Foldout("UI/ForgeDetailsPanel")]
    public RectTransform forgeDetailsPanel;
    public RectTransform forgeDetailsUpgradeScreen;
    public RectTransform forgeDetailsForgeScreen;
    public ButtonFeel upgradeForgeButton;
    public ButtonFeel forgeEyeButton;
    public TextMeshProUGUI forgeDetailsForgeText;
    public List<ResourceRequirement> forgeDetailsResourceRequirements;
    [EndFoldout]
    
    [Foldout("UI/TraderPanel")]
    public RectTransform traderTransactionPanel;
    public RectTransform traderInventoryPanel;
    public RectTransform traderInventoryParent;
    public RectTransform traderTransactionInventoryParent;
    public TextMeshProUGUI traderTransactionInfoText;
    public Image traderXpLevelFill;
    public TextMeshProUGUI traderLevelText;
    public TextMeshProUGUI traderRemainingXpText;
    public TextMeshProUGUI traderItemRefreshTimeText;
    public TransactionPanel transactionPanel;
    [EndFoldout]

    [Foldout("UI/MapSelectionPanel")]
    public RectTransform mapSelectionPanel;
    public Button[] mapSelectionButtons;
    [EndFoldout]
    
    [Foldout("UI/QuestsPanel")]
    public RectTransform questsPanel;
    public RectTransform questsParent;
    public RectTransform questSelectionParent;
    public ToggleButtonGroup questToggleButtonGroup;
    [EndFoldout]
    
    [Foldout("UI/SkillsTab")]
    public SkillsPanel skillsPanel;
    public PlayerStatsPanel playerStatsPanel;
    [EndFoldout]

    [Foldout("UI/Health&Currency")]
    public GameObject playerInfoParent;
    public GameObject healthBarParent;
    public GameObject weightBarParent;
    public GameObject soulsCurrencyParent;
    public GameObject coinsCurrencyParent;
    public GameObject bleedDebuffIcon;
    public TextMeshProUGUI soulsCurrencyText;
    public TextMeshProUGUI coinCurrencyText;
    [EndFoldout]
    
    [Foldout("UI/InRaid")]
    public RectTransform lootInventoryPanel;
    public RectTransform lootInventoryParent;
    public GameObject lootSearchingText;
    public Image healthBarFillImage;
    public Image weightBarFillImage;
    public TextMeshProUGUI interactPrompt;
    public TextMeshProUGUI interactionDetails;
    public RectTransform portalArrow;
    [EndFoldout]

    [Foldout("UI/RaidInfoPanel")]
    public GameObject raidInfoPanelParent;
    public GameObject finalWaveCountdownParent;
    public TextMeshProUGUI finalWaveCountdownText;
    public GameObject exitPortalCountdownParent;
    public TextMeshProUGUI exitPortalCountdownText;
    public GameObject exitPortalActiveNotifier;
    public GameObject finalWaveActiveNotifier;
    public GameObject finalExitPortalNotifier;
    [EndFoldout]
    
    [Foldout("UI/DamageNumbers")]
    public RectTransform damageNumbersParent;
    [EndFoldout]
    
    [Foldout("UpgradePaths")]
    public UpgradePath crucibleUpgradePath; 
    [EndFoldout]
    
    [Foldout("TraderLevels")]
    public TraderLevels traderLevels;
    [EndFoldout]
    
    [Foldout("Sfx")]
    public GameObject dynamicAudioSourcePrefab;
    public DynamicClip shootClip;
    public DynamicClip stoneBreakClip;
    public DynamicClip stoneHitClip;
    public DynamicClip projectileImpact;
    public DynamicClip bloodBurstClip;
    public DynamicClip footStepClip;
    public DynamicClip teleportInClip;
    public DynamicClip teleportOutClip;
    public DynamicClip portalSpawnClip;
    public DynamicClip portalDespawnClip;
    public DynamicClip finalWaveStingerClip;
    public AudioClip ambienceClip;
    public AudioMixerGroup ambienceMixerGroup;
    [EndFoldout]
    
    private InputAction moveInputAction;
    private InputAction interactInputAction;
    private InputAction inventoryInputAction;
    private InputAction selectItemInputAction;
    private InputAction placeSingleItemInputAction;
    private InputAction useItemInputAction;
    private InputAction moveStackInputAction;
    private InputAction splitStackInputAction;
    private InputAction escapeInputAction;
    private InputAction quickUse1Action;
    private InputAction quickUse2Action;
    private InputAction quickUse3Action;
    private InputAction quickUse4Action;
    
    private EntityPool<Entity> itemDropPool;
    private EntityPool<Entity> bloodDropPool;
    private EntityPool<Projectile> projectilePool;
    private EntityPool<Projectile> boneShatterProjectilePool;
    private EntityPool<Projectile> gooProjectilePool;
    private EntityPool<Projectile> piercingShotProjectilePool;
    private EntityPool<Entity> poisonDebuffPool;
    private EntityPool<Entity> explosionPool;
    private EntityPool<Entity> projectileImpactPool;
    private EntityPool<Entity> teleportInPool;
    private EntityPool<Entity> teleportOutPool;
    private EntityPool<Entity> bloodSplatterPool;
    private EntityPool<Entity> runSmokePool;
    private EntityPool<Entity> damageNumberPool;
    private EntityPool<Entity> forgeExplosionPool;
    private EntityPool<Entity> forgeDustExplosionPool;
    private EntityPool<Entity> blastPool;
    
    private State mainMenuState;
    private State mapSelectionState;
    private State hideoutState;
    private State raidState;
    private State gameOverState;
    private State winExitState;
    private State earlyExitState;
    private StateMachine gameStateMachine = new();

    public static Action<InventorySlot[]> onSoldItemsToTrader;
    public static Action<InventorySlot[]> onReturnedFromRaid;
    public static Action<string> customQuestEvent;
    
    private void Start() {
        gameInstance = this;
        
        LoadAllResources();
        InitAudio();
        
        BuildSavePaths();
        player = SpawnEntity<Player>(playerPrefab, Vector3.zero, Quaternion.identity, null, EntityLifetime.Global);
        LoadAndAssignPlayerSaveData(player);

        DemonEyeTween.Init();
        CreateDropPools();
        
        InitInventories();
        InitButtonCallbacks();
        InitTrader();
        InitQuests();
        
        Cursor.visible = true;
        OnGameStartInitUI();
        
        itemDropPool = CreateEntityPool<Entity>(itemDropPrefab, 20, null);
        bloodDropPool = CreateEntityPool<Entity>(bloodDropPrefab, 10, null);
        projectilePool = CreateEntityPool<Projectile>(baseProjectilePrefab, 20, OnSpawnProjectile);
        boneShatterProjectilePool = CreateEntityPool<Projectile>(boneShatterProjectilePrefab, 20, OnSpawnProjectile);
        gooProjectilePool = CreateEntityPool<Projectile>(gooProjectilePrefab, 20, OnSpawnProjectile);
        piercingShotProjectilePool = CreateEntityPool<Projectile>(piercingProjectilePrefab, 20, OnSpawnProjectile);
        poisonDebuffPool = CreateEntityPool<Entity>(poisonDebuffPrefab, 10, null);
        explosionPool = CreateEntityPool<Entity>(explosionPrefab, 5, null);
        projectileImpactPool = CreateEntityPool<Entity>(projectileImpactPrefab, 20, null);
        teleportInPool = CreateEntityPool<Entity>(teleportInPrefab, 20, null);
        teleportOutPool = CreateEntityPool<Entity>(teleportOutPrefab, 20, null);
        bloodSplatterPool = CreateEntityPool<Entity>(bloodSplatterPrefab, 20, null);
        runSmokePool = CreateEntityPool<Entity>(runSmokePrefab, 5, null);
        damageNumberPool = CreateEntityPool<Entity>(damageNumberPrefab, 20, null);
        forgeExplosionPool = CreateEntityPool<Entity>(forgeExplosionPrefab, 10, null);
        forgeDustExplosionPool = CreateEntityPool<Entity>(forgeDustExplosionPrefab, 10, null);
        blastPool = CreateEntityPool<Entity>(blastPrefab, 5, null);

        equipedEye = emptyDemonEye;

        moveInputAction = InputSystem.actions.FindAction("Move");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        selectItemInputAction = InputSystem.actions.FindAction("SelectItem");
        placeSingleItemInputAction = InputSystem.actions.FindAction("PlaceSingleItem");
        splitStackInputAction = InputSystem.actions.FindAction("SplitStack");
        moveStackInputAction = InputSystem.actions.FindAction("MoveStack");
        useItemInputAction = InputSystem.actions.FindAction("UseItem");
        escapeInputAction = InputSystem.actions.FindAction("Escape");
        quickUse1Action = InputSystem.actions.FindAction("QuickUse1");
        quickUse2Action = InputSystem.actions.FindAction("QuickUse2");
        quickUse3Action = InputSystem.actions.FindAction("QuickUse3");
        quickUse4Action = InputSystem.actions.FindAction("QuickUse4");
        
        var menuMove = InputSystem.actions.FindAction("MenuMove");
        menuMove.performed += OnMoveInput;

        escapeInputAction.performed += OnEscapePressed;

        mainMenuState = gameStateMachine.CreateState(null, OnMainMenuStateEnter, OnMainMenuStateExit);
        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        mapSelectionState = gameStateMachine.CreateState(OnMapSelectionUpdate, OnMapSelectionEnter, OnMapSelectionExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEnter, OnRaidStateExit);
        gameOverState = gameStateMachine.CreateState(null, OnGameOverEnter, OnGameOverExit);
        earlyExitState = gameStateMachine.CreateState(null, OnEarlyExitEnter, OnEarlyExitExit);
        winExitState = gameStateMachine.CreateState(null, OnWinExitEnter, OnWinExitExit);
        
        raidState.To(gameOverState).When(() => player.health <= 0);
    }

    Vector2 controllerPos;
    
    private void OnMoveInput(InputAction.CallbackContext context) {
        if (!context.performed) return;
        
        GameObject[,] mainMenuGrid = new GameObject[4, 1];
        mainMenuGrid[0, 0] = mainMenuPlayButton.gameObject;
        mainMenuGrid[1, 0] = mainMenuHideoutButton.gameObject;
        mainMenuGrid[2, 0] = mainMenuSettingsButton.gameObject;
        mainMenuGrid[3, 0] = mainMenuExitButton.gameObject;
        
        Vector2 dir = context.ReadValue<Vector2>();
        dir = new(dir.x, -dir.y);
        
        if (gameStateMachine.CurState == mainMenuState) {
            if (!mainMenuGrid.IndexInRange(controllerPos + dir)) return;
            controllerPos += dir;
            
            GameObject selected = mainMenuGrid[(int)controllerPos.y, (int)controllerPos.x];
            HightlightControllerSelection(selected);
            print(selected.gameObject.name);
        }
    }

    private GameObject currentlyHiglighted;
    
    private void HightlightControllerSelection(GameObject selectedGameObject) {
        if (currentlyHiglighted) {
            DehighlightControllerSelection(currentlyHiglighted);     
        }
        
        if (selectedGameObject.TryGetComponent(out ButtonFeel button)) {
            button.OnPointerEnter(null);
        }
        
        currentlyHiglighted = selectedGameObject;
    }
    
    private void DehighlightControllerSelection(GameObject selectedGameObject) {
        if (selectedGameObject.TryGetComponent(out ButtonFeel button)) {
            button.OnPointerExit(null);
        }
    }

    private void Update() {
        gameStateMachine.Tick();
        DemonEyeTween.Update();
        UpdateTrader();
        UpdateQuests();
        foreach (Inventory inventory in allInventories) {
            RefreshInventoryDisplay(inventory);
        }
        UpdateGraySlots();
        
        #if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.middleButton.isPressed) {
            Time.timeScale = 4f;
        }
        else {
            Time.timeScale = 1f;
        }
        #endif
    }

    private void FixedUpdate() {
        if (!InRaid) return;
        loadedMapInst.grid.CompleteFlowFieldCalculation();
        loadedMapInst.grid.ScheduleFlowFieldCalculation(player.position);
        FixedUpdateEnemies();
    }

    private void LateUpdate() {
        UpdatePlayerPanelUI();
        UpdateForgeInfoPanel();
        UpdateHotBarUI();
        UpdateDragAndDropItemToCursor();
        UpdateCurrencyNumbers();
        if (InRaid) {
            UpdateInRaidUI();
        }
    }

    private void OnApplicationQuit() {
        SaveTrader();
    }

    private void UpdateTimers() {
        discoverLootTimer.Tick();
    }

    private void OnMainMenuStateEnter() {
        Cursor.visible = true;
        ShowMainMenuUI();
    }

    private void OnMainMenuStateExit() {
        CloseMainMenuUI();
    }

    private void OnHideoutStateEnter() {
        ShowHideoutUI();
        RefreshSkillsPanel();
    }

    private void OnHideoutStateExit() {
        CloseHideoutUI();
        SavePlayerData();
        SaveTrader();
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
        SaveInventory(crucibleInventory);
        SaveActiveQuestProgresses();
    }

    private void OnHideoutStateUpdate() {
        UpdateInventory();
        RefreshTransactionUI();
        UpdateForgeState();
    }

    private void OnMapSelectionEnter() {
        ShowMapSelectionUI();
    }

    private void OnMapSelectionExit() {
        CloseMapSelectionUI();
    }

    private void OnMapSelectionUpdate() {
        CheckForHotBarInteractions();
        UpdateInventory();
    }

    private void OnRaidStateEnter() {
        InitRaid();
        PlayAmbience();
    }

    private void OnRaidStateExit() {
        DeinitPlayer();
        ClosePlayerInventory();
        CloseLootInventory();
        HideInventoryItemPopup();
        HideUIElementPopup();
        CloseRaidUI();
        StopAmbience();
    }

    private void OnRaidStateUpdate() {
        UpdateRaidState();
        UpdateTimers();
        CheckForInteractions();
        CheckForHotBarInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        UpdateSpawnManager();
        loadedMapInst.grid.FeedPlayerVelocity(player.position, player.velocity);
        UpdateEnemies();
    }

    private void OnEarlyExitEnter() {
        OnSaveWhenRaidIsOver();
        AnimateEarlyExitSequence(() => gameStateMachine.SetStateIfNotCurrent(mainMenuState));
    }
    
    private void OnEarlyExitExit() {
        DeinitRaid();
    }
    
    private void OnWinExitEnter() {
        OnSaveWhenRaidIsOver();
        AnimateGameWinSequence(() => gameStateMachine.SetStateIfNotCurrent(mainMenuState));
    }

    private void OnWinExitExit() {
        DeinitRaid();
    }

    private void OnGameOverEnter() {
        ClearInventory(playerInventory);
        OnSaveWhenRaidIsOver();
        AnimateGameOverSequence(() => gameStateMachine.SetStateIfNotCurrent(mainMenuState)); 
    }
    
    private void OnGameOverExit() {
        player.health = FullPlayerHealth;
        DeinitRaid();
    }
    
    private enum RaidState { None, InitialWaves, FinalWave, PostFinalWave }
    private RaidState curRaidState;
    private bool raidStateSwitchedThisFrame;
    private Sequence raidEnterSequence;
    
    private void InitRaid() {
        curRaidState = RaidState.None;
        
        Cursor.visible = false;
        ShowRaidUI();

        deathBackgroundImage.enabled = false;
        
        loadedMapInst.gameObject.SetActive(true);
        loadedMapInst.grid.Init();

        int randomSpawnIndex = Random.Range(0, loadedMapInst.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = loadedMapInst.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.position = randomSpawnPos;
        player.gameObject.SetActive(false);
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, cinemachineCamera.transform.position.z);
        cinemachineCamera.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        cinemachineCamera.Follow = player.trans;
        
        ClearInteractions();
        ResetDamageHandlingTempData();
        InitSpawnManager(loadedMapData.waves);
        SpawnMapResources(loadedMapInst.resourceParent);
        SpawnInitialExitPortals(loadedMapInst.exitPortalsParent, loadedMapData.exitPortalsCount);
        
        // Animation Sequence
        {
            int initialPPU = pixelPerfectCamera.assetsPPU;
            pixelPerfectCamera.assetsPPU = 80;
            
            raidEnterSequence = Sequence.Create();
            
            deathBackgroundImage.enabled = true;
            deathBackgroundImage.fillAmount = 1f;
            raidEnterSequence.Chain(Tween.Alpha(deathBackgroundImage, 1f, 0f, 0.5f, Ease.InCubic));
            
            raidEnterSequence.ChainDelay(0.25f);

            raidEnterSequence.ChainCallback(() => {
                Entity inTeleportEntity = SpawnEntity(teleportInPool, OffsetY(player.position, -0.05f), Quaternion.identity);
                DestroyEntity(inTeleportEntity, CurrentClipLength(inTeleportEntity.animator));
                PlayAudioClip(teleportInClip, inTeleportEntity.position);
            });
            
            raidEnterSequence.ChainDelay(0.35f);
            raidEnterSequence.ChainCallback(() => {
                player.gameObject.SetActive(true);
                InitPlayer();
            });
            raidEnterSequence.Chain(Tween.Scale(player.trans, 0f, 1f, 0.2f, Ease.InOutBack));
            
            raidEnterSequence.ChainDelay(0.6f);
            raidEnterSequence.Chain(Tween.Custom(pixelPerfectCamera.assetsPPU, initialPPU, 0.25f, ease: Ease.OutQuad, onValueChange: val => {
                pixelPerfectCamera.assetsPPU = (int)val;
            }));
        }
    }

    private void UpdateRaidState() {
        RaidState prevState = curRaidState;
        
        if (spawnManager.timeUntilFinalWave >= 0f) {
            curRaidState = RaidState.InitialWaves;
        }
        else if (!spawnManager.isFinishedSpawning || enemies.Count > 0) {
            curRaidState = RaidState.FinalWave;
        }
        else {
            curRaidState = RaidState.PostFinalWave;
        }

        raidStateSwitchedThisFrame = prevState != curRaidState;
        
        if (raidStateSwitchedThisFrame && curRaidState == RaidState.FinalWave) {
            PlayAudioClip(finalWaveStingerClip, player.position);
        }

        if (raidStateSwitchedThisFrame && curRaidState == RaidState.PostFinalWave) {
            Tween.Delay(0.25f, static () => {
                gameInstance.AnimateLargeRaidText(ColorText("Map Cleared!", gameInstance.styles.increaseDescColor), 1.8f);
                gameInstance.SpawnFinalExitPortal();
            });
        }
    }
    
    private void DeinitRaid() {
        enemies.Clear();
        projectiles.Clear();
        loadedMapInst.grid.Deinit();
        DestroyEntities(EntityLifetime.Level);
        UnloadCurrentMapAsync();
    }
    
    private void OnSaveWhenRaidIsOver() {
        SaveInventory(playerInventory);
        SavePlayerData();
        SaveActiveQuestProgresses();
    }

    // *******************************
    // Interactions 
    // *******************************
    
    private float timeSpentSummoningPortal;
    
    private void ClearInteractions() {
        timeSpentSummoningPortal = 0f;
    }
    
    private void CheckForInteractions() { 
        DisableInteractionPrompt();
        
        Vector2 checkCenter = player.position + new Vector3(0f, 0.05f, 0f);
        List<Collider2D> cols = OverlapCircle(checkCenter, 0.1f, Masks.ItemMask);
        
        foreach (Collider2D col in cols) {
            if (col.CompareTag(Tags.Pickup)) {
                ItemDrop itemDrop = col.GetComponent<ItemDrop>();
                Item dropItemRef = itemDrop.ItemInstance.ItemRef;
                
                Color itemColor = styles.GetColorForRarity(dropItemRef.GetRarity());
                string details = ColorText($"{dropItemRef.displayName} x{itemDrop.ItemInstance.count}", itemColor);
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), details);
                
                if (interactInputAction.WasPressedThisFrame()) {
                    InventoryAddResult result = TryAddItemToInventory(playerInventory, itemDrop.ItemInstance);
                    if (result.type == InventoryAddResult.ResultType.Success) {
                        Entity droppedEntity = entityLookup[itemDrop.gameObject];
                        PickupDroppedItem(droppedEntity); 
                        itemDrop.circleCollider.enabled = false;
                    }
                    else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
                        itemDrop.ItemInstance.count -= result.addedCount;
                    }
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Body");
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInventoryPtr.slots = deadBodySlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }
            
            if (col.CompareTag(Tags.Bush)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Bush");
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInventoryPtr.slots = bushSlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.Altar)) {
                int soulsPrice = loadedMapData.altarSoulPrice;
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), $"{soulsPrice} Souls");
                if (interactInputAction.WasPressedThisFrame() && player.soulCurrency >= soulsPrice) {
                    player.soulCurrency -= soulsPrice;
                    Item dropItem = GetItemFromDropPool(eyeUpgradesDropPool);
                    Entity item = SpawnItemAsEntity(dropItem, 1, OffsetY(col.transform.position, 0.2f), Quaternion.identity);
                    item.spriteRenderer.sortingOrder = 1;
                    col.enabled = false;
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                ExitPortal portal = GetExitPortalFromTransform(col.transform);
                
                if (!portal.hasBeenSummoned && timeSpentSummoningPortal < gameplayConfig.portalSummonTime) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Summon Exit Portal");
                    if (interactInputAction.IsPressed()) {
                        timeSpentSummoningPortal += Time.deltaTime;
                        if (timeSpentSummoningPortal >= gameplayConfig.portalSummonTime) {
                            StartSummoningExitPortal(col.transform);
                            timeSpentSummoningPortal = 0f;
                        }
                    }
                    else {
                        timeSpentSummoningPortal = 0f;
                    }
                }
                
                if (portal.canTake) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Take Exit Portal");
                    if (interactInputAction.WasPressedThisFrame()) {
                        exitPortalTakenByPlayer = portal;
                        exitPortalTakenByPlayer.closingCountdownSequence.Stop();
                        gameStateMachine.SetStateIfNotCurrent(curRaidState == RaidState.PostFinalWave ? winExitState : earlyExitState);
                        
                        customQuestEvent?.Invoke("FirstExtract");
                        onReturnedFromRaid?.Invoke(playerInventory.slots);
                    }
                }
            }
        }
    }

    private void PickupDroppedItem(Entity droppedEntity) {
        Vector3 playerPickupTarget = new(0f, 0.07f, 0f);
        
        droppedEntity.GetEffect(EffectsIndicies.Bounce).Stop();
        droppedEntity.trans.SetParent(player.trans, true);
        
        TweenSettings horizontalSettings = new() {
            duration = 0.15f,
            ease = Ease.InQuart,
        };
        
        TweenSettings verticalSettings = new() {
            duration = 0.09f,
            ease = Ease.InQuart,
        };
        
        TweenSettings itemScaleSettings = new() {
            startDelay = 0.03f,
            duration = 0.15f,
            ease = Ease.InCubic,
        };
        
        ShakeSettings playerScaleSettings = new() {
            startDelay = 0.1f,
            duration = 0.08f,
            strength = Vector2.one * 0.15f,
            frequency = 5f,
        };
        
        Tween.LocalPositionX(droppedEntity.trans, playerPickupTarget.x, horizontalSettings)
        .Group(Tween.LocalPositionY(droppedEntity.trans, playerPickupTarget.y, verticalSettings))
        .Group(Tween.Scale(droppedEntity.trans, 0f,itemScaleSettings))
        .Group(Tween.PunchScale(player.trans, playerScaleSettings))
        .OnComplete(() => DestroyEntity(droppedEntity));
    }
    
    // *******************************
    // Hot Bar 
    // *******************************
    
    private InventorySlotUI[] hotBarItemUIs;
    
    private void UpdateHotBarUI() {
        if (!hotBarParent.gameObject.activeInHierarchy) return;

        hotBarItemUIs ??= hotBarParent.GetComponentsInChildren<InventorySlotUI>();
        Assert.IsTrue(hotBarItemUIs.Length == playerQuickUseSize, "Make sure to match hot bar inventory UIs count with quick use count");

        for (int i = 0; i < playerQuickUseSize; i++) {
            int itemIndex = i + playerEquipmentSize;
            hotBarItemUIs[i].ClearItem();

            ItemInstance itemInstance = playerInventory.slots[itemIndex].itemInstance;
            if (itemInstance != null) {
                hotBarItemUIs[i].SetItem(itemInstance.ItemRef, itemInstance.count);
            } 
        }
    }

    private List<InputAction> quickUseActions;
    
    private void CheckForHotBarInteractions() {
        Item itemToConsume = null;
        
        quickUseActions ??= new() {
            quickUse1Action, quickUse2Action, quickUse3Action, quickUse4Action,
        };

        int playerInventorySlotIndex = playerEquipmentSize;
        foreach (InputAction action in quickUseActions) {
            if (action.WasPressedThisFrame()) {
                itemToConsume = playerInventory.slots[playerInventorySlotIndex].itemInstance?.ItemRef;
                break;
            }
            playerInventorySlotIndex++;
        }

        if (itemToConsume) {
            HavePlayerConsumeItem(playerInventory, playerInventorySlotIndex);
        }
    }

    // ***************************
    // Exit Portals 
    // ***************************
    
    private List<ExitPortal> activeExitPortals = new();
    private ExitPortal exitPortalTakenByPlayer;
    
    private class ExitPortal {
        public Transform transform;
        public Sequence summoningPortalSequence;
        public Sequence closingCountdownSequence;
        public bool hasBeenSummoned;
        public bool canTake;
    }
    
    private void StartSummoningExitPortal(Transform exitPortalTrans) {
        ExitPortal portal = GetExitPortalFromTransform(exitPortalTrans);
        portal.hasBeenSummoned = true;
        
        portal.summoningPortalSequence = Sequence.Create();
        portal.summoningPortalSequence.ChainDelay(gameplayConfig.portalPostSummonDelay);
        portal.summoningPortalSequence.Chain(Tween.Scale(portal.transform, Vector3.one, 0.25f, Ease.OutBack));
        
        portal.summoningPortalSequence.OnComplete(portal, static (portal) => {
            portal.canTake = true;
            gameInstance.StartClosingExitPortal(portal);
        });
    }
    
    private void StartClosingExitPortal(ExitPortal portal) {
        portal.closingCountdownSequence = Sequence.Create();
        portal.closingCountdownSequence.ChainDelay(gameplayConfig.portalActiveDuration);
        portal.closingCountdownSequence.ChainCallback(portal, static (portal) => {
            portal.canTake = false;
            gameInstance.activeExitPortals.Remove(portal);
            Tween.Scale(portal.transform, Vector3.zero, 0.25f, Ease.OutCubic);
        });
    }
    
    private ExitPortal GetExitPortalFromTransform(Transform trans) {
        foreach (ExitPortal portal in activeExitPortals) {
            if (portal.transform == trans) {
                return portal;
            }
        }
        Assert.IsTrue(false, "We should not be requesting a portal from a non-valid transform");
        return null;
    }
    
    private void SpawnInitialExitPortals(Transform exitPortalParent, int exitPortalsCount) {
        Assert.IsTrue(exitPortalsCount > 0, $"{nameof(exitPortalsCount)} needs to be 1 or more");
        
        activeExitPortals.Clear();
        exitPortalTakenByPlayer = null;
        
        using var _ = ListPool<Transform>.Get(out List<Transform> possibleExitPortals);
        
        foreach (Transform portal in exitPortalParent) {
            portal.gameObject.SetActive(false);
            if (Vector2.Distance(player.position, portal.position) > 5) {
                possibleExitPortals.Add(portal);
            }
        }
        
        possibleExitPortals.Shuffle();
        
        for (int i = 0; i < exitPortalsCount; i++) {
            activeExitPortals.Add(new() {
                transform = possibleExitPortals[i],
            });
            possibleExitPortals[i].gameObject.SetActive(true);
            possibleExitPortals[i].transform.localScale = Vector3.one * 0.25f;
        }
    }
    
    private void SpawnFinalExitPortal() {
        for (int i = 0; i < 100; i++) {
            Vector2 randomPos = player.position.ToVector2() + Random.insideUnitCircle * Random.Range(0.5f, 1.5f);
            if (OverlapCircle(randomPos, 0.2f, Masks.StaticLevelMask).Count > 0) continue;
            
            Transform exitPortalParent = loadedMapInst.exitPortalsParent;
            int randomSpawnIndex = Random.Range(0, exitPortalParent.childCount);
            Transform newExitPortalTrans = exitPortalParent.GetChild(randomSpawnIndex);
            
            newExitPortalTrans.gameObject.SetActive(true);
            newExitPortalTrans.position = randomPos;
            
            activeExitPortals.Add(new() {
                transform = newExitPortalTrans,
                canTake = true,
            });
            
            Tween.Scale(newExitPortalTrans, 0f, 1f, 0.5f, Ease.OutBack);
            PlayAudioClip(portalSpawnClip, newExitPortalTrans.position);
            return;
        }
        
        // This is a fail safe incase we couldn't spawn the final portal
        gameStateMachine.SetState(winExitState);
    }
    
    // ***************************
    // Helpers 
    // ***************************

    public static bool RollProbability(float probability) {
        return Random.value <= probability;
    }
    
    private Vector2 ScreenCenter => new(Screen.width / 2f, Screen.height / 2f);
    
    private bool InHideout => gameStateMachine.CurState == hideoutState;
    
    private bool InMapSelection => gameStateMachine.CurState == mapSelectionState;
    
    public bool InRaid => gameStateMachine.CurState == raidState;

    public bool ControllerPluggedIn => Gamepad.current != null;

    private Vector3 RotationVector360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }
    
    private Vector3 RotationVector(float degrees) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right;
    }
    
    private Vector3 RotationVector(float degrees, float minDist, float maxDist) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }

    private Vector3 RandomizeVectorAngle(Vector3 vector, float degreeDelta) {
        return Quaternion.AngleAxis(Random.Range(-degreeDelta, degreeDelta), Vector3.forward) * vector;
    }
    
    private Quaternion RandomRotation() {
        return Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
    }
    
    private Vector2 OffsetY(Vector2 pos, float yOffset) {
        return new(pos.x, pos.y + yOffset);
    }
    
    private Vector2 OffsetX(Vector2 pos, float xOffset) {
        return new(pos.x + xOffset, pos.y);
    }

    private float RandomSign() {
        return Random.Range(-1, 2);
    }

    private float CurrentClipLength(Animator anim) {
        return anim.GetCurrentAnimatorStateInfo(0).length;
    }

    private List<Collider2D> _overlapResults = new(1000);
    
    private List<Collider2D> OverlapCircle(Vector2 center, float radius, LayerMask mask) {
        _overlapResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCircle(center, radius, contactFilter, _overlapResults);
        Assert.IsFalse(count > _overlapResults.Capacity);
        
        return _overlapResults;
    }
    
    private string GetCountdownText(float timeLeft) {
        float time = Mathf.Clamp(timeLeft, 0f, float.MaxValue);
        int minutesLeft = Mathf.FloorToInt(time / 60f);
        int secondsLeft = Mathf.FloorToInt(time % 60f);
        return $"{minutesLeft:00}:{secondsLeft:00}";
    }

    private static string SizeText(string text, int fontSize) {
        return $"<size={fontSize}>{text}</size>";
    }
    
    public static string ColorText(string text, Color color) {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }
    
    public static string DisplayProb(float probability) {
        return ColorText($"{Mathf.FloorToInt(probability * 100f)}%", gameInstance.styles.timeDescColor);
    }

    public static string DisplayProbIncDec(float probability) {
        return probability >= 0f ? DisplayProbIncrease(probability) : DisplayProbDecrease(probability);
    }

    public static string DisplayProbIncrease(float probability) {
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", gameInstance.styles.increaseDescColor);
    }
    
    public static string DisplayProbDecrease(float probability) {
        return ColorText($"-{Mathf.Abs(Mathf.FloorToInt(probability * 100f))}%", gameInstance.styles.decreaseDescColor);
    }

    public static string DisplayNumber(int number) {
        return ColorText(number.ToString(), gameInstance.styles.timeDescColor);
    }
    
    public static string DisplayNumber(float number) {
        return ColorText(number.ToString("0.00"), gameInstance.styles.timeDescColor);
    }

    public static string DisplayIncDec(int amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(int amount) {
        return ColorText($"+{amount}", gameInstance.styles.increaseDescColor);
    }
    
    public static string DisplayDecrease(int amount) {
        return ColorText($"-{Mathf.Abs(amount)}", gameInstance.styles.decreaseDescColor);
    }

    public static string DisplayIncDec(float amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(float amount) {
        return ColorText($"+{amount:0.00}", gameInstance.styles.increaseDescColor);
    }
    
    public static string DisplayDecrease(float amount) {
        return ColorText($"-{Mathf.Abs(amount):0.00}", gameInstance.styles.decreaseDescColor);
    }
    
    public static string DisplayMultiplier(float multiplier) {
        Color textColor = multiplier >= 1f ? gameInstance.styles.increaseDescColor : gameInstance.styles.decreaseDescColor;
        return ColorText($"{multiplier:0.00}x", textColor);
    }

    public static string DisplayMultiplierIncDec(float multiplier) {
        return multiplier >= 0f ? DisplayMultiplierIncrease(multiplier) : DisplayMultiplierDecrease(multiplier);
    }

    public static string DisplayMultiplierIncrease(float multiplier) {
        return ColorText($"+{multiplier:0.00}x", gameInstance.styles.increaseDescColor);
    }
    
    public static string DisplayMultiplierDecrease(float multiplier) {
        return ColorText($"-{multiplier:0.00}x", gameInstance.styles.decreaseDescColor);
    }

    public static string DisplaySeconds(float time) {
        if (time == 1f) {
            return ColorText($"{time:0}<space=0.12em>s", gameInstance.styles.timeDescColor);
        }
        
        bool isWholeNumber = time % 1 == 0;
        if (isWholeNumber) {
            return ColorText($"{time:0}<space=0.12em>s", gameInstance.styles.timeDescColor);
        }
        
        return ColorText($"{time:0.0#}<space=0.12em>s", gameInstance.styles.timeDescColor);
    }
    
    public static int TaperInteger(int value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return Mathf.RoundToInt(value * Mathf.Pow(stackCount, taper));
    }

    public static float TaperFloat(float value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return value * Mathf.Pow(stackCount, taper);
    }

    private enum CardinalDir { Right, Left, Up, Down }

    private CardinalDir CardinalDirFromVector(Vector2 vector) {
        float dot = Vector2.Dot(Vector2.right, vector.normalized);
        if (Mathf.Abs(dot) >= 0.2f) {
            return vector.x > 0 ? CardinalDir.Right : CardinalDir.Left;
        } 
        return vector.y > 0 ? CardinalDir.Up : CardinalDir.Down;
    }

}
