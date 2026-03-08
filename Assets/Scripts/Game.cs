using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;
using Assert = UnityEngine.Assertions.Assert;
using Debug = UnityEngine.Debug;
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public class Game : MonoBehaviour {

    public static Game inst;
    
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
    
    [NonSerialized] public List<Entity> entities = new();
    [NonSerialized] public Dictionary<GameObject, Entity> entityLookup = new();
    [NonSerialized] public List<Enemy> enemies = new();
    
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

    public static Action<Enemy> onEnemyDeath;
    public static Action<InventorySlot[]> onSoldItemsToTrader;
    public static Action<InventorySlot[]> onReturnedFromRaid;
    public static Action<string> customQuestEvent;
    
    private void Start() {
        inst = this;
        
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
        UpdateCrucibleInfoPanel();
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
        UpdateTraderTransactionState();
        UpdateCrucibleState();
    }

    private void OnMapSelectionEnter() {
        ShowMapSelectionUI();
    }

    private void OnMapSelectionExit() {
        CloseMapSelectionUI();
    }

    private void OnMapSelectionUpdate() {
        CheckForHotBarInteractions();
    }

    private void OnRaidStateEnter() {
        InitRaid();
        PlayAmbience();
    }

    private void OnRaidStateExit() {
        DeinitPlayer();
        ClosePlayerInventory();
        CloseLootInventory();
        HideItemDescPopup();
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
        player.health = gameplayConfig.postDeathStartingHealth;
        DeinitRaid();
    }
    
    // *****************************
    // Tutorial 
    // *****************************

    [Serializable]
    public class TutorialState {
        public enum State { NotStarted, PlayFirstGame, VisitQuestsTab, VisitForgeTab, CraftFirstDemonEye, EquipFirstDemonEye,  Finished }
        public State curState = State.NotStarted;
    }

    private TutorialState tutorialState;
    private bool inTutorial;

    private void InitTutorialState() { 
        tutorialState = LoadFromFileOrCreateNew<TutorialState>(tutorialSavePath);
        inTutorial = tutorialState.curState != TutorialState.State.Finished;
    }
    
    private void SaveTutorialState() {
        SaveToFile(tutorialSavePath, tutorialState);
    }
    
    // *****************************
    // Entity
    // *****************************
    
    public interface IEntityPooler { 
        public void ReleaseEntity(Entity entity);
    }

    public class EntityPool<T> : IEntityPooler where T : Entity {
        public Action<T> OnSpawnCallback;
        public GameObject prefab;
        public List<T> standbyList = new();

        public void ReleaseEntity(Entity entity) {
            Assert.IsFalse(standbyList.Contains((T)entity), "Already released this entity!");
            entity.gameObject.SetActive(false);
            standbyList.Add((T)entity);
        }
    }
    
    private EntityPool<T> CreateEntityPool<T>(GameObject gameObj, int initialSize, Action<T> onSpawnCallback) where T : Entity, new() {
        EntityPool<T> pool = new() {
            OnSpawnCallback = onSpawnCallback,
            prefab = gameObj,
        };
        
        for (int i = 0; i < initialSize; i++) {
            GameObject obj = Instantiate(gameObj, Vector3.zero, Quaternion.identity, transform);
            T entity = InitializeEntity<T>(obj, EntityLifetime.Level);
            entity.entityPool = pool;
            entity.gameObject.SetActive(false);
            pool.standbyList.Add(entity);
        }
        
        return pool;
    }

    public enum EntityLifetime { Level, Global }

    public class Entity {
        public Transform trans;
        public Collider2D collider;
        public Rigidbody2D rigidbody;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public TextMeshProUGUI textMesh;
        public EntityLifetime lifetime;
        
        public readonly MaterialPropertyBlock matPropertyBlock = new();
        public IEntityPooler entityPool;
        public int health;
        public int obstacleCellRadius;
        public Vector2 obstaclePosition;

        public int delayedDamage;
        public bool delayedDamageIsCrit;
        
        public PoisonedEffect poisonedEffect;
        public BounceEffect bounceEffect;
        public ParentToEntity parentEffect;
        public ShakeEffect shakeEffect;

        public enum EffectsIndicies { HitFlash, Poisoned, Bounce, Parent, Shake }
        public readonly Tween[] tweenEffects = new Tween[5];
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }

        public Vector3 Center => collider.bounds.center;
        public GameObject gameObject => trans.gameObject;
        public Tween GetEffect(EffectsIndicies effectIndex) => tweenEffects[(int)effectIndex];
        public void SetEffect(EffectsIndicies effectIndex, Tween tween) => tweenEffects[(int)effectIndex] = tween;
    }

    private bool EntityIsValid(Entity entity) {
        return entity.trans && inst.entityLookup.ContainsKey(entity.gameObject);
    }

    private Entity SpawnItemAsEntity(Item item, int count, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) {
        Entity entity = SpawnEntity(itemDropPool, position, rotation, parent, lifetime);
        entity.gameObject.GetComponent<ItemDrop>().Init(item, count);
        return entity;
    }
    
    private T SpawnEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) where T : Entity, new() {
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        T entity = InitializeEntity<T>(obj, lifetime);
        ResetEntity(entity);
        RegisterEntity(entity);
        return entity;
    }
    
    private T SpawnEntity<T>(EntityPool<T> pool, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) where T : Entity, new() {
        T entity;
        if (pool.standbyList.Count > 0) {
            entity = pool.standbyList.PopLast();
            entity.lifetime = lifetime;
            entity.trans.SetPositionAndRotation(position, rotation);
            entity.trans.SetParent(parent, true);
            ResetEntity(entity);
            RegisterEntity(entity);
        }
        else { 
            entity = SpawnEntity<T>(pool.prefab, position, rotation, parent, lifetime);
            entity.entityPool = pool;
        }
        
        entity.gameObject.SetActive(true);
        pool.OnSpawnCallback?.Invoke(entity);
        return entity;
    }
    
    private T InitializeEntity<T>(GameObject objInstance, EntityLifetime lifetime) where T : Entity, new() {
        T newEntity = new() {
            trans = objInstance.transform,
            collider = objInstance.TryGetComponent(out Collider2D col) ? col : null,
            rigidbody = objInstance.TryGetComponent(out Rigidbody2D rbody) ? rbody : null,
            spriteRenderer = objInstance.TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer : null,
            animator = objInstance.TryGetComponent(out Animator anim) ? anim : null,
            textMesh = objInstance.TryGetComponent(out TextMeshProUGUI text) ? text : null,
            lifetime = lifetime,
        };
        return newEntity;
    }

    private void ResetEntity<T>(T entity) where T : Entity {
        entity.health = 100;
        entity.trans.localScale = Vector3.one;
        if (entity.animator) {
            entity.animator.enabled = true;
            entity.animator.Rebind();
        }
        if (entity.gameObject.activeInHierarchy) {
            entity.animator?.Update(0);
        }
    }

    private void RegisterEntity<T>(T entity) where T : Entity {
        entities.Add(entity);
        entityLookup.Add(entity.gameObject, entity);
    }
    
    private void DestroyEntity(Entity rootEntity) {
        using var autoRelease = ListPool<Entity>.Get(out List<Entity> entityHierarchy); 
        GetEntityHierarchy(rootEntity.trans, entityHierarchy);

        foreach (Entity entity in entityHierarchy) {
            for (int i = 0; i < entity.tweenEffects.Length; i++) {
                entity.tweenEffects[i].Complete();
            }
            
            bool enemyWasInLookup = entityLookup.Remove(entity.gameObject, out _);
            if (enemyWasInLookup) {
                entities.Remove(entity);
                DestroyOrReleaseEntitysGameObject(entity);
            }
        }
    }

    private void GetEntityHierarchy(Transform root, List<Entity> entityHierarchy) {
        if (entityLookup.TryGetValue(root.gameObject, out Entity associatedEntity)) {
            entityHierarchy.Add(associatedEntity);
        }
        foreach (Transform trans in root) {
            GetEntityHierarchy(trans, entityHierarchy);
        }
    }

    private void DestroyOrReleaseEntitysGameObject(Entity entity) {
        if (entity.entityPool == null) {
            Destroy(entity.gameObject);
            return;
        }
        entity.gameObject.transform.SetParent(transform, true);
        entity.entityPool.ReleaseEntity(entity);
    }

    private void DestroyEntity(Entity entity, float delay) {
        Delay(entity, delay, static entity => inst.DestroyEntity(entity));
    }

    private static int damageFlashTintPropertyId = Shader.PropertyToID("_DamageFlashTint");
    
    private void AddFlashHitEffect(Entity entity) {
        float duration = hitFlashCurve.keys[^1].time;
        
        entity.GetEffect(EffectsIndicies.HitFlash).Stop();
        
        Tween tween = Tween.Custom(entity, 0f, 1f, duration, ease: Ease.Linear, onValueChange: static (entity, val) => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, inst.hitFlashCurve.Evaluate(val));
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        })
        .OnComplete(entity, static entity => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 0);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        });

        entity.SetEffect(EffectsIndicies.HitFlash, tween);
    }

    private static int poisonedPropertyId = Shader.PropertyToID("_Poisoned");
    
    public struct PoisonedEffect {
        public Entity poisonDebuffEntity;
    }
    
    public void AddPoisonedEffect(Entity entity, float duration) {
        if (!entity.GetEffect(EffectsIndicies.Poisoned).isAlive) {
            Entity poisonDebuff = SpawnEntity(poisonDebuffPool, OffsetY(entity.position, -0.01f), Quaternion.identity, entity.trans);
            entity.poisonedEffect = new() {
                poisonDebuffEntity = poisonDebuff,
            };
        }
        
        entity.GetEffect(EffectsIndicies.Poisoned).Stop();
        
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(poisonedPropertyId, 1);
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);

        Tween tween = Delay(entity, duration, static entity => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(poisonedPropertyId, 0);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
            inst.DestroyEntity(entity.poisonedEffect.poisonDebuffEntity);
        });
        
        entity.SetEffect(EffectsIndicies.Poisoned, tween);
    }
    
    public struct BounceEffect {
        public Vector2 targetPos;
        public Vector2 initialPos;
    }

    private void AddBounceEffect(Entity entity, Vector3 pos, float duration) {
        entity.bounceEffect = new() {
            targetPos = pos,
            initialPos = entity.position,
        };
        
        Tween.Custom(entity, 0f, 1f, duration, ease: Ease.Linear, onValueChange: static (entity, val) => {
            float yPos = inst.bounceCurve.Evaluate(val);
            Vector2 newPos = Vector2.Lerp(entity.bounceEffect.initialPos, entity.bounceEffect.targetPos, val);
            entity.position = new(newPos.x, newPos.y + yPos, entity.position.z);
        });
    }

    public struct ParentToEntity {
        public Entity parentEntity;
        public Vector2 localOffset;
    }

    private void AddParentEffect(Entity entity, Entity parent, float duration) {
        entity.parentEffect = new() {
            parentEntity = parent,
            localOffset = parent.position - entity.position,
        };

        Tween tween = Tween.Custom(entity, 0f, 0f, duration, static (entity, _) => {
            if (!inst.EntityIsValid(entity.parentEffect.parentEntity)) { 
                entity.GetEffect(EffectsIndicies.Parent).Stop();
                return;
            }
            entity.position = entity.parentEffect.parentEntity.position + entity.parentEffect.localOffset.ToVector3();
        });
        
        entity.SetEffect(EffectsIndicies.Parent, tween);
    }

    public struct ShakeEffect {
        public float jitter;
        public float magnitude;
        public AnimationCurve animCurve;
        public Vector2 randomSeed;
        public Vector3 entityStartPos;
        public float noisePos;
    }

    private void AddShakeEffect(Entity entity, float jitter, float magnitude, float time, AnimationCurve animCurve) {
        entity.shakeEffect = new() {
            jitter = jitter,
            magnitude = magnitude,
            animCurve = animCurve,
            randomSeed = new(Random.Range(int.MinValue, int.MaxValue), Random.Range(int.MinValue, int.MaxValue)),
            noisePos = 0f,
            entityStartPos = entity.position,
        }; 
        
        entity.GetEffect(EffectsIndicies.Shake).Stop();
        
        Tween tween = Tween.Custom(entity, 0f, 1f, time, ease: Ease.Linear, onValueChange: static (entity, val) => {
            ShakeEffect shakeEffect = entity.shakeEffect;
            float magnitude = shakeEffect.animCurve.Evaluate(val) * shakeEffect.magnitude;
            shakeEffect.noisePos = (shakeEffect.noisePos + shakeEffect.jitter * Time.deltaTime) % 1f;
            float x = (Mathf.PerlinNoise(shakeEffect.randomSeed.x, shakeEffect.noisePos) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(shakeEffect.randomSeed.y, shakeEffect.noisePos + 100f) - 0.5f) * 2f;
            Vector3 targetVector = new Vector3(x, y, entity.position.z) * magnitude;
            entity.position = shakeEffect.entityStartPos + targetVector;
            entity.shakeEffect = shakeEffect;
        });
        
        entity.SetEffect(EffectsIndicies.Shake, tween);
    }
    
    // *****************************
    // Enemy 
    // *****************************
    
    private int walkSideAnim = Animator.StringToHash("WalkSide");
    private int walkUpAnim = Animator.StringToHash("WalkUp");
    private int walkDownAnim = Animator.StringToHash("WalkDown");

    private int attackSideAnim = Animator.StringToHash("AttackSide");
    private int attackUpAnim = Animator.StringToHash("AttackUp");
    private int attackDownAnim = Animator.StringToHash("AttackDown");
    
    private Limiter enemyReteleportLimitter;
    private int enemyReteleportCount;
    
    public class Enemy : Entity {
        public float teleportTime;
        public float flowFieldAcc;
        public float prevAverageDistFromPlayer;
        public float curRunningSumDistFromPlayer;
        public int curRunningSumFrameCount;
        public float averageDistFromPlayerTime;
        public bool gettingFurtherFromPlayer;
        public Collider2D enemySpacerCollider;
        public EnemyData data;
        public Timer applyDamageTimer;
        public BleedModifierItem.InstanceData? bleed;
        public PoisonModifierItem.InstanceData? poison;
        public SlowModifierItem.InstanceData? slow;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public Limiter changeDirLimiter;
    }
    
    private void UpdateEnemies() {
        bool timeHasPassed = enemyReteleportLimitter.TimeHasPassed(1f);
        if (timeHasPassed) {
            enemyReteleportCount = 0;
        }
        int maxTeleportCount = Mathf.Max(Mathf.RoundToInt(enemies.Count * 0.1f), 8);
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            enemy.applyDamageTimer.Tick();
            
            float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);

            enemy.curRunningSumFrameCount++;
            enemy.curRunningSumDistFromPlayer += distFromPlayer;
            
            enemy.averageDistFromPlayerTime += Time.deltaTime;
            if (enemy.averageDistFromPlayerTime > 2.5f) {
                enemy.averageDistFromPlayerTime = 0f;

                if (enemy.prevAverageDistFromPlayer != 0f) {
                    float curAverage = enemy.curRunningSumDistFromPlayer / enemy.curRunningSumFrameCount;
                    enemy.gettingFurtherFromPlayer = curAverage - enemy.prevAverageDistFromPlayer > 0.1f;
                }
                
                enemy.prevAverageDistFromPlayer = enemy.curRunningSumDistFromPlayer / enemy.curRunningSumFrameCount;
                enemy.curRunningSumDistFromPlayer = 0f;
                enemy.curRunningSumFrameCount = 0;
            }
            
            bool canReteleport = timeHasPassed && enemyReteleportCount < maxTeleportCount;

            if (canReteleport && enemy.gettingFurtherFromPlayer && distFromPlayer > 1.2f) {
                Vector2Int repositionCellRange = spawnManager.CurSpawnPhase.repositionCellRange;
                Vector2 randomSpawnGridPos = loadedMapInst.grid.GetSpawnPosition(player.position, repositionCellRange.x, repositionCellRange.y);
                if (Vector2.Distance(randomSpawnGridPos, player.position) < distFromPlayer) {
                    TeleportEnemy(enemy, randomSpawnGridPos, TeleportType.Reposition);
                    enemyReteleportCount++;
                }
                continue;
            }
            
            bool playingAttackAnimation = EnemyPlayingAttackAnimation(enemy);
            Vector2 dirToPlayer = (player.position - enemy.position).normalized;

            if (!playingAttackAnimation) {
                if (enemy.animator.Playing(walkSideAnim)) {
                    enemy.graphicalDir = enemy.spriteRenderer.flipX ? Vector2.left : Vector2.right;
                }
                else if (enemy.animator.Playing(walkUpAnim)) {
                    enemy.graphicalDir = Vector2.up;
                }
                else {
                    enemy.graphicalDir = Vector2.down;
                }
            }

            bool canStartAttack = !playingAttackAnimation;
            bool withinAttackDist = distFromPlayer < enemy.data.attackDistance;
            bool facingPlayer = Vector2.Dot(enemy.graphicalDir, dirToPlayer) >= 0.5f;
            
            if (canStartAttack && withinAttackDist && facingPlayer) {
                switch (CardinalDirFromVector(enemy.graphicalDir)) {
                    case CardinalDir.Right:
                    case CardinalDir.Left:
                        enemy.animator.Play(attackSideAnim);
                        break;
                    case CardinalDir.Up:
                        enemy.animator.Play(attackUpAnim);
                        break;
                    case CardinalDir.Down:
                        enemy.animator.Play(attackDownAnim);
                        break;
                }
                
                if (enemy.data.type == EnemyData.EnemyType.Boomon) {
                    Delay(enemy, enemy.data.attackDamageDelay, static (enemy) => {
                        const int projectileCount = 3;
                        const float angleDeltaPerDrop = 360f /  projectileCount;
                        const float randomRangePerDrop = angleDeltaPerDrop * 0.25f;

                        for (int i = 0; i <  projectileCount; i++) {
                            float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                            Vector3 velocity = inst.RotationVector(randomAngle) * 0.62f;
                            inst.SpawnProjectile(inst.OffsetY(enemy.position, 0.2f), velocity, inst.gooProjectilePool, 
                                flatDamage: enemy.data.damage, lifetime: 2f, layermask: Masks.PlayerHurtMask);
                        }
                        
                        enemy.health = 0;
                    });
                }
                else {
                    Delay(enemy, enemy.data.attackDamageDelay, static (enemy) => {
                        Vector2 attackCheckPos = enemy.position;
                        switch (inst.CardinalDirFromVector(enemy.graphicalDir)) {
                            case CardinalDir.Right:
                                attackCheckPos += enemy.data.sideAttackOffset;
                                break;
                            case CardinalDir.Left:
                                attackCheckPos += new Vector2(-enemy.data.sideAttackOffset.x, enemy.data.sideAttackOffset.y);
                                break;
                            case CardinalDir.Up:
                                attackCheckPos += enemy.data.upAttackOffset;
                                break;
                            case CardinalDir.Down:
                                attackCheckPos += enemy.data.donwAttackOffset;
                                break;
                        }

                        Collider2D col = Physics2D.OverlapCircle(attackCheckPos, enemy.data.attackRadius, Masks.PlayerHurtMask);
                        
                        if (!col) { 
                            col = Physics2D.OverlapCircle(enemy.Center, enemy.data.attackRadius, Masks.PlayerHurtMask);
                        }

                        if (enemy.data.type == EnemyData.EnemyType.Doughmon) {
                            Entity smokeSlam = inst.SpawnEntity<Entity>(inst.slamSmokePrefab, attackCheckPos, Quaternion.identity);
                            inst.DestroyEntity(smokeSlam, inst.CurrentClipLength(smokeSlam.animator));
                        }

                        if (col) {
                            inst.DamagePlayer(enemy.data.damage, enemy.data.changeToCauseBleed);
                        }
                    });
                }
            }
            
            if (enemy.bleed.TryGetValue(out var bleed)) {
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    int bleedDamage = bleed.bleedDamage;
                    enemy.health -= bleedDamage;
                    bleed.lastBleedTime = Time.time;
                    enemy.bleed = bleed;
                    Entity bloodDrop = SpawnEntity(bloodDropPool, OffsetY(enemy.position, 0.015f), Quaternion.identity);
                    AddParentEffect(bloodDrop, enemy, 0.4f);
                    DestroyEntity(bloodDrop, 0.8f);
                    SpawnDamageNumber(EnemyDamageNumberSpawnPos(enemy), bleedDamage, DamageColor.Blood);
                }
            }

            if (enemy.health <= 0) {
                Enemy deadEnemy = enemies[i];
                const float deathDelay = 0.12f;
                Delay(deadEnemy, deathDelay, static (deadEnemy) => {
                    if (RollProbability(deadEnemy.data.chanceToDropItem)) {
                        Item dropItem = inst.GetItemFromEnemyDropPool(deadEnemy.data);
                        if (dropItem) {
                            inst.SpawnItemAsEntity(dropItem, 1, deadEnemy.position, Quaternion.identity);
                        }
                    }

                    inst.player.soulCurrency += deadEnemy.data.soulWorthPerKill;
                    onEnemyDeath?.Invoke(deadEnemy);
                    
                    Entity bloodSplatterEntity = inst.SpawnEntity(inst.bloodSplatterPool, deadEnemy.position, Quaternion.identity);
                    inst.DestroyEntity(bloodSplatterEntity, inst.CurrentClipLength(bloodSplatterEntity.animator));
                    
                    inst.PlayAudioClip(inst.bloodBurstClip, deadEnemy.position);

                    inst.DestroyEntity(deadEnemy);
                });
                enemies.RemoveAt(i);
            }
        }
    }
    
    private void FixedUpdateEnemies() {
        if (!InRaid) return;
        
        foreach (Enemy enemy in enemies) {
            float speed = enemy.data.speed;
            
            float totalSlowPercentage = 0f;
            if (enemy.slow.TryGetValue(out var slow)) {
                totalSlowPercentage += slow.speedReductionPercent;
                enemy.enemySpacerCollider.excludeLayers = Masks.EnemySpacerMask;
                if (Time.time > slow.activationTime + slow.duration) {
                    enemy.enemySpacerCollider.excludeLayers = 0;
                    enemy.slow = null;
                }
            }
            speed = Mathf.Clamp(speed * Mathf.Clamp01(1f - totalSlowPercentage), 0.05f, enemy.data.speed);

            bool enemyIsAttacking = EnemyPlayingAttackAnimation(enemy);
            if (enemyIsAttacking) {
                speed = 0f;
            }

            Vector3 targetDir = Vector3.zero;
            if (enemy.data.usesFlowField) {
                targetDir = loadedMapInst.grid.GetFlowFieldDirection(enemy.position);
            }
            if (targetDir == Vector3.zero) {
                targetDir = (player.position - enemy.position).normalized;
            }
            
            enemy.moveDir = Vector3.Lerp(enemy.moveDir, targetDir, enemy.flowFieldAcc * Time.fixedDeltaTime);
            enemy.rigidbody.linearVelocity = enemy.moveDir * speed;

            if (!enemyIsAttacking && enemy.changeDirLimiter.TimeHasPassed(0.15f)) {
                switch (CardinalDirFromVector(enemy.moveDir)) {
                    case CardinalDir.Right:
                        enemy.animator.PlayIfNotAlready(walkSideAnim);
                        enemy.spriteRenderer.flipX = false;
                        break;
                    case CardinalDir.Left:
                        enemy.animator.PlayIfNotAlready(walkSideAnim);
                        enemy.spriteRenderer.flipX = true;
                        break;
                    case CardinalDir.Up:
                        enemy.animator.PlayIfNotAlready(walkUpAnim);
                        enemy.spriteRenderer.flipX = false;
                        break;
                    case CardinalDir.Down:
                        enemy.animator.PlayIfNotAlready(walkDownAnim);
                        enemy.spriteRenderer.flipX = false;
                        break;
                }
            }
        }
    }

    private bool EnemyPlayingAttackAnimation(Enemy enemy) {
        var stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
        int animStateHash = stateInfo.shortNameHash;
        bool playingAttackAnim = animStateHash == attackSideAnim || animStateHash == attackUpAnim || animStateHash == attackDownAnim;
        bool clipIsNotFinished = stateInfo.normalizedTime <= 1f;
        return playingAttackAnim && clipIsNotFinished;
    }

    private enum TeleportType { Spawn, Reposition }

    private void TeleportEnemy(Enemy enemy, Vector3 position, TeleportType teleportType) {
        if (teleportType == TeleportType.Reposition) {
            Entity outTeleportFxEntity = SpawnEntity(teleportOutPool, enemy.position, Quaternion.identity);
            DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
            PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        }
        
        enemy.position = position;
        enemy.gameObject.SetActive(false);
        
        Entity inTeleportFxEntity = SpawnEntity(teleportInPool, enemy.position, Quaternion.identity);
        float spawnAnimDuration = CurrentClipLength(inTeleportFxEntity.animator);
        DestroyEntity(inTeleportFxEntity, spawnAnimDuration);
        
        PlayAudioClip(teleportInClip, inTeleportFxEntity.position);

        float spawnDelay = spawnAnimDuration * 0.7f;
        
        Delay(enemy, spawnDelay, static (enemy) => {
            enemy.gameObject.SetActive(true);
            enemy.teleportTime = 0f;
        });
    }
    
    public class EnemySpawnManager {
        public float timeInPhase;
        public float totalTimeLeft;
        public float timeUntilFinalWave;
        public int curPhaseIndex;
        public RaidSpawnPattern spawnPattern;
        public bool isFinishedSpawning;
        
        public const int prefixedSumResolution = 500;
        public float[] prefixedSums = new float[prefixedSumResolution];

        public List<(float time, EnemyData enemy)> spawnEvents = new();
        public int spawnTimeIndex;
        
        public RaidSpawnPattern.SpawnPhase CurSpawnPhase => spawnPattern?.spawnPhases[curPhaseIndex];
    }

    [NonSerialized] private EnemySpawnManager spawnManager = new();

    private void InitSpawnManager(RaidSpawnPattern pattern) {
        spawnManager.spawnEvents.Clear();
        spawnManager.isFinishedSpawning = false;
        spawnManager.spawnPattern = pattern;
        spawnManager.curPhaseIndex = -1;
        spawnManager.timeInPhase = 0f;
        spawnManager.totalTimeLeft = pattern.timeBeforeFirstPhase;
        foreach (RaidSpawnPattern.SpawnPhase phase in spawnManager.spawnPattern.spawnPhases) {
            spawnManager.totalTimeLeft += phase.phaseDuration;
        }
        spawnManager.timeUntilFinalWave = spawnManager.totalTimeLeft - spawnManager.spawnPattern.spawnPhases[^1].phaseDuration;
    }
    
    private Limiter spawnLimiterForEnemyBatching;
    
    private void UpdateSpawnManager() {
        EnemySpawnManager sm = spawnManager;
        
        if (sm.isFinishedSpawning) return;
        
        sm.timeInPhase += Time.deltaTime;
        sm.totalTimeLeft -= Time.deltaTime;
        sm.timeUntilFinalWave -= Time.deltaTime;
        
        float waveDuration = sm.curPhaseIndex == -1 ? 
            sm.spawnPattern.timeBeforeFirstPhase : 
            sm.spawnPattern.spawnPhases[sm.curPhaseIndex].phaseDuration;

        bool startNextWave = sm.timeInPhase >= waveDuration;
        if (startNextWave) {
            sm.curPhaseIndex++;

            RaidSpawnPattern.SpawnPhase curPhase = sm.spawnPattern.spawnPhases[sm.curPhaseIndex];

            #if UNITY_EDITOR
            foreach (RaidSpawnPattern.EnemyBatch batch in curPhase.enemyBatches) {
                if (batch.enemyCount >= EnemySpawnManager.prefixedSumResolution) {
                    Debug.LogError($"Wave cannot have more enemies than {nameof(EnemySpawnManager.prefixedSumResolution)}");
                }
            }
            #endif
            
            sm.timeInPhase = 0f;
            sm.spawnTimeIndex = 0;

            float totalWeight = 0f;
            for (int i = 0; i < EnemySpawnManager.prefixedSumResolution; i++) {
                float sliceIndex = i / (float)(EnemySpawnManager.prefixedSumResolution - 1);
                float weight = Mathf.Clamp01(curPhase.spawnRateCurve.Evaluate(sliceIndex));
                totalWeight += weight;
                sm.prefixedSums[i] = totalWeight;
            }

            // Build spawntimes for this next wave
            {
                sm.spawnEvents.Clear();
                
                foreach (RaidSpawnPattern.EnemyBatch waveUnit in curPhase.enemyBatches) {
                    int enemySpawnCount = waveUnit.enemyCount;
                    for (int i = 0; i < enemySpawnCount; i++) {
                        float targetWeight = (i / (float)(enemySpawnCount - 1)) * totalWeight;

                        // Find the corresponding time using linear search
                        int weightIndex = 0;
                        while (weightIndex < EnemySpawnManager.prefixedSumResolution && sm.prefixedSums[weightIndex] < targetWeight) {
                            weightIndex++;
                        }

                        float normalizedTime = weightIndex / (float)(EnemySpawnManager.prefixedSumResolution - 1);
                        sm.spawnEvents.Add((normalizedTime * curPhase.spawnDuration, waveUnit.enemyData));
                    }
                }
                
                // Due to the way we add elements we need to sort by time so its chronologically ordered 
                sm.spawnEvents.Sort((x, y) => x.time.CompareTo(y.time));
            }
            
            spawnLimiterForEnemyBatching.MakeCurrent();
        }

        if (sm.spawnEvents.Count <= 0) return;

        if (!spawnLimiterForEnemyBatching.TimeHasPassed(1f)) return;
        
        while (sm.spawnEvents.IndexInRange(sm.spawnTimeIndex) && sm.spawnEvents[sm.spawnTimeIndex].time <= sm.timeInPhase) {
            Vector2Int spawnCellRange = spawnManager.CurSpawnPhase.spawnCellRange;
            Vector2 randomSpawnPos = loadedMapInst.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y);

            EnemyData enemyToSpawn = sm.spawnEvents[sm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity);
            enemy.health = enemyToSpawn.health;
            enemy.data = enemyToSpawn;
            enemy.animator.runtimeAnimatorController = enemyToSpawn.animatorOverride;
            enemy.enemySpacerCollider = enemy.trans.GetChild(0).GetComponent<Collider2D>();
            enemy.enemySpacerCollider.excludeLayers = enemyToSpawn.excludeCollisionLayers;
            enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
            enemies.Add(enemy);
            
            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);

            sm.spawnTimeIndex++;
        }

        bool outOfSpawnsInPhase = !sm.spawnEvents.IndexInRange(sm.spawnTimeIndex);
        bool onLastPhase = sm.spawnPattern.spawnPhases.Count - 1 == sm.curPhaseIndex;

        if (outOfSpawnsInPhase && onLastPhase) {
            sm.isFinishedSpawning = true;
        }
    }
    
    // *******************************
    // Raid 
    // *******************************
    
    private enum RaidState { None, InitialWaves, FinalWave, FinalWaveWithExit, PostFinalWave }
    private RaidState curRaidState;
    private bool raidStateSwitchedThisFrame;
    private Sequence raidEnterSequence;
    
    private void InitRaid() {
        curRaidState = RaidState.None;
        demonEyeRaidStats = new();
        callingExitPortalSequence.Stop();
        closeExitPortalSequence.Stop();
        canTakeExitPortal = false;
        timeSpentSummoningPortal = 0f;
        
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
        
        InitSpawnManager(loadedMapData.waves);
        SpawnResources(loadedMapInst.resourceParent);
        InitEarlyExitPortal(loadedMapInst.exitPortalsParent, spawnManager.timeUntilFinalWave + loadedMapData.waves.timeBeforePortalSpawns);
        
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
            curRaidState = activeExitPortal ? RaidState.FinalWaveWithExit : RaidState.FinalWave;
        }
        else {
            curRaidState = RaidState.PostFinalWave;
        }

        raidStateSwitchedThisFrame = prevState != curRaidState;
        
        if (raidStateSwitchedThisFrame && curRaidState == RaidState.FinalWave) {
            // DespawnEarlyExitPortal();
            PlayAudioClip(finalWaveStingerClip, player.position);
        }

        if (raidStateSwitchedThisFrame && curRaidState == RaidState.PostFinalWave) {
            Tween.Delay(0.25f, static () => {
                inst.AnimateLargeRaidText(ColorText("Map Cleared!", inst.styles.increaseDescColor), 1.8f);
                // inst.SpawnFinalExitPortal();
            });
        }
    }
    
    private void DeinitRaid() {
        loadedMapInst.grid.Deinit();
        
        DestroyLevelEntities();
        UnloadCurrentMapAsync();
    }
    
    private void OnSaveWhenRaidIsOver() {
        SaveInventory(playerInventory);
        SavePlayerData();
        SaveActiveQuestProgresses();
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
            }
            else if (curRaidState == RaidState.FinalWaveWithExit) {
                exitPortalCountdownParent.SetActive(false);
                exitPortalActiveNotifier.SetActive(true);
                Tween.Scale(exitPortalActiveNotifier.transform, 0f, 1f, 0.5f, Ease.OutBack);
                AnimateSmallRaidText(ColorText("Exit Portal is Open", styles.increaseDescColor));
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
                inst.largeRaidText.characterSpacing = val;
            }));
            sequence.ChainDelay(0.35f);
            sequence.ChainCallback(static () => inst.largeRaidTextTypewriter.StartDisappearingText());
        }
    }

    private void AnimateSmallRaidText(string text) {
        smallRaidText.gameObject.SetActive(true);
        smallRaidTextTypewriter.ShowText($"{{incr}}{{fade}}{{smallwave}}{{#fade}}{{#smallwave}}{text}");
        
        smallRaidTextTypewriter.onTextShowed.AddListener(OnTypewriterFinish);
        
        void OnTypewriterFinish() {
            Sequence sequence = Sequence.Create();
            sequence.ChainDelay(0.8f);
            sequence.ChainCallback(static () => inst.smallRaidTextTypewriter.StartDisappearingText());
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
        
        // There could be a rare case where an exit portal doesn't spawn, which is handled
        if (activeExitPortal) {
            sequence.ChainDelay(0.05f);
            sequence.ChainCallback(static () => {
                inst.PlayAudioClip(inst.portalDespawnClip, inst.activeExitPortal.position);
            });
            sequence.Chain(Tween.Scale(activeExitPortal, Vector3.zero, 0.25f, Ease.InOutBounce));
        }
        
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
        sequence.Chain(Tween.Scale(activeExitPortal.transform, Vector3.zero, 0.25f, Ease.InOutBounce));
        
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
    // Inventory
    // *******************************
    
    [Serializable]
    public class InventoryItem {
        public int itemOrInstanceUuid;
        public List<int> nestedUuids;
        public int count = 1;

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public bool traderOwned;
        [NonSerialized] public int traderSlotIndex;
        [NonSerialized] public bool foundInLastRaid;
        [NonSerialized] public Item _itemRef; // Used for items created at runtime, like demon eyes

        public Item ItemRef => _itemRef ? _itemRef : resourceLookup[itemOrInstanceUuid] as Item;
        public bool IsFullStack => count == ItemRef.MaxStackCount;

        public InventoryItem(Item item = null, int count = 1) {
            if (item == null) return;
            this.itemOrInstanceUuid = item.uuid;
            this.count = count;
        }
        
        public InventoryItem Clone() {
            InventoryItem clonedItem = new() {
                itemOrInstanceUuid = itemOrInstanceUuid,
                count = count,
                notDiscovered = notDiscovered,
                traderOwned = traderOwned,
                traderSlotIndex = traderSlotIndex,
                _itemRef = ItemRef,
            };

            if (nestedUuids != null) {
                foreach (int modifierUuid in nestedUuids) {
                    clonedItem.nestedUuids ??= new();     
                    clonedItem.nestedUuids.Add(modifierUuid);
                }
            }

            return clonedItem;
        }

    }

    public class InventorySlot {
        public InventoryItem item;
        public InventorySlotUI ui;
    }

    public class Inventory {
        public InventorySlot[] slots;
        public RectTransform parent;
    }
    
    [NonSerialized] public Inventory playerInventory;
    [NonSerialized] public Inventory stashInventory;
    [NonSerialized] private Inventory crucibleInventory;
    [NonSerialized] private Inventory transactionInventory;
    [NonSerialized] private Inventory traderInventory;
    [NonSerialized] private Inventory lootInvetoryPtr;
    [NonSerialized] private InventorySlotUI[] lootInventorySlotUis;
    [NonSerialized] private List<Inventory> allInventories = new();
    
    private const int playerPocketSize = 10;
    private const int playerQuickUseSize = 4;
    private const int playerEquipmentSize = 3;
    private int NakedPlayerInventorySize => playerPocketSize + playerQuickUseSize + playerEquipmentSize;

    private const int traderInventoryColCount = 6;
    private const int traderInventoryRowCount = 5;

    private Timer discoverLootTimer;
    private int discoverLootIndex;

    private int stashValue;

    private bool InventoryIsOpen => playerPanel.gameObject.activeInHierarchy;
    private bool LootInventoryIsOpen => lootInventoryPanel.gameObject.activeInHierarchy;

    private bool OnCharacterTab => characterTabButton.image.sprite == tabSelectedSprite;
    private bool OnEyeForgeTab => eyeForgeTabButton.image.sprite == tabSelectedSprite;
    private bool OnTradingTab => traderTabButton.image.sprite == tabSelectedSprite;
    
    private void InitInventories() {
        const int maxBackpackSize = 30;
        SpawnUiSlots(playerPassiveParent, playerQuickUseSize);
        SpawnUiSlots(playerPocketParent, playerPocketSize + maxBackpackSize);
        playerInventory = CreateInventory(playerInventoryParent, NakedPlayerInventorySize);
        LoadInventory(playerInventory);

        InventorySlotUI[] quickUseSlots = playerPassiveParent.GetComponentsInChildren<InventorySlotUI>();
        foreach (InventorySlotUI slotUI in quickUseSlots) {
            slotUI.onlyAcceptedItemType = quickUseType;
        }
        
        int stashInventorySize = 40;
        SpawnUiSlots(stashInventoryParent, stashInventorySize);
        stashInventory = CreateInventory(stashInventoryParent, stashInventorySize);
        LoadInventory(stashInventory);
       
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(lootInventoryParent, cachedLootInventorySize); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventorySize);
        lootInventorySlotUis = lootInventoryParent.GetComponentsInChildren<InventorySlotUI>(true);

        const int traderInventorySize = traderInventoryRowCount * traderInventoryColCount;
        SpawnUiSlots(traderInventoryParent, traderInventorySize);
        traderInventory = CreateInventory(traderInventoryParent, traderInventorySize);
        LoadInventory(traderInventory);
        
        const int transactionInventorySize = 25;
        SpawnUiSlots(traderTransactionInventoryParent, transactionInventorySize);
        transactionInventory = CreateInventory(traderTransactionInventoryParent, transactionInventorySize);

        const int maxCrucibleInventorySize = 6;
        const int startingCrucibleInventorySize = 2;
        SpawnUiSlots(crucibleParent, maxCrucibleInventorySize, eyeForgeSlotPrefab);
        crucibleInventory = CreateInventory(crucibleParent, startingCrucibleInventorySize + player.crucibleLevel);
        SetupEyeCrucibleInventorySlots();
        LoadInventory(crucibleInventory);
    }
    
    private void SetupEyeCrucibleInventorySlots() {
        int inventoryLength = crucibleInventory.slots.Length;
        for (int i = 0; i < inventoryLength; i++) {
            InventorySlotUI slotUI = crucibleInventory.slots[i].ui;
            slotUI.disallowItemStacking = true;
            slotUI.onlyAcceptedItemType = i == 0 ? eyeType : eyeModifierType;

            if (i == 0) {
                slotUI.gameObject.transform.position = crucibleParent.position;
                continue;
            }
            
            float deg = 360f / (inventoryLength - 1) * (i - 1);
            Vector3 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 180f;
            slotUI.gameObject.transform.position = crucibleParent.position + spawnDir;
        }
    }
    
    private void SaveInventory(Inventory inventory) {
        cachedInventoryForSaving.Clear();
        foreach (InventorySlot slot in inventory.slots) {
            cachedInventoryForSaving.Add(slot.item); 
        }
        SaveToFile(GetInventorySavePath(inventory), cachedInventoryForSaving);
    }

    private void LoadInventory(Inventory inventory) {
        List<InventoryItem> items = LoadFromFile<List<InventoryItem>>(GetInventorySavePath(inventory));
        if (items == null) return;

        if (inventory == playerInventory && items.Count != inventory.slots.Length) {
            ChangeInventorySize(inventory, items.Count);
        }
        
        // Items can be null because we save all inventory slots, including empty ones
        foreach (InventoryItem item in items) {
            bool isDemonEye = item?.nestedUuids != null;
            if (isDemonEye) {
                BuildAndRegisterEye(item);
            }
        }
        
        CopyItemsToInventory(items, inventory);
    }
    
    private void CopyItemsToInventory(List<InventoryItem> items, Inventory toInventory) {
        if (items == null || toInventory == null) return;
        
        for (int i = 0; i < toInventory.slots.Length; i++) {
            if (!toInventory.slots.IndexInRange(i) || !items.IndexInRange(i)) break;
            toInventory.slots[i].item = items[i];
        }
    }
    
    private InventorySlot[] CreateLootInventoryInstance(List<InventoryItem> inventoryItems) {
        var slots = new InventorySlot[lootInvetoryPtr.slots.Length];
        
        for (int j = 0; j < lootInvetoryPtr.slots.Length; j++) {
            InventoryItem inventoryItem = null;
            if (inventoryItems.IndexInRange(j)) {
                inventoryItem = inventoryItems[j];
            }
            slots[j] = new() {
                item = inventoryItem,
                ui = lootInventorySlotUis[j],
            };
        }
        
        return slots;
    }

    private void UpdateInventory() {
        CheckForEquipmentChange();
        
        if (InRaid) {
            if (inventoryInputAction.WasPressedThisFrame()) {
                if (!InventoryIsOpen) {
                    OpenPlayerInventory();
                }
                else {
                    ClosePlayerInventory();
                }
                if (LootInventoryIsOpen) {
                    CloseLootInventory();
                }
            }
            
            if (!InventoryIsOpen && !LootInventoryIsOpen) {
                HideItemDescPopup();
                return;
            }
        }
        
        InventoryHoverInfo invHoverInfo = UpdateInventoryHover();
        CheckToMoveItem(invHoverInfo);

        if (UpdateInventoryDragAndDrop(invHoverInfo)) {
            HideItemDescPopup();
            HideUIElementPopup();
        }
        else {
            UpdateItemDescPopup(invHoverInfo);
            CheckToConsumeItem(invHoverInfo);
            UpdateUIElementPopup();
        }
    }

    private void UpdateItemDescPopup(InventoryHoverInfo invHoverInfo) {
        bool hoveringOverItem = TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem _);
        
        const float hoverTimeUntilTooltip = 0.32f;
        bool spentEnoughTimeHovering = invHoverInfo.timeSpentHovering >= hoverTimeUntilTooltip;
        
        if (hoveringOverItem && spentEnoughTimeHovering) {
            ShowItemDescPopup(invHoverInfo);
        }
        else {
            HideItemDescPopup();
        }
    }
    
    private void ShowItemDescPopup(InventoryHoverInfo info) {
        if (itemDescPopup.gameObject.activeInHierarchy) return;
        
        InventorySlot hoveredSlot = info.inventory.slots[info.slotIndex];
        
        itemDescPopup.gameObject.SetActive(true);
        itemDescPopup.Set(hoveredSlot.item);
        TweenPopUp(itemDescPopup.rectTransform);
        
        // Fit popup size to text elements
        FitPopupSize(itemDescPopup.rectTransform, itemDescPopup.tagsParent.rect, itemDescPopup.nameText.rectTransform.rect, itemDescPopup.descText.rectTransform.rect);

        // Set popup position
        Vector2 hoveredSlotCenter = hoveredSlot.ui.rectTransform.WorldRect().center;
        float halfPopupWidth = itemDescPopup.rectTransform.rect.width / 2f;
        Vector2 popupOffset = new(45 + halfPopupWidth, 40);
        if (hoveredSlotCenter.x < ScreenCenter.x) {
            itemDescPopup.transform.position = hoveredSlotCenter + popupOffset;
        }
        else {
            itemDescPopup.transform.position = hoveredSlotCenter + new Vector2(-popupOffset.x, popupOffset.y);
        }

        // Add mechanic desctiption if necessary
        if (hoveredSlot.item.ItemRef.type == eyeModifierType) {
            ModifierItem modifierItem = (ModifierItem)hoveredSlot.item.ItemRef;
            if (modifierItem.relativeMechanicDesc) {
                mechanicDescPopup.gameObject.SetActive(true);
                mechanicDescPopup.nameText.text = modifierItem.relativeMechanicDesc.displayName;
                mechanicDescPopup.descText.text = modifierItem.relativeMechanicDesc.description;
                mechanicDescPopup.transform.position = itemDescPopup.rectTransform.WorldRect().min;
                
                mechanicDescPopup.nameFitter.ForceRecalculate();
                mechanicDescPopup.descFitter.ForceRecalculate();
                FitPopupSize(mechanicDescPopup.rectTransform, mechanicDescPopup.nameText.rectTransform.rect, mechanicDescPopup.descText.rectTransform.rect);
                
                TweenPopUp(mechanicDescPopup.rectTransform);
            } 
        }
    }

    public string GetDemonEyeModDescription(ModifierItem modifierItem, int count) {
        string title = ColorText($"<size=108%>{modifierItem.displayName}</size> <size=87%>x{count}</size>", styles.headerTextColor);
        return $"<line-height=95%>{title}\n{modifierItem.GetDescription(count)}<line-height=140%>\n";
    }

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

    private void HideItemDescPopup() {
        mechanicDescPopup.gameObject.SetActive(false);
        
        itemDescPopup.nameText.text = string.Empty;
        itemDescPopup.descText.text = string.Empty;
        itemDescPopup.gameObject.SetActive(false);
        
        mechanicDescPopup.nameText.text = string.Empty;
        mechanicDescPopup.descText.text = string.Empty;
        mechanicDescPopup.gameObject.SetActive(false);
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
    
    private void CheckToMoveItem(InventoryHoverInfo invHoverInfo) {
        if (!moveStackInputAction.WasPressedThisFrame()) return;

        Inventory hoveredInventory = invHoverInfo.inventory;
        if (hoveredInventory == null) return;
        
        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem hoveredItem)) return;
        if (NotAllowedToMoveOrPickupItem(invHoverInfo)) return;
        if (ClickedOnEquipedBackpackWithItems(invHoverInfo.inventory, invHoverInfo.slotIndex)) return;

        MoveItemOption moveOption = MoveItemOption.FullStack;
        Inventory destinationInventory = null;
        
        if (InRaid) {
            if (hoveredInventory == playerInventory && LootInventoryIsOpen) {
                destinationInventory = lootInvetoryPtr;
            }
            else if (hoveredInventory == lootInvetoryPtr) {
                destinationInventory = playerInventory;
            }
        }
        else if (OnCharacterTab) {
            if (hoveredInventory == playerInventory) {
                destinationInventory = stashInventory;
            }
            else if (hoveredInventory == stashInventory) {
                destinationInventory = playerInventory;
            }
        }
        else if (OnEyeForgeTab) {
            if (hoveredInventory == stashInventory) {
                bool hoveredItemIsDemonEye = hoveredItem.ItemRef.type == demonEyeType;
                destinationInventory = hoveredItemIsDemonEye ? playerInventory : crucibleInventory;
            }
            else if (hoveredInventory == crucibleInventory) {
                destinationInventory = stashInventory;
            }
            else if (hoveredInventory == playerInventory) {
                destinationInventory = stashInventory;
            }
        }
        else if (OnTradingTab) {
            // if (transactionState == TransactionState.Buying) {
            //     if (hoveredInventory == traderInventory) {
            //         destinationInventory = transactionInventory;
            //         moveOption = MoveItemOption.Single;
            //     }
            //     else if (hoveredInventory == transactionInventory) {
            //         destinationInventory = traderInventory;
            //     }
            // }
            /*else*/ if (transactionState == TransactionState.Selling) {
                if (hoveredInventory == stashInventory) {
                    destinationInventory = transactionInventory;
                }
                else if (hoveredInventory == transactionInventory) {
                    destinationInventory = stashInventory;
                }
            }
            // else {
            //     if (hoveredInventory == traderInventory) {
            //         destinationInventory = transactionInventory;
            //         moveOption = MoveItemOption.Single;
            //     }
            //     else if (hoveredInventory == stashInventory) {
            //         destinationInventory = transactionInventory;
            //     }
            // }
        }

        if (destinationInventory == null) return;
        
        MoveItemBetweenInventories(hoveredInventory, destinationInventory, invHoverInfo.slotIndex, moveOption);
    }

    private void CheckToConsumeItem(InventoryHoverInfo invHoverInfo) {
        if (!useItemInputAction.WasPressedThisFrame()) return;
        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem hoveredItem)) return;
        if (hoveredItem.ItemRef.type != quickUseType) return;
        HavePlayerConsumeItem(invHoverInfo.inventory, invHoverInfo.slotIndex);
    }

    private void UpdatePlayerPanelUI() {
        if (!playerInventoryParent.gameObject.activeInHierarchy) return;
            
        playerPanelHealthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/{FullPlayerHealth}";

        int inventoryWeight = GetInventoryWeight(playerInventory);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanelWeightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
        
        // agilityStatValueText.text = (player.agilityLevel + 1).ToString("0.0");
        // healthStatValueText.text = (player.healthLevel + 1).ToString("0.0");
        // bleedResStatValueText.text = (player.bleedResLevel + 1).ToString("0.0");
        // strengthStatValueText.text = (player.strengthLevel + 1).ToString("0.0");
    }

    private bool TryGetItemFromHoverInfo(InventoryHoverInfo invHoverInfo, out InventoryItem hoveredItem) {
        hoveredItem = null;
        
        int hoveredSlot = invHoverInfo.slotIndex;
        Inventory hoveredInventory = invHoverInfo.inventory;
        
        if (hoveredInventory == null) return false;
        if (!hoveredInventory.slots.IndexInRange(hoveredSlot)) return false;
        if (hoveredInventory.slots[hoveredSlot].item == null) return false;
        if (hoveredInventory.slots[hoveredSlot].item.notDiscovered) return false;
        
        hoveredItem = hoveredInventory.slots[hoveredSlot].item;
        return true;
    }

    private bool IsHoveredItemGrayedOut(InventoryHoverInfo invHoverInfo) {
        Assert.IsTrue(TryGetItemFromHoverInfo(invHoverInfo, out _), 
            $"Method requires that you're hovering over an item, call {nameof(TryGetItemFromHoverInfo)} before to make sure.");
        
        int hoveredSlot = invHoverInfo.slotIndex;
        Inventory hoveredInventory = invHoverInfo.inventory;
        return hoveredInventory.slots[hoveredSlot].ui.itemUI.IsGrayedOut;
    }
    
    private bool NotAllowedToMoveOrPickupItem(InventoryHoverInfo info) {
        if (IsHoveredItemGrayedOut(info)) {
            return true;
        }
        // Because we wait to reduce the consumed inventory item count until the consuming tween has finished,
        // we can't drag it from its slot, why not just use decrement a reference to InventoryItem? Because when moving it 
        // between inventories the item reference may become stale.
        if (playerConsumingTween.isAlive && info.inventory == consumingInventory && info.slotIndex == consumingSlotIndex) {
            return true;
        }
        if (info.inventory == crucibleInventory && PlayingForgeAnimation) {
            return true;
        }
        return false;
    }
    
    private void SpawnUiSlots(RectTransform parent, int numSlots, GameObject slotPrefab = null) {
        for (int i = 0; i < numSlots; i++) {
            Instantiate(slotPrefab ? slotPrefab : inventorySlotPrefab, Vector3.zero, Quaternion.identity, parent);
        }
    }
    
    private Inventory CreateInventory(RectTransform uiParent, int slotCount) {
        Inventory inventory = new() {
            parent = uiParent,
            slots = new InventorySlot[slotCount],
        };
        inventory.slots.InitalizeWithDefault();
        LinkInventoryWithUiSlots(inventory);
        allInventories.Add(inventory);
        return inventory;
    }

    private void LinkInventoryWithUiSlots(Inventory inventory) {
        InventorySlotUI[] slotUis = inventory.parent.GetComponentsInChildren<InventorySlotUI>(true);
        foreach (InventorySlotUI slotUi in slotUis) {
            slotUi.gameObject.SetActive(false);
        }
        
        for (int i = 0; i < inventory.slots.Length; i++) {
            inventory.slots[i].ui = slotUis[i];
            inventory.slots[i].ui.gameObject.SetActive(true);
        }
    }

    private void ChangeInventorySize(Inventory inventory, int newSlotCount) {
        bool expanding = newSlotCount > inventory.slots.Length;
        
        InventorySlot[] oldSlots = inventory.slots;
        inventory.slots = new InventorySlot[newSlotCount];
        inventory.slots.InitalizeWithDefault();

        LinkInventoryWithUiSlots(inventory);
        int copyLength = expanding ? oldSlots.Length : inventory.slots.Length;
        for (int i = 0; i < copyLength; i++) {
            inventory.slots[i] = oldSlots[i];
        }
    }

    private bool IsEquipmentSlot(Inventory inventory, int slotIndex) {
        return inventory == playerInventory && slotIndex < playerEquipmentSize;
    }
    
    private bool EquipedBackpackHasItems() {
        int startingIndex = NakedPlayerInventorySize;
        for (int i = startingIndex; i < playerInventory.slots.Length; i++) {
            if (playerInventory.slots[i].item != null) {
                return true;
            }
        }
        return false;
    }
    
    private bool ClickedOnEquipedBackpackWithItems(Inventory inventory, int slotIndex) {
        if (inventory.slots[slotIndex].item.ItemRef.type != backpackType) {
            return false;
        }
        return IsEquipmentSlot(inventory, slotIndex) && EquipedBackpackHasItems();
    }
    
    private InventoryItem prevEquippedEyeItem;
    private InventoryItem prevEquippedBackpackItem;
    private InventoryItem prevEquippedTrinketItem;
    
    private void CheckForEquipmentChange() {
        InventoryItem curEyeItem = playerInventory.slots[0].item;
        InventoryItem curBackpackItem = playerInventory.slots[1].item;

        if (prevEquippedEyeItem != curEyeItem) {
            prevEquippedEyeItem = curEyeItem;
            equipedEye = curEyeItem == null ? emptyDemonEye : eyeInstanceFromItemId[curEyeItem.itemOrInstanceUuid];
            if (equipedEye != emptyDemonEye) {
                customQuestEvent?.Invoke("FirstDemonEyeEquiped");
            }
        }
        
        if (prevEquippedBackpackItem != curBackpackItem) {
            prevEquippedBackpackItem = curBackpackItem;
            if (curBackpackItem != null) {
                Assert.IsTrue(curBackpackItem.ItemRef is BackpackItem);
                int backpackSize = (curBackpackItem.ItemRef as BackpackItem).additionalStorageSlots;
                ChangeInventorySize(playerInventory, NakedPlayerInventorySize + backpackSize);
            }
            else {
                ChangeInventorySize(playerInventory, NakedPlayerInventorySize);
            }
        }
    }

    public struct InventoryHoverInfo {
        public Inventory inventory;
        public int slotIndex;
        public float timeSpentHovering;
    }

    private InventoryHoverInfo lastInventoryHoverInfo;
    
    private InventoryHoverInfo UpdateInventoryHover() {
        InventoryHoverInfo info = new();
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        foreach (Inventory inventory in allInventories) {
            if (!inventory.parent.gameObject.activeInHierarchy) continue;
            
            Vector2 localMousePos = inventory.parent.InverseTransformPoint(mousePos);
            Bounds localUiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(inventory.parent);
            if (!localUiBounds.Contains(localMousePos)) continue;
            
            info.inventory = inventory;
            info.slotIndex = GetHoveredInventorySlot(inventory);

            bool hoveringOverPrevSlot = info.inventory == lastInventoryHoverInfo.inventory && info.slotIndex == lastInventoryHoverInfo.slotIndex;
            if (hoveringOverPrevSlot && !IsDraggingItem) {
                info.timeSpentHovering = lastInventoryHoverInfo.timeSpentHovering + Time.deltaTime;
            }
            else {
                info.timeSpentHovering = 0f;
            }
            
            break;
        }

        lastInventoryHoverInfo = info;
        return info;
    }
    
    private int GetHoveredInventorySlot(Inventory inventory) {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        for (int i = 0; i < inventory.slots.Length; i++) {
            RectTransform rectTrans = inventory.slots[i].ui.rectTransform;
            bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
            if (mouseInRect) {
                return i;
            }
        }
        return -1;
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


    private InventoryItem dragItem;
    private InventoryHoverInfo startDragInfo;
    
    private bool IsDraggingItem => dragItem != null;

    private bool UpdateInventoryDragAndDrop(InventoryHoverInfo hoverInfo) {
        bool pickupInputUsed = selectItemInputAction.WasPressedThisFrame() || splitStackInputAction.WasPressedThisFrame();
        bool placeInputUsed = selectItemInputAction.WasPressedThisFrame() || placeSingleItemInputAction.WasPressedThisFrame();
        
        if (!pickupInputUsed && !placeInputUsed) {
            return IsDraggingItem;
        }

        // We don't allow trader items to be picked up
        if (hoverInfo.inventory == traderInventory && !IsDraggingItem) {
            if (TryGetItemFromHoverInfo(hoverInfo, out InventoryItem item)) {
                SetTradingItem(item); 
            }
            // if (transactionState != TransactionState.Selling) {
            //     MoveItemBetweenInventories(traderInventory, transactionInventory, hoverInfo.slotIndex, MoveItemOption.Single);
            // }
            return IsDraggingItem;
        }

        // If we are putting trader items back, then we also don't want to pick up the items
        if (!IsDraggingItem && hoverInfo.inventory == transactionInventory && transactionState == TransactionState.Buying) {
            MoveItemBetweenInventories(transactionInventory, traderInventory, hoverInfo.slotIndex, MoveItemOption.Single);
            return IsDraggingItem;
        }
        
        bool pickingUpItem = dragItem == null;
        if (pickingUpItem && pickupInputUsed) {
            if (!TryGetItemFromHoverInfo(hoverInfo, out InventoryItem item)) {
                return IsDraggingItem;
            }

            if (NotAllowedToMoveOrPickupItem(hoverInfo)) {
                return IsDraggingItem;
            }
            
            if (ClickedOnEquipedBackpackWithItems(hoverInfo.inventory, hoverInfo.slotIndex)) {
                return IsDraggingItem;
            }

            bool splittingStack = splitStackInputAction.WasPressedThisFrame() && item.count > 1;
            if (splittingStack) {
                int firstHalf = item.count / 2;
                int secondHalf = item.count - firstHalf;
                
                dragItem = item.Clone();
                dragItem.count = secondHalf;
                
                AdjustItemCountInInventory(hoverInfo.inventory, hoverInfo.slotIndex, firstHalf);
            }
            else {
                dragItem = item;
                RemoveItemFromInventory(hoverInfo.inventory, hoverInfo.slotIndex);
            }

            startDragInfo = hoverInfo;
            dragAndDropItemUI.gameObject.SetActive(true);
            dragAndDropItemUI.SetItem(dragItem.ItemRef, dragItem.count);
            TweenItemMove(dragAndDropItemUI);
        }

        bool placingItem = !pickingUpItem;
        if (placingItem && placeInputUsed) {
            bool droppingItemInHideout = hoverInfo.inventory == null && InHideout;
            bool tryingToPlaceItemToSellWhileBuying = hoverInfo.inventory == transactionInventory && transactionState == TransactionState.Buying;
            bool tryingToPlaceInTraderInventory = hoverInfo.inventory == traderInventory;
            
            if (droppingItemInHideout || tryingToPlaceItemToSellWhileBuying || tryingToPlaceInTraderInventory) {
                TryAddItemToInventory(startDragInfo.inventory, dragItem, startDragInfo.slotIndex);
                EndDragAndDropItem();
                return IsDraggingItem;
            }

            bool droppingItemInRaid = hoverInfo.inventory == null && InRaid;
            if (droppingItemInRaid) {
                bool droppingEntireStack = selectItemInputAction.WasPressedThisFrame();
                if (droppingEntireStack) {
                    DropItemFromInventory(dragItem);
                    dragItem.count = 0;
                }
                else {
                    DropItemFromInventory(dragItem, 1);
                    dragItem.count--;
                    dragAndDropItemUI.UpdateCount(dragItem.count);
                }

                if (dragItem.count <= 0) {
                    EndDragAndDropItem();
                }
                return IsDraggingItem;
            }

            bool swappingItems = false;
            if (TryGetItemFromHoverInfo(hoverInfo, out InventoryItem swapItem)) {
                bool itemsCanSwap = swapItem.itemOrInstanceUuid != dragItem.itemOrInstanceUuid || (swapItem.IsFullStack || dragItem.IsFullStack);
                swappingItems = itemsCanSwap && selectItemInputAction.WasPressedThisFrame();
            }
            
            if (swappingItems && IsHoveredItemGrayedOut(hoverInfo)) {
                return IsDraggingItem;
            }
            
            if (swappingItems) {
                InventorySlot targetSlot = hoverInfo.inventory.slots[hoverInfo.slotIndex];
                if (targetSlot.ui.disallowItemStacking && dragItem.count > 1) {
                    return IsDraggingItem;
                }
                if (!targetSlot.ui.AcceptsAllTypes && targetSlot.ui.onlyAcceptedItemType != dragItem.ItemRef.type) {
                    return IsDraggingItem;
                }

                targetSlot.item = dragItem;
                dragItem = swapItem;
                dragAndDropItemUI.SetItem(dragItem.ItemRef, dragItem.count);
                TweenItemMove(dragAndDropItemUI);

                return IsDraggingItem;
            }

            bool placingSingleItemFromStack = placeSingleItemInputAction.WasPressedThisFrame();
            if (placingSingleItemFromStack) {
                InventoryAddResult result = TryAddItemToInventory(hoverInfo.inventory, dragItem.ItemRef, 1, hoverInfo.slotIndex);

                dragItem.count -= result.addedCount;
                if (dragItem.count <= 0) {
                    EndDragAndDropItem();
                }
                else {
                    dragAndDropItemUI.SetItem(dragItem.ItemRef, dragItem.count);
                    TweenItemMove(dragAndDropItemUI);
                }
            }

            bool placingEntireStack = !placingSingleItemFromStack;
            if (placingEntireStack) {
                InventoryAddResult result = TryAddItemToInventory(hoverInfo.inventory, dragItem, hoverInfo.slotIndex);

                if (result.type == InventoryAddResult.ResultType.Success) {
                    EndDragAndDropItem();
                }
                else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
                    dragItem.count -= result.addedCount;
                    dragAndDropItemUI.SetItem(dragItem.ItemRef, dragItem.count);
                    TweenItemMove(dragAndDropItemUI);
                }
            }
        }

        return IsDraggingItem;
    }

    private void DropItemFromInventory(InventoryItem inventoryItem, int count = -1) {
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dropDir = (mouseWorldPos - player.position.ToVector2()).normalized;
        
        int dropCount = count <= 0 ? inventoryItem.count : count;
        
        Vector3 endPos = player.position + RandomizeVectorAngle(dropDir, 20f) * 0.2f;
        Entity itemDropEntity = SpawnItemAsEntity(inventoryItem.ItemRef, dropCount, player.position, Quaternion.identity);
        
        AddBounceEffect(itemDropEntity, endPos, 0.8f);
    }

    private void UpdateDragAndDropItemToCursor() {
        if (dragItem == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        dragAndDropItemUI.GetComponent<RectTransform>().position = mousePos;
    }
    
    private void EndDragAndDropItem() {
        if (dragItem == null) return;
        dragItem = null;
        dragAndDropItemUI.ClearItem();
        dragAndDropItemUI.gameObject.SetActive(false);
    }
    

    public struct InventoryAddResult {
        public enum ResultType { Success, Failure, FailureToAddAll };
        public ResultType type;
        public int addedCount;
    }
    
    public InventoryAddResult TryAddItemToInventory(Inventory inventory, Item item, int count, int slotIndex = -1) {
        InventoryItem newInventoryItem = new(item, count);
        return TryAddItemToInventory(inventory, newInventoryItem, slotIndex);
    }

    public InventoryAddResult TryAddItemToInventory(Inventory inventory, InventoryItem item, int slotIndex = -1) {
        InventoryAddResult result = new() { type = InventoryAddResult.ResultType.Failure };
        
        bool allowInfiniteStacking = inventory == traderInventory;
        bool droppingItemInSpecificSlot = slotIndex != -1;

        using var _ = ListPool<InventorySlot>.Get(out List<InventorySlot> availableSlots);
        
        if (droppingItemInSpecificSlot) {
            availableSlots.Add(inventory.slots[slotIndex]);
        }
        else {
            availableSlots.AddRange(inventory.slots[..]);
        }

        int remainingItemCount = item.count;

        // If we can stack the item then we just do that
        foreach (InventorySlot slot in availableSlots) {
            if (slot.item == null || slot.item.itemOrInstanceUuid != item.itemOrInstanceUuid) continue;
            if (slot.ui.disallowItemStacking || (!allowInfiniteStacking && slot.item.IsFullStack)) continue;

            TweenItemMove(slot.ui.itemUI);
                
            if (allowInfiniteStacking) {
                slot.item.count += item.count;
                result.addedCount += item.count;
                result.type = InventoryAddResult.ResultType.Success;
                return result;
            }
            
            int overflowAmount = (remainingItemCount + slot.item.count) - slot.item.ItemRef.MaxStackCount;
            if (overflowAmount > 0) {
                int addCount = slot.item.ItemRef.MaxStackCount - slot.item.count;
                
                slot.item.count += addCount;
                remainingItemCount = overflowAmount;
                
                result.addedCount += addCount;
                result.type = InventoryAddResult.ResultType.FailureToAddAll;

                if (!droppingItemInSpecificSlot) continue;

                return result;
            }
            
            slot.item.count += remainingItemCount;
            result.addedCount += remainingItemCount;
            result.type = InventoryAddResult.ResultType.Success;
            
            return result;
        }

        // Otherwise add to empty inventory slot
        foreach (InventorySlot slot in availableSlots) {
            if (slot.item != null || slot.ui.SlotIsInactive) continue;

            if (!slot.ui.AcceptsItem(item.ItemRef)) continue;

            int newItemCount = allowInfiniteStacking ? remainingItemCount : Mathf.Clamp(remainingItemCount, 0, item.ItemRef.MaxStackCount);
            newItemCount = slot.ui.disallowItemStacking ? 1 : newItemCount;
            result.addedCount += newItemCount;
            
            InventoryItem newItem = item.Clone();
            newItem.count = newItemCount;
            slot.item = newItem;
            
            TweenItemMove(slot.ui.itemUI);
            
            bool movedEntireStack = newItemCount == remainingItemCount;
            result.type = movedEntireStack ? InventoryAddResult.ResultType.Success : InventoryAddResult.ResultType.FailureToAddAll;
            
            return result;
        }
        
        return result;
    }

    private enum MoveItemOption { FullStack, Single }

    private void MoveItemBetweenInventories(Inventory fromInventory, Inventory toInventory, int slotIndex, MoveItemOption moveOption) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, slotIndex);
        if (inventoryItem == null || inventoryItem.notDiscovered) return;

        int specificSlotToMoveTo = -1;
        if (fromInventory == transactionInventory && toInventory == traderInventory) {
            specificSlotToMoveTo = inventoryItem.traderSlotIndex;
        }
        
        if (moveOption == MoveItemOption.Single) {
            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = 1;
            
            InventoryAddResult result = TryAddItemToInventory(toInventory, newItem, specificSlotToMoveTo);
            if (result.type is InventoryAddResult.ResultType.Success or InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - result.addedCount;
                AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
            }
            return;
        }

        MoveEntireItemStack(fromInventory, toInventory, slotIndex, specificSlotToMoveTo);
    }

    private void MoveEntireItemStack(Inventory fromInventory, Inventory toInventory, int fromSlotIndex, int toSlotIndex = -1) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, fromSlotIndex);
        if (inventoryItem == null) return;
        
        InventoryAddResult moveResult = TryAddItemToInventory(toInventory, inventoryItem, toSlotIndex);
        if (moveResult.type == InventoryAddResult.ResultType.Success) {
            RemoveItemFromInventory(fromInventory, fromSlotIndex);
        }
        else if (moveResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
            int keepItemCount = inventoryItem.count - moveResult.addedCount;
            AdjustItemCountInInventory(fromInventory, fromSlotIndex, keepItemCount);
        }
    }

    private void MoveEntireInventory(Inventory fromInventory, Inventory toInventory) {
        for (int i = 0; i < fromInventory.slots.Length; i++) {
            if (fromInventory.slots[i].item == null) continue;
            MoveEntireItemStack(fromInventory, toInventory, i);
        }
    }

    private void TweenItemMove(ItemUI itemUI) {
        Tween.PunchScale(itemUI.rectTransform, Vector3.one * 0.15f, 0.135f);
    }

    private void ClearInventory(Inventory inventory) {
        for (int i = 0; i < inventory.slots.Length; i++) {
            RemoveItemFromInventory(inventory, i);
        }
        if (inventory == playerInventory) {
            CheckForEquipmentChange();
        }
    }

    private void ClearInventory(InventorySlot[] inventorySlots) {
        foreach (InventorySlot slot in inventorySlots) {
            slot.item = null;
            slot.ui.ClearItem();
        }
    }

    private void RemoveItemFromInventory(Inventory inventory, int slotIndex) {
        inventory.slots[slotIndex].item = null;
        inventory.slots[slotIndex].ui.ClearItem();
    }

    // Returns the count of items we removed
    private int RemoveNumberOfItemsFromInventory(Inventory inventory, Item item, int count) {
        int removedCount = 0;
        
        for (int i = 0; i < inventory.slots.Length; i++) {
            InventorySlot slot = inventory.slots[i];
            if (slot.item == null || slot.item.itemOrInstanceUuid != item.uuid) continue;
            
            if (slot.item.count >= count) {
                removedCount += count;
                AdjustItemCountInInventory(inventory, i, slot.item.count - count);
                return removedCount;
            }
            
            removedCount += slot.item.count;
            count -= slot.item.count;
            RemoveItemFromInventory(inventory, i);
        }
        
        return removedCount;
    }

    private void RemoveNumberOfOwnedItems(Item item, int count) {
        int removedCount = RemoveNumberOfItemsFromInventory(stashInventory, item, count);
        if (removedCount != count) {
            int additionalRemoveCount = count - removedCount;
            removedCount += RemoveNumberOfItemsFromInventory(playerInventory, item, additionalRemoveCount);
        }
        Assert.IsTrue(removedCount == count, "Did not remove the specified number of item, this is bad");
    }
    
    private InventoryItem GetInventoryItem(Inventory inventory, int slotIndex) {
        if (slotIndex < 0 || slotIndex >= inventory.slots.Length) {
            return null;
        }
        return inventory.slots[slotIndex].item;
    }

    // Returns true if we reduced the item to nothing
    private bool ReduceItemCountInInventory(Inventory inventory, int slotIndex, int reduction = 1) {
        var item = GetInventoryItem(inventory, slotIndex);
        item.count -= reduction;
        if (item.count <= 0) {
            RemoveItemFromInventory(inventory, slotIndex);
            return true;
        }
        return false;
    }
    
    private void AdjustItemCountInInventory(Inventory inventory, int slotIndex, int newCount) {
        InventoryItem item = GetInventoryItem(inventory, slotIndex);
        item.count = newCount;
        if (item.count <= 0) {
            RemoveItemFromInventory(inventory, slotIndex);
        }
    }

    public void RefreshInventoryDisplay(Inventory inventory) {
        foreach (InventorySlot slot in inventory.slots) {
            slot.ui.ClearItem();
        }

        for (int i = 0; i < inventory.slots.Length; i++) {
            InventoryItem item = inventory.slots[i].item;
            if (item == null || item.notDiscovered) continue;
            inventory.slots[i].ui.SetItem(item.ItemRef, item.count);
        }
    }
    
    private void UpdateGraySlots() {
        if (OnEyeForgeTab) {
            foreach (InventorySlot slot in stashInventory.slots) {
                if (slot.item == null) continue;
                Item item = slot.item.ItemRef;
                if (item.type != eyeType && item.type != eyeModifierType) {
                    slot.ui.itemUI.ToggleGray();
                }
            }
        }
    }

    public int GetInventoryItemCount(Inventory inventory) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            count++;
        }
        return count;
    }

    public int GetItemCountInInventory(Inventory inventory, Item item) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            if (slot.item.ItemRef.uuid == item.uuid) {
                count += slot.item.count;
            }
        }
        return count;
    }

    public int GetOwnedCountOfItem(Item item) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(stashInventory, item);
        itemCount += GetItemCountInInventory(playerInventory, item);
        return itemCount;
    }

    private bool MeetsSingleUpgradeRequirement(UpgradePath.Requirement req) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(stashInventory, req.item);
        itemCount += GetItemCountInInventory(playerInventory, req.item);
        return itemCount >= req.count; 
    }

    private int GetInventoryWeight(Inventory inventory) {
        int weight = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            weight += slot.item.ItemRef.Weight * slot.item.count;
        }
        return weight;
    }
    
    private enum InventoryValueType { Buy, Sell }

    private int GetInventoryValue(Inventory inventory, InventoryValueType valueType) {
        int value = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            switch (valueType) {
                case InventoryValueType.Buy:
                    value += slot.item.ItemRef.type == demonEyeType ? GetDemonEyeSellPrice(slot.item) : slot.item.ItemRef.buyPrice * slot.item.count;
                    break;
                case InventoryValueType.Sell:
                    value += slot.item.ItemRef.type == demonEyeType ? GetDemonEyeSellPrice(slot.item) : slot.item.ItemRef.GetSellPrice() * slot.item.count;
                    break;
            }
        }
        return value;
    }

    private void OpenPlayerInventory() {
        playerPanel.gameObject.SetActive(true);
        Cursor.visible = true;
    }

    private void ClosePlayerInventory() {
        playerPanel.gameObject.SetActive(false);
        Cursor.visible = false;
        EndDragAndDropItem();
    }

    private Sequence searchSequence;
    private Tween searchCirclePopInTween;
    
    private void OpenLootInventory() {
        if (lootInventoryPanel.gameObject.activeInHierarchy) return;
        
        discoverLootIndex = -1;
        lootInventoryPanel.gameObject.SetActive(true);
        
        foreach (InventorySlot slot in lootInvetoryPtr.slots) {
            slot.ui.ClearItem();
            slot.ui.MakeSlotActive();
        }

        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].item == null) continue;
            
            InventorySlotUI slotUI = lootInvetoryPtr.slots[i].ui;
            
            if (lootInvetoryPtr.slots[i].item.notDiscovered) {
                discoverLootIndex = discoverLootIndex == -1 ? i : discoverLootIndex;
            }
            else {
                InventoryItem item = lootInvetoryPtr.slots[i].item;
                slotUI.SetItem(item.ItemRef, item.count);
            }
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        lootSearchingText.SetActive(true);

        searchSequence = Sequence.Create();
        
        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].item == null) continue;
            
            InventorySlotUI slotUI = lootInvetoryPtr.slots[i].ui;
            
            if (lootInvetoryPtr.slots[i].item.notDiscovered) {
                searchSequence.Chain(Tween.PunchScale(slotUI.rectTransform, Vector3.one * 2f, 0.1f, 2f, startDelay: 0.01f * i));
                searchSequence.ChainCallback(slotUI, (target) => target.MakeSlotInactive());
            }
        }

        searchSequence.ChainDelay(0.15f);

        searchSequence.ChainCallback(target: this, (target) => {
            InventorySlot slot = target.lootInvetoryPtr.slots[target.discoverLootIndex];
            if (slot.item != null) {
                target.AnimateSlotSearch(slot.ui);
                target.discoverLootTimer.SetTime(1f);
            }
        });
        
        discoverLootTimer.EndAction ??= () => {
            InventoryItem item = lootInvetoryPtr.slots[discoverLootIndex].item;
            item.notDiscovered = false;
            
            InventorySlotUI slotUI = lootInvetoryPtr.slots[discoverLootIndex].ui;
            slotUI.MakeSlotActive();
            slotUI.StopSlotSearching();
            slotUI.SetItem(item.ItemRef, item.count);

            Tween.PunchScale(slotUI.itemUI.image.rectTransform, Vector3.one * 4f, 0.1f, 2f); 
            
            discoverLootIndex++;
            
            if (discoverLootIndex < lootInvetoryPtr.slots.Length && lootInvetoryPtr.slots[discoverLootIndex].item != null) {
                slotUI = lootInvetoryPtr.slots[discoverLootIndex].ui;
                AnimateSlotSearch(slotUI);
                discoverLootTimer.SetTime(1f);
            }
            else {
                lootSearchingText.SetActive(false);
            }
        };
    }

    private void AnimateSlotSearch(InventorySlotUI slotUI) {
        slotUI.MakeSlotSearching();
        searchCirclePopInTween = Tween.Scale(slotUI.searchingCircle.transform, Vector3.one * 0.2f, Vector3.one * 1f, 0.25f, Ease.OutElastic); 
    }

    private void CloseLootInventory() {
        lootSearchingText.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
        discoverLootTimer.Stop();
        searchSequence.Stop();
        searchCirclePopInTween.Stop();
        
        // Reset all tweening properties because the animations might have stopped while playing 
        foreach (InventorySlot slot in lootInvetoryPtr.slots) {
            slot.ui.rectTransform.localScale = Vector3.one;
            slot.ui.StopSlotSearching();
        }
    }
    
    // **********************************
    // Player
    // **********************************

    public class Player : Entity {
        public Vector3 velocity;
        public bool bleeding;
        public bool interactingWithPortal;
        
        public int nextIdleAnimHash;
        public int nextIdleDir;
        public Limiter bleedLimiter;
        
        public int crucibleLevel;
        public int soulCurrency;
        public int coinCurrency;
        
        public int hasteSkillLevel;
        public int intellectSkillLevel;
        public int lifeBloodSkillLevel;
        public int strengthSkillLevel;

        public enum Stat {
            Armor, CarryCapacity, CritChance, CritMulti, Damage, Firerate, Health, 
            HealingAmount, HealingSpeed, LootingSpeed, MovementSpeedPercentage, ProjectileCount, Range,
        }
    }

    private Player player;
    
    private int playerRunSideAnim = Animator.StringToHash("PlayerRunSide");
    private int playerRunUpAnim = Animator.StringToHash("PlayerRunUp");
    private int playerRunDownAnim = Animator.StringToHash("PlayerRunDown");
    private int playerIdleSideAnim = Animator.StringToHash("PlayerIdleSide");
    private int playerIdleUpAnim = Animator.StringToHash("PlayerIdleUp");
    private int playerIdleDownAnim = Animator.StringToHash("PlayerIdleDown");
    private int playerDeathAnim = Animator.StringToHash("PlayerDeath");
    private int playerDrinkAnim = Animator.StringToHash("PlayerDrink");
    private int playerEatAnim = Animator.StringToHash("PlayerEat");
    private int playerBandageAnim = Animator.StringToHash("PlayerBandage");

    private float lastShotTime;
    private int consecutiveShotCount;
    private float curStepDistance;

    private Sprite defaultPlayerPreviewSprite;
    private Tween bleedPulseTween;

    private void InitPlayer() {
        player.animator.Play(playerIdleDownAnim);
        player.nextIdleAnimHash = playerIdleDownAnim;
        player.interactingWithPortal = false;
        defaultPlayerPreviewSprite ??= playerPreviewImage.sprite;
    }
    
    private void DeinitPlayer() {
        player.bleeding = false;
        playerPreviewImage.sprite = defaultPlayerPreviewSprite;
    }
    
    private void UpdatePlayer() {
        bleedDebuffIcon.gameObject.SetActive(player.bleeding);
        
        if (raidEnterSequence.isAlive) return;
        
        if (player.bleeding && player.bleedLimiter.TimeHasPassed(3.5f)) {
            const int bleedDamage = 5;
            player.health -= bleedDamage;
            
            Entity bloodDrop = SpawnEntity(bloodDropPool, OffsetY(player.position, 0.11f), Quaternion.identity);
            AddParentEffect(bloodDrop, player, 0.4f);
            DestroyEntity(bloodDrop, 0.8f);
            
            SpawnDamageNumber(OffsetY(player.position, 0.05f), bleedDamage, DamageColor.Blood);
            AddFlashHitEffect(player);
            Tween.PunchScale(bleedDebuffIcon.transform, Vector3.one * 0.8f, 0.25f, 5f);

            if (PlayerHealthIsAtAutoBleedStop()) {
                player.bleeding = false;
            }
        }
        
        bool consumingItem = playerConsumingTween.isAlive;
        if (consumingItem) {
            player.nextIdleAnimHash = playerIdleDownAnim;
            return;
        }

        if (InventoryIsOpen || player.interactingWithPortal) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        Vector2 prevPos = player.position;
        
        float speed = GetPlayerSpeed();
        Vector3 frameVelocity = new Vector3(moveInput.x, moveInput.y, 0f) * speed;

        const float acceleration = 18f;
        player.velocity = Vector3.Lerp(player.velocity, frameVelocity, acceleration * Time.deltaTime);
        
        player.position += player.velocity * Time.deltaTime;
        curStepDistance += Vector2.Distance(prevPos, player.position);

        if (moveInput != Vector2.zero) {
            player.spriteRenderer.flipX = moveInput.x < 0;
            player.nextIdleDir = (int)Mathf.Sign(moveInput.x);
        }
        else {
            player.spriteRenderer.flipX = player.nextIdleDir < 0;
        }
        
        bool movingProdominatelyVertical = Mathf.Abs(Vector2.Dot(Vector2.up, moveInput)) > 0.9f;
        
        if (moveInput.magnitude > 0.1f && !movingProdominatelyVertical) {
            player.animator.Play(playerRunSideAnim);
            player.nextIdleAnimHash = playerIdleSideAnim;
        }
        else if (moveInput.y > 0) {
            player.animator.Play(playerRunUpAnim);
            player.nextIdleAnimHash = playerIdleUpAnim;
        }
        else if (moveInput.y < 0) {
            player.animator.Play(playerRunDownAnim);
            player.nextIdleAnimHash = playerIdleDownAnim;
        }
        else {
            player.animator.Play(player.nextIdleAnimHash);
        }
        
        if (moveInput != Vector2.zero && curStepDistance > 0.18f) {
            Entity runSmokeEntity = SpawnEntity(runSmokePool, OffsetY(player.position, 0.01f), Quaternion.identity);
            DestroyEntity(runSmokeEntity, CurrentClipLength(runSmokeEntity.animator));
            PlayAudioClip(footStepClip, player.position);
            curStepDistance = 0f;
        }
        
        float projCount = 1f + GetStatAdjustmentValue(StatAdjustmentType.ProjectileCount);
        int targetCount = Mathf.FloorToInt(projCount);
        float extraProjChance = projCount % 1;
        if (RollProbability(extraProjChance)) {
            targetCount++;
        }
        
        if (equipedEye.projectileCount.TryGetValue(out var projectileCount)) {
            for (int i = 0; i < projectileCount.extraProjectileCount; i++) {
                if (RollProbability(projectileCount.probability)) {
                    targetCount++;
                }
            }
        }
        
        bool canShoot = attackLimiter.TimeHasPassed(GetFirerateDelayBasedOnStats());

        List<Vector3> attackTargets = GetAttackTargets(targetCount);
        if (attackTargets.Count <= 0 || !canShoot) return;
        
        PlayAudioClip(shootClip, player.position);
        for (int i = 0; i < attackTargets.Count; i++) {
            Vector3 attackTarget = attackTargets[i];
            
            bool isPrimaryShot = i == 0;
            if (isPrimaryShot) {
                ShootProjectile(attackTarget);
            }
            else if (equipedEye.multiProjectileCritAugment.TryGetValue(out var multiProjCrit)) {
                ShootProjectile(attackTarget, flatCritChance: multiProjCrit.probability);
            }

            if (equipedEye.doubleTapAugment.TryGetValue(out var doubleTap) && RollProbability(doubleTap.probability)) { 
                ShootProjectile(attackTarget, spawnDelay: doubleTap.delayBetweenShots);
            }
        }

        float consecutiveShotDelay = gameplayConfig.attackDelay * 1.5f;
        if (Time.time - lastShotTime <= consecutiveShotDelay) {
            consecutiveShotCount++;
        }
        else {
            consecutiveShotCount = 0;
        }
        
        if (equipedEye.blast.TryGetValue(out var blast) && consecutiveShotCount > 0 && consecutiveShotCount % blast.numshotsUntilOverheat == 0) {
            Vector2 spawnPos = OffsetY(player.position, 0.1f);
            
            Entity expEntity = SpawnEntity(blastPool, spawnPos, Quaternion.identity); 
            DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
            
            List<Collider2D> cols = OverlapCircle(spawnPos, blast.radius, Masks.EnemyMask);
            foreach (Collider2D col in cols) {
                Enemy enemy = entityLookup[col.gameObject] as Enemy;
                int damage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * blast.damageMulti);
                DamageEnemyAfterDelay(entityLookup[col.gameObject], damage, false, 0.15f);
            }
        }
            
        lastShotTime = Time.time;
    }
    
    private Tween playerConsumingTween;
    private Inventory consumingInventory;
    private int consumingSlotIndex;
    
    private void HavePlayerConsumeItem(Inventory fromInventory, int slotIndex) {
        if (playerConsumingTween.isAlive) return;
        ConsumableItem item = fromInventory.slots[slotIndex].item.ItemRef as ConsumableItem;

        if (!item) return;
        
        bool itemHeals = item.healingAmount > 0;
        bool itemStopsBleeds = item.bandageAmount > 0;

        if (itemHeals && itemStopsBleeds) {
            if (player.health == FullPlayerHealth && !player.bleeding) return;
        }
        else if (itemHeals) {
            if (player.health == FullPlayerHealth) return;
        }
        else if (itemStopsBleeds) {
            if (!player.bleeding) return;
        }
        
        const float additionalConsumeDelay = 0.15f;
        const float performActionAtAnimationCompletion = 0.9f;
        
        int animationHash = item.animationType switch {
            ConsumableItem.AnimationType.Drink   => playerDrinkAnim,
            ConsumableItem.AnimationType.Eat     => playerEatAnim,
            ConsumableItem.AnimationType.Bandage => playerBandageAnim,
        };

        player.animator.Play(animationHash);
        player.animator.Update(0);

        float animationLength = CurrentClipLength(player.animator); 
        float actionDelay = animationLength * performActionAtAnimationCompletion;
        float postActionDelay = animationLength * (1f - performActionAtAnimationCompletion);

        // So we don't allocate any memory for the closure
        consumingSlotIndex = slotIndex;
        consumingInventory = fromInventory;
        
        playerConsumingTween = Tween.Delay(item, actionDelay, static (item) => {
            if (item.healingAmount > 0) {
                inst.HealPlayer(item.healingAmount);
            }
            if (item.bandageAmount > 0) {
                inst.player.bleeding = false;
            }
            inst.ReduceItemCountInInventory(inst.consumingInventory, inst.consumingSlotIndex);
        });
        
        playerConsumingTween.OnUpdate(this, static (_, _) => {
            if (inst.playerPreviewImage.sprite != inst.player.spriteRenderer.sprite) {
                inst.playerPreviewImage.sprite = inst.player.spriteRenderer.sprite;     
            }
        });
        
        playerConsumingTween.Chain(Tween.Delay(postActionDelay, static () => {
            inst.player.animator.Play(inst.playerIdleDownAnim);
            inst.player.animator.Update(0f);
            if (inst.playerPreviewImage.sprite != inst.player.spriteRenderer.sprite) {
                inst.playerPreviewImage.sprite = inst.player.spriteRenderer.sprite;     
            }
        }))
        .Chain(Tween.Delay(additionalConsumeDelay));
    }

    private void HealPlayer(int healing) {
        player.health = Mathf.Clamp(player.health + healing, 0, FullPlayerHealth);
    }

    private void DamagePlayer(int damage, float chanceToBleed = 0f) {
        // if (!player.bleeding && !PlayerHealthIsAtAutoBleedStop() && RollProbability(chanceToBleed)) {
        //     player.bleeding = true;
        // }
        if (timeSpentSummoningPortal < gameplayConfig.portalSummonTime) {
            timeSpentSummoningPortal = 0f;
        }
        
        float damageReductionFromArmor = GetStatAdjustmentValue(StatAdjustmentType.ArmorDamageReductionPercentage);
        Assert.IsTrue(damageReductionFromArmor >= 0f && damageReductionFromArmor < 1f, "Armor stat needs to be [0, 1)");
        
        int damageReduction = Mathf.RoundToInt(damage * damageReductionFromArmor);
        damage = Mathf.Clamp(damage - damageReduction, 0, int.MaxValue);
        
        player.health -= damage;
        AddFlashHitEffect(player);
        SpawnDamageNumber(player.position, damage, DamageColor.Blood);
    }
    
    private bool PlayerHealthIsAtAutoBleedStop() {
        const float percentageOfHealthBleedingStops = 0.10f;
        return player.health <= FullPlayerHealth * percentageOfHealthBleedingStops;
    }

    private int GetPlayerStatLevel(Player.Stat stat) {
        return stat switch {
            Player.Stat.CarryCapacity   => player.strengthSkillLevel,
            Player.Stat.CritChance      => player.intellectSkillLevel,
            Player.Stat.CritMulti       => player.intellectSkillLevel,
            Player.Stat.Damage          => player.intellectSkillLevel,
            Player.Stat.Firerate        => player.hasteSkillLevel,
            Player.Stat.Health          => player.lifeBloodSkillLevel,
            Player.Stat.HealingAmount   => player.lifeBloodSkillLevel,
            Player.Stat.HealingSpeed    => player.lifeBloodSkillLevel,
            Player.Stat.LootingSpeed    => player.hasteSkillLevel,
            Player.Stat.MovementSpeedPercentage   => player.hasteSkillLevel,
            Player.Stat.ProjectileCount => player.strengthSkillLevel,
            _                           => -1,
        };
    }

    private float GetPlayerStat(Player.Stat stat) {
        return stat switch {
            Player.Stat.CarryCapacity   => GetPlayerStatLevel(Player.Stat.CarryCapacity) * gameplayConfig.carryCapacityIncPerLevel,
            Player.Stat.CritChance      => GetPlayerStatLevel(Player.Stat.CritChance) * gameplayConfig.critChanceIncPerLevel,
            Player.Stat.CritMulti       => GetPlayerStatLevel(Player.Stat.CritMulti) * gameplayConfig.critMultiplierIncPerLevel,
            Player.Stat.Damage          => GetPlayerStatLevel(Player.Stat.Damage) * gameplayConfig.damageIncPerLevel,
            Player.Stat.Firerate        => GetPlayerStatLevel(Player.Stat.Firerate) * gameplayConfig.firerateIncPerLevel,
            Player.Stat.Health          => GetPlayerStatLevel(Player.Stat.Health) * gameplayConfig.healthIncPerLevel,
            Player.Stat.HealingAmount   => GetPlayerStatLevel(Player.Stat.HealingAmount) * gameplayConfig.healingIncPerLevel,
            Player.Stat.HealingSpeed    => GetPlayerStatLevel(Player.Stat.HealingSpeed) * gameplayConfig.healingSpeedIncPerLevel,
            Player.Stat.LootingSpeed    => GetPlayerStatLevel(Player.Stat.LootingSpeed) * gameplayConfig.lootingSpeedIncPerLevel,
            Player.Stat.MovementSpeedPercentage   => GetPlayerStatLevel(Player.Stat.MovementSpeedPercentage) * gameplayConfig.movementSpeedIncPerLevel,
            Player.Stat.ProjectileCount => GetPlayerStatLevel(Player.Stat.ProjectileCount) * gameplayConfig.projectileCountIncPerLevel,
            _                           => -1,
        };
    }
    
    public enum StatAdjustmentType {
        ArmorDamageReductionPercentage, FireratePercentage, MovementSpeedPercentage, 
        Damage, CritChance, CritMulti, ProjectileCount, RangeInSeconds,
    }
    
    private float GetStatAdjustmentValue(StatAdjustmentType stat) {
        float statSum = 0f;
        
        for (int i = 0; i < playerEquipmentSize; i++) {
            Item item = playerInventory.slots[i].item?.ItemRef;
            if (!item || !item.modifiesStats) continue;
            
            switch (stat) {
                case StatAdjustmentType.ArmorDamageReductionPercentage:
                    statSum += item.armorPercent; 
                    break;
                case StatAdjustmentType.MovementSpeedPercentage:
                    statSum += item.movementSpeedPercentage;
                    break;
            }
        }

        foreach (EquipedModInstance mod in equipedEye.modInstances) {
            ModifierItem modifierItem = mod.ModifierItem;
            if (!modifierItem.modifiesStats) continue;

            switch (stat) {
                case StatAdjustmentType.CritChance:
                    statSum += modifierItem.critChance; 
                    break;
                case StatAdjustmentType.CritMulti:
                    statSum += modifierItem.critMultiplier; 
                    break;
                case StatAdjustmentType.Damage:
                    statSum += modifierItem.damage; 
                    break;
                case StatAdjustmentType.FireratePercentage:
                    statSum += modifierItem.fireratePercentage; 
                    break;
                case StatAdjustmentType.ProjectileCount:
                    statSum += modifierItem.projectileCount; 
                    break;
                case StatAdjustmentType.RangeInSeconds:
                    statSum += modifierItem.rangeInSeconds;
                    break;
            }
        }

        return statSum;
    }

    private int FullPlayerHealth => 100 + (gameplayConfig.healthIncPerLevel * GetPlayerStatLevel(Player.Stat.Health));

    private float GetPlayerSpeed() {
        float playerSpeed = gameplayConfig.baseSpeed;
        playerSpeed += playerSpeed * GetStatAdjustmentValue(StatAdjustmentType.MovementSpeedPercentage);
        
        float speedReductionFromWeight = Mathf.Lerp(0f, gameplayConfig.maxEncumberedSpeedReduction, GetOverweightCompletion());
        speedReductionFromWeight = Mathf.Clamp(speedReductionFromWeight, 0f, gameplayConfig.maxEncumberedSpeedReduction);

        playerSpeed -= speedReductionFromWeight;
        return playerSpeed;
    }

    private float GetFirerateDelayBasedOnStats() {
        if (equipedEye == emptyDemonEye) {
            return gameplayConfig.attackDelay;
        }

        float attackDelay = gameplayConfig.attackDelay;
        attackDelay -= attackDelay * GetStatAdjustmentValue(StatAdjustmentType.FireratePercentage);
        return Mathf.Clamp(attackDelay, gameplayConfig.cappedMinAttackDelay, gameplayConfig.attackDelay);
    }

    private int GetCarryCapacityStat() {
        int carryCapacityStat = GetPlayerStatLevel(Player.Stat.CarryCapacity);
        return carryCapacityStat;
    }

    private void GetEncumberingWeightRange(out int startingWeight, out int endingWeight) {
        int encumberingIncreaseFromStrength = GetCarryCapacityStat() * gameplayConfig.carryCapacityIncPerLevel;
        endingWeight = gameplayConfig.maxEncumberedWeight + encumberingIncreaseFromStrength;
        startingWeight = gameplayConfig.defaultStartingEncumberingWeight + encumberingIncreaseFromStrength;
    }

    private float GetOverweightCompletion() {
        GetEncumberingWeightRange(out int startingEncumberingWeight, out int endingEncumberingWeight);
        int inventoryWeight = GetInventoryWeight(playerInventory);
        int curOverweightAmount = Mathf.Clamp(inventoryWeight - startingEncumberingWeight, 0, int.MaxValue);
        float maxOverweightAmount = (float)endingEncumberingWeight - startingEncumberingWeight;
        float overweightComp = curOverweightAmount / maxOverweightAmount;
        return Mathf.Clamp01(overweightComp);
    }
    
    // ************************ 
    // Demon Eye
    // ************************ 
    
    public struct EquipedModInstance {
        public int uuid;
        public int stackCount;
        
        public ModifierItem ModifierItem => resourceLookup[uuid] as ModifierItem;
        public void ApplyToEnemy(Enemy enemy) => ModifierItem.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => ModifierItem.AddInstanceToEye(eyeInstance, stackCount);
    }

    public struct EquipedAugmentInstance {
        public int uuid;
        
        public Augment Augment => resourceLookup[uuid] as Augment;
        public void ApplyToEnemy(Enemy enemy) => Augment.AddInstanceToEnemy(enemy);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Augment.AddInstanceToEye(eyeInstance);
    }

    public class DemonEyeInstance {
        public List<EquipedModInstance> modInstances = new();
        public List<EquipedAugmentInstance> augmentInstances = new();
        
        public FirerateModifierItem.InstanceData? firerate;
        public TrishotModifierItem.InstanceData? trishot;
        public RangeModifierItem.InstanceData? range;
        public PenetrationModifierItem.InstanceData? penetration;
        public BackwardsShotModifierItem.InstanceData? backwardShot;
        public ExplosionModifierItem.InstanceData? explosion;
        public OverheatBlast.InstanceData? blast;
        public BoneShatterModifierItem.InstanceData? boneShatter;
        public StoppingPowerModifierItem.InstanceData? stoppingPower;
        public ProjectileCountModifierItem.InstanceData? projectileCount;
        
        public BleedCritAugment.InstanceData? bleedCritAugment;
        public DoubleCritAugment.InstanceData? doubleCritAugment;
        public DistanceDamageAugment.InstanceData? distanceDamage;
        public PenetrationDamageAugment.InstanceData? penetrationDamageAugment;
        public DoubleTapAugment.InstanceData? doubleTapAugment;
        public BackwardsPiercingAugment.InstanceData? backwardsPiercingAugment;
        public MultiProjectileCritAugment.InstanceData? multiProjectileCritAugment;
    }
    
    public class DemonEyeRaidStats {
        public int consecutiveCriticalHits;
        public float lastDoubleCritActivationTime;
    }

    // Need to reset this at the beginning of every raid
    private DemonEyeRaidStats demonEyeRaidStats;

    public Dictionary<int, DemonEyeInstance> eyeInstanceFromItemId = new();
    private readonly DemonEyeInstance emptyDemonEye = new();
    private DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    private DemonEyeInstance BuildAndRegisterEye(InventoryItem item) {
        item.itemOrInstanceUuid = GenerateNewItemUuid();
        item._itemRef = demonEyeItem;
        
        Dictionary<ModifierItem, int> modCountFromItem = new();
        List<EquipedAugmentInstance> equipedAugments = new();
        
        foreach (int modUuid in item.nestedUuids) {
            UuidScriptableObject nestedObject = resourceLookup[modUuid];
            if (nestedObject is ModifierItem modifierItem) {
                if (!modCountFromItem.TryAdd(modifierItem, 1)) {
                    modCountFromItem[modifierItem]++;
                }
            }
            else if (nestedObject is Augment augment) {
                equipedAugments.Add(new() { uuid = augment.uuid });
            }
        }

        List<EquipedModInstance> equipedMods = new();
        foreach ((ModifierItem modItem, int stackCount) in SortModsFromDictionary(modCountFromItem)) {
            equipedMods.Add(new() {
                uuid = modItem.uuid,
                stackCount = stackCount,
            });
        }
        
        DemonEyeInstance newDemonEye = new() {
            modInstances = equipedMods,
            augmentInstances = equipedAugments,
        };
        
        foreach (EquipedModInstance modInstance in equipedMods) { 
            modInstance.ApplyToEye(newDemonEye); 
        }
        foreach (EquipedAugmentInstance augmentInstance in equipedAugments) { 
            augmentInstance.ApplyToEye(newDemonEye); 
        }
        
        eyeInstanceFromItemId.Add(item.itemOrInstanceUuid, newDemonEye);
        return newDemonEye;
    }

    private List<(ModifierItem, int)> SortModsFromDictionary(Dictionary<ModifierItem, int> soulcardsAndStackCount) {
        List<(ModifierItem, int)> eyeModifiers = new();
        foreach (KeyValuePair<ModifierItem, int> pair in soulcardsAndStackCount) {
            eyeModifiers.Add(new(pair.Key, pair.Value));
        }
        eyeModifiers = eyeModifiers.OrderByDescending(m => m.Item1.GetRarity()).ThenBy(m => m.Item1.displayName).ToList();
        return eyeModifiers;
    }

    public int GetDemonEyeSellPrice(InventoryItem demonEyeInventoryItem) {
        // We need to use the InventoryItem's ID because the Item's ID is the demon eye Scriptable Object
        DemonEyeInstance demonEye = eyeInstanceFromItemId[demonEyeInventoryItem.itemOrInstanceUuid]; 
        
        int sellPrice = 0;
        foreach (EquipedModInstance modInstance in demonEye.modInstances) {
            sellPrice += modInstance.ModifierItem.GetSellPrice() * modInstance.stackCount;
        }
        return sellPrice;
    } 

    private List<Vector3> GetAttackTargets(int targetCount) {
        float overlapDist = gameplayConfig.projectileSpeed * GetProjectileRangeInSeconds();
        List<Collider2D> cols = OverlapCircle(player.position, overlapDist, Masks.EnemyMask);
        
        if (cols.Count <= 0) {
            cols = OverlapCircle(player.position, overlapDist, Masks.MineableMask);
        }
        
        cols.Sort(static (a, b) => {
            float aScore = GetTargetScore(a);
            float bScore = GetTargetScore(b);
            return aScore.CompareTo(bScore);
        });

        int count = Mathf.Min(targetCount, cols.Count);
        List<Vector3> targets = new();
        for (int i = 0; i < count; i++) {
            targets.Add(entityLookup[cols[i].gameObject].Center);
        }
        return targets;
    }

    private static float GetTargetScore(Collider2D col) {
        Entity entity = inst.entityLookup[col.gameObject];
        float dist = Vector2.Distance(col.transform.position, inst.player.position);

        if (entity is Enemy enemy) {
            const float distWeight = 1f;
            const float healthWeight = -0.003f;
            float bleedingWeight = enemy.bleed.HasValue ? 0.1f : 0f;
            return (dist * distWeight) + (enemy.health * healthWeight) + bleedingWeight;
        }
        
        return dist;
    }

    private void ShootProjectile(Vector2 targetPos, float? spawnDelay = default, float? flatCritChance = default) {
        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - gameplayConfig.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = gameplayConfig.projectileSpeed;
        Vector2 dir = (targetPos - PlayerEyePos.ToVector2()).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * projectileSpeed; 
        _SpawnProjectile(PlayerEyePos, velocity, projectilePool);
        
        if (equipedEye.trishot.TryGetValue(out var trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            _SpawnProjectile(PlayerEyePos, secondShotVelocity, projectilePool, flgs: ProjectileTypeFlags.Trishot);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            _SpawnProjectile(PlayerEyePos, thirdShotVelocity, projectilePool, flgs: ProjectileTypeFlags.Trishot);
        }

        if (equipedEye.backwardShot.TryGetValue(out var backShot) && RollProbability(backShot.probability)) {
            const float backwardsShotSpeedScaler = 1.1f;
            EntityPool<Projectile> pool = equipedEye.backwardsPiercingAugment.HasValue ? piercingShotProjectilePool : projectilePool; 
            _SpawnProjectile(PlayerEyePos, -velocity * backwardsShotSpeedScaler, pool, flgs: ProjectileTypeFlags.BackwardsShot);
        }
        
        // Helper method just to forward the passed in parameters
        void _SpawnProjectile(Vector2 pos, Vector2 vel, EntityPool<Projectile> pool, ProjectileTypeFlags flgs = ProjectileTypeFlags.None) {
            SpawnProjectile(pos, vel, pool, typeFlags: flgs, spawnDelay: spawnDelay, flatCritChance: flatCritChance);
        }
    }

    private Projectile SpawnProjectile(Vector2 spawnPos, Vector2 velocity, EntityPool<Projectile> pool, 
        Quaternion? rotation = default, int? flatDamage = default, float? spawnDelay = default, float? lifetime = default, 
        float? flatCritChance = default, LayerMask? layermask = default, ProjectileTypeFlags typeFlags = ProjectileTypeFlags.None) 
    {
        Quaternion projectileRotation = rotation ?? Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, velocity.normalized), Vector3.forward);
        Projectile projectile = SpawnEntity(pool, spawnPos, projectileRotation);
        
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = equipedEye;
        projectile.flatDamage = flatDamage;
        projectile.flatCritChance = flatCritChance;
        projectile.lifeTimeDuration = lifetime ?? GetProjectileRangeInSeconds();
        projectile.layerMask = layermask ?? Masks.DamagableMask;
        projectile.typeFlags = typeFlags;

        if (!spawnDelay.HasValue) {
            projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
            return projectile;
        }

        Delay(projectile, spawnDelay.Value, static (projectile) => {
            projectile.gameObject.SetActive(true);
            inst.projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
        });

        return projectile;
    }
    
    private float GetProjectileRangeInSeconds() {
        return gameplayConfig.rangeInSeconds + GetStatAdjustmentValue(StatAdjustmentType.RangeInSeconds);
    }

    private Vector3 PlayerEyePos => player.position + new Vector3(0f, 0.13f, 0f);

    // *******************************
    // Interactions 
    // *******************************
    
    private Sequence callingExitPortalSequence;
    private bool canTakeExitPortal;
    private float timeSpentSummoningPortal;
    
    private void CheckForInteractions() { 
        interactPrompt.gameObject.SetActive(false);
        interactionDetails.gameObject.SetActive(false);
        
        Vector2 checkCenter = player.position + new Vector3(0f, 0.05f, 0f);
        List<Collider2D> cols = OverlapCircle(checkCenter, 0.1f, Masks.ItemMask);
        
        foreach (Collider2D col in cols) {
            if (col.CompareTag(Tags.Pickup)) {
                ItemDrop itemDrop = col.GetComponent<ItemDrop>();
                
                Color itemColor = styles.GetColorForRarity(itemDrop.Item.GetRarity());
                string details = ColorText($"{itemDrop.Item.displayName} x{itemDrop.dropCount}", itemColor);
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), details);
                
                if (interactInputAction.WasPressedThisFrame()) {
                    InventoryAddResult result = TryAddItemToInventory(playerInventory, itemDrop.Item, itemDrop.dropCount);
                    if (result.type == InventoryAddResult.ResultType.Success) {
                        Entity droppedEntity = entityLookup[itemDrop.gameObject];
                        PickupDroppedItem(droppedEntity); 
                        itemDrop.circleCollider.enabled = false;
                    }
                    else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
                        itemDrop.dropCount -= result.addedCount;
                    }
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Body");
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInvetoryPtr.slots = deadBodySlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }
            
            if (col.CompareTag(Tags.Bush)) {
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), "Search Bush");
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInvetoryPtr.slots = bushSlotsLookup[col.gameObject];
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
                if (timeSpentSummoningPortal < gameplayConfig.portalSummonTime) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Summon Exit Portal");
                    if (interactInputAction.IsPressed()) {
                        player.interactingWithPortal = true;
                        timeSpentSummoningPortal += Time.deltaTime;

                        if (timeSpentSummoningPortal >= gameplayConfig.portalSummonTime && !callingExitPortalSequence.isAlive) {
                            player.interactingWithPortal = false;
                            callingExitPortalSequence = Sequence.Create();
                            callingExitPortalSequence.ChainDelay(gameplayConfig.portalPostSummonDelay);
                            callingExitPortalSequence.Chain(Tween.Scale(activeExitPortal, Vector3.one, 0.25f, Ease.OutBack));
                            callingExitPortalSequence.OnComplete(static () => {
                                inst.canTakeExitPortal = true; 
                                inst.OnExitPortalSummoned();
                            });
                        }
                    }
                    else {
                        player.interactingWithPortal = false;
                        timeSpentSummoningPortal = 0f;
                    }
                }
                if (canTakeExitPortal) {
                    EnableInteractionPrompt(OffsetY(col.transform.position, 0.21f), "Take Exit Portal");
                    if (interactInputAction.WasPressedThisFrame()) {
                        gameStateMachine.SetStateIfNotCurrent(curRaidState == RaidState.PostFinalWave ? winExitState : earlyExitState);
                        closeExitPortalSequence.Stop();
                        
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

    private void EnableInteractionPrompt(Vector3 position, string detailsString) {
        interactionDetails.gameObject.SetActive(true);
        interactionDetails.text = detailsString;
        
        interactPrompt.gameObject.SetActive(true);
        interactPrompt.text = $"<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}>";
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position);
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

            InventoryItem item = playerInventory.slots[itemIndex].item;
            if (item != null) {
                hotBarItemUIs[i].SetItem(item.ItemRef, item.count);
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
                itemToConsume = playerInventory.slots[playerInventorySlotIndex].item?.ItemRef;
                break;
            }
            playerInventorySlotIndex++;
        }

        if (itemToConsume) {
            HavePlayerConsumeItem(playerInventory, playerInventorySlotIndex);
        }
    }

    // *******************************
    // Projectiles
    // *******************************

    [NonSerialized] public List<Projectile> projectiles = new();
    [Flags] public enum ProjectileTypeFlags { None, Trishot, BackwardsShot, }
    
    public class Projectile : Entity {
        public ProjectileTypeFlags typeFlags;
        public int? flatDamage;
        public float? flatCritChance;
        public float lifeTimeDuration;
        public float curTimeAlive;
        public float distTraveled;
        public Vector2 velocity;
        public LayerMask layerMask;
        public DemonEyeInstance eyeInstanceSpawnedFrom;
        public List<Entity> ignoreEntities;
    }
    
    private static void OnSpawnProjectile(Projectile projectile) {
        projectile.typeFlags = ProjectileTypeFlags.None;
        projectile.flatDamage = default;
        projectile.flatCritChance = default;
        projectile.lifeTimeDuration = default;
        projectile.curTimeAlive = default;
        projectile.distTraveled = default;
        projectile.velocity = default;
        projectile.layerMask = default;
        projectile.eyeInstanceSpawnedFrom = default;
        if (projectile.ignoreEntities != null) {
            ListPool<Entity>.Release(projectile.ignoreEntities);
        }
        projectile.ignoreEntities = default;
    }
    
    private void UpdateProjectiles() {
        const float projectileRadius = 0.035f;
        
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.curTimeAlive += Time.deltaTime;
            proj.trans.position += proj.velocity.ToVector3() * Time.deltaTime;
            proj.distTraveled += proj.velocity.magnitude * Time.deltaTime;
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, projectileRadius, proj.layerMask);
            if (!col) continue;
            
            // Hack for identifying if the projectile hit the player 
            if (proj.layerMask == Masks.PlayerHurtMask) {
                Assert.IsTrue(proj.flatDamage.HasValue, "Projectiles that damage the player need to have a flat damage value");
                DamagePlayer(proj.flatDamage.Value);
                DestroyEntity(projectiles[i]);
                projectiles.RemoveAt(i);
                continue;
            }
            
            Entity entity = entityLookup[col.gameObject];
                    
            if (!ProjectileIsIgnoringEntity(proj, entity)) {
                HandleDamage(proj, entity);
                PlayAudioClip(projectileImpact, proj.position);
            }

            if (entity is Enemy && ProjectileShouldPassThrough(proj, entity)) continue;
            
            Entity impact = SpawnEntity(projectileImpactPool, proj.position, RandomRotation());
            DestroyEntity(impact, CurrentClipLength(impact.animator));
            
            DestroyEntity(projectiles[i]);
            projectiles.RemoveAt(i);
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].curTimeAlive > projectiles[i].lifeTimeDuration) {
                const float despawnTime = 0.2f;
                Tween.Scale(projectiles[i].trans, 0f, despawnTime, Ease.InOutBounce);
                DestroyEntity(projectiles[i], despawnTime);
                projectiles.RemoveAt(i);
            }
        }
    }

    private bool ProjectileShouldPassThrough(Projectile proj, Entity entity) {
        if (ProjectileIsIgnoringEntity(proj, entity)) {
            return true;
        }

        if (ProjectileIsType(proj, ProjectileTypeFlags.BackwardsShot) && proj.eyeInstanceSpawnedFrom.backwardsPiercingAugment.HasValue) {
            ProjectileMarkEntityToIgnore(proj, entity);
            return true;
        }

        if (proj.eyeInstanceSpawnedFrom.penetration.TryGetValue(out var penetration)) {
            ProjectileMarkEntityToIgnore(proj, entity);
            int alreadyPenetratedCount = proj.ignoreEntities?.Count ?? 0;
            return alreadyPenetratedCount <= penetration.goThroughCount;
        }

        return false;
    }

    private void ProjectileMarkEntityToIgnore(Projectile proj, Entity entity) {
        bool alreadyContainsEntity = proj.ignoreEntities?.Contains(entity) ?? false;
        if (EntityIsValid(entity) && !alreadyContainsEntity) {
            proj.ignoreEntities ??= ListPool<Entity>.Get();
            proj.ignoreEntities.Add(entity);
        }
    }

    private bool ProjectileIsIgnoringEntity(Projectile proj, Entity entity) {
        return proj.ignoreEntities?.Contains(entity) ?? false;
    }
    
    private bool ProjectileIsType(Projectile proj, ProjectileTypeFlags flags) {
        return (proj.typeFlags & flags) != 0;
    }

    // ***********************************
    // Damage Handling 
    // ***********************************

    private void DamageEnemyAfterDelay(Entity enemy, int damage, bool isCriticalStrike, float delay) {
        enemy.delayedDamage = damage;
        enemy.delayedDamageIsCrit = isCriticalStrike;
        Delay(enemy, delay, static enemy => {
            inst.DamageEnemy(enemy, enemy.delayedDamage, enemy.delayedDamageIsCrit);
        });
    }
    
    private void DamageEnemy(Entity enemy, int damage, bool isCriticalStrike) {
        enemy.health -= damage;
        AddFlashHitEffect(enemy);
        SpawnDamageNumber(EnemyDamageNumberSpawnPos(enemy), damage, isCriticalStrike ? DamageColor.Crit : DamageColor.Normal);
        Tween.PunchScale(enemy.trans, Vector3.one * 0.15f, 0.1f, 15f);
    }
    
    private void HandleDamage(Projectile projectile, Entity entity) {
        if (entity == null) return;
        
        DemonEyeInstance eyeInstance = projectile.eyeInstanceSpawnedFrom;
        
        if (entity.gameObject.CompareTag(Tags.Enemy)) {
            if (entityLookup[entity.gameObject] is not Enemy enemy) return;

            if (projectile.flatDamage.HasValue) {
                DamageEnemy(enemy, projectile.flatDamage.Value, false);
                return;
            }
            
            bool isCriticalStrike = RollProbability(GetCriticalStrikeProbability(projectile, enemy));
            if (isCriticalStrike) {
                demonEyeRaidStats.consecutiveCriticalHits++;
            }
            else {
                demonEyeRaidStats.consecutiveCriticalHits = 0;
            }

            int damage = GetProjectileDamage(projectile, enemy, isCriticalStrike);
            DamageEnemy(enemy, damage, isCriticalStrike);
            
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                modInstance.ApplyToEnemy(enemy);
            }
            
            if (eyeInstance.explosion.TryGetValue(out var explosion) && RollProbability(explosion.probability)) {
                Vector2 expSpawnPos = projectile.position + (enemy.position - projectile.position) / 2f;
                
                Entity expEntity = SpawnEntity(explosionPool, expSpawnPos, Quaternion.identity); 
                DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
                
                List<Collider2D> cols = OverlapCircle(expSpawnPos, explosion.radius, Masks.EnemyMask);
                foreach (Collider2D col in cols) {
                    Enemy explosionEnemy = entityLookup[col.gameObject] as Enemy;
                    int explosionDamage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(explosionEnemy) * explosion.damageMulti);
                    DamageEnemyAfterDelay(entityLookup[col.gameObject], explosionDamage, false, 0.1f);
                }
            }
            
            if (equipedEye.boneShatter.TryGetValue(out var boneShatter) && RollProbability(boneShatter.probability)) {
                for (int i = 0; i < boneShatter.shardsCount; i++) {
                    float randomDelay = Random.Range(0f, 0.06f);
                    float randomSpeedScaler = Random.Range(0.4f, 0.6f);
                    Vector2 boneShatterVelocity = RandomizeVectorAngle(projectile.velocity * randomSpeedScaler, 40f);
                    int boneDamage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * boneShatter.perShardDamageMulti);
                    Projectile boneShatterProj = SpawnProjectile(enemy.position, boneShatterVelocity, boneShatterProjectilePool, 
                        rotation: RandomRotation(), spawnDelay: randomDelay, flatDamage: boneDamage, lifetime: boneShatter.lifeTime);
                    ProjectileMarkEntityToIgnore(boneShatterProj, enemy);
                }
            }
        }
        else {
            entity.health -= gameplayConfig.damage;

            PlayAudioClip(stoneHitClip, entity.position);
                
            if (entity.health <= 0) {
                if (entity.obstacleCellRadius > 0) {
                    loadedMapInst.grid.ClearObstacle(entity.obstaclePosition, entity.obstacleCellRadius);
                }
                
                Entity smokeEntity = SpawnEntity<Entity>(rockSmokePrefab, entity.position, Quaternion.identity);
                DestroyEntity(smokeEntity, 0.417f);
                DestroyEntity(entity);
                
                PlayAudioClip(stoneBreakClip, entity.position);

                Item dropItem = null;
                if (RollProbability(loadedMapData.eyeUpgradeFromRockChance)) {
                    dropItem = GetItemFromDropPool(eyeUpgradesDropPool);
                }
                else {
                    dropItem = GetItemFromDropPool(rockStonesDropPool);
                }

                Vector3 endPos = entity.position + RotationVector(Random.Range(0f, 360f), 0.18f, 0.25f);
                Entity rockDrop = SpawnItemAsEntity(dropItem, 1, entity.position, Quaternion.identity);
                AddBounceEffect(rockDrop, endPos, 0.8f);
            }
            else {
                AddFlashHitEffect(entity);
                AddShakeEffect(entity, 8f, 0.038f, 0.35f, shakeCurve);
                Tween.PunchScale(entity.trans, Vector3.one * 0.12f, 0.1f, 15f);
            }
        }
    }

    private float GetCriticalStrikeProbability(Projectile proj, Enemy enemy) {
        if (proj.flatCritChance.TryGetValue(out float flatCrit)) {
            return flatCrit;
        }
        
        if (ProjectileIsType(proj, ProjectileTypeFlags.BackwardsShot)) {
            return 1f;
        }
        
        float criticalStrikeProb = gameplayConfig.defaultCritChance + GetStatAdjustmentValue(StatAdjustmentType.CritChance);

        if (equipedEye.bleedCritAugment.HasValue && enemy.bleed.HasValue) {
            criticalStrikeProb += equipedEye.bleedCritAugment.Value.probability;
        }

        return criticalStrikeProb;
    }

    private int GetProjectileDamage(Projectile proj, Enemy enemy, bool isCriticalHit) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        
        int damage = GetBaseDamage();
        
        // Phase 1 : Additions
        {
            if (equipedEye.distanceDamage.TryGetValue(out var distDamage)) {
                float convertedUnits = proj.distTraveled / gameplayConfig.distancePerUnit;
                int increasedDamageFromDist = Mathf.FloorToInt(convertedUnits) * distDamage.damageIncreasePerUnitTraveled;
                damage += increasedDamageFromDist;
            }
        }
        
        // Phase 2 : Multipliers
        {
            float damageMultiplier = GetDamageMultiplierOnEnemy(enemy);
            
            if (isCriticalHit) {
                float critMultiplier = gameplayConfig.defaultCritMulti + GetStatAdjustmentValue(StatAdjustmentType.CritMulti);
                damageMultiplier += critMultiplier;
            }
            
            if (ProjectileIsType(proj, ProjectileTypeFlags.Trishot) && eyeInstance.trishot.TryGetValue(out var triShot)) {
                damageMultiplier += triShot.damageMultiplier;
            }
            
            if (equipedEye.doubleCritAugment.TryGetValue(out var doubleCrit)) {
                int consecutiveCriticalHits = demonEyeRaidStats.consecutiveCriticalHits;
                if (consecutiveCriticalHits > 0 && consecutiveCriticalHits % 2 == 0) {
                    demonEyeRaidStats.lastDoubleCritActivationTime = Time.time;
                }

                if (Time.time - demonEyeRaidStats.lastDoubleCritActivationTime <= doubleCrit.multiplierDuration) {
                    damageMultiplier += doubleCrit.damageMulti;
                }
            }

            if (equipedEye.penetrationDamageAugment.TryGetValue(out var penetrationDamage)) {
                int penetrationCountBeforeDamagingThisEnemy = proj.ignoreEntities == null ? 0 : proj.ignoreEntities.Count;
                if (penetrationCountBeforeDamagingThisEnemy > 0) {
                    damageMultiplier += penetrationCountBeforeDamagingThisEnemy * penetrationDamage.damageMultiplierPerPenetration;
                }
            }
            
            damage = Mathf.RoundToInt(damage * damageMultiplier);
        }

        return damage;
    }

    private int GetBaseDamage() {
        int damage = gameplayConfig.damage;
        int damageRange = Mathf.RoundToInt(damage * 0.1f);
        damage += Random.Range(-damageRange, damageRange);
        damage += Mathf.RoundToInt(GetStatAdjustmentValue(StatAdjustmentType.Damage));
        return damage;
    }

    private float GetDamageMultiplierOnEnemy(Enemy enemy) {
        float damageMultiplier = 1f;
        
        if (enemy.poison.TryGetValue(out var poison)) {
            if (enemy.health >= enemy.data.health * poison.minHealthPercentForMulti) {
                damageMultiplier *= poison.damageMulti;
            }
        }

        return damageMultiplier;
    }

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
        
        Entity damageNumber = SpawnEntity(damageNumberPool, spawnPos, Quaternion.identity, damageNumbersParent);
        damageNumber.textMesh.text = damage.ToString();
        
        const float alpha = 0.68f;
        switch (damageColor) {
            case DamageColor.Normal:
                damageNumber.textMesh.color = styles.normalDamageColor.Alpha(alpha);
                break;
            case DamageColor.Crit:
                damageNumber.textMesh.color = styles.critDamageColor.Alpha(alpha);
                break;
            case DamageColor.Blood:
                damageNumber.textMesh.color = styles.bleedDamageColor.Alpha(alpha);
                break;
            case DamageColor.Poison:
                damageNumber.textMesh.color = styles.poisonDamageColor.Alpha(alpha);
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
    
    // ***************************
    // Exit Portals 
    // ***************************
    
    private Transform activeExitPortal;
    // private Tween exitPortalTween;

    private void InitEarlyExitPortal(Transform exitPortalParent, float timeBeforePortalsSpawn) {
        activeExitPortal = null;
        
        using var autoRelease = ListPool<Transform>.Get(out List<Transform> possibleExitPortals);
        
        foreach (Transform portal in exitPortalParent) {
            portal.gameObject.SetActive(false);
            if (Vector2.Distance(player.position, portal.position) > 5) {
                possibleExitPortals.Add(portal);
            }
        }
        
        possibleExitPortals.Shuffle();
        activeExitPortal = possibleExitPortals[0];
        activeExitPortal.gameObject.SetActive(true);
        activeExitPortal.transform.localScale = Vector3.one * 0.25f;

        // exitPortalTween = Tween.Delay(timeBeforePortalsSpawn, () => {
        //     int randomSpawnIndex = Random.Range(0, exitPortalParent.childCount);
        //     activeExitPortal = exitPortalParent.GetChild(randomSpawnIndex);
        //     activeExitPortal.gameObject.SetActive(true);
        //     Tween.Scale(activeExitPortal, 0f, 1f, 0.5f, Ease.OutBack);
        //     PlayAudioClip(portalSpawnClip, activeExitPortal.position);
        // });
    }

    private Sequence closeExitPortalSequence;
    
    private void OnExitPortalSummoned() {
        closeExitPortalSequence = Sequence.Create();
        closeExitPortalSequence.ChainDelay(gameplayConfig.portalActiveDuration);
        closeExitPortalSequence.ChainCallback(static () => {
            inst.canTakeExitPortal = false;
            Tween.Scale(inst.activeExitPortal, Vector3.zero, 0.25f, Ease.OutCubic);
        });
    }

    private void DespawnEarlyExitPortal() {
        activeExitPortal.GetComponent<Collider2D>().enabled = false;
        PlayAudioClip(portalDespawnClip, activeExitPortal.position);
        
        Sequence sequence = Sequence.Create();
        sequence.Chain(Tween.Scale(activeExitPortal, 1f, 0f, 0.5f, Ease.InBack));
        sequence.ChainCallback(this, static (inst) => {
            inst.activeExitPortal.gameObject.SetActive(false);
            inst.activeExitPortal = null;
        });
    }
    
    private void SpawnFinalExitPortal() {
        for (int i = 0; i < 100; i++) {
            Vector2 randomPos = player.position.ToVector2() + Random.insideUnitCircle * Random.Range(0.5f, 1.5f);
            if (OverlapCircle(randomPos, 0.2f, Masks.StaticLevelMask).Count > 0) continue;
            
            Transform exitPortalParent = loadedMapInst.exitPortalsParent;
            int randomSpawnIndex = Random.Range(0, exitPortalParent.childCount);
            activeExitPortal = exitPortalParent.GetChild(randomSpawnIndex);
            activeExitPortal.gameObject.SetActive(true);
            activeExitPortal.GetComponent<Collider2D>().enabled = true;
            activeExitPortal.position = randomPos;
            Tween.Scale(activeExitPortal, 0f, 1f, 0.5f, Ease.OutBack);
            PlayAudioClip(portalSpawnClip, activeExitPortal.position);
            return;
        }
        
        // This is a fail safe incase we couldn't spawn the final portal
        gameStateMachine.SetState(winExitState);
    }

    // ***************************
    // Spawning Map Items
    // ***************************
    
    private Dictionary<GameObject, InventorySlot[]> deadBodySlotsLookup = new();
    private Dictionary<GameObject, InventorySlot[]> bushSlotsLookup = new();

    private void SpawnResources(Transform resourceSpawnParent) {
        var resourceSpawns = resourceSpawnParent.GetComponentsInChildren<ResourceSpawn>().ToList();
        
        foreach (ResourceSpawn resourceSpawn in resourceSpawns) { 
            GameObject prefab = resourceSpawn.GetPrefabToSpawn();
            if (!prefab) continue;
            
            Entity resourceEntity = SpawnResource<Entity>(prefab, resourceSpawn.transform, 1);

            switch (resourceEntity.gameObject.tag) {
                case Tags.Mineable:
                    resourceEntity.health = 50;
                    break;
                case Tags.DeadBody:
                    InitDeadBody(resourceEntity);
                    break;
                case Tags.Bush:
                    InitBush(resourceEntity); 
                    break;
            }
        } 
        
        if (QuestIsActive(pickPocketQuest)) {
            InventorySlot[] chosenDeadbody = deadBodySlotsLookup.RandomValue();
            for (int i = 0; i < chosenDeadbody.Length; i++) {
                if (chosenDeadbody[i].item == null) {
                    chosenDeadbody[i].item = new() {
                        itemOrInstanceUuid = pickPocketQuest.objectives[1].targetItem.uuid,
                        count = 1,
                        notDiscovered = true,
                    };
                    break;
                }
            }
        }
    }
    
    private T SpawnResource<T>(GameObject resourcePrefab, Transform spawnPoint, int obstacleCellRadius = 0) where T : Entity, new() {
        T resource = SpawnEntity<T>(resourcePrefab, spawnPoint.position, spawnPoint.rotation);
        if (obstacleCellRadius > 0) {
            loadedMapInst.grid.AddObstacle(resource.position, obstacleCellRadius);
            resource.obstacleCellRadius = obstacleCellRadius;
            resource.obstaclePosition = resource.position;
        }
        return resource;
    }
    
    private void InitDeadBody(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
        using var __ = ListPool<InventoryItem>.Get(out var inventoryItems);
            
        int maxDeadBodyItemCount = Random.Range(2, 6);
        GetUniqueItemsFromDropPool(bodyDropPool, maxDeadBodyItemCount, items);
            
        bool spawnEyeUpgrade = RollProbability(loadedMapData.eyeUpgradeOnBodyChance);
        while (spawnEyeUpgrade && items.Count < lootInvetoryPtr.slots.Length) {
            items.Add(GetItemFromDropPool(eyeUpgradesDropPool));
            spawnEyeUpgrade = RollProbability(loadedMapData.eyeUpgradeOnBodyChance);
        }

        foreach (Item item in items) {
            int stackCount = 1;
            float spawnRateTaper = 0f;
            while (RollProbability(item.chanceToSpawnOnBody - spawnRateTaper)) {
                stackCount++;
                spawnRateTaper += item.chanceToSpawnOnBody * 0.15f;
            }
                    
            inventoryItems.Add(new() {
                itemOrInstanceUuid = item.uuid, 
                count = stackCount,
                notDiscovered = true,
            });
        }
            
        deadBodySlotsLookup.Add(entity.gameObject, CreateLootInventoryInstance(inventoryItems));
    }
    
    private void InitBush(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
        using var __ = ListPool<InventoryItem>.Get(out var inventoryItems);
            
        int maxBushItemCount = Random.Range(1, 3);
        GetUniqueItemsFromDropPool(bushesDropPool, maxBushItemCount, items);
            
        foreach (Item item in items) {
            int stackCount = 1;
            float spawnRateTaper = 0f;
            while (RollProbability(item.chanceToSpawnFromBush - spawnRateTaper)) {
                stackCount++;
                spawnRateTaper += item.chanceToSpawnFromBush * 0.15f;
            }
                    
            inventoryItems.Add(new() {
                itemOrInstanceUuid = item.uuid, 
                count = stackCount,
                notDiscovered = true,
            });
        }
            
        bushSlotsLookup.Add(entity.gameObject, CreateLootInventoryInstance(inventoryItems));
    }
    
    private void DestroyLevelEntities() {
        for (int i = entities.Count - 1; i >= 0; i--) {
            if (entities[i].lifetime == EntityLifetime.Level) {
                DestroyEntity(entities[i]);
            }
        }

        deadBodySlotsLookup.Clear();
        enemies.Clear();
        projectiles.Clear();
    }

    // ***************************
    // Saving and Loading
    // ***************************
    
    private string playerInventorySavePath;
    private string stashSavePath;
    private string crucibleSavePath;
    private string hideoutDataSavePath;
    private string raidDataSavePath;
    private string playerSavePath;
    private string questSavePath;
    private string traderSavePath;
    private string traderInventorySavePath;
    private string tutorialSavePath;
    private List<InventoryItem> cachedInventoryForSaving = new(50);
    
    private void BuildSavePaths() {
        playerInventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
        crucibleSavePath = $"{Application.persistentDataPath}/crucible";
        hideoutDataSavePath = $"{Application.persistentDataPath}/hideoutData"; 
        raidDataSavePath = $"{Application.persistentDataPath}/raidStateData";
        playerSavePath = $"{Application.persistentDataPath}/player";
        questSavePath = $"{Application.persistentDataPath}/quests";
        traderSavePath = $"{Application.persistentDataPath}/traders";
        traderInventorySavePath = $"{Application.persistentDataPath}/traderInventory";
        tutorialSavePath = $"{Application.persistentDataPath}/tutorial";
    }

    private string GetInventorySavePath(Inventory inventory) {
        if (inventory == playerInventory) return playerInventorySavePath;
        if (inventory == stashInventory) return stashSavePath;
        if (inventory == crucibleInventory) return crucibleSavePath;
        if (inventory == traderInventory) return traderInventorySavePath;
        Assert.IsTrue(false, "Inventory does not have associated save path");
        return string.Empty;
    }

    private void SaveToFile(string path, object obj) {
        if (obj == null) return;
        BinaryFormatter bf = new();
        using FileStream file = File.Create(path);
        bf.Serialize(file, obj);
    }

    private T LoadFromFileOrCreateNew<T>(string path) where T : class, new() {
        return LoadFromFile<T>(path) ?? new T();
    }

    private T LoadFromFile<T>(string path) where T : class {
        if (File.Exists(path)) {
            BinaryFormatter bf = new();
            using FileStream file = File.Open(path, FileMode.Open);
            return (T)bf.Deserialize(file);
        }
        return null;
    }
    
    private int GenerateNewItemUuid() {
        int newItemId = UuidScriptableObject.GetIntUuid();
        while (resourceLookup.ContainsKey(newItemId)) {
            newItemId = UuidScriptableObject.GetIntUuid();
        }
        return newItemId;
    }
    
    public static Dictionary<int, UuidScriptableObject> resourceLookup = new();
    public List<Item> allItems = new();
    public List<Augment> allAugments = new();
    
    private void LoadAllResources() {
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        foreach (UuidScriptableObject res in resourceObjects) {
            resourceLookup.Add(res.uuid, res);
            if (res is Item item) {
                allItems.Add(item);
            }
            if (res is Augment augment) {
                allAugments.Add(augment);
            }
        }
    }

    [Serializable]
    private class PlayerSaveData {
        public int health;
        public int crucibleLevel;
        public int soulCurrency;
        public int coinCurrency;
        
        public int hasteSkillLevel;
        public int intellectSkillLevel;
        public int lifeBloodSkillLevel;
        public int strengthSkillLevel;
    }

    private void SavePlayerData() {
        PlayerSaveData data = new() {
            health = player.health,
            crucibleLevel = player.crucibleLevel,
            soulCurrency = player.soulCurrency,
            coinCurrency = player.coinCurrency,
            hasteSkillLevel = player.hasteSkillLevel,
            intellectSkillLevel = player.intellectSkillLevel,
            lifeBloodSkillLevel = player.lifeBloodSkillLevel,
            strengthSkillLevel = player.strengthSkillLevel,
        };
        SaveToFile(playerSavePath, data);
    }

    private void LoadAndAssignPlayerSaveData(Player instancedPlayer) {
        PlayerSaveData data = LoadFromFile<PlayerSaveData>(playerSavePath);
        if (data != null) {
            instancedPlayer.health = data.health;
            instancedPlayer.crucibleLevel = data.crucibleLevel;
            instancedPlayer.soulCurrency = data.soulCurrency;
            instancedPlayer.coinCurrency = data.coinCurrency;
            instancedPlayer.hasteSkillLevel = data.hasteSkillLevel;
            instancedPlayer.intellectSkillLevel = data.intellectSkillLevel;
            instancedPlayer.lifeBloodSkillLevel = data.lifeBloodSkillLevel;
            instancedPlayer.strengthSkillLevel = data.strengthSkillLevel;
        }
        
        // We want to make sure that the player health is never <= zero
        instancedPlayer.health = player.health <= 0f ? gameplayConfig.postDeathStartingHealth : player.health;
    }

    // ************************************
    // UI 
    // ************************************

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
        HideItemDescPopup(); 
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
        HideItemDescPopup(); 
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

            if (inTutorial) {
                customQuestEvent.Invoke("MetTraderInForge");
            }
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
        
        forgeEyeButton.AddListener(() => {
            if (PlayingForgeAnimation) return;
            
            int eyeSlotIndex = 0;
            InventoryItem eyeItem = null;

            for (int i = 0; i < crucibleInventory.slots.Length; i++) {
                InventorySlot slot = crucibleInventory.slots[i];
                if (slot.ui.OnlyAcceptsType(eyeType)) {
                    eyeItem = slot.item;
                    eyeSlotIndex = i;
                }
            }

            if (eyeItem == null) return;

            for (int i = 0; i < crucibleInventory.slots.Length; i++) {
                if (i == eyeSlotIndex) continue;
                if (crucibleInventory.slots[i].item != null) break;
                if (i == crucibleInventory.slots.Length - 1) return;
            }

            toggledOffHoverableUIElement = forgeEyeButton.rectTransform;
            
            forgeEyeButton.KeepPressed();
            string prevButtonText = forgeEyeButton.text.text;
            forgeEyeButton.text.text = "Forging...";
            
            DoEyeForgeAnimation(() => {
                forgeEyeButton.StopKeepPressed();
                forgeEyeButton.text.text = prevButtonText;
                
                InventoryItem newDemonEyeItem = new() {
                    nestedUuids = new(),
                };

                foreach (InventorySlot slot in crucibleInventory.slots) {
                    slot.ui.itemUI.rectTransform.anchoredPosition = Vector2.zero;
                    slot.ui.itemUI.rectTransform.localScale = Vector3.one;
                    
                    if (slot.item == null) continue;
                    
                    if (slot.ui.OnlyAcceptsType(eyeModifierType)) {
                        newDemonEyeItem.nestedUuids.Add(slot.item.ItemRef.uuid);
                    }
                    slot.item = null;
                }
                
                int? additionalAugmentUuid = null;
                foreach (Augment augment in allAugments) {
                    if (augment.MeetsRequirements(newDemonEyeItem.nestedUuids)) {
                        additionalAugmentUuid = augment.uuid;
                        break;
                    }
                }

                if (additionalAugmentUuid.HasValue) {
                    newDemonEyeItem.nestedUuids.Add(additionalAugmentUuid.Value);
                }

                DemonEyeInstance newDemonEye = BuildAndRegisterEye(newDemonEyeItem);
                crucibleInventory.slots[eyeSlotIndex].item = newDemonEyeItem;
            });
        });
        
        upgradeForgeButton.AddListener(() => {
            toggledOffHoverableUIElement = upgradeForgeButton.rectTransform;
            
            UpgradePath.UpgradeRequirements requirements = crucibleUpgradePath.pathUpgrades[player.crucibleLevel];
            
            bool canUpgrade = true;
            foreach (UpgradePath.Requirement requirement in requirements.requirements) {
                if (MeetsSingleUpgradeRequirement(requirement)) continue;
                canUpgrade = false;
                break;
            }
        
            if (!canUpgrade) return;
            
            foreach (UpgradePath.Requirement requirement in requirements.requirements) {
                int stashRemoveCount = RemoveNumberOfItemsFromInventory(stashInventory, requirement.item, requirement.count);
                if (stashRemoveCount == requirement.count) continue;
                RemoveNumberOfItemsFromInventory(playerInventory, requirement.item, requirement.count - stashRemoveCount);
            }
            
            player.crucibleLevel++;
            SavePlayerData();
            
            upgradeForgeButton.KeepPressed();
            string prevButtonText = upgradeForgeButton.text.text;
            upgradeForgeButton.text.text = "Upgrading...";
            
            DoForgeUpgradeAnimation(() => {
                upgradeForgeButton.StopKeepPressed();
                upgradeForgeButton.text.text = prevButtonText;
            });
        });
        
        transactionPanel.sellButton.AddListener(() => { 
            if (transactionState == TransactionState.Selling && GetInventoryItemCount(transactionInventory) <= 0) return;
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            // Before selling items we pass the transaction inventory to callbacks that want to know what we sold
            onSoldItemsToTrader?.Invoke(transactionInventory.slots); 
            player.coinCurrency += sellPrice;
            ClearInventory(transactionInventory);
        });
        
        transactionPanel.moneyPurchaseButton.AddListener(() => {
            if (transactionState == TransactionState.Buying && curTradingItem == null) return;
            int buyPrice = curTradingItem.ItemRef.buyPrice;
            if (player.coinCurrency >= buyPrice) {
                player.coinCurrency -= buyPrice;
                TryAddItemToInventory(stashInventory, curTradingItem.ItemRef, 1);
                ReduceTradingItemStock();
                // After buying items we just make sure all items in stash are no longer trader owned
                ClearItemsAsTraderOwned(stashInventory);
            }
        });
        
        transactionPanel.barterPurchaseButton.AddListener(() => {
            if (curTradingItem == null) return;

            foreach (ItemWithCount barterReq in curTradingItem.ItemRef.barterRequirements) {
                if (GetOwnedCountOfItem(barterReq.item) < barterReq.count) return;
            }
            
            foreach (ItemWithCount barterReq in curTradingItem.ItemRef.barterRequirements) {
                int removedCount = RemoveNumberOfItemsFromInventory(stashInventory, barterReq.item, barterReq.count);
                if (removedCount != barterReq.count) {
                    int additionalRemoveCount = barterReq.count - removedCount;
                    RemoveNumberOfItemsFromInventory(playerInventory, barterReq.item, additionalRemoveCount);
                }
            }

            TryAddItemToInventory(stashInventory, curTradingItem.ItemRef, 1);
            ReduceTradingItemStock();
        });
        
        transactionPanel.buyToggle.AddListener(() => {
            transactionState = TransactionState.Buying;
            traderTransactionInventoryParent.gameObject.SetActive(false);
            // Move any selling items back to stash
            foreach (InventorySlot slot in transactionInventory.slots) {
                if (slot.item == null) continue;
                TryAddItemToInventory(stashInventory, slot.item);
            }
            ClearInventory(transactionInventory);
        });
        
        transactionPanel.sellToggle.AddListener(() => {
            transactionState = TransactionState.Selling;
            traderTransactionInventoryParent.gameObject.SetActive(true);
            SetTradingItem(null);
        });

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

    private Sequence eyeForgeSequence;
    private bool PlayingForgeAnimation => eyeForgeSequence.isAlive;
    
    private void DoEyeForgeAnimation(Action onAnimationEndCallback) {
        const float fillDuration = 4.5f;
        const float perUpgradeExplosionDelay = 0.15f;
        const float popOutDuration = 0.15f;

        float upgradeExplosionsDuration = perUpgradeExplosionDelay * (GetInventoryItemCount(crucibleInventory) - 1);
        float totalAnimationDuration = fillDuration + upgradeExplosionsDuration + popOutDuration;

        Tween.Custom(this, 0f, 1f, fillDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.pentagramFillCurve.Evaluate(val));
        });
        
        Tween.Custom(this, 1f, 0f, fillDuration * 0.3f, startDelay: totalAnimationDuration, ease: Ease.Linear, onValueChange: (target, val) => {
            target.SetPentagramFill(target.pentagramFillCurve.Evaluate(val));
        });

        for (int i = 0; i < crucibleInventory.slots.Length; i++) {
            InventorySlot slot = crucibleInventory.slots[i];
            if (slot.item == null) continue;

            RectTransform rectTransform = slot.ui.itemUI.rectTransform;

            // Use our own shake because prime tween shake's curve does not work
            rectTransform.DoTweenShake(10f, 3.3f, totalAnimationDuration, itemShakeCurve);

            Sequence sequence = Sequence.Create();

            if (slot.item.ItemRef.type == eyeType) {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 1.25f, new() {
                    duration = fillDuration,
                    ease = Ease.InCubic,
                }));
                
                sequence.ChainDelay(upgradeExplosionsDuration + perUpgradeExplosionDelay);
                
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 1.35f, Vector3.one, new() {
                    duration = popOutDuration,
                    ease = Ease.InOutBounce,
                }));
                
                sequence.Group(Tween.Delay(0f, () => {
                    Entity forgeExplosion = SpawnEntity(forgeExplosionPool, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel, Vector3.one * 0.05f, 1f, 15f);
                }));
                
                eyeForgeSequence = sequence;
                eyeForgeSequence.OnComplete(onAnimationEndCallback);
            }
            else {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 0.87f, new() {
                    duration = fillDuration,
                    ease = Ease.InCubic,
                }));
                
                sequence.ChainDelay(perUpgradeExplosionDelay * i);
                
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 0.87f, Vector3.zero, new() {
                    duration = popOutDuration,
                    ease = Ease.InBounce,
                }));
                
                sequence.Group(Tween.Delay(popOutDuration / 2f, () => {
                    Entity forgeExplosion = SpawnEntity(forgeExplosionPool, slot.ui.rectTransform.position, Quaternion.identity, eyeForgePanel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                    Tween.PunchScale(eyeForgePanel, Vector3.one * 0.04f, 0.15f, 15f);
                }));
            }
        }
    }
    
    private int fillParamProperty = Shader.PropertyToID("_Fill");
    
    private void SetPentagramFill(float value) {
        pentagramFillImage.material.SetFloat(fillParamProperty, value);
    }
    
    private bool playingForgeUpgradeAnimation;
    
    private void DoForgeUpgradeAnimation(Action onAnimationEndCallback) {
        playingForgeUpgradeAnimation = true;
        
        const float explosionDelay = 0.1f;
        
        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        
        for (int i = 1; i < crucibleInventory.slots.Length; i++) {
            RectTransform slotTransform = crucibleInventory.slots[i].ui.rectTransform;
            sequence.Group(Tween.Scale(slotTransform, Vector3.one, Vector3.zero, 0.15f, Ease.InOutBounce, startDelay: explosionDelay * i));
            sequence.Group(Tween.PunchScale(eyeForgePanel, Vector3.one * 0.05f, 0.1f, 15f, startDelay: explosionDelay * i));
            sequence.Group(Tween.Delay(0.1f * i, () => {
                Entity forgeExplosion = SpawnEntity(forgeDustExplosionPool, OffsetY(slotTransform.position, 10f), Quaternion.identity, eyeForgePanel);
                DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
            }));
        }
        
        sequence.ChainDelay(0.25f);
        
        sequence.ChainCallback(() => {
            ChangeInventorySize(crucibleInventory, crucibleInventory.slots.Length + 1);
            SetupEyeCrucibleInventorySlots();
            crucibleInventory.slots[^1].ui.rectTransform.localScale = Vector3.zero;
            
            for (int i = 1; i < crucibleInventory.slots.Length; i++) {
                RectTransform slotTransform = crucibleInventory.slots[i].ui.rectTransform;
                Tween.Scale(slotTransform, Vector3.zero, Vector3.one, 0.15f, Ease.InOutBounce, startDelay: explosionDelay * i);
                Tween.PunchScale(eyeForgePanel, Vector3.one * 0.05f, 0.1f, 15f, startDelay: explosionDelay * i);
                Tween.Delay(explosionDelay * i, () => {
                    Entity forgeExplosion = SpawnEntity(forgeDustExplosionPool, OffsetY(slotTransform.position, 10f), Quaternion.identity, eyeForgePanel);
                    DestroyEntity(forgeExplosion, CurrentClipLength(forgeExplosion.animator));
                });
                
                if (i == crucibleInventory.slots.Length - 1) {
                    const float additionalCompletionDelay = 0.15f;
                    Tween.Delay(this, explosionDelay * i + additionalCompletionDelay, (inst) => {
                        inst.playingForgeUpgradeAnimation = false;
                        onAnimationEndCallback?.Invoke();
                    });
                }
            }
        });
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


    private enum CrucibleMode { Empty, Forging, ForgingButJustEye, ForgingButWithoutEye, NeedToRemoveDemonEye }
    private CrucibleMode crucibleMode;

    private void UpdateCrucibleState() {
        if (!OnEyeForgeTab) return;

        if (playingForgeUpgradeAnimation) {
            if (!forgeEyeButton.isDisabled) {
                forgeEyeButton.Disable();
            }
            return;
        }

        int crucibleItemCount = GetInventoryItemCount(crucibleInventory);
        InventoryItem eyeSlotItem = crucibleInventory.slots[0].item;

        if (crucibleItemCount <= 0) {
            crucibleMode = CrucibleMode.Empty;
        }
        else if (eyeSlotItem != null && eyeSlotItem.ItemRef.type == demonEyeType) {
            crucibleMode = CrucibleMode.NeedToRemoveDemonEye;
        }
        else if (eyeSlotItem != null && crucibleItemCount == 1) {
            crucibleMode = CrucibleMode.ForgingButJustEye;
        }
        else if (eyeSlotItem == null) {
            crucibleMode = CrucibleMode.ForgingButWithoutEye;
        }
        else {
            crucibleMode = CrucibleMode.Forging;
        }
    }

    private void UpdateCrucibleInfoPanel() {
        if (!OnEyeForgeTab) return;
        
        bool forging = crucibleMode is CrucibleMode.Forging;
        if (forging && forgeEyeButton.isDisabled) {
            forgeEyeButton.Enable();
        }
        else if (!forging && !forgeEyeButton.isDisabled) {
            forgeEyeButton.Disable();
        }

        if (PlayingForgeAnimation) return;

        bool showUpgradeScreen = crucibleMode is CrucibleMode.Empty or CrucibleMode.NeedToRemoveDemonEye;
        forgeDetailsUpgradeScreen.gameObject.SetActive(showUpgradeScreen);
        forgeDetailsForgeScreen.gameObject.SetActive(!showUpgradeScreen);
        
        if (showUpgradeScreen) {
            bool meetsAllUpgradeRequirements = true;
            List<UpgradePath.Requirement> curUpgradeReqs = crucibleUpgradePath.pathUpgrades[player.crucibleLevel].requirements;
            
            for (int i = 0; i < forgeDetailsResourceRequirements.Count; i++) {
                if (!curUpgradeReqs.IndexInRange(i)) {
                    forgeDetailsResourceRequirements[i].gameObject.SetActive(false);
                    continue;
                }
                forgeDetailsResourceRequirements[i].gameObject.SetActive(true);
                
                UpgradePath.Requirement req = curUpgradeReqs[i];
                int ownedCount = GetOwnedCountOfItem(req.item);
                forgeDetailsResourceRequirements[i].Set(req.item, req.count, ownedCount);

                if (ownedCount < req.count) {
                    meetsAllUpgradeRequirements = false;
                }
            }

            if (upgradeForgeButton.isDisabled && meetsAllUpgradeRequirements) {
                upgradeForgeButton.Enable();
            }
            else if (!upgradeForgeButton.isDisabled && !meetsAllUpgradeRequirements) {
                upgradeForgeButton.Disable();
            }
            
            return;
        }
        
        if (crucibleMode == CrucibleMode.Empty) {
            forgeDetailsForgeText.text = "Place an eyeball in the center to start the Demon Eye forging process";
        }
        else if (crucibleMode == CrucibleMode.ForgingButJustEye) {
            forgeDetailsForgeText.text = $"Requires at least {DisplayNumber(1)} eye upgrade to forge a Demon Eye";
        }
        else if (crucibleMode == CrucibleMode.ForgingButWithoutEye) {
            forgeDetailsForgeText.text = $"Missing eyeball in the center";
        }
        else {
            int eyeUpgradeCount = GetInventoryItemCount(crucibleInventory) - 1;
            int totalUpgradeCount = crucibleInventory.slots.Length - 1;
            forgeDetailsForgeText.text = $"<size=90%>Previewing Upgrades {ColorText(eyeUpgradeCount.ToString(), styles.timeDescColor)}/{totalUpgradeCount}</size><line-height=150%>\n";
            
            Dictionary<ModifierItem, int> allSoulCards = new();
            
            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.item == null || slot.item.ItemRef.type != eyeModifierType) continue;    
                ModifierItem modifierItem = resourceLookup[slot.item.itemOrInstanceUuid] as ModifierItem;
                if (!allSoulCards.TryAdd(modifierItem, 1)) {
                    allSoulCards[modifierItem]++;
                }
            }

            List<(ModifierItem, int)> sortedSoulcards = SortModsFromDictionary(allSoulCards);
            
            string eyeDescription = "";
            foreach ((ModifierItem soulcard, int count) in sortedSoulcards) {
                eyeDescription += GetDemonEyeModDescription(soulcard, count);
            }
            forgeDetailsForgeText.text += eyeDescription;
        }
    }

    private InventoryItem curTradingItem;
    
    private void SetTradingItem(InventoryItem item) {
        curTradingItem = item;
        transactionPanel.UpdateBuyItem(item);
        if (curTradingItem != null && transactionState == TransactionState.Selling) {
            transactionPanel.toggleGroup.ManualyToggle(transactionPanel.buyToggle);
        }
    }

    private void ReduceTradingItemStock() {
        bool reducedToNothing = ReduceItemCountInInventory(traderInventory, curTradingItem.traderSlotIndex);
        if (reducedToNothing) {
            SetTradingItem(null);
        }
    }

    private enum TransactionState { Selling, Buying }
    private TransactionState transactionState;
    
    private void UpdateTraderTransactionState() {
        if (!OnTradingTab) return;
        RefreshTransactionUI();
    }
    
    private void RefreshTransactionUI() {
        if (transactionState == TransactionState.Buying) {
            transactionPanel.UpdateBuyItem(curTradingItem);
            transactionPanel.toggleGroup.ManualyToggleCosmetically(transactionPanel.buyToggle);
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            transactionPanel.UpdateSellPrice(sellPrice);
            transactionPanel.toggleGroup.ManualyToggleCosmetically(transactionPanel.sellToggle);
        }
    }
    
    // ************************
    // Traders
    // ************************

    private const float traderItemRefreshTime = 480f;
    
    [Serializable]
    public class TradersSaveData {
        public int traderRep;
        public float refreshItemsTime;
    }
    
    private TradersSaveData traderSaveData;

    private void SaveTrader() {
        SaveToFile(traderSavePath, traderSaveData);
        SaveInventory(traderInventory);
    }

    private void InitTrader() {
        traderSaveData = LoadFromFileOrCreateNew<TradersSaveData>(traderSavePath);
        MarkTraderItemsAsTraderOwned();
        SetTraderRepBar();
    }

    private void UpdateTrader() {
        traderSaveData.refreshItemsTime -= Time.deltaTime;
        
        if (OnTradingTab) {
            traderItemRefreshTimeText.text = $"Items Refresh In: {GetCountdownText(traderSaveData.refreshItemsTime)}";
        }
        
        if (traderSaveData.refreshItemsTime <= 0f) {
            ClearTraderItemsFromTransactionInventory();
            FillTraderInventoryWithItems();
            traderSaveData.refreshItemsTime = traderItemRefreshTime;
            SaveTrader();
        }
    }
    
    private void IncreaseTraderRep(int repGain) {
        if (ReachedTraderMaxRep()) return;

        int prevLevel = GetTraderRepLevel();
        traderSaveData.traderRep += repGain;
        SaveTrader();
        int repLevel = GetTraderRepLevel();
        bool increasedLevel = prevLevel < repLevel;
        
        if (increasedLevel) {
            FillTraderInventoryWithItems();
        }
        SetTraderRepBar();
    }

    private void SetTraderRepBar() {
        int levelIndex = GetTraderRepLevel();
        
        if (ReachedTraderMaxRep()) {
            traderXpLevelFill.fillAmount = 1f;
            traderRemainingXpText.text = string.Empty;
            traderLevelText.text = $"Level {levelIndex} (Max)";
            return;
        }
        
        int prefixedSumAtCurLevel = traderLevels.prefixedSumRepForLevel[levelIndex];
        int prefixedSumAtPrevLevel = traderLevels.prefixedSumRepForLevel[levelIndex - 1];
        int repNeededForThisLevel = prefixedSumAtCurLevel - prefixedSumAtPrevLevel;

        int traderRep = traderSaveData.traderRep;
        int repCompletedAtCurLevel = traderRep - prefixedSumAtPrevLevel;
        int repLeftToGo = prefixedSumAtCurLevel - traderRep;
        
        traderXpLevelFill.fillAmount = repCompletedAtCurLevel / (float)repNeededForThisLevel;
        traderRemainingXpText.text = $"{repLeftToGo} Rep Left";
        traderLevelText.text = $"Level {levelIndex}";
    }

    private int GetTraderRepLevel() {
        int rep = traderSaveData.traderRep;
        for (int i = 0; i < traderLevels.prefixedSumRepForLevel.Length; i++) {
            if (rep < traderLevels.prefixedSumRepForLevel[i]) {
                return i;
            }
        }
        return traderLevels.prefixedSumRepForLevel.Length;
    }

    private void FillTraderInventoryWithItems() {
        ClearInventory(traderInventory);
        int curTraderLevel = GetTraderRepLevel();
        
        float raritySkew = curTraderLevel switch { 
            0 => 0f, 
            1 => 0.20f, 
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
        
        using var _ = ListPool<Item>.Get(out List<Item> items);
        GetUniqueItemsFromDropPool(traderDropPool, traderInventoryColCount * traderInventoryRowCount, items, raritySkew);
        items = items.OrderBy(x => x.type.name).ThenBy(x => x.GetRarity()).ThenBy(x => x.buyPrice).ToList();
        
        foreach (Item item in items) {
            if (item.traderLevelRequired > curTraderLevel) continue;
            
            int lowerRange = item.traderStockRange.x;
            int maxUpperRange = item.traderStockRange.y;
            int weightedUpperRange = lowerRange + ((maxUpperRange - lowerRange) / 2);
            while (weightedUpperRange < maxUpperRange && RollProbability(stockCountSkew)) {
                weightedUpperRange++;
            }
            int stackCount = Random.Range(lowerRange, weightedUpperRange); 
            TryAddItemToInventory(traderInventory, item, stackCount);
        }
        
        MarkTraderItemsAsTraderOwned();
    }
    
    private void MarkTraderItemsAsTraderOwned() {
        for (int i = 0; i < traderInventory.slots.Length; i++) {
            InventorySlot slot = traderInventory.slots[i];
            if (slot.item == null) continue;
            slot.item.traderOwned = true;
            slot.item.traderSlotIndex = i;
        }
    }

    private void ClearItemsAsTraderOwned(Inventory inventory) {
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            slot.item.traderOwned = false;
            slot.item.traderSlotIndex = -1;
        }
    }
    
    private void ClearTraderItemsFromTransactionInventory() {
        SetTradingItem(null);
        return;
        for (int i = 0; i < transactionInventory.slots.Length; i++) {
            InventorySlot slot = transactionInventory.slots[i];
            if (slot.item == null || !slot.item.traderOwned) continue;
            RemoveItemFromInventory(transactionInventory, i);
        }
    }
    
    private bool ReachedTraderMaxRep() {
        int rep = traderSaveData.traderRep;
        return rep >= traderLevels.prefixedSumRepForLevel[^1];
    }
    
    // ************************
    // Quests 
    // ************************
    
    private class QuestPackage {
        public QuestGraphRuntime.Node questNode;
        public QuestUI questUI;
        public ToggleButton questToggleButton;

        public void RefreshDisplay() => questUI.Display(questNode.curQuest);
    }

    private Queue<QuestPackage> reservedQuestPackages = new();
    private List<QuestPackage> activeQuestPackages = new();
    
    private QuestPackage presentingQuestPackage;

    [Serializable]
    private class QuestSaveData {
        public Quest.ProgressSave[] progressSaves;
        public bool[] submissionStates;
    }
    
    private QuestSaveData questSaveData;
    
    private void SaveActiveQuestProgresses() {
        foreach (QuestPackage questPackage in activeQuestPackages) {
            QuestGraphRuntime.Node node = questPackage.questNode;
            questSaveData.progressSaves[node.saveIndex] = node.curQuest.GetProgressSave();
        }
        SaveToFile(questSavePath, questSaveData);
    }

    private void SaveAndMarkQuestAsSubmitted(QuestGraphRuntime.Node questNode) {
        questSaveData.submissionStates[questNode.saveIndex] = true;
        questSaveData.progressSaves[questNode.saveIndex] = questNode.curQuest.GetProgressSave();
        SaveToFile(questSavePath, questSaveData);
    }

    private void InitQuests() {
        questSaveData = LoadFromFile<QuestSaveData>(questSavePath);
        
        if (questSaveData == null) {
            questSaveData = new() {
                progressSaves = new Quest.ProgressSave[questGraph.questCount],
                submissionStates = new bool[questGraph.questCount],
            };
            questSaveData.progressSaves.InitalizeWithDefault();
            SaveToFile(questSavePath, questSaveData);
        }
        
        HashSet<QuestGraphRuntime.Node> initialQuestNodes = new();
        foreach (QuestGraphRuntime.Node node in questGraph.rootNode.nextNodes) {
            FindStartingQuestNodes(initialQuestNodes, node);
        }
        
        const int questUiPoolSize = 6;
        for (int i = 0; i < questUiPoolSize; i++) {
            ReleaseQuestPackage(CreateQuestPackage());
        }
        
        foreach (QuestGraphRuntime.Node questNode in initialQuestNodes) {
            Quest.ProgressSave progressSave = questSaveData.progressSaves[questNode.saveIndex];
            questNode.curQuest.LoadProgressSave(progressSave);
            ActivateQuest(questNode); 
        }
        
        RefreshQuestDisplays();
    }

    private void UpdateQuests() {
        foreach (QuestPackage questPackage in activeQuestPackages) {
            questPackage.questNode.curQuest.Update();
        }
    }
    
    private void FindStartingQuestNodes(HashSet<QuestGraphRuntime.Node> nodes, QuestGraphRuntime.Node curNode) {
        bool questHasBeenSubmitted = questSaveData.submissionStates[curNode.saveIndex];
        
        if (!questHasBeenSubmitted) {
            nodes.Add(curNode);
            return;
        }
        
        foreach (QuestGraphRuntime.Node nextNode in curNode.nextNodes) {
            FindStartingQuestNodes(nodes, nextNode);
        }
    }

    private void RefreshQuestDisplays() {
        if (activeQuestPackages.Count <= 0) return;

        if (presentingQuestPackage == null || presentingQuestPackage.questNode == null) {
            presentingQuestPackage = activeQuestPackages[0];
            questToggleButtonGroup.ManualyToggle(presentingQuestPackage.questToggleButton);
        }
        
        foreach (QuestPackage questPackage in activeQuestPackages) {
            questPackage.questUI.gameObject.SetActive(false);
        }
        
        presentingQuestPackage.questUI.gameObject.SetActive(true);
        presentingQuestPackage.RefreshDisplay();
    }

    private void ActivateQuest(QuestGraphRuntime.Node questNode) {
        questNode.curQuest.Init();
        QuestPackage questPackage = GetQuestPackage();
        questPackage.questNode = questNode;
        questPackage.questToggleButton.gameObject.SetActive(true);
        questPackage.questToggleButton.text.text = questNode.curQuest.title;
        activeQuestPackages.Add(questPackage);
    }

    private void DeactivateQuest(QuestPackage questPackage) {
        questPackage.questNode.curQuest.Deinit();
        activeQuestPackages.Remove(questPackage);
        ReleaseQuestPackage(questPackage);
    }

    private QuestPackage GetQuestPackage() {
        return reservedQuestPackages.TryDequeue(out QuestPackage reserved) ? reserved : CreateQuestPackage();
    }
    
    private void ReleaseQuestPackage(QuestPackage package) {
        package.questUI.gameObject.SetActive(false);
        package.questToggleButton.gameObject.SetActive(false);
        package.questNode = null;
        reservedQuestPackages.Enqueue(package);
    }
    
    private QuestPackage CreateQuestPackage() {
        QuestUI ui = Instantiate(questPrefab, questsParent).GetComponent<QuestUI>();
        
        ToggleButton toggle = Instantiate(questSelectionTogglePrefab, questSelectionParent).GetComponent<ToggleButton>();
        questToggleButtonGroup.Add(toggle);
        
        QuestPackage questPackage = new() {
            questNode = null,
            questUI = ui,
            questToggleButton = toggle,
        };
            
        toggle.button.onClick.AddListener(() => OnQuestToggleClicked(questPackage));
        ui.completeButton.AddListener(() => OnQuestCompleteClicked(questPackage));
        
        return questPackage;
    }
    
    private void OnQuestToggleClicked(QuestPackage questPackage) {
        presentingQuestPackage = questPackage;
        RefreshQuestDisplays();
    }

    private void OnQuestCompleteClicked(QuestPackage questPackage) {
        QuestGraphRuntime.Node compQuestNode = questPackage.questNode;
        IncreaseTraderRep(compQuestNode.curQuest.traderReputationReward);
        SaveAndMarkQuestAsSubmitted(compQuestNode);
        
        foreach (Quest.Objective objective in questPackage.questNode.curQuest.objectives) {
            if (objective.type == Quest.Objective.Type.Fetch && !objective.keepFetchedItems) {
                RemoveNumberOfOwnedItems(objective.targetItem, objective.targetValue);
                SaveInventory(playerInventory);
                SaveInventory(stashInventory);
            }    
        }
        
        if (questPackage.questNode.nextNodes != null) {
            foreach (QuestGraphRuntime.Node nextQuestNode in compQuestNode.nextNodes) {
                bool questHasBeenSubmitted = questSaveData.submissionStates[nextQuestNode.saveIndex];
                if (questHasBeenSubmitted || QuestIsActive(nextQuestNode.curQuest)) continue;
                ActivateQuest(nextQuestNode);
            }
        }
        
        DeactivateQuest(questPackage); 
        RefreshQuestDisplays();
    }
    
    private bool QuestIsActive(Quest quest) {
        foreach (QuestPackage activeQuestPackage in activeQuestPackages) {
            if (quest == activeQuestPackage.questNode.curQuest && !quest.IsComplete()) {
                return true;
            }
        } 
        return false;
    }

    // ************************
    // Leveling Skills
    // ************************

    private void InitSkillsPanel() {
        skillsPanel.hasteSkillRow.Init(hasteUpgradePath.MaxLevel, 
            $"{DisplayIncrease(gameplayConfig.movementSpeedIncPerLevel)} Movement Speed\n" +
            $"{DisplayIncrease(gameplayConfig.lootingSpeedIncPerLevel)} Looting Speed\n" +
            $"{DisplayIncrease(gameplayConfig.firerateIncPerLevel)} Firerate"
        );
        skillsPanel.intellectSkillRow.Init(intellectUpgradePath.MaxLevel, 
            $"{DisplayIncrease(gameplayConfig.critChanceIncPerLevel)} Critical Strike Chance\n" +
            $"{DisplayIncrease(gameplayConfig.critMultiplierIncPerLevel)} Critical Strike Multiplier\n" +
            $"{DisplayIncrease(gameplayConfig.damageIncPerLevel)} Damage"
        );
        skillsPanel.lifeBloodSkillRow.Init(lifeBloodUpgradePath.MaxLevel, 
            $"{DisplayIncrease(gameplayConfig.healthIncPerLevel)} Health\n" +
            $"{DisplayIncrease(gameplayConfig.healingSpeedIncPerLevel)} Healing Speed\n" +
            $"{DisplayIncrease(gameplayConfig.healingIncPerLevel)} Healing Amount"
        );
        skillsPanel.strengthSkillRow.Init(strengthUpgradePath.MaxLevel, 
            $"{DisplayIncrease(gameplayConfig.carryCapacityIncPerLevel)} Carry Capacity\n" +
            $"{DisplayIncrease(gameplayConfig.projectileCountIncPerLevel)} Projectile Count"
        );
    }

    private void OnLevelupButtonPressed(SkillUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeSkill(upgradePath, playerStatLevel);
        if (result == UpgradeStatResult.CantAfford || result == UpgradeStatResult.AtMaxLevel) return;
        
        player.soulCurrency -= upgradePath.soulsNeededPerLevel[playerStatLevel];

        if (upgradePath == hasteUpgradePath) {
            player.hasteSkillLevel++;
        }
        else if (upgradePath == intellectUpgradePath) {
            player.intellectSkillLevel++;
        }
        else if (upgradePath == lifeBloodUpgradePath) {
            int prevFullPlayerHealth = FullPlayerHealth;
            player.lifeBloodSkillLevel++;
            int newFullPlayerHealth = FullPlayerHealth;
            player.health += newFullPlayerHealth - prevFullPlayerHealth;
        }
        else if (upgradePath == strengthUpgradePath) {
            player.strengthSkillLevel++;
        }
        
        SavePlayerData();
        RefreshSkillsPanel();
    }
    
    private void RefreshSkillsPanel() {
        playerStatsPanel.carryCapacityRow.statValueText.text = DisplayIncrease(GetPlayerStat(Player.Stat.CarryCapacity));
        playerStatsPanel.critChanceRow.statValueText.text = DisplayProbIncrease(GetPlayerStat(Player.Stat.CritChance));
        playerStatsPanel.critMultiRow.statValueText.text = DisplayIncrease(GetPlayerStat(Player.Stat.CritMulti));
        playerStatsPanel.damageRow.statValueText.text = DisplayIncrease(GetPlayerStat(Player.Stat.Damage));
        playerStatsPanel.firerateRow.statValueText.text = DisplayProbIncrease(GetPlayerStat(Player.Stat.Firerate));
        playerStatsPanel.healthRow.statValueText.text = DisplayIncrease(GetPlayerStat(Player.Stat.Health));
        playerStatsPanel.healingAmountRow.statValueText.text = DisplayIncrease(GetPlayerStat(Player.Stat.HealingAmount));
        playerStatsPanel.healingSpeedRow.statValueText.text = DisplayProbIncrease(GetPlayerStat(Player.Stat.HealingSpeed));
        playerStatsPanel.lootingSpeedRow.statValueText.text = DisplayProbIncrease(GetPlayerStat(Player.Stat.LootingSpeed));
        playerStatsPanel.movementSpeedRow.statValueText.text = DisplayProbIncrease(GetPlayerStat(Player.Stat.MovementSpeedPercentage));
        playerStatsPanel.projectileCountRow.statValueText.text = DisplayIncrease(GetPlayerStat(Player.Stat.ProjectileCount));
        
        RefreshSkillRow(skillsPanel.hasteSkillRow, hasteUpgradePath, player.hasteSkillLevel);
        RefreshSkillRow(skillsPanel.intellectSkillRow, intellectUpgradePath, player.intellectSkillLevel);
        RefreshSkillRow(skillsPanel.lifeBloodSkillRow, lifeBloodUpgradePath, player.lifeBloodSkillLevel);
        RefreshSkillRow(skillsPanel.strengthSkillRow, strengthUpgradePath, player.strengthSkillLevel);
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
        if (player.soulCurrency >= upgradePath.soulsNeededPerLevel[playerSkillLevel]) {
            return UpgradeStatResult.Affordable;    
        }
        return UpgradeStatResult.CantAfford;
    }
    
    // ************************
    // Audio
    // ************************
    
    private Dictionary<int, List<DynamicClipRecord>> clipRecords = new(50);
    private Queue<AudioSource> sources;
    
    private struct DynamicClipRecord {
        public float timePlayed;
        public Vector2 positionPlayed;
    }

    private void InitAudio() {
        const int numberOfSources = 20;
        sources = new(numberOfSources);
        
        for (int i = 0; i < numberOfSources; i++) {
            GameObject audioGo = Instantiate(dynamicAudioSourcePrefab, transform);
            sources.Enqueue(audioGo.GetComponent<AudioSource>());
        }
    }
    
    private void PlayAudioClip(DynamicClip dynamicClip, Vector2 position, float volumeScaler = 1f) {
        if (ClipIsViolatingLocalArea(dynamicClip, position)) return;
        
        AudioSource source = sources.Dequeue();
        sources.Enqueue(source);

        float distFromPlayer = Vector2.Distance(player.position, position);
        float volumeLerp = distFromPlayer / dynamicClip.maxDistance;
        float volume = Mathf.Lerp(1f, 0f, volumeLerp) * volumeScaler;
        
        source.transform.position = position;
        source.rolloffMode = dynamicClip.rolloffMode;
        source.clip = dynamicClip.clips[Random.Range(0, dynamicClip.clips.Length)];
        source.outputAudioMixerGroup = dynamicClip.mixerGroup;
        source.volume = volume;
        source.pitch = Random.Range(dynamicClip.minPitch, dynamicClip.maxPitch);
        source.minDistance = dynamicClip.minDistance;
        source.maxDistance = dynamicClip.maxDistance;
        source.loop = false;
        source.Play();
    }

    private bool ClipIsViolatingLocalArea(DynamicClip clip, Vector2 clipPos) {
        if (clip.localAreaCooldownTime <= 0f || clip.localAreaDistance <= 0f) {
            return false;
        }
        
        bool recordsExits = clipRecords.TryGetValue(clip.GetInstanceID(), out List<DynamicClipRecord> records);
        
        if (!recordsExits) {
            const int initCapacity = 10;
            List<DynamicClipRecord> newRecords = new(initCapacity);
            
            newRecords.Add(new() {  
                timePlayed = Time.time, 
                positionPlayed = clipPos 
            });
            
            clipRecords.Add(clip.GetInstanceID(), newRecords);
            return false;
        }
        
        float cooldownTime = clip.localAreaCooldownTime;
        float areaDistance = clip.localAreaDistance;
        
        // Remove any records that have been expired
        for (int i = records.Count - 1; i >= 0; i--) {
            bool recordHadExpired = Time.time >= records[i].timePlayed + cooldownTime;
            if (recordHadExpired) {
                records.RemoveAt(i);         
            }
        }
        
        // After removing expired records, check to see if one is too close to the potential pos
        foreach (DynamicClipRecord record in records) {
            if (Vector3.Distance(record.positionPlayed, clipPos) < areaDistance) {
                return true;
            } 
        }
        
        // Add a new record since we are going to play the sound
        records.Add(new() {  
            timePlayed = Time.time, 
            positionPlayed = clipPos 
        });

        return false;
    }

    
    private AudioSource ambienceAudioSource;
    
    private void PlayAmbience() {
        ambienceAudioSource = sources.Dequeue();
        ambienceAudioSource.transform.position = Vector3.zero;
        ambienceAudioSource.volume = 1f;
        ambienceAudioSource.pitch = 1f;
        ambienceAudioSource.rolloffMode = AudioRolloffMode.Linear;
        ambienceAudioSource.minDistance = 500;
        ambienceAudioSource.maxDistance = 500;

        ambienceAudioSource.loop = true;
        ambienceAudioSource.clip = ambienceClip;
        ambienceAudioSource.outputAudioMixerGroup = ambienceMixerGroup;
        ambienceAudioSource.Play();
    }

    private void StopAmbience() {
        ambienceAudioSource.Stop();
        sources.Enqueue(ambienceAudioSource);
        ambienceAudioSource = null;
    }
    
    // ************************
    // Scene Management 
    // ************************

    private enum MapLoadingState { Unloaded, Loaded, Loading, Unloading }
    private MapLoadingState mapLoadingState;

    [NonSerialized] public MapData loadedMapData;
    [NonSerialized] public MapInstance loadedMapInst;

    public void LoadMapAsync(MapData mapData, Action onLoadedCallback) {
        if (LoadingMapInProgress()) return;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mapData.sceneReference, LoadSceneMode.Additive);
        if (loadOperation == null) return;

        mapLoadingState = MapLoadingState.Loading;
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!loadOperation.isDone) {
                yield return null;
            }
            
            mapLoadingState = MapLoadingState.Loaded;
            loadedMapData = mapData;

            List<GameObject> loadedMapRoots = ListPool<GameObject>.Get();
            
            Scene loadedMapScene = SceneManager.GetSceneByName(mapData.sceneReference);
            loadedMapScene.GetRootGameObjects(loadedMapRoots);
            
            foreach (GameObject root in loadedMapRoots) {
                if (!root.TryGetComponent(out MapInstance map)) continue;
                loadedMapInst = map;
                map.gameObject.SetActive(false);
                break;
            }
            
            ListPool<GameObject>.Release(loadedMapRoots);
            onLoadedCallback?.Invoke();
        }
    }

    public void UnloadCurrentMapAsync() {
        if (UnloadingMapInProgress()) return;
            
        loadedMapInst.gameObject.SetActive(false); 
        
        Scene loadedMap = SceneManager.GetSceneByName(loadedMapData.sceneReference);
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedMap);

        if (unloadOperation == null) return;
        
        mapLoadingState = MapLoadingState.Unloading;
        loadedMapData = null;
        
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!unloadOperation.isDone) {
                yield return null;
            }
            mapLoadingState = MapLoadingState.Unloaded;
        }
    } 
    
    public bool LoadingMapInProgress() {
        return mapLoadingState == MapLoadingState.Loading;
    }
    
    public bool UnloadingMapInProgress() {
        return mapLoadingState == MapLoadingState.Unloading;
    }

    // ************************
    // Item Dropping
    // ************************

    private enum DropOrigin { Rock, Body, Trader, Enemy, ExistsInLevel, Bush }

    private struct DropPool {
        public List<Item> items;
        public DropOrigin dropOrigin;
        public bool HasItems => items.Count > 0;
    }

    private DropPool rockStonesDropPool;
    private DropPool eyeUpgradesDropPool;
    private DropPool bodyDropPool;
    private DropPool traderDropPool;
    private DropPool enemyDropPool;
    private DropPool foragingDropPool;
    private DropPool bushesDropPool;

    private void CreateDropPools() {
        rockStonesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        eyeUpgradesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        bodyDropPool = new() { items = new(), dropOrigin = DropOrigin.Body };
        traderDropPool = new() { items = new(), dropOrigin = DropOrigin.Trader };
        enemyDropPool = new() { items = new(), dropOrigin = DropOrigin.Enemy };
        foragingDropPool = new() { items = new(), dropOrigin = DropOrigin.ExistsInLevel };
        bushesDropPool = new() { items = new(), dropOrigin = DropOrigin.Bush };

        foreach (Item item in allItems) {
            if (item.chanceToSpawnOnTrader > 0f) {
                traderDropPool.items.Add(item); 
            }

            if (item.chanceToSpawnFromEnemy > 0f) {
                enemyDropPool.items.Add(item);
            }
        }
    }
    
    private void CreateDropPoolsForMap(MapData map) { 
        rockStonesDropPool.items.Clear();
        eyeUpgradesDropPool.items.Clear();
        bodyDropPool.items.Clear();
        foragingDropPool.items.Clear();
        bushesDropPool.items.Clear();
        
        foreach (Item item in allItems) {
            bool spawnsOnCurrentMap = item.spawnsOnAllMaps || item.spawnsOnMaps.Contains(map);
            if (!spawnsOnCurrentMap) continue;

            if (item.chanceToSpawnFromRock > 0f) {
                if (item.type == eyeModifierType) {
                    eyeUpgradesDropPool.items.Add(item);
                }
                else {
                    rockStonesDropPool.items.Add(item);
                }
            }
            if (item.chanceToSpawnOnBody > 0f) {
                bodyDropPool.items.Add(item);
            }
            if (item.chanceToExistInLevel > 0f) {
                foragingDropPool.items.Add(item);
            }
            if (item.chanceToSpawnFromBush > 0f) {
                bushesDropPool.items.Add(item);
            }
        }
    }

    private Item GetItemFromEnemyDropPool(EnemyData enemy) {
        DropPool tempEnemyPool = new() {
            items = ListPool<Item>.Get(),
            dropOrigin = DropOrigin.Enemy,
        };
        
        foreach (Item enemyItem in enemyDropPool.items) {
            if (enemyItem.spawnsFromEnemies.Contains(enemy)) {
                tempEnemyPool.items.Add(enemyItem);
            }
        }

        if (tempEnemyPool.items.Count <= 0) {
            ListPool<Item>.Release(tempEnemyPool.items);
            return null;
        }
        
        Item item = GetItemFromDropPool(tempEnemyPool);
        ListPool<Item>.Release(tempEnemyPool.items);
        return item;
    }

    private Item GetItemFromDropPool(DropPool dropPool) {
        Assert.IsFalse(dropPool.items == enemyDropPool.items, $"Use {nameof(GetItemFromEnemyDropPool)} for enemies");
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling"); 
        
        dropPool.items.Shuffle();
        
        foreach (Item drop in dropPool.items) {
            float dropChance = GetDropChanceOfItem(drop, dropPool.dropOrigin);
            if (Random.value < dropChance) {
                return drop;
            }
        }

        return dropPool.items[^1];
    }
    
    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, List<Item> items, float raritySkew = 0f) {
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling"); 
        
        dropPool.items.Shuffle();
        
        foreach (Item item in dropPool.items) {
            float itemDropChance = GetDropChanceOfItem(item, dropPool.dropOrigin) + raritySkew;
            if (Random.value < itemDropChance) {
                items.Add(item);
            }
        }
        
        bool itemListNeedsTrimming = items.Count > maxCount;
        if (itemListNeedsTrimming) {
            items.RemoveRange(maxCount, items.Count - maxCount);
        }

    }

    private float GetDropChanceOfItem(Item item, DropOrigin origin) {
        float addChanceToSpawnFromLuck = 0f;
        
        if (origin != DropOrigin.Trader) {
            float raritySkewIncreaseFromMap = loadedMapData.increasedLootRarityChance;
            addChanceToSpawnFromLuck = item.GetRarity() switch {
                // Scaling the luck increase exponentionally (the adding/subtracting 1 is because rarity skew is a decimal)
                Item.Rarity.Uncommon  => Mathf.Pow(1f + raritySkewIncreaseFromMap, 1.1f) - 1f,
                Item.Rarity.Rare      => Mathf.Pow(1f + raritySkewIncreaseFromMap, 1.2f) - 1f,
                Item.Rarity.Legendary => Mathf.Pow(1f + raritySkewIncreaseFromMap, 1.3f) - 1f,
                _                     => 0f,
            };
        }
        
        return origin switch {
            DropOrigin.Rock   => Mathf.Clamp01(item.chanceToSpawnFromRock + addChanceToSpawnFromLuck),
            DropOrigin.Body   => Mathf.Clamp01(item.chanceToSpawnOnBody + addChanceToSpawnFromLuck),
            DropOrigin.Trader => Mathf.Clamp01(item.chanceToSpawnOnTrader + addChanceToSpawnFromLuck),
            DropOrigin.Enemy  => Mathf.Clamp01(item.chanceToSpawnFromEnemy + addChanceToSpawnFromLuck),
            DropOrigin.Bush   => Mathf.Clamp01(item.chanceToSpawnFromBush + addChanceToSpawnFromLuck),
            _                 => 0f,
        };
    }
    
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
        return ColorText($"{Mathf.FloorToInt(probability * 100f)}%", inst.styles.timeDescColor);
    }

    public static string DisplayProbIncDec(float probability) {
        return probability >= 0f ? DisplayProbIncrease(probability) : DisplayProbDecrease(probability);
    }

    public static string DisplayProbIncrease(float probability) {
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", inst.styles.increaseDescColor);
    }
    
    public static string DisplayProbDecrease(float probability) {
        return ColorText($"-{Mathf.Abs(Mathf.FloorToInt(probability * 100f))}%", inst.styles.decreaseDescColor);
    }

    
    public static string DisplayNumber(int number) {
        return ColorText(number.ToString(), inst.styles.timeDescColor);
    }
    
    public static string DisplayNumber(float number) {
        return ColorText(number.ToString("0.00"), inst.styles.timeDescColor);
    }

    public static string DisplayIncDec(int amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(int amount) {
        return ColorText($"+{amount}", inst.styles.increaseDescColor);
    }
    
    public static string DisplayDecrease(int amount) {
        return ColorText($"-{Mathf.Abs(amount)}", inst.styles.decreaseDescColor);
    }

    public static string DisplayIncDec(float amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(float amount) {
        return ColorText($"+{amount:0.00}", inst.styles.increaseDescColor);
    }
    
    public static string DisplayDecrease(float amount) {
        return ColorText($"-{Mathf.Abs(amount):0.00}", inst.styles.decreaseDescColor);
    }
    
    public static string DisplayMultiplier(float multiplier) {
        Color textColor = multiplier >= 1f ? inst.styles.increaseDescColor : inst.styles.decreaseDescColor;
        return ColorText($"{multiplier:0.00}x", textColor);
    }

    public static string DisplayMultiplierIncDec(float multiplier) {
        return multiplier >= 0f ? DisplayMultiplierIncrease(multiplier) : DisplayMultiplierDecrease(multiplier);
    }

    public static string DisplayMultiplierIncrease(float multiplier) {
        return ColorText($"+{multiplier:0.00}x", inst.styles.increaseDescColor);
    }
    
    public static string DisplayMultiplierDecrease(float multiplier) {
        return ColorText($"-{multiplier:0.00}x", inst.styles.decreaseDescColor);
    }

    public static string DisplaySeconds(float time) {
        if (time == 1f) {
            return ColorText($"{time:0}<space=0.12em>s", inst.styles.timeDescColor);
        }
        
        bool isWholeNumber = time % 1 == 0;
        if (isWholeNumber) {
            return ColorText($"{time:0}<space=0.12em>s", inst.styles.timeDescColor);
        }
        
        return ColorText($"{time:0.0#}<space=0.12em>s", inst.styles.timeDescColor);
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

    private Tween Delay<T>(T entity, float delay, Action<T> callback) where T: Entity {
        return Tween.Delay(entity, delay, onComplete: callback, onValidate: EntityIsValid);
    }
    
}
