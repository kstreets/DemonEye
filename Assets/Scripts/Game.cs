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
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public class Game : MonoBehaviour {

    public static Game inst;
    
    public StartingItemsConfig startingItems;
    public Styles styles;
    public GameplayConfig gameplayConfig;
    public List<QuestLine> questLines;

    [Foldout("Traders")]
    public Trader potionManTrader;
    public Trader armsDealerTrader;
    public Trader hatManTrader;
    [EndFoldout]

    [Foldout("Maps")]
    public MapData lighthouseMap;
    public MapData customsMap;
    public MapData terminalMap;
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
    public ItemType trinketType;
    public ItemType soulcardType;
    public ItemType gemType;
    public ItemType passiveType;
    [EndFoldout]

    [Foldout("Stat Upgrade Paths")]
    public StatUpgradePath agilityUpgradePath;
    public StatUpgradePath luckUpgradePath;
    public StatUpgradePath healthUpgradePath;
    public StatUpgradePath strengthUpgradePath;
    [EndFoldout]
    
    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public PixelPerfectCamera pixelPerfectCamera;

    public GameObject playerPrefab;
    public GameObject gemRockPrefab;
    public GameObject deadBodyPrefab;

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
    public Button levelupTabButton;
    public TextMeshProUGUI characterTabText;
    public TextMeshProUGUI eyeForgeTabText;
    public TextMeshProUGUI traderTabText;
    public TextMeshProUGUI questsTabText;
    public TextMeshProUGUI levelupTabText;
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
    public Button traderDealButton;
    [EndFoldout]

    [Foldout("UI/MapSelectionPanel")]
    public RectTransform mapSelectionPanel;
    public Button easyMapButton;
    public Button mediumMapButton;
    [EndFoldout]
    
    [Foldout("UI/QuestsPanel")]
    public RectTransform questsPanel;
    public RectTransform questsParent;
    [EndFoldout]
    
    [Foldout("UI/LevelupPanel")]
    public RectTransform levelupPanel;
    public ButtonFeel agilityUpgradeButton;
    public ButtonFeel corruptionUpgradeButton;
    public ButtonFeel healthUpgradeButton;
    public ButtonFeel strengthUpgradeButton;
    public TextMeshProUGUI agilityUpgradeInfoText;
    public TextMeshProUGUI healthUpgradeInfoText;
    public TextMeshProUGUI bleedResInfoText;
    public TextMeshProUGUI strengthUpgradeInfoText;
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
    private InputAction lookInputAction;
    private InputAction attackInputAction;
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
    
    public static Dictionary<int, Item> itemLookup = new();
    public static Dictionary<int, Soulcard> eyeModifierLookup = new();

    private EntityPool<Entity> itemDropPool;
    private EntityPool<Entity> bloodDropPool;
    private EntityPool<Projectile> projectilePool;
    private EntityPool<Projectile> boneShatterProjectilePool;
    private EntityPool<Projectile> gooProjectilePool;
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
    public static Action<MapData> onTeleportToMap;
    public static Action<DemonEyeInstance> onEyeForged;
    public static Action<InventorySlot[]> onSoldItemsToTrader;
    
    private void Start() {
        inst = this;
        
        LoadAllItems();
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
        lookInputAction = InputSystem.actions.FindAction("Look");
        attackInputAction = InputSystem.actions.FindAction("Attack");
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
        foreach (Inventory inventory in allInventories) {
            RefreshInventoryDisplay(inventory);
        }
        UpdateGraySlots();
        
        #if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.middleButton.isPressed) {
            Time.timeScale = 8f;
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
            UpdateExitPortalArrowUI();
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
        RefreshLevelUpPossibilities();
    }

    private void OnHideoutStateExit() {
        CloseHideoutUI();
        SavePlayerData();
        SaveTrader();
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
        SaveInventory(crucibleInventory);
        SaveQuestStates();
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
            entity.position = Vector2.Lerp(entity.bounceEffect.initialPos, entity.bounceEffect.targetPos, val);
            entity.position = new(entity.position.x, entity.position.y + yPos, entity.position.y);
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
        public Collider2D enemySpacerCollider;
        public EnemyData data;
        public Timer applyDamageTimer;
        public BleedSoulcard.InstanceData? bleed;
        public PoisonSoulcard.InstanceData? poison;
        public SlowInstance? slow;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public Limiter changeDirLimiter;
    }
    
    private void UpdateEnemies() {
        bool timeHasPassed = enemyReteleportLimitter.TimeHasPassed(1f);
        if (timeHasPassed) {
            enemyReteleportCount = 0;
        }
        int maxTeleportCount = Mathf.RoundToInt(enemies.Count * 0.08f);
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            enemy.applyDamageTimer.Tick();

            enemy.teleportTime += Time.deltaTime;
            float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);
            
            bool canReteleport = timeHasPassed && enemyReteleportCount < maxTeleportCount;

            if (canReteleport && enemy.teleportTime >= 6.5f && distFromPlayer > 1.1f) {
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
                            Projectile proj = inst.SpawnProjectile(inst.OffsetY(enemy.position, 0.2f), velocity, inst.gooProjectilePool, Masks.PlayerHurtMask);
                            proj.simpleDamage = enemy.data.damage;
                            proj.lifeTimeDuration = 2f;
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

        if (!spawnLimiterForEnemyBatching.TimeHasPassed(3f)) return;
        
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
        
        onTeleportToMap?.Invoke(loadedMapData);

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
        SaveQuestStates();
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
        public List<int> modifierUuids;
        public int count = 1;

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public bool traderOwned;
        [NonSerialized] public int traderSlotIndex;
        [NonSerialized] public Item _itemRef; // Used for items created at runtime, like demon eyes

        public Item ItemRef => _itemRef ? _itemRef : itemLookup[itemOrInstanceUuid];
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

            if (modifierUuids != null) {
                foreach (int modifierUuid in modifierUuids) {
                    clonedItem.modifierUuids ??= new();     
                    clonedItem.modifierUuids.Add(modifierUuid);
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
        // SpawnUiSlots(playerBackpackParent, maxBackpackSize);
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

        const int traderInventorySize = traderInventoryRowCount * traderInventoryColCount;
        SpawnUiSlots(traderInventoryParent, traderInventorySize);
        traderInventory = CreateInventory(traderInventoryParent, traderInventorySize);
        LoadInventory(traderInventory);
        
        const int transactionInventorySize = 25;
        SpawnUiSlots(traderTransactionInventoryParent, transactionInventorySize);
        transactionInventory = CreateInventory(traderTransactionInventoryParent, transactionInventorySize);

        const int maxCrucibleInventorySize = 13;
        const int startingCrucibleInventorySize = 4;
        SpawnUiSlots(crucibleParent, maxCrucibleInventorySize, eyeForgeSlotPrefab);
        crucibleInventory = CreateInventory(crucibleParent, startingCrucibleInventorySize + player.crucibleLevel);
        ArrangeEyeCrucibleInventorySlots();
        LoadInventory(crucibleInventory);
    }
    
    private void ArrangeEyeCrucibleInventorySlots() {
        int inventoryLength = crucibleInventory.slots.Length;
        for (int i = 0; i < inventoryLength; i++) {
            InventorySlotUI slotUI = crucibleInventory.slots[i].ui;
            slotUI.disallowItemStacking = true;
            slotUI.onlyAcceptedItemType = i == 0 ? eyeType : soulcardType;

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
            bool isDemonEye = item?.modifierUuids != null;
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
        
        itemDescPopup.gameObject.SetActive(true);
        TweenPopUp(itemDescPopup.rectTransform);
        
        InventorySlot hoveredSlot = info.inventory.slots[info.slotIndex];
        TextMeshProUGUI nameText = itemDescPopup.nameText;
        TextMeshProUGUI metaInfoText = itemDescPopup.metaInfoText;
        TextMeshProUGUI descText = itemDescPopup.descText;
        
        Item.Rarity itemRarity = hoveredSlot.item.ItemRef.GetRarity();
        Color itemRarityColor = styles.GetColorForRarity(itemRarity);
        float tagTextPadding = styles.tagTextPadding;

        itemDescPopup.tag1.gameObject.SetActive(true);
        itemDescPopup.tag1.color = itemRarityColor;
        
        if (hoveredSlot.item.ItemRef.type == quickUseType) {
            itemDescPopup.tag1Text.text = "Quick Use";
        } 
        else if (hoveredSlot.item.ItemRef.type == soulcardType) {
            itemDescPopup.tag1Text.text = "Eye Upgrade";
        }
        else if (hoveredSlot.item.ItemRef.type == passiveType) {
            itemDescPopup.tag1Text.text = "Passive Item";
        }
        else if (hoveredSlot.item.ItemRef.type == backpackType) {
            itemDescPopup.tag1Text.text = "Backpack";
        }
        else {
            itemDescPopup.tag1.gameObject.SetActive(false);
        }
        
        itemDescPopup.tag1ContentFitter.ForceRecalculate();
        itemDescPopup.tag1.rectTransform.ResizeWidth(itemDescPopup.tag1Text.rectTransform.rect.width + tagTextPadding);
        
        itemDescPopup.tag2.color = itemRarityColor;
        itemDescPopup.tag2Text.text = itemRarity.ToString();
        itemDescPopup.tag2ContentFitter.ForceRecalculate();
        itemDescPopup.tag2.rectTransform.ResizeWidth(itemDescPopup.tag2Text.rectTransform.rect.width + tagTextPadding);
        
        Item item = hoveredSlot.item.ItemRef;
        nameText.text = item.displayName;

        int sellOrBuyPrice = 0;
        if (item.type == demonEyeType) { 
            sellOrBuyPrice = GetDemonEyeSellPrice(hoveredSlot.item);
        }
        else {
            bool itemIsOwnedByTrader = info.inventory.slots[info.slotIndex].item.traderOwned;
            sellOrBuyPrice = itemIsOwnedByTrader ? item.buyPrice : item.sellPrice * hoveredSlot.item.count;
        }
                             
        string coinText = $"<sprite=0>{ColorText(sellOrBuyPrice.ToString(), styles.coinCurrencyColor)}";
        
        string tintedWeightSprite = $"<sprite=2 color=#{ColorUtility.ToHtmlStringRGBA(styles.underWeightColor)}>";
        string weightText = tintedWeightSprite + ColorText(item.Weight.ToString(), styles.underWeightColor);
        
        metaInfoText.text = coinText + "  " + weightText;
        
        // Set description
        if (hoveredSlot.item.ItemRef.type == demonEyeType) {
            DemonEyeInstance eyeInstance = eyeInstanceFromItemId[hoveredSlot.item.itemOrInstanceUuid];
            string eyeDescription = "";
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += GetDemonEyeModDescription(modInstance.Soulcard, modInstance.stackCount);
            }
            descText.text = eyeDescription;
        }
        else {
            descText.text = hoveredSlot.item.ItemRef.GetDescription();
        }
        
        if (hoveredSlot.item.ItemRef.type == quickUseType && !hoveredSlot.item.traderOwned) {
            descText.text += $"<line-height=150%>\n<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}> " +
                             $"<size=80%>{ColorText("Right click to consume", styles.inputIconTint)}</size>";
        } 

        // Set popup position
        Vector2 hoveredSlotCenter = hoveredSlot.ui.rectTransform.WorldRect().center;
        float halfPopupWidth = itemDescPopup.rectTransform.rect.width / 2f;
        Vector2 popupOffset = new(32 + halfPopupWidth, 40);
        if (hoveredSlotCenter.x < ScreenCenter.x) {
            itemDescPopup.transform.position = hoveredSlotCenter + popupOffset;
        }
        else {
            itemDescPopup.transform.position = hoveredSlotCenter + new Vector2(-popupOffset.x, popupOffset.y);
        }

        // Fit popup size to text elements
        itemDescPopup.nameContentFitter.ForceRecalculate();
        itemDescPopup.descContentFitter.ForceRecalculate();
        FitPopupSize(itemDescPopup.rectTransform, itemDescPopup.tagsParent.rect, itemDescPopup.nameText.rectTransform.rect, itemDescPopup.descText.rectTransform.rect);
        
        // Add mechanic desctiption if necessary
        if (hoveredSlot.item.ItemRef.type == soulcardType) {
            Soulcard soulcard = (Soulcard)hoveredSlot.item.ItemRef;
            if (soulcard.relativeMechanicDesc) {
                mechanicDescPopup.gameObject.SetActive(true);
                mechanicDescPopup.nameText.text = soulcard.relativeMechanicDesc.displayName;
                mechanicDescPopup.descText.text = soulcard.relativeMechanicDesc.description;
                mechanicDescPopup.transform.position = itemDescPopup.rectTransform.WorldRect().min;
                
                mechanicDescPopup.nameFitter.ForceRecalculate();
                mechanicDescPopup.descFitter.ForceRecalculate();
                FitPopupSize(mechanicDescPopup.rectTransform, mechanicDescPopup.nameText.rectTransform.rect, mechanicDescPopup.descText.rectTransform.rect);
                
                TweenPopUp(mechanicDescPopup.rectTransform);
            } 
        }
    }

    private string GetDemonEyeModDescription(Soulcard soulcard, int count) {
        string title = ColorText($"<size=108%>{soulcard.displayName}</size> <size=87%>x{count}</size>", styles.headerTextColor);
        return $"<line-height=95%>{title}\n{soulcard.GetStackDescription(count)}<line-height=140%>\n";
    }

    private void FitPopupSize(RectTransform popupRect, params Rect[] rects) {
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
            if (transactionState == TransactionState.Buying) {
                if (hoveredInventory == traderInventory) {
                    destinationInventory = transactionInventory;
                    moveOption = MoveItemOption.Single;
                }
                else if (hoveredInventory == transactionInventory) {
                    destinationInventory = traderInventory;
                }
            }
            else if (transactionState == TransactionState.Selling) {
                if (hoveredInventory == stashInventory) {
                    destinationInventory = transactionInventory;
                }
                else if (hoveredInventory == transactionInventory) {
                    destinationInventory = stashInventory;
                }
            }
            else {
                if (hoveredInventory == traderInventory) {
                    destinationInventory = transactionInventory;
                    moveOption = MoveItemOption.Single;
                }
                else if (hoveredInventory == stashInventory) {
                    destinationInventory = transactionInventory;
                }
            }
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
        
        agilityStatValueText.text = (player.agilityLevel + 1).ToString("0.0");
        healthStatValueText.text = (player.healthLevel + 1).ToString("0.0");
        bleedResStatValueText.text = (player.bleedResLevel + 1).ToString("0.0");
        strengthStatValueText.text = (player.strengthLevel + 1).ToString("0.0");
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
    
    private void CheckForEquipmentChange() {
        InventoryItem curEyeItem = playerInventory.slots[0].item;
        InventoryItem curBackpackItem = playerInventory.slots[1].item;

        if (prevEquippedEyeItem != curEyeItem) {
            prevEquippedEyeItem = curEyeItem;
            equipedEye = curEyeItem == null ? emptyDemonEye : eyeInstanceFromItemId[curEyeItem.itemOrInstanceUuid];
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
            if (transactionState != TransactionState.Selling) {
                MoveItemBetweenInventories(traderInventory, transactionInventory, hoverInfo.slotIndex, MoveItemOption.Single);
            }
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
    
    private InventoryItem GetInventoryItem(Inventory inventory, int slotIndex) {
        if (slotIndex < 0 || slotIndex >= inventory.slots.Length) {
            return null;
        }
        return inventory.slots[slotIndex].item;
    }

    private void ReduceItemCountInInventory(Inventory inventory, int slotIndex, int reduction = 1) {
        var item = GetInventoryItem(inventory, slotIndex);
        item.count -= reduction;
        if (item.count <= 0) {
            RemoveItemFromInventory(inventory, slotIndex);
        }
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
                if (item.type != eyeType && item.type != soulcardType) {
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
                    value += slot.item.ItemRef.type == demonEyeType ? GetDemonEyeSellPrice(slot.item) : slot.item.ItemRef.sellPrice * slot.item.count;
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
        public int agilityLevel;
        public int bleedResLevel;
        public int healthLevel;
        public int strengthLevel;
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
        
        float speed = GetPlayerSpeedBasedOnStats();
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
        
        int targetCount = 1;
        if (equipedEye.projectileCount.TryGetValue(out var projectileCount)) {
            for (int i = 0; i < projectileCount.extraProjectileCount; i++) {
                if (RollProbability(projectileCount.probability)) {
                    targetCount += projectileCount.extraProjectileCount;
                }
            }
        }
        
        List<Vector3> attackTargets = GetAttackTargets(targetCount);

        if (attackTargets.Count <= 0 || !CanShoot()) return;
        
        PlayAudioClip(shootClip, player.position);
        foreach (Vector3 attackTarget in attackTargets) {
            ShootProjectile(attackTarget);
        }

        float consecutiveShotDelay = gameplayConfig.attackDelay * 1.5f;
        if (Time.time - lastShotTime <= consecutiveShotDelay) {
            consecutiveShotCount++;
        }
        else {
            consecutiveShotCount = 0;
        }
        
        if (equipedEye.blast.TryGetValue(out var blast) && consecutiveShotCount > 0 && consecutiveShotCount % blast.numshotsUntilOverheat == 0) {
            SpawnExplosion(blastPool, OffsetY(player.position, 0.1f), blast.radius, blast.damage, Masks.EnemyMask, 0.15f);
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
        player.health -= damage;
        AddFlashHitEffect(player);
        SpawnDamageNumber(player.position, damage, DamageColor.Blood);
    }
    
    private bool PlayerHealthIsAtAutoBleedStop() {
        const float percentageOfHealthBleedingStops = 0.10f;
        return player.health <= FullPlayerHealth * percentageOfHealthBleedingStops;
    }
    
    private const float defaultPlayerSpeed = 0.52f;
    private const float maxPlayerSpeed = 0.61f;

    private const int encumberingIncreasePerStrengthPoint = 50;
    private const int defaultStartingEncumberingWeight = 140;
    private const int maxEncumberedWeight = 190;
    private const float maxEncumberedSpeedReduction = 0.2f;

    private const float reducedChanceForBleedPerLevel = 0.1f;

    private const int healthIncreasePerStatLevel = 10;
    private int FullPlayerHealth => 100 + (healthIncreasePerStatLevel * player.healthLevel);

    private float GetPlayerSpeedBasedOnStats() {
        int agilityStat = player.agilityLevel;
        for (int i = 0; i < playerEquipmentSize; i++) {
            InventoryItem item = playerInventory.slots[i].item;
            if (item == null) continue;
            if (item.ItemRef.modifiesStats && item.ItemRef.agilityStatAdjustment != 0) {
                agilityStat += item.ItemRef.agilityStatAdjustment;
            }
        }
        float playerSpeed = Mathf.Lerp(defaultPlayerSpeed, maxPlayerSpeed, (float)agilityStat / agilityUpgradePath.MaxLevel);
        
        float speedReductionFromWeight = Mathf.Lerp(0f, maxEncumberedSpeedReduction, GetOverweightCompletion());
        speedReductionFromWeight = Mathf.Clamp(speedReductionFromWeight, 0f, maxEncumberedSpeedReduction);

        playerSpeed -= speedReductionFromWeight;
        return playerSpeed;
    }

    private int GetStrengthStat() {
        int strengthStat = player.strengthLevel;
        for (int i = 0; i < playerEquipmentSize; i++) {
            InventoryItem item = playerInventory.slots[i].item;
            if (item == null) continue;
            if (item.ItemRef.modifiesStats && item.ItemRef.strengthStatAdjustment != 0) {
                strengthStat += item.ItemRef.strengthStatAdjustment;
            }
        }
        return strengthStat;
    }

    private void GetEncumberingWeightRange(out int startingWeight, out int endingWeight) {
        int encumberingIncreaseFromStrength = GetStrengthStat() * encumberingIncreasePerStrengthPoint;
        endingWeight = maxEncumberedWeight + encumberingIncreaseFromStrength;
        startingWeight = defaultStartingEncumberingWeight + encumberingIncreaseFromStrength;
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
        public int modId;
        public int stackCount;
        
        public Soulcard Soulcard => eyeModifierLookup[modId];
        public void ApplyToEnemy(Enemy enemy) => Soulcard.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Soulcard.AddInstanceToEye(eyeInstance, stackCount);
    }

    public class DemonEyeInstance {
        public List<EquipedModInstance> modInstances = new();
        public FirerateSoulcard.InstanceData? firerate;
        public TrishotSoulcard.InstanceData? trishot;
        public BleedCritSoulcard.InstanceData? bleedCrit;
        public RangeSoulcard.InstanceData? range;
        public FarDamageSoulcard.InstanceData? farDamage;
        public PenetrationSoulcard.InstanceData? penetration;
        public DoubleCritSoulcard.InstanceData? doubleCrit;
        public BackwardsShotSoulcard.InstanceData? backwardShot;
        public ExplosionSoulcard.InstanceData? explosion;
        public OverheatBlast.InstanceData? blast;
        public BoneShatterSoulcard.InstanceData? boneShatter;
        public StoppingPowerSoulcard.InstanceData? stoppingPower;
        public ProjectileCountSoulcard.InstanceData? projectileCount;
    }
    
    public class DemonEyeRaidStats {
        public int consecutiveCriticalHits;
        public float lastDoubleCritActivationTime;
    }

    // Need to reset this at the beginning of every raid
    private DemonEyeRaidStats demonEyeRaidStats;

    private Dictionary<int, DemonEyeInstance> eyeInstanceFromItemId = new();
    private readonly DemonEyeInstance emptyDemonEye = new();
    private DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    private DemonEyeInstance BuildAndRegisterEye(InventoryItem item) {
        item.itemOrInstanceUuid = GenerateNewItemUuid();
        item._itemRef = demonEyeItem;
        
        Dictionary<Soulcard, int> eyeModCountFromSoulcard = new();
        foreach (int modUuid in item.modifierUuids) {
            Soulcard soulcard = itemLookup[modUuid] as Soulcard;
            if (!eyeModCountFromSoulcard.TryAdd((Soulcard)itemLookup[modUuid], 1)) {
                eyeModCountFromSoulcard[soulcard]++;
            }
        }

        List<(Soulcard, int)> sortedSoulcardsWithCount = SortSoulcardsFromDictionary(eyeModCountFromSoulcard);
        
        List<EquipedModInstance> eyeModifiers = new();
        foreach ((Soulcard soulcard, int stackCount) in sortedSoulcardsWithCount) {
            eyeModifiers.Add(new() {
                modId = soulcard.uuid,
                stackCount = stackCount,
            });
        }
        
        DemonEyeInstance newDemonEye = new() {
            modInstances = eyeModifiers,
        };
        
        foreach (EquipedModInstance modInstance in eyeModifiers) { 
            modInstance.ApplyToEye(newDemonEye); 
        }
        
        eyeInstanceFromItemId.Add(item.itemOrInstanceUuid, newDemonEye);
        return newDemonEye;
    }

    private List<(Soulcard, int)> SortSoulcardsFromDictionary(Dictionary<Soulcard, int> soulcardsAndStackCount) {
        List<(Soulcard, int)> eyeModifiers = new();
        foreach (KeyValuePair<Soulcard, int> pair in soulcardsAndStackCount) {
            eyeModifiers.Add(new(pair.Key, pair.Value));
        }
        eyeModifiers = eyeModifiers.OrderByDescending(m => m.Item1.GetRarity()).ThenBy(m => m.Item1.displayName).ToList();
        return eyeModifiers;
    }

    private int GetDemonEyeSellPrice(InventoryItem demonEyeInventoryItem) {
        // We need to use the InventoryItem's ID because the Item's ID is the demon eye Scriptable Object
        DemonEyeInstance demonEye = eyeInstanceFromItemId[demonEyeInventoryItem.itemOrInstanceUuid]; 
        
        int sellPrice = 0;
        foreach (EquipedModInstance modInstance in demonEye.modInstances) {
            sellPrice += modInstance.Soulcard.sellPrice * modInstance.stackCount;
        }
        return sellPrice;
    } 

    private List<Vector3> GetAttackTargets(int targetCount) {
        float overlapDist = GetProjectileSpeed() * GetProjectileLifeTimeSeconds();
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

    private bool CanShoot() {
        float attackDelay = gameplayConfig.attackDelay;
        if (equipedEye.firerate.TryGetValue(out var firerate)) {
            attackDelay -= attackDelay * firerate.rateIncrasePercentage;
            attackDelay = Mathf.Clamp(attackDelay, gameplayConfig.cappedMinAttackDelay, gameplayConfig.attackDelay);
        }
        return attackLimiter.TimeHasPassed(attackDelay);
    }

    private void ShootProjectile(Vector2 targetPos) {
        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - gameplayConfig.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = GetProjectileSpeed();
        Vector2 dir = (targetPos - PlayerEyePos.ToVector2()).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * projectileSpeed; 
        Projectile proj = SpawnProjectile(PlayerEyePos, velocity, projectilePool);

        if (equipedEye.penetration.TryGetValue(out var penetration)) {
            proj.enemyPenetrationCount = penetration.goThroughCount;
        }

        if (equipedEye.trishot.TryGetValue(out var trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(PlayerEyePos, secondShotVelocity, projectilePool).isTriShot = true;
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(PlayerEyePos, thirdShotVelocity, projectilePool).isTriShot = true;
        }

        if (equipedEye.backwardShot.TryGetValue(out var backShot) && RollProbability(backShot.probability)) {
            SpawnProjectile(PlayerEyePos, -velocity, projectilePool).isBackwardsShot = true;
        }
    }
    
    private Projectile SpawnProjectile(Vector2 spawnPos, Vector2 velocity, EntityPool<Projectile> pool, LayerMask mask = default) {
        float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
        Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Projectile projectile = SpawnEntity(pool, spawnPos, projectileRotation);
        projectile.lifeTimeDuration = GetProjectileLifeTimeSeconds();
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = equipedEye;
        projectile.layerMask = mask == default ? Masks.DamagableMask : mask;
        projectiles.Add(projectile);

        projectile.trans.localScale = Vector3.zero;
        Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
        
        return projectile;
    }

    private float GetProjectileLifeTimeSeconds() {
        const float defaultTimeAlive = 0.65f;
        float projLifeTime = defaultTimeAlive;
        if (equipedEye.range.TryGetValue(out var rangeIncrease)) {
            projLifeTime += rangeIncrease.timeAliveIncrease;
        }
        return projLifeTime;
    }

    private float GetProjectileSpeed() {
        float projectileSpeed = gameplayConfig.projectileSpeed;
        if (equipedEye.stoppingPower.TryGetValue(out var stoppingPower)) {
            projectileSpeed *= 1f - stoppingPower.percentSpeedReduction;
        }
        return projectileSpeed;
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
                
                Color itemColor = styles.GetColorForRarity(itemDrop.item.GetRarity());
                string details = ColorText($"{itemDrop.item.displayName} x{itemDrop.dropCount}", itemColor);
                EnableInteractionPrompt(OffsetY(col.transform.position, 0.1f), details);
                
                if (interactInputAction.WasPressedThisFrame()) {
                    InventoryAddResult result = TryAddItemToInventory(playerInventory, itemDrop.item, itemDrop.dropCount);
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
    
    public class Projectile : Entity {
        public float curTimeAlive;
        public float lifeTimeDuration;
        public float distTraveled;
        public bool isTriShot;
        public bool isBackwardsShot;
        public Vector2 velocity;
        public int simpleDamage;
        public int enemyPenetrationCount;
        public LayerMask layerMask;
        public DemonEyeInstance eyeInstanceSpawnedFrom;
        public List<Entity> ignoreEntities;
    }
    
    private static void OnSpawnProjectile(Projectile projectile) {
        projectile.curTimeAlive = default;
        projectile.lifeTimeDuration = default;
        projectile.distTraveled = default;
        projectile.isBackwardsShot = default;
        projectile.isTriShot = default;
        projectile.velocity = default;
        projectile.simpleDamage = default;
        projectile.enemyPenetrationCount = default;
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

            if (proj.layerMask == Masks.PlayerHurtMask) {
                DamagePlayer(proj.simpleDamage);
                DestroyEntity(projectiles[i]);
                projectiles.RemoveAt(i);
                continue;
            }
            
            Entity entity = entityLookup[col.gameObject];
                    
            if (proj.ignoreEntities == null || !proj.ignoreEntities.Contains(entity)) {
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
        if (proj.ignoreEntities != null && proj.ignoreEntities.Contains(entity)) {
            return true;
        }
        
        if (proj.enemyPenetrationCount <= 0) {
            return false;
        }
        
        ProjectileIgnoreEntity(proj, entity);
        int alreadyPenetratedCount = proj.ignoreEntities?.Count ?? 0;
        return alreadyPenetratedCount <= proj.enemyPenetrationCount;
    }

    private void ProjectileIgnoreEntity(Projectile proj, Entity entity) {
        bool alreadyContainsEntity = proj.ignoreEntities?.Contains(entity) ?? false;
        if (EntityIsValid(entity) && !alreadyContainsEntity) {
            proj.ignoreEntities ??= ListPool<Entity>.Get();
            proj.ignoreEntities.Add(entity);
        }
    }

    // ***********************************
    // Damage Handling 
    // ***********************************
    
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

            if (projectile.simpleDamage != 0) {
                DamageEnemy(enemy, projectile.simpleDamage, false);
                return;
            }
            
            bool isCriticalStrike = RollProbability(GetCriticalStrikeProbability(projectile, enemy));
            if (isCriticalStrike) {
                demonEyeRaidStats.consecutiveCriticalHits++;
            }
            else {
                demonEyeRaidStats.consecutiveCriticalHits = 0;
            }

            int damage = Mathf.RoundToInt(GetBaseDamage(projectile) * GetDamageMultiplier(projectile, enemy, isCriticalStrike));
            DamageEnemy(enemy, damage, isCriticalStrike);
            
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                modInstance.ApplyToEnemy(enemy);
            }
            
            if (eyeInstance.explosion.TryGetValue(out var explosion) && RollProbability(explosion.probability)) {
                Vector2 expSpawnPos = projectile.position + (enemy.position - projectile.position) / 2f;
                SpawnExplosion(explosionPool, expSpawnPos, explosion.radius, explosion.damage, Masks.EnemyMask, 0.1f);
            }
            
            if (equipedEye.boneShatter.TryGetValue(out var boneShatter) && RollProbability(boneShatter.probability)) {
                for (int i = 0; i < boneShatter.shardsCount; i++) {
                    Vector2 boneShatterVelocity = RandomizeVectorAngle(projectile.velocity / 1.5f, 40f);
                    Projectile boneShatterProj = SpawnProjectile(enemy.position, boneShatterVelocity, boneShatterProjectilePool);
                    boneShatterProj.trans.rotation = RandomRotation();
                    boneShatterProj.simpleDamage = boneShatter.perShardDamage;
                    boneShatterProj.lifeTimeDuration = boneShatter.lifeTime;
                    ProjectileIgnoreEntity(boneShatterProj, enemy);
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

                int dropCount = Random.Range(3, 5);
                float angleDeltaPerDrop = 360f / dropCount;
                float randomRangePerDrop = angleDeltaPerDrop * 0.25f;

                int gemsSpawned = 0;
                int maxGemsAllowedToSpawn = loadedMapData.maxGemCountPerRock;
                
                int upgradeDropIndex = -1;
                if (RollProbability(loadedMapData.eyeUpgradeFromRockChance)) {
                    upgradeDropIndex = Random.Range(0, dropCount);
                    gemsSpawned++;
                }

                for (int i = 0; i < dropCount; i++) {
                    Item dropItem = null;
                    if (i == upgradeDropIndex) {
                        dropItem = GetItemFromDropPool(eyeUpgradesDropPool);
                    }
                    else {
                        do dropItem = GetItemFromDropPool(rockStonesDropPool);
                        while (gemsSpawned == maxGemsAllowedToSpawn && dropItem.type == gemType);

                        if (dropItem.type == gemType) {
                            gemsSpawned++;
                        }
                    }
                    
                    float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                    Vector3 endPos = entity.position + RotationVector(randomAngle, 0.18f, 0.25f);
                    Entity rockDrop = SpawnItemAsEntity(dropItem, 1, entity.position, Quaternion.identity);
                    AddBounceEffect(rockDrop, endPos, 0.8f);
                }
            }
            else {
                AddFlashHitEffect(entity);
                AddShakeEffect(entity, 8f, 0.038f, 0.35f, shakeCurve);
                Tween.PunchScale(entity.trans, Vector3.one * 0.12f, 0.1f, 15f);
            }
        }
    }

    private float GetCriticalStrikeProbability(Projectile proj, Enemy enemy) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        float criticalStrikeProb = gameplayConfig.defaultCritChance;

        if (eyeInstance.bleedCrit.HasValue && enemy.bleed.HasValue) {
            criticalStrikeProb += eyeInstance.bleedCrit.Value.probability;
        }

        return criticalStrikeProb;
    }

    private int GetBaseDamage(Projectile proj) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        int damage = gameplayConfig.damage;

        int damageRange = Mathf.RoundToInt(damage * 0.1f);
        damage += Random.Range(-damageRange, damageRange);

        if (proj.isTriShot && eyeInstance.trishot.TryGetValue(out var triShot)) {
            damage = Mathf.RoundToInt(damage * triShot.reducedDamageMultiplier);
        }
        
        if (eyeInstance.farDamage.TryGetValue(out var farDamage)) {
            float convertedUnits = proj.distTraveled / gameplayConfig.distancePerUnit;
            int increasedDamageFromDist = Mathf.FloorToInt(convertedUnits) * farDamage.damageIncreasePerUnitTraveled;
            damage += increasedDamageFromDist;
        }

        if (eyeInstance.stoppingPower.TryGetValue(out var stoppingPower)) {
            damage += stoppingPower.extraDamage;
        }
        
        return damage;
    }
    
    private float GetDamageMultiplier(Projectile proj, Enemy enemy, bool isCriticalHit) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        
        if (!isCriticalHit && proj.isBackwardsShot) {
            isCriticalHit = true;
        }
        
        float multiplier = isCriticalHit ? gameplayConfig.defaultCritMultiplier : 1f;
        
        if (eyeInstance.doubleCrit.TryGetValue(out var doubleCrit)) {
            int consecutiveCriticalHits = demonEyeRaidStats.consecutiveCriticalHits;
            if (consecutiveCriticalHits > 0 && consecutiveCriticalHits % 2 == 0) {
                demonEyeRaidStats.lastDoubleCritActivationTime = Time.time;
            }

            if (Time.time - demonEyeRaidStats.lastDoubleCritActivationTime <= doubleCrit.multiplierDuration) {
                multiplier += doubleCrit.damageMultiplier;
            }
        }

        if (enemy.poison.TryGetValue(out var poison)) {
            if (enemy.health >= enemy.data.health * poison.minHealthPercentForMulti) {
                multiplier += poison.damageMulti;
            }
        }
        
        return multiplier;
    }

    private void SpawnExplosion(EntityPool<Entity> entityPool, Vector2 spawnPos, float radius, int damage, LayerMask mask, float damageDelay) {
        Entity expEntity = SpawnEntity(entityPool, spawnPos, Quaternion.identity); 
        DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
        
        Tween.Delay(damageDelay, () => {
            List<Collider2D> cols = inst.OverlapCircle(spawnPos, radius, mask);
            foreach (Collider2D col in cols) {
                if (mask == Masks.PlayerHurtMask) {
                    inst.DamagePlayer(damage);
                    continue;
                }
                Entity entity = inst.entityLookup[col.gameObject];
                if (entity is Enemy) {
                    inst.DamageEnemy(entityLookup[col.gameObject], damage, false);
                }
            }
        });
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

    private void UpdateExitPortalArrowUI() {
        return;
        if (!activeExitPortal) {
            portalArrow.gameObject.SetActive(false);
            return;
        }

        Vector3 portalPosInScreenSpace = mainCamera.WorldToScreenPoint(activeExitPortal.position);

        // Handle behind-camera targets by mirroring the direction
        if (portalPosInScreenSpace.z < 0) {
            portalPosInScreenSpace.x = Screen.width - portalPosInScreenSpace.x;
            portalPosInScreenSpace.y = Screen.height - portalPosInScreenSpace.y;
        }

        Vector2 screenCenter = new(Screen.width / 2f, Screen.height / 2f);
        Vector2 dirFromScreenCenter = ((Vector2)portalPosInScreenSpace - screenCenter).normalized;

        bool portalIsOnScreen = portalPosInScreenSpace.x > 0f && portalPosInScreenSpace.x < Screen.width 
                                && portalPosInScreenSpace.y > 0f && portalPosInScreenSpace.y < Screen.height;

        if (portalIsOnScreen) {
            portalArrow.gameObject.SetActive(false);
            return;
        }

        if (!portalArrow.gameObject.activeInHierarchy) {
            portalArrow.gameObject.SetActive(true);
            Tween.Scale(portalArrow, 0f, 1f, 0.5f, Ease.OutBack);
        }
        
        const float distFromScreenEdge = 50f;
        const float extraTopPadding = 130f;
        const float extraBottomPadding = 100f;
        
        float minX = distFromScreenEdge;
        float maxX = Screen.width - distFromScreenEdge;
        float minY = distFromScreenEdge + extraBottomPadding;
        float maxY = Screen.height - extraTopPadding - distFromScreenEdge;
        
        // Find where direction hits edge
        Vector2 edgePos = screenCenter;
        float slope = dirFromScreenCenter.y / dirFromScreenCenter.x;

        edgePos.x = dirFromScreenCenter.x > 0 ? maxX : minX;
        edgePos.y = screenCenter.y + (edgePos.x - screenCenter.x) * slope;

        // Clamp vertically with padding
        if (edgePos.y > maxY) {
            edgePos.y = maxY;
            edgePos.x = screenCenter.x + (edgePos.y - screenCenter.y) / slope;
        }
        else if (edgePos.y < minY) {
            edgePos.y = minY;
            edgePos.x = screenCenter.x + (edgePos.y - screenCenter.y) / slope;
        }

        // Convert to canvas-local coordinates (camera passed as null because canvas is set to screen overlay)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mainCanvasRectTransform, edgePos, null, out Vector2 canvasPos);
        portalArrow.anchoredPosition = canvasPos;

        // Rotate to face toward target
        float angle = Mathf.Atan2(dirFromScreenCenter.y, dirFromScreenCenter.x) * Mathf.Rad2Deg - 90f;
        portalArrow.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // ***************************
    // Spawning Map Items
    // ***************************
    
    private Dictionary<GameObject, InventorySlot[]> deadBodySlotsLookup = new();

    private void SpawnResources(Transform resourceSpawnParent) {
        List<Transform> spawnPoints = resourceSpawnParent.GetComponentsInChildren<Transform>().ToList();
        spawnPoints.RemoveAt(0); // Remove resourceSpawnParent
        
        int gemRocksToSpawn = Random.Range(loadedMapData.minRockCount, loadedMapData.maxRockCount);
        for (int i = 0; i < gemRocksToSpawn; i++) {
            Entity mineableRockEntity = SpawnResource<Entity>(gemRockPrefab, spawnPoints, 1);
            mineableRockEntity.health = 50;
        }

        int foragablesToSpawn = Random.Range(loadedMapData.minForageCount, loadedMapData.maxForageCount);
        for (int i = 0; i < foragablesToSpawn; i++) {
            SpawnResource(GetItemFromDropPool(foragingDropPool), spawnPoints);
        }
        
        int deadBodiesToSpawn = Random.Range(loadedMapData.minBodyCount, loadedMapData.maxBodyCount);
        InventorySlotUI[] lootInventorySlotUis = lootInventoryParent.GetComponentsInChildren<InventorySlotUI>(true);
        
        for (int i = 0; i < deadBodiesToSpawn; i++) {
            using var autoRelease = ListPool<Item>.Get(out List<Item> deadBodyItems);
            
            int maxDeadBodyItemCount = Random.Range(2, 6);
            GetUniqueItemsFromDropPool(bodyDropPool, maxDeadBodyItemCount, deadBodyItems);

            bool spawnEyeUpgrade = RollProbability(loadedMapData.eyeUpgradeOnBodyChance);
            while (spawnEyeUpgrade && deadBodyItems.Count < lootInvetoryPtr.slots.Length) {
                deadBodyItems.Add(GetItemFromDropPool(eyeUpgradesDropPool));
                spawnEyeUpgrade = RollProbability(loadedMapData.eyeUpgradeOnBodyChance);
            }
            
            InventorySlot[] deadBodySlots = new InventorySlot[lootInvetoryPtr.slots.Length];
            for (int j = 0; j < deadBodySlots.Length; j++) {
                InventoryItem inventoryItem = null;
                if (deadBodyItems.IndexInRange(j)) {
                    Item spawnItem = deadBodyItems[j];

                    int stackCount = 1;
                    float spawnRateTaper = 0f;
                    while (RollProbability(spawnItem.chanceToSpawnOnBody - spawnRateTaper)) {
                        stackCount++;
                        spawnRateTaper += spawnItem.chanceToSpawnOnBody * 0.15f;
                    }
                    
                    inventoryItem = new() {
                        itemOrInstanceUuid = spawnItem.uuid, 
                        count = stackCount,
                        notDiscovered = true,
                    };
                }
                deadBodySlots[j] = new() {
                    item = inventoryItem,
                    ui = lootInventorySlotUis[j],
                };
            }
            
            Entity body = SpawnResource<Entity>(deadBodyPrefab, spawnPoints);
            deadBodySlotsLookup.Add(body.gameObject, deadBodySlots);
        }
        
    }
    
    private T SpawnResource<T>(GameObject resourcePrefab, List<Transform> spawnPoints, int obstacleCellRadius = 0) where T : Entity, new() {
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform spawnTrans = spawnPoints[randomIndex];
        spawnPoints.RemoveAt(randomIndex);
        
        T resource = SpawnEntity<T>(resourcePrefab, spawnTrans.position, spawnTrans.rotation);

        if (obstacleCellRadius > 0) {
            loadedMapInst.grid.AddObstacle(resource.position, obstacleCellRadius);
            resource.obstacleCellRadius = obstacleCellRadius;
            resource.obstaclePosition = resource.position;
        }

        return resource;
    }
    
    private Entity SpawnResource(Item item, List<Transform> spawnPoints) {
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform spawnTrans = spawnPoints[randomIndex];
        spawnPoints.RemoveAt(randomIndex);
        return SpawnItemAsEntity(item, 1, spawnTrans.position, spawnTrans.rotation);
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

    [Serializable]
    private class RaidStateData {
        public int raidDifficulty;
        public List<string> mapSceneNames;
        [NonSerialized] public MapInstance CurrentMapInstance;
    }
    
    [Serializable]
    private class HideoutStateData {
        public int crucibleLevel;
        public int stashLevel;
        public int traderLevel;
        public int curTraderXpForLevel;
    }
    
    private string playerInventorySavePath;
    private string stashSavePath;
    private string crucibleSavePath;
    private string hideoutDataSavePath;
    private string raidDataSavePath;
    private string playerSavePath;
    private string questSavePath;
    private string traderSavePath;
    private string traderInventorySavePath;
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
        while (itemLookup.ContainsKey(newItemId)) {
            newItemId = UuidScriptableObject.GetIntUuid();
        }
        return newItemId;
    }
    
    private void LoadAllItems() {
        Item[] itemsFoundInFolder = Resources.LoadAll<Item>(string.Empty);
        foreach (Item item in itemsFoundInFolder) {
            if (item is Soulcard mod) {
                eyeModifierLookup.Add(mod.uuid, mod);
            }
            itemLookup.Add(item.uuid, item);
        }
    }

    [Serializable]
    private class PlayerSaveData {
        public int health;
        public int crucibleLevel;
        public int soulCurrency;
        public int coinCurrency;
        public int agilityLevel;
        public int bleedResLevel;
        public int healthLevel;
        public int strengthLevel;
    }

    private void SavePlayerData() {
        PlayerSaveData data = new() {
            health = player.health,
            crucibleLevel = player.crucibleLevel,
            soulCurrency = player.soulCurrency,
            coinCurrency = player.coinCurrency,
            agilityLevel = player.agilityLevel,
            bleedResLevel = player.bleedResLevel,
            healthLevel = player.healthLevel,
            strengthLevel = player.strengthLevel,
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
            instancedPlayer.agilityLevel = data.agilityLevel;
            instancedPlayer.bleedResLevel = data.bleedResLevel;
            instancedPlayer.healthLevel = data.healthLevel;
            instancedPlayer.strengthLevel = data.strengthLevel;
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
        
        menuBackButton.gameObject.SetActive(false);
        largeRaidTextTypewriter.gameObject.SetActive(false);

        // Set the stat upgrade info once at startup because each increase is the same
        {
            const float speedRange = maxPlayerSpeed - defaultPlayerSpeed;
            float speedPercentIncreasePerLevel = (speedRange / agilityUpgradePath.MaxLevel) / defaultPlayerSpeed;
            agilityUpgradeInfoText.text = $"+{(speedPercentIncreasePerLevel * 100f):0}% Speed";
            healthUpgradeInfoText.text = $"+{healthIncreasePerStatLevel} Health";
            bleedResInfoText.text = $"+{(reducedChanceForBleedPerLevel * 100f):0}% Bleed Resistance";
            strengthUpgradeInfoText.text = $"+{encumberingIncreasePerStrengthPoint} Weight Carry Capacity";
        }
        
        // Leaving this in so I remember how to make things hoverable
        // hoverableUIElements.Add(forgeEyeButton.rectTransform);
        // hoverableUIElements.Add(upgradeForgeButton.rectTransform);
        
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
        ToggleSlimPlayerPanel(false);
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
        levelupTabButton.image.sprite = tabNonSelectedSprite;
        
        characterTabText.margin = styles.nonSelectedHideoutTabMargin;
        eyeForgeTabText.margin = styles.nonSelectedHideoutTabMargin;
        traderTabText.margin = styles.nonSelectedHideoutTabMargin;
        questsTabText.margin = styles.nonSelectedHideoutTabMargin;
        levelupTabText.margin = styles.nonSelectedHideoutTabMargin;
        
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
        levelupPanel.gameObject.SetActive(false);
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
            ToggleSlimPlayerPanel(false);
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
        
        levelupTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(levelupTabButton, levelupTabText);
            ToggleSlimPlayerPanel(false);
            ToggleHideoutPanels(playerPanel, levelupPanel);
        });

        agilityUpgradeButton.AddListener(() => OnLevelupButtonPressed(agilityUpgradePath, player.agilityLevel));
        corruptionUpgradeButton.AddListener(() => OnLevelupButtonPressed(luckUpgradePath, player.bleedResLevel));
        healthUpgradeButton.AddListener(() => OnLevelupButtonPressed(healthUpgradePath, player.healthLevel));
        strengthUpgradeButton.AddListener(() => OnLevelupButtonPressed(strengthUpgradePath, player.strengthLevel));
        
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
                    modifierUuids = new(),
                };

                foreach (InventorySlot slot in crucibleInventory.slots) {
                    slot.ui.itemUI.rectTransform.anchoredPosition = Vector2.zero;
                    slot.ui.itemUI.rectTransform.localScale = Vector3.one;
                    
                    if (slot.item == null) continue;
                    
                    if (slot.ui.OnlyAcceptsType(soulcardType)) {
                        newDemonEyeItem.modifierUuids.Add(slot.item.ItemRef.uuid);
                    }
                    slot.item = null;
                }

                DemonEyeInstance newDemonEye = BuildAndRegisterEye(newDemonEyeItem);
                crucibleInventory.slots[eyeSlotIndex].item = newDemonEyeItem;
                
                onEyeForged?.Invoke(newDemonEye);
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
        
        traderDealButton.onClick.AddListener(() => {
            InventoryValueType valueType = transactionState == TransactionState.Buying ? InventoryValueType.Buy : InventoryValueType.Sell;
            if (GetInventoryItemCount(transactionInventory) <= 0) return;
            
            int price = GetInventoryValue(transactionInventory, valueType);
            
            if (transactionState == TransactionState.Buying && player.coinCurrency >= price) {
                player.coinCurrency -= price;
                
                for (int i = 0; i < transactionInventory.slots.Length; i++) { 
                    MoveEntireItemStack(transactionInventory, stashInventory, i);
                }
                
                // After buying items we just make sure all items in stash are no longer trader owned
                ClearItemsAsTraderOwned(stashInventory);
            }
            else if (transactionState == TransactionState.Selling) {
                // Before selling items we pass the transaction inventory to callbacks that want to know what we sold
                onSoldItemsToTrader?.Invoke(transactionInventory.slots);
                player.coinCurrency += price;
                ClearInventory(transactionInventory);
            }
        });
        
        easyMapButton.onClick.AddListener(() => {
            LoadMapAsync(lighthouseMap, () => {
                CreateDropPoolsForMap(lighthouseMap);
                gameStateMachine.SetStateIfNotCurrent(raidState);
            });
        });
        
        mediumMapButton.onClick.AddListener(() => {
            LoadMapAsync(customsMap, () => {
                CreateDropPoolsForMap(customsMap);
                gameStateMachine.SetStateIfNotCurrent(raidState);
            });
        });
    }

    private void UpdateEyeForgeInfoPanel() {
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
            ArrangeEyeCrucibleInventorySlots();
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

    // Its better just to have these as constants because the canvas layout recalculates in LateUpdate
    private const float playerPanelWidth = 570f;
    private const float playerPanelLeftHalfWidth = 290f;
    
    private void ToggleSlimPlayerPanel(bool toggle) {
        if (toggle) {
            playerPocketsBackpackParent.gameObject.SetActive(false);
            playerPanel.GetComponent<LayoutElement>().preferredWidth = playerPanelLeftHalfWidth;
            return;
        }
        
        playerPocketsBackpackParent.gameObject.SetActive(true);
        playerPanel.GetComponent<LayoutElement>().preferredWidth = playerPanelWidth;
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
            
            Dictionary<Soulcard, int> allSoulCards = new();
            
            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.item == null || slot.item.ItemRef.type != soulcardType) continue;    
                Soulcard soulcard = itemLookup[slot.item.itemOrInstanceUuid] as Soulcard;
                if (!allSoulCards.TryAdd(soulcard, 1)) {
                    allSoulCards[soulcard]++;
                }
            }

            List<(Soulcard, int)> sortedSoulcards = SortSoulcardsFromDictionary(allSoulCards);
            
            string eyeDescription = "";
            foreach ((Soulcard soulcard, int count) in sortedSoulcards) {
                eyeDescription += GetDemonEyeModDescription(soulcard, count);
            }
            forgeDetailsForgeText.text += eyeDescription;
        }
    }

    private enum TransactionState { Empty, Buying, Selling }
    private TransactionState transactionState;
    
    private void UpdateTraderTransactionState() {
        if (!OnTradingTab) return;
        
        if (GetInventoryItemCount(transactionInventory) <= 0) {
            transactionState = TransactionState.Empty;
            RefreshTransactionUI();
            return;
        }
        
        bool itemsAreTraderOwned = false;
        foreach (InventorySlot slot in transactionInventory.slots) {
            if (slot.item == null) continue;
            itemsAreTraderOwned = slot.item.traderOwned;
        }

        transactionState = itemsAreTraderOwned ? TransactionState.Buying : TransactionState.Selling;
        RefreshTransactionUI();
    }
    
    private void RefreshTransactionUI() {
        if (transactionState == TransactionState.Empty) {
            traderTransactionInfoText.text = "Place an item to begin transaction";
            return;
        }
        
        if (transactionState == TransactionState.Buying) {
            int buyPrice = GetInventoryValue(transactionInventory, InventoryValueType.Buy);
            string buyPriceString = ColorText(buyPrice.ToString("N0"), styles.coinCurrencyColor);
            traderTransactionInfoText.text = $"Purchase for <sprite=0>{buyPriceString}";
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            string sellPriceString = ColorText(sellPrice.ToString("N0"), styles.coinCurrencyColor);
            traderTransactionInfoText.text = $"Sell for <sprite=0>{sellPriceString}";
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

        if (AddToTraderRep(repGain)) {
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

    private bool AddToTraderRep(int repGain) {
        int prevLevel = GetTraderRepLevel();
        traderSaveData.traderRep += repGain;
        SaveTrader();
        int repLevel = GetTraderRepLevel();
        return prevLevel < repLevel;
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

    private const int activeQuestCount = 2;
    private Quest[] activeQuests = new Quest[activeQuestCount];
    private QuestUI[] questUIs = new QuestUI[activeQuestCount];
    
    [Serializable]
    private class QuestlineStateData {
        public Quest.SaveState[] questSaveStates = new Quest.SaveState[activeQuestCount];
        public int[] questLineIndicies = new int[activeQuestCount];
    }
    
    private QuestlineStateData questlineState;
    
    private void SaveQuestStates() {
        for (int i = 0; i < activeQuestCount; i++) {
            questlineState.questSaveStates[i] = activeQuests[i]?.GetSaveState();
        }
        SaveToFile(questSavePath, questlineState);
    }
    
    private void InitQuests() {
        questlineState = LoadFromFileOrCreateNew<QuestlineStateData>(questSavePath);
        
        for (int i = 0; i < activeQuestCount; i++) {
            Quest quest = questLines[i].quests[questlineState.questLineIndicies[i]];
            activeQuests[i] = quest;
            
            quest.Init();
            
            Quest.SaveState saveState = questlineState.questSaveStates[i];
            if (saveState != null) {
                quest.LoadSaveState(saveState); 
            }

            QuestUI ui = Instantiate(questPrefab, questsParent).GetComponent<QuestUI>();
            questUIs[i] = ui;
            
            int callbackIndex = i;
            ui.completeButton.AddListener(() => OnQuestCompleteClicked(callbackIndex));
            ui.Display(quest);
        }
    }

    private void RefreshQuestDisplays() {
        for (int i = 0; i < activeQuests.Length; i++) {
            questUIs[i].Display(activeQuests[i]);
        }
    }

    private void OnQuestCompleteClicked(int activeQuestIndex) {
        activeQuests[activeQuestIndex].Deinit();
    }

    // ************************
    // Leveling Up
    // ************************

    private void OnLevelupButtonPressed(StatUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeStat(upgradePath, playerStatLevel);
        if (result == UpgradeStatResult.CantAfford || result == UpgradeStatResult.AtMaxLevel) return;
        
        player.soulCurrency -= upgradePath.soulsNeededPerLevel[playerStatLevel];

        if (upgradePath == agilityUpgradePath) {
            player.agilityLevel++;
        }
        else if (upgradePath == luckUpgradePath) {
            player.bleedResLevel++;
        }
        else if (upgradePath == healthUpgradePath) {
            int prevFullPlayerHealth = FullPlayerHealth;
            player.healthLevel++;
            int newFullPlayerHealth = FullPlayerHealth;
            player.health += newFullPlayerHealth - prevFullPlayerHealth;
        }
        else if (upgradePath == strengthUpgradePath) {
            player.strengthLevel++;
        }
        
        SavePlayerData();
        RefreshLevelUpPossibilities();
    }
    
    private void RefreshLevelUpPossibilities() {
        ToggleStatUpgradeButton(agilityUpgradeButton, agilityUpgradePath, player.agilityLevel);
        ToggleStatUpgradeButton(corruptionUpgradeButton, luckUpgradePath, player.bleedResLevel);
        ToggleStatUpgradeButton(healthUpgradeButton, healthUpgradePath, player.healthLevel);
        ToggleStatUpgradeButton(strengthUpgradeButton, strengthUpgradePath, player.strengthLevel);
    }

    private void ToggleStatUpgradeButton(ButtonFeel button, StatUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeStat(upgradePath, playerStatLevel);
        switch (result) {
            case UpgradeStatResult.CantAfford:
                button.Disable();
                button.text.text = $"{upgradePath.soulsNeededPerLevel[playerStatLevel]:N0} Souls";
                break;
            case UpgradeStatResult.Affordable:
                button.Enable();
                button.text.text = $"{upgradePath.soulsNeededPerLevel[playerStatLevel]:N0} Souls";
                break;
            case UpgradeStatResult.AtMaxLevel:
                button.Disable();
                button.text.text = "Max";
                break;
        }
    }

    private enum UpgradeStatResult { CantAfford, Affordable, AtMaxLevel }
    
    private UpgradeStatResult CanUpgradeStat(StatUpgradePath upgradePath, int playerStatLevel) {
        if (!upgradePath.soulsNeededPerLevel.IndexInRange(playerStatLevel)) {
            return UpgradeStatResult.AtMaxLevel;
        }
        if (player.soulCurrency >= upgradePath.soulsNeededPerLevel[playerStatLevel]) {
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

    private enum DropOrigin { Rock, Body, Trader, Enemy, ExistsInLevel }

    private struct DropPool {
        public List<Item> items;
        public DropOrigin dropOrigin;
    }

    private DropPool rockStonesDropPool;
    private DropPool eyeUpgradesDropPool;
    private DropPool bodyDropPool;
    private DropPool traderDropPool;
    private DropPool enemyDropPool;
    private DropPool foragingDropPool;

    private void CreateDropPools() {
        rockStonesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        eyeUpgradesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        bodyDropPool = new() { items = new(), dropOrigin = DropOrigin.Body };
        traderDropPool = new() { items = new(), dropOrigin = DropOrigin.Trader };
        enemyDropPool = new() { items = new(), dropOrigin = DropOrigin.Enemy };
        foragingDropPool = new() { items = new(), dropOrigin = DropOrigin.ExistsInLevel };

        foreach ((int _, Item item) in itemLookup) {
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
        
        foreach ((int _, Item item) in itemLookup) {
            bool spawnsOnCurrentMap = item.spawnsOnAllMaps || item.spawnsOnMaps.Contains(map);
            if (!spawnsOnCurrentMap) continue;
            
            if (item.chanceToSpawnFromRock > 0f) {
                if (item.type == soulcardType) {
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

    private Item GetItemFromDropPool(DropPool dropPool, bool allowNullReturns = false) {
        Assert.IsFalse(dropPool.items == enemyDropPool.items, $"Use {nameof(GetItemFromEnemyDropPool)} for enemies");
        
        dropPool.items.Shuffle();
        
        foreach (Item drop in dropPool.items) {
            float dropChance = GetDropChanceOfItem(drop, dropPool.dropOrigin);
            if (Random.value < dropChance) {
                return drop;
            }
        }

        return allowNullReturns ? null : dropPool.items[^1];
    }
    
    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, List<Item> items, float raritySkew = 0f) {
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
            DropOrigin.Rock => Mathf.Clamp01(item.chanceToSpawnFromRock + addChanceToSpawnFromLuck),
            DropOrigin.Body => Mathf.Clamp01(item.chanceToSpawnOnBody + addChanceToSpawnFromLuck),
            DropOrigin.Trader => Mathf.Clamp01(item.chanceToSpawnOnTrader + addChanceToSpawnFromLuck),
            DropOrigin.Enemy => Mathf.Clamp01(item.chanceToSpawnFromEnemy + addChanceToSpawnFromLuck),
            _ => 0f,
        };
    }
    
    public static bool RollProbability(float probability) {
        return Random.value < probability;
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
    
    public static string DisplayProbIncrease(float probability) {
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", inst.styles.increaseDescColor);
    }
    
    public static string DisplayProbDecrease(float probability) {
        return ColorText($"-{Mathf.FloorToInt(probability * 100f)}%", inst.styles.decreaseDescColor);
    }

    
    public static string DisplayNumber(int number) {
        return ColorText(number.ToString(), inst.styles.timeDescColor);
    }
    
    public static string DisplayNumber(float number) {
        return ColorText(number.ToString("0.00"), inst.styles.timeDescColor);
    }


    public static string DisplayIncrease(int amount) {
        return ColorText($"+{amount}", inst.styles.increaseDescColor);
    }
    
    public static string DisplayIncrease(float amount) {
        return ColorText($"+{amount:0.00}", inst.styles.increaseDescColor);
    }
    
    public static string DisplayDecrease(float amount) {
        return ColorText($"-{amount:0.00}", inst.styles.decreaseDescColor);
    }
    
    public static string DisplayMultiplier(float multiplier) {
        Color textColor = multiplier >= 1f ? inst.styles.increaseDescColor : inst.styles.decreaseDescColor;
        return ColorText($"{multiplier:0.00}x", textColor);
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