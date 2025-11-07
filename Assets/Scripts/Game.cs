using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;

public class Game : MonoBehaviour {

    public static Game instance;
    
    public TraderConfig traderConfig;
    public StartingItemsConfig startingItems;
    public Styles styles;
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
    public GameObject stoppingPowerProjectilePrefab;
    public GameObject bloodDropPrefab;
    public GameObject poisonDebuffPrefab;
    public GameObject explosionPrefab;
    public GameObject projectileImpactPrefab;
    public GameObject teleportInPrefab;
    public GameObject teleportOutPrefab;
    public GameObject bloodSplatterPrefab;
    public GameObject runSmokePrefab;
    [EndFoldout]
    
    [Foldout("Item Type Refs")]
    public ItemType consumableType;
    public ItemType backpackType;
    public ItemType eyeType;
    public ItemType demonEyeType;
    public ItemType trinketType;
    public ItemType soulcardType;
    public ItemType passiveType;
    [EndFoldout]

    [Foldout("Item Refs")]
    public Item bandageItem;
    public Item healthPotionItem;
    public Item demonSteakItem;
    public Item pouchItem;
    public Item ruckSackItem;
    public DemonClaw demonClawItem;
    [EndFoldout]
    
    [Foldout("Stat Upgrade Paths")]
    public StatUpgradePath agilityUpgradePath;
    public StatUpgradePath corruptionUpgradePath;
    public StatUpgradePath healthUpgradePath;
    public StatUpgradePath strengthUpgradePath;
    [EndFoldout]
    
    [Foldout("Gameplay Variables")]
    [Range(0f, 1f)] public float defaultCriticalStrikeChange;
    public float defaultCriticalStrikeMultiplier;
    [EndFoldout]

    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public PixelPerfectCamera pixelPerfectCamera;
    public RectTransform crosshairTrans;

    public GameObject playerPrefab;
    public GameObject gemRockPrefab;
    public GameObject deadBodyPrefab;

    public BaseCharacterStats baseStats;
    public CoreAttack defaultAttack;
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
    public GameObject questPrefab;
    [EndFoldout]

    [Foldout("UI/MiscRefs")]
    public RectTransform mainCanvasRectTransform;
    public ItemDescPopup itemDescPopup;
    public MechanicDescPopup mechanicDescPopup;
    public UIElementPopup uiElementPopup;
    public RectTransform hideoutParent;
    public RectTransform hideoutHeaderParent;
    public ItemUI dragAndDropItemUI;
    public Image menuBackgroundImage;
    public GameObject currenciesParent;
    public TextMeshProUGUI soulsCurrencyText;
    public TextMeshProUGUI coinCurrencyText;
    public Image deathBackgroundImage;
    public RectTransform portalArrow;
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
    public RectTransform playerBackpackParent;
    public RectTransform playerPocketsBackpackParent;
    public RectTransform playerPassiveParent;
    public RectTransform playerInventoryParent;
    public TextMeshProUGUI playerPanelHealthText;
    public TextMeshProUGUI playerPanelWeightText;
    public TextMeshProUGUI agilityStatValueText;
    public TextMeshProUGUI armorStatValueText;
    public TextMeshProUGUI bleedResStatValueText;
    public TextMeshProUGUI healthStatValueText;
    public TextMeshProUGUI luckStatValueText;
    public TextMeshProUGUI strengthStatValueText;
    [EndFoldout]
    
    [Foldout("UI/StashPanel")]
    public RectTransform stashPanel;
    public RectTransform stashInventoryParent;
    public TextMeshProUGUI stashValueText;
    public Button stashUpgradeButton;
    [EndFoldout]
    
    [Foldout("UI/EyeForgePanel")]
    public RectTransform eyeForgePanel;
    public RectTransform crucibleParent;
    public ButtonFeel forgeEyeButton;
    public ButtonFeel upgradeForgeButton;
    public Image pentagramFillImage;
    public AnimationCurve pentagramFillCurve;
    public AnimationCurve itemShakeCurve;
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
    public Button traderDealButton;
    public TraderButton potionManTraderButton;
    public TraderButton armsDealerTraderButton;
    public TraderButton hatManTraderButton;
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
    public TextMeshProUGUI luckUpgradeInfoText;
    public TextMeshProUGUI strengthUpgradeInfoText;
    [EndFoldout]

    [Foldout("UI/InRaid")]
    public RectTransform lootInventoryPanel;
    public RectTransform lootInventoryParent;
    public RectTransform playerBarsPanel;
    public Image healthBarFillImage;
    public Image weightBarFillImage;
    public GameObject interactPrompt;
    public TextMeshProUGUI exitPortalStatusText;
    public TextMeshProUGUI raidTimerText;
    [EndFoldout]

    [Foldout("UI/DamageNumbers")]
    public RectTransform damageNumbersParent;
    [EndFoldout]
    
    [Foldout("UpgradePaths")]
    public UpgradePath crucibleUpgradePath; 
    public UpgradePath stashUpgradePath; 
    [EndFoldout]
    
    [Foldout("TraderLevels")]
    public TraderLevels traderLevels;
    [EndFoldout]
    
    [Foldout("Sfx")]
    public GameObject dynamicAudioSourcePrefab;
    public DynamicClip shootClip;
    public DynamicClip stoneBreakClip;
    public DynamicClip stoneHitClip;
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
    
    [NonSerialized] public List<Entity> entities = new();
    [NonSerialized] public Dictionary<GameObject, Entity> entityLookup = new();
    [NonSerialized] public List<Enemy> enemies = new();
    
    public static Dictionary<int, Item> itemLookup = new();
    public static Dictionary<int, Soulcard> eyeModifierLookup = new();

    private Timer exitPortalTimer;
    private int consecutiveCriticalHits;

    private EntityPool<Entity> itemDropPool;
    private EntityPool<Entity> bloodDropPool;
    private EntityPool<Projectile> projectilePool;
    private EntityPool<Projectile> stoppingPowerProjectilePool;
    private EntityPool<Entity> poisonDebuffPool;
    private EntityPool<Entity> explosionPool;
    private EntityPool<Entity> projectileImpactPool;
    private EntityPool<Entity> teleportInPool;
    private EntityPool<Entity> teleportOutPool;
    private EntityPool<Entity> bloodSplatterPool;
    private EntityPool<Entity> runSmokePool;
    
    private State mainMenuState;
    private State mapSelectionState;
    private State hideoutState;
    private State raidState;
    private State gameOverState;
    private State gameWinState;
    private StateMachine gameStateMachine = new();

    public static Action<Enemy> onEnemyDeath;
    
    private void Start() {
        instance = this;
        
        LoadAllItems();
        InitAudio();
        
        BuildSavePaths();
        player = SpawnEntity<Player>(playerPrefab, Vector3.zero, Quaternion.identity, null, EntityLifetime.Global);
        player.gameObject.SetActive(false);
        LoadAndAssignPlayerSaveData(player);

        DemonEyeTween.Init();
        CreateDropPools();
        
        InitInventories();
        InitButtonCallbacks();
        InitTraders();
        InitQuests();
        
        Cursor.visible = true;
        OnGameStartInitUI();
        
        itemDropPool = CreateEntityPool<Entity>(itemDropPrefab, 20, null);
        bloodDropPool = CreateEntityPool<Entity>(bloodDropPrefab, 10, null);
        projectilePool = CreateEntityPool<Projectile>(baseProjectilePrefab, 20, OnSpawnProjectile);
        stoppingPowerProjectilePool = CreateEntityPool<Projectile>(stoppingPowerProjectilePrefab, 20, OnSpawnProjectile);
        poisonDebuffPool = CreateEntityPool<Entity>(poisonDebuffPrefab, 10, null);
        explosionPool = CreateEntityPool<Entity>(explosionPrefab, 5, null);
        projectileImpactPool = CreateEntityPool<Entity>(projectileImpactPrefab, 20, null);
        teleportInPool = CreateEntityPool<Entity>(teleportInPrefab, 20, null);
        teleportOutPool = CreateEntityPool<Entity>(teleportOutPrefab, 20, null);
        bloodSplatterPool = CreateEntityPool<Entity>(bloodSplatterPrefab, 20, null);
        runSmokePool = CreateEntityPool<Entity>(runSmokePrefab, 5, null);

        equipedEye = new() { coreAttack = defaultAttack };
        
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

        escapeInputAction.performed += OnEscapePressed;

        mainMenuState = gameStateMachine.CreateState(null, OnMainMenuStateEnter, OnMainMenuStateExit);
        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        mapSelectionState = gameStateMachine.CreateState(null, OnMapSelectionEnter, OnMapSelectionExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEnter, OnRaidStateExit);
        gameWinState = gameStateMachine.CreateState(null, OnGameWinEnter, OnGameWinExit);
        gameOverState = gameStateMachine.CreateState(null, OnGameOverEnter, OnGameOverExit);
        
        raidState.To(gameOverState).When(() => player.health <= 0);
    }

    private void Update() {
        UpdateDelayedEntitiesToDestroy();
        gameStateMachine.Tick();
        DemonEyeTween.Update();
        UpdateQuests();
        foreach (Inventory inventory in allInventories) {
            RefreshInventoryDisplay(inventory);
        }
        UpdateGraySlots();
    }

    private void FixedUpdate() {
        if (!InRaid) return;
        currentMapInstance.grid.CompleteFlowFieldCalculation();
        currentMapInstance.grid.ScheduleFlowFieldCalculation(player.position);
        FixedUpdateEnemies();
    }

    private void LateUpdate() {
        UpdateDragAndDropItemToCursor();
        if (currenciesParent.activeInHierarchy) {
            UpdateCurrencyNumbers();
        }
        if (InRaid) {
            UpdateInRaidUI();
            UpdateExitPortalArrowUI();
        }
        if (InHideout || InRaid) {
            UpdatePlayerPanelUI();
        }
    }

    private void UpdateTimers() {
        exitPortalTimer.Tick();
        discoverLootTimer.Tick();
    }

    private void OnMainMenuStateEnter() {
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
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
        SaveInventory(crucibleInventory);
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

    private void OnRaidStateEnter() {
        Cursor.visible = false;
        ShowRaidUI();

        deathBackgroundImage.enabled = false;
        
        currentMapInstance.gameObject.SetActive(true);
        currentMapInstance.grid.Init();

        int randomSpawnIndex = Random.Range(0, currentMapInstance.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = currentMapInstance.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.gameObject.SetActive(true);
        player.position = randomSpawnPos;
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, cinemachineCamera.transform.position.z);
        cinemachineCamera.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        cinemachineCamera.Follow = player.trans;
        
        InitSpawnManager(currentMapInstance.waves);
        SpawnResources(currentMapInstance.resourceParent);
        InitExitPortals(currentMapInstance.exitPortalsParent, currentMapInstance.waves.timeBeforeExitPortalsSpawn);
    }

    private void OnRaidStateExit() {
        ClosePlayerInventory();
        CloseLootInventory();
        HideItemDescPopup();
        HideUIElementPopup();
        
        Cursor.visible = true;
        CloseRaidUI();
    }

    private void OnRaidStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        UpdateSpawnManager();
        UpdateEnemies();
        UpdateEntityEffects();
    }

    private void OnGameWinEnter() {
        OnSaveWhenRaidIsOver();
        gameStateMachine.SetStateIfNotCurrent(hideoutState);
    }

    private void OnGameWinExit() {
        DeinitRaid();
    }

    private void OnGameOverEnter() {
        ClearInventory(playerInventory);
        OnSaveWhenRaidIsOver();
        
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
        
        RemoveHitFlashEffect(player);
        player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
        player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 1f);
        player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 0f;

        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        sequence.Chain(Tween.UIFillAmount(deathBackgroundImage, 1f, 1f, Ease.InOutQuad));
        sequence.ChainCallback(() => {
            player.animator.enabled = true;
            player.animator.Play(PlayerDeathHash);
        });
        
        sequence.Group(Tween.Custom(1f, 0f, 0.5f, val => {
            player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
            player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, val);
            player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        }, Ease.OutExpo));
        
        int initialRefResoultion = pixelPerfectCamera.refResolutionX;
        
        sequence.Group(Tween.Custom(pixelPerfectCamera.refResolutionX, 15, 0.8f, val => {
            pixelPerfectCamera.refResolutionX = (int)val;
            pixelPerfectCamera.refResolutionY = (int)val;
        }, Ease.InOutQuad));
        
        sequence.ChainDelay(1f);

        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Chain(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.5f));

        sequence.Group(Tween.Scale(player.trans, Vector3.zero, 1.5f, Ease.InOutQuint));
        
        sequence.OnComplete(() => {
            player.spriteRenderer.sortingLayerName = "Entity";
            player.trans.localScale = Vector3.one;
            pixelPerfectCamera.refResolutionX = initialRefResoultion;
            pixelPerfectCamera.refResolutionY = initialRefResoultion;
            gameStateMachine.SetStateIfNotCurrent(mainMenuState);
        });
    }
    
    private void OnGameOverExit() {
        player.health = FullPlayerHealth;
        DeinitRaid();
    }

    private void DeinitRaid() {
        currentMapInstance.grid.Deinit();
        
        DestroyLevelEntities();
        UnloadCurrentMapAsync();
        playerBarsPanel.gameObject.SetActive(false);
        player.gameObject.SetActive(false);
        
        RefillTraderSlotsWithItems(potionManTrader);
        RefillTraderSlotsWithItems(armsDealerTrader);
        RefillTraderSlotsWithItems(hatManTrader);
    }
    
    private void OnSaveWhenRaidIsOver() {
        SaveInventory(playerInventory);
        SavePlayerData();
        SaveQuestStates();
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
        public List<T> inUseList = new();

        public void ReleaseEntity(Entity entity) {
            Assert.IsFalse(standbyList.Contains((T)entity), "Already released this entity!");
            entity.gameObject.SetActive(false);
            standbyList.Add((T)entity);
            inUseList.Remove((T)entity);
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
        
        public MaterialPropertyBlock matPropertyBlock = new();
        public IEntityPooler entityPool;
        public int health;
        public int damageAccumilation;
        
        public ScaleEffect? scaleEffect;
        public HitFlashEffect? hitFlashEffect;
        public PoisonedEffect? poisonedEffect;
        public BounceEffect? bounceEffect;
        public ParentToEntity? parentEffect;
        public TweenPosition? tweenPosition;
        public ShakeEffect? shakeEffect;
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }

        public Vector3 Center => collider.bounds.center;
        public bool IsValid => trans;
        public GameObject gameObject => trans.gameObject;
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
        pool.inUseList.Add(entity);
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
    
    private void DestroyEntity(GameObject gameObj) {
        DestroyEntity(entityLookup[gameObj]);
    }
    
    private void DestroyEntityAtIndex(int entityIndex) {
        Entity entity = entities[entityIndex];
        entityLookup.Remove(entity.gameObject);
        entities.RemoveAt(entityIndex);
        DestroyEntity(entity);
    }
    
    private void DestroyEntity(Entity entity) {
        RemoveHitFlashEffect(entity);
        RemovePoisonedEffect(entity);
        entity.bounceEffect = null;
        entity.parentEffect = null;

        // Remove from delay list here to prevent possible double frees
        if (entitiesWaitingToBeDestroyed.Contains(entity)) {
            int index = delayedEntitiesToDestroy.FindIndex(x => x.Item1 == entity);
            delayedEntitiesToDestroy.RemoveAt(index);
            entitiesWaitingToBeDestroyed.Remove(entity);
        }
        
        // May be removed already from DestroyAtIndex which is faster than Remove
        bool enemyWasInLookup = entityLookup.Remove(entity.gameObject, out _);
        if (enemyWasInLookup) {
            entities.Remove(entity);
        }
        
        DestroyOrReleaseEntitysGameObject(entity);
    }
    
    private void DestroyOrReleaseEntitysGameObject(Entity entity) {
        if (entity.entityPool == null) {
            Destroy(entity.gameObject);
            return;
        }
        entity.gameObject.transform.SetParent(transform, true);
        entity.entityPool.ReleaseEntity(entity);
    }

    private List<(Entity, float)> delayedEntitiesToDestroy = new(20);
    private HashSet<Entity> entitiesWaitingToBeDestroyed = new(20);
    
    private void DestroyEntity(Entity entity, float delay) {
        Assert.IsFalse(entitiesWaitingToBeDestroyed.Contains(entity), "Already added entity to be destroyed");
        entitiesWaitingToBeDestroyed.Add(entity);
        delayedEntitiesToDestroy.Add((entity, delay));
    }

    private void UpdateDelayedEntitiesToDestroy() {
        for (int i = delayedEntitiesToDestroy.Count - 1; i >= 0; i--) {
            (Entity entity, float time) = delayedEntitiesToDestroy[i];
            time -= Time.deltaTime;
            if (time <= 0f) {
                DestroyEntity(entity);
                continue;
            }
            delayedEntitiesToDestroy[i] = (entity, time);
        }
    }
    
    private void UpdateEntityEffects() {
        foreach (Entity entity in entities) {
            UpdateScaleEffect(entity);
            UpdateHitFlashEffect(entity);
            UpdatePoisonedEffect(entity);
            UpdateBounceEffect(entity);
            UpdateParentEffect(entity);
            UpdateTweenPosition(entity);
            UpdateShakeEffect(entity);
        }
    }
    
    public struct ScaleEffect {
        public Vector3 targetScale;
        public Timer timer;
    }

    private void AddScaleEffect(Entity entity, float scalePercent, float duration) {
        if (entity.scaleEffect.TryGetValue(out var oldScale)) {
            entity.trans.localScale = oldScale.targetScale;
        }
        ScaleEffect scale = new() {
            targetScale = entity.trans.localScale,
        };
        entity.trans.localScale *= scalePercent;
        scale.timer.SetTime(duration);
        entity.scaleEffect = scale;
    }

    private void UpdateScaleEffect(Entity entity) {
        if (!entity.scaleEffect.TryGetValue(out var scale)) return;
        scale.timer.Tick();
        float comp = scale.timer.Comp();
        entity.trans.localScale = Vector3.Lerp(entity.trans.localScale, scale.targetScale, comp);  
        entity.scaleEffect = scale;
    }

    
    private int damageFlashTintPropertyId = Shader.PropertyToID("_DamageFlashTint");
    
    public struct HitFlashEffect {
        public Timer timer;
    }

    private void AddFlashHitEffect(Entity entity) {
        HitFlashEffect hitFlash = new();
        float duration = hitFlashCurve.keys[^1].time;
        hitFlash.timer.SetTime(duration);
        entity.hitFlashEffect = hitFlash;
    }

    private void UpdateHitFlashEffect(Entity entity) {
        if (!entity.hitFlashEffect.TryGetValue(out var hitFlash)) return;

        if (hitFlash.timer.IsFinished) {
            RemoveHitFlashEffect(entity);
            return;
        }
        
        hitFlash.timer.Tick();
        float comp = hitFlash.timer.Comp();
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, hitFlashCurve.Evaluate(comp));
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        entity.hitFlashEffect = hitFlash;
    }

    private void RemoveHitFlashEffect(Entity entity) {
        if (!entity.hitFlashEffect.HasValue) return;
        
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 0);
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        entity.hitFlashEffect = null;
    }


    private static int poisonedPropertyId = Shader.PropertyToID("_Poisoned");
    
    public struct PoisonedEffect {
        public Timer timer;
        public Entity poisonDebuff;
    }

    public void AddPoisonedEffect(Entity entity, float duration) {
        if (entity.poisonedEffect.TryGetValue(out var oldPoison)) {
            DestroyEntity(oldPoison.poisonDebuff);
        }
        
        PoisonedEffect poisoned = new();
        poisoned.timer.SetTime(duration);
        poisoned.poisonDebuff = SpawnEntity(poisonDebuffPool, OffsetY(entity.position, -0.14f), Quaternion.identity, entity.trans);
        entity.poisonedEffect = poisoned;
        
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(poisonedPropertyId, 1);
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
    }

    private void UpdatePoisonedEffect(Entity entity) {
        if (!entity.poisonedEffect.TryGetValue(out var poison)) return;

        if (poison.timer.IsFinished) {
            RemovePoisonedEffect(entity);
            return;
        }
        
        poison.timer.Tick();
        entity.poisonedEffect = poison;
    }

    private void RemovePoisonedEffect(Entity entity) {
        if (!entity.poisonedEffect.HasValue) return;
        
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(poisonedPropertyId, 0);
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        DestroyEntity(entity.poisonedEffect.Value.poisonDebuff);
        entity.poisonedEffect = null;
    }
    
    
    public struct BounceEffect {
        public Vector2 targetPos;
        public Vector2 initialPos;
        public Timer timer;
    }

    private void AddBounceEffect(Entity entity, Vector3 pos, float duration) {
        BounceEffect bounce = new() {
            targetPos = pos,
            initialPos = entity.position
        };
        bounce.timer.SetTime(duration);
        entity.bounceEffect = bounce;
    }

    private void UpdateBounceEffect(Entity entity) {
        if (!entity.bounceEffect.TryGetValue(out var bounce)) return;
        
        bounce.timer.Tick();
        float comp = bounce.timer.Comp();
        float yPos = bounceCurve.Evaluate(comp);
        entity.position = Vector2.Lerp(bounce.initialPos, bounce.targetPos, comp);
        entity.position = new(entity.position.x, entity.position.y + yPos, entity.position.y);
        entity.bounceEffect = bounce;
    }

    
    public struct ParentToEntity {
        public Entity parentEntity;
        public Vector2 localOffset;
        public float endTime;
    }

    private void AddParentEffect(Entity entity, Entity parent, float duration) {
        ParentToEntity parentToEntity = new() {
            parentEntity = parent,
            localOffset = parent.position - entity.position,
            endTime = Time.time + duration,
        };
        entity.parentEffect = parentToEntity;
    }

    private void UpdateParentEffect(Entity entity) {
        if (!entity.parentEffect.TryGetValue(out var parentToEntity)) return;
        if (Time.time > parentToEntity.endTime || !parentToEntity.parentEntity.IsValid) { 
            entity.parentEffect = null;
            return;
        }
        entity.position = parentToEntity.parentEntity.position + parentToEntity.localOffset.ToVector3();
    }


    public struct TweenPosition {
        public Vector2 startPos;
        public Vector2 endPos;
        public Timer timer;
        public DemonEyeTween.Curve curve;
    }

    private void AddTweenPosition(Entity entity, Vector2 endPos, float duration, DemonEyeTween.Curve curve = DemonEyeTween.Curve.Linear) {
        TweenPosition tween = new() {
            startPos = entity.position,
            endPos = endPos,
            curve = curve,
        };
        tween.timer.SetTime(duration);
        entity.tweenPosition = tween;
    }

    private void UpdateTweenPosition(Entity entity) {
        if (!entity.tweenPosition.TryGetValue(out var tween)) return;

        tween.timer.Tick();
        float comp = DemonEyeTween.ConvertCompletion(tween.timer.Comp(), tween.curve);
        entity.position = Vector2.Lerp(tween.startPos, tween.endPos, comp);
        entity.tweenPosition = tween;
    }

    
    public struct ShakeEffect {
        public float jitter;
        public float magnitude;
        public AnimationCurve animCurve;
        public Vector2 randomSeed;
        public Vector3 entityStartPos;
        public Timer timer;
        public float noisePos;
    }

    private void AddShakeEffect(Entity entity, float jitter, float magnitude, float time, AnimationCurve animCurve) {
        entity.shakeEffect = new() {
            jitter = jitter,
            magnitude = magnitude,
            animCurve = animCurve,
            randomSeed = new(Random.Range(int.MinValue, int.MaxValue), Random.Range(int.MinValue, int.MaxValue)),
            timer = new(time),
            noisePos = 0f,
            entityStartPos = entity.position,
        }; 
    }
    
    private void UpdateShakeEffect(Entity entity) {
        if (!entity.shakeEffect.TryGetValue(out var shakeEffect)) return;

        shakeEffect.timer.Tick(); 
        float magnitude = shakeEffect.animCurve.Evaluate(shakeEffect.timer.Comp()) * shakeEffect.magnitude;
        shakeEffect.noisePos = (shakeEffect.noisePos + shakeEffect.jitter * Time.deltaTime) % 1f;
        float x = (Mathf.PerlinNoise(shakeEffect.randomSeed.x, shakeEffect.noisePos) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(shakeEffect.randomSeed.y, shakeEffect.noisePos + 100f) - 0.5f) * 2f;
        Vector3 targetVector = new Vector3(x, y, entity.position.z) * magnitude;
        entity.position = shakeEffect.entityStartPos + targetVector;
        entity.shakeEffect = shakeEffect.timer.IsFinished ? null : shakeEffect;
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
    
    public class Enemy : Entity {
        public float teleportTime;
        public Collider2D enemySpacerCollider;
        public EnemyData data;
        public Timer applyDamageTimer;
        public BleedModInstance? bleed;
        public PoisonSoulcard.InstanceData? poisoned;
        public SlowInstance? slow;
        public Vector2 moveDir;
        public Limiter changeDirLimiter;
    }
    
    private void UpdateEnemies() {
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            enemy.applyDamageTimer.Tick();

            enemy.teleportTime += Time.deltaTime;
            float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);

            if (enemy.teleportTime >= 10f && distFromPlayer > 2.3f) {
                CoolerGrid.GridCell randomSpawnGridPos = currentMapInstance.grid.GetSpawnPosition(player.position);
                TeleportEnemy(enemy, randomSpawnGridPos.position, TeleportType.Reposition);
                continue;
            }
            
            Vector2 dirToPlayer = (player.position - enemy.position).normalized;

            Vector2 graphicalEnemyDir;
            if (enemy.animator.Playing(walkSideAnim)) {
                graphicalEnemyDir = enemy.spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }
            else if (enemy.animator.Playing(walkUpAnim)) {
                graphicalEnemyDir = Vector2.up;
            }
            else {
                graphicalEnemyDir = Vector2.down;
            }

            if (!enemy.poisoned.HasValue && distFromPlayer < 0.25f && !EnemyPlayingAttackAnimation(enemy) 
                && Vector2.Dot(graphicalEnemyDir, dirToPlayer) >= 0.5f) 
            {
                switch (CardinalDirFromVector(enemy.moveDir)) {
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
                enemy.applyDamageTimer.SetTime(0.31f);
                enemy.applyDamageTimer.EndAction = () => {
                    Vector2 attackCheckPos = enemy.Center.ToVector2() + dirToPlayer * 0.15f;
                    Collider2D col = Physics2D.OverlapCircle(attackCheckPos, 0.15f, Masks.PlayerMask);
                    if (col != null) {
                        DamagePlayer(enemy.data.damage,enemy.data.changeToCauseBleed);
                    }
                };
            }
            
            if (enemy.bleed.TryGetValue(out var bleed)) {
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    
                    int bleedDamage = bleed.bleedDamage;
                    if (CarryingPassiveItem(demonClawItem, out int count)) {
                        bleedDamage += demonClawItem.GetBleedDamageIncrease(count);
                    }
                    
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
                if (Random.value < 0.05f) {
                    Item dropItem = GetItemFromEnemyDropPool(enemy.data);
                    SpawnItemAsEntity(dropItem, 1, enemy.position, Quaternion.identity);
                }

                player.soulCurrency += enemy.data.soulWorthPerKill;
                onEnemyDeath?.Invoke(enemy);
                
                Entity bloodSplatterEntity = SpawnEntity(bloodSplatterPool, enemy.position, Quaternion.identity);
                DestroyEntity(bloodSplatterEntity, CurrentClipLength(bloodSplatterEntity.animator));

                DestroyEntity(enemies[i]);
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
            
            enemy.moveDir = currentMapInstance.grid.GetFlowFieldDirection(enemy.position);
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
            Entity outEntity = SpawnEntity(teleportOutPool, enemy.position, Quaternion.identity);
            DestroyEntity(outEntity, CurrentClipLength(outEntity.animator));
        }
        
        enemy.position = position;
        enemy.gameObject.SetActive(false);
        
        Entity inEntity = SpawnEntity(teleportInPool, enemy.position, Quaternion.identity);
        float spawnAnimDuration = CurrentClipLength(inEntity.animator);
        DestroyEntity(inEntity, spawnAnimDuration);

        Tween.Delay(target: enemy, spawnAnimDuration * 0.7f, (enemy) => {
            // Only teleport in if we are still in the raid
            if (enemy == null || !InRaid) return;
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
        
        public const int prefixedSumResolution = 500;
        public float[] prefixedSums = new float[prefixedSumResolution];

        public List<(float time, EnemyData enemy)> spawnEvents = new();
        public int spawnTimeIndex;
    }

    [NonSerialized] private EnemySpawnManager spawnManager = new();
    
    private void InitSpawnManager(RaidSpawnPattern pattern) {
        spawnManager.spawnPattern = pattern;
        spawnManager.curPhaseIndex = -1;
        spawnManager.timeInPhase = 0f;
        spawnManager.totalTimeLeft = pattern.timeBeforeFirstPhase;
        foreach (RaidSpawnPattern.SpawnPhase phase in spawnManager.spawnPattern.spawnPhases) {
            spawnManager.totalTimeLeft += phase.phaseDuration;
        }
        spawnManager.timeUntilFinalWave = spawnManager.totalTimeLeft - spawnManager.spawnPattern.spawnPhases[^1].phaseDuration;
    }
    
    private void UpdateSpawnManager() {
        EnemySpawnManager sm = spawnManager;
        
        if (sm.curPhaseIndex >= sm.spawnPattern.spawnPhases.Count) return;
        
        sm.timeInPhase += Time.deltaTime;
        sm.totalTimeLeft -= Time.deltaTime;
        sm.timeUntilFinalWave -= Time.deltaTime;
        
        float waveDuration = sm.curPhaseIndex == -1 ? 
            sm.spawnPattern.timeBeforeFirstPhase : 
            sm.spawnPattern.spawnPhases[sm.curPhaseIndex].phaseDuration;

        bool startNextWave = sm.timeInPhase >= waveDuration;
        if (startNextWave) {
            sm.curPhaseIndex++;
            if (!sm.spawnPattern.spawnPhases.IndexInRange(sm.curPhaseIndex)) return;

            RaidSpawnPattern.SpawnPhase curPhase = sm.spawnPattern.spawnPhases[sm.curPhaseIndex];

            foreach (RaidSpawnPattern.EnemyBatch batch in curPhase.enemyBatches) {
                if (batch.enemyCount >= EnemySpawnManager.prefixedSumResolution) {
                    Debug.LogError($"Wave cannot have more enemies than {nameof(EnemySpawnManager.prefixedSumResolution)}");
                }
            }
            
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
        }

        if (sm.spawnEvents.Count <= 0) return;
        
        while (sm.spawnEvents.IndexInRange(sm.spawnTimeIndex) && sm.spawnEvents[sm.spawnTimeIndex].time <= sm.timeInPhase) {
            CoolerGrid.GridCell randomSpawnGridPos = currentMapInstance.grid.GetSpawnPosition(player.position);
            Vector2 randomSpawnPos = randomSpawnGridPos.position;

            EnemyData enemyToSpawn = sm.spawnEvents[sm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity);
            enemy.health = enemyToSpawn.health;
            enemy.data = enemyToSpawn;
            enemy.animator.runtimeAnimatorController = enemyToSpawn.animatorOverride;
            enemy.enemySpacerCollider = enemy.trans.GetChild(0).GetComponent<Collider2D>();
            enemies.Add(enemy);
            
            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);

            sm.spawnTimeIndex++;
        }
    }

    // *******************************
    // Inventory
    // *******************************
    
    [Serializable]
    public class InventoryItem {
        public int itemDataUuid;
        public List<int> modifierUuids;
        public int count = 1;

        [NonSerialized] public bool notDiscovered;
        [NonSerialized] public bool traderOwned;
        [NonSerialized] public Item _itemRef; // Used for items created at runtime, like demon eyes

        public Item ItemRef => _itemRef ? _itemRef : itemLookup[itemDataUuid];
        public bool IsFullStack => count == ItemRef.MaxStackCount;

        public InventoryItem(Item item = null, int count = 1) {
            if (item == null) return;
            this.itemDataUuid = item.uuid;
            this.count = count;
        }
        
        public InventoryItem Clone() {
            InventoryItem clonedItem = new() {
                itemDataUuid = itemDataUuid,
                count = count,
                notDiscovered = notDiscovered,
                traderOwned = traderOwned,
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
    [NonSerialized] private Inventory traderInventoryPtr;
    [NonSerialized] private Inventory lootInvetoryPtr;
    [NonSerialized] private List<Inventory> allInventories = new();
    
    private const int playerPocketSize = 6;
    private const int playerQuickUseSize = 4;
    private const int playerEquipmentSize = 3;
    private int NakedPlayerInventorySize => playerPocketSize + playerQuickUseSize + playerEquipmentSize;

    private const int traderInventoryColCount = 6;
    private const int traderInventoryRowCount = 4;

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
        SpawnUiSlots(playerPocketParent, playerPocketSize);
        SpawnUiSlots(playerBackpackParent, maxBackpackSize);
        playerInventory = CreateInventory(playerInventoryParent, NakedPlayerInventorySize);
        LoadInventory(playerInventory);
        
        int stashInventorySize = 40;
        SpawnUiSlots(stashInventoryParent, stashInventorySize);
        stashInventory = CreateInventory(stashInventoryParent, stashInventorySize);
        LoadInventory(stashInventory);
       
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(lootInventoryParent, cachedLootInventorySize); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventorySize);

        const int traderInventorySize = traderInventoryRowCount * traderInventoryColCount;
        SpawnUiSlots(traderInventoryParent, traderInventorySize);
        traderInventoryPtr = CreateInventory(traderInventoryParent, traderInventorySize);
        
        const int transactionInventorySize = 25;
        SpawnUiSlots(traderTransactionInventoryParent, transactionInventorySize);
        transactionInventory = CreateInventory(traderTransactionInventoryParent, transactionInventorySize);

        const int maxCrucibleInventorySize = 13;
        const int startingCrucibleInventorySize = 6;
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
        
        CheckForEquipmentChange();
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
        
        InventorySlot hoveredSlot = info.inventory.slots[info.slotIndex];
        TextMeshProUGUI nameText = itemDescPopup.nameText;
        TextMeshProUGUI metaInfoText = itemDescPopup.metaInfoText;
        TextMeshProUGUI descText = itemDescPopup.descText;
        
        Item.Rarity itemRarity = hoveredSlot.item.ItemRef.GetRarity();
        Color itemRarityColor = styles.GetColorForRarity(itemRarity);
        float tagTextPadding = styles.tagTextPadding;

        itemDescPopup.tag1.gameObject.SetActive(true);
        itemDescPopup.tag1.color = itemRarityColor;
        
        if (hoveredSlot.item.ItemRef.type == consumableType) {
            itemDescPopup.tag1Text.text = "Consumable";
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
        
        bool itemIsOwnedByTrader = info.inventory.slots[info.slotIndex].item.traderOwned;
        int sellOrBuyPrice = itemIsOwnedByTrader ? item.buyPrice : item.sellPrice;
                             
        nameText.text = item.displayName;
        string coinText = $"<sprite=0>{ColorText(sellOrBuyPrice.ToString(), styles.coinCurrencyColor)}";
        
        string tintedWeightSprite = $"<sprite=2 color=#{ColorUtility.ToHtmlStringRGBA(styles.underWeightColor)}>";
        string weightText = tintedWeightSprite + ColorText(item.Weight.ToString(), styles.underWeightColor);
        
        metaInfoText.text = coinText + "  " + weightText;
        
        // Set description
        if (hoveredSlot.item.ItemRef.type == demonEyeType) {
            DemonEyeInstance eyeInstance = eyeInstanceFromItemId[hoveredSlot.item.itemDataUuid];
            string eyeDescription = "";
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += modInstance.GetDescriptionForEye() + "\n";
            }
            descText.text = eyeDescription;
        }
        else {
            descText.text = hoveredSlot.item.ItemRef.GetDescription();
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
            } 
        }
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
        
        if (!hoverInfo.hoveringTransform) {
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
        uiElementPopup.gameObject.SetActive(true);
        
        if (hoverInfo.hoveringTransform == upgradeForgeButton.rectTransform) {
            if (crucibleState == CrucibleState.Upgrade) {
                uiElementPopup.descText.text = "Add an additional slot to the pentagram!\nCosts:";
                List<UpgradePath.Requirement> requirements = crucibleUpgradePath.pathUpgrades[player.crucibleLevel].requirements;
                foreach (UpgradePath.Requirement req in requirements) {
                    bool meetsSingleReq = MeetsSingleUpgradeRequirement(req); 
                    Color textColor = meetsSingleReq ? styles.increaseDescColor : styles.decreaseDescColor; 
                    uiElementPopup.descText.text += ColorText($"\n{req.item.displayName} x{req.count}", textColor);
                }
            }
        }
        
        if (hoverInfo.hoveringTransform == forgeEyeButton.rectTransform) {
            if (crucibleState == CrucibleState.Forging) {
                uiElementPopup.descText.text = "Eye Preview";
            }

            Dictionary<Soulcard, int> allSoulCards = new();
            
            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.item == null || slot.item.ItemRef.type != soulcardType) continue;    
                Soulcard soulcard = itemLookup[slot.item.itemDataUuid] as Soulcard;
                if (!allSoulCards.TryAdd(soulcard, 1)) {
                    allSoulCards[soulcard]++;
                }
            }
            
            string eyeDescription = "";
            foreach ((Soulcard soulcard, int count) in allSoulCards) {
                eyeDescription += "\n" + soulcard.GetStackDescription(count);
            }
            uiElementPopup.descText.text += eyeDescription;
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
        if (IsHoveredItemGrayedOut(invHoverInfo)) return;
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
                if (hoveredInventory == traderInventoryPtr) {
                    destinationInventory = transactionInventory;
                    moveOption = MoveItemOption.Single;
                }
                else if (hoveredInventory == transactionInventory) {
                    destinationInventory = traderInventoryPtr;
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
                if (hoveredInventory == traderInventoryPtr) {
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
        if (hoveredItem.ItemRef.type != consumableType) return;

        if (hoveredItem.ItemRef == bandageItem) {
            if (!player.bleeding) return;
            player.bleeding = false;
        }
        else if (hoveredItem.ItemRef == healthPotionItem) {
            if (player.health >= 100f) return;
            HealPlayer(25);
        }
        else if (hoveredItem.ItemRef == demonSteakItem) {
            if (player.health >= 100f) return;
            HealPlayer(15);
        }
        
        ReduceItemCountInInventory(invHoverInfo.inventory, invHoverInfo.slotIndex);
    }

    private void UpdatePlayerPanelUI() {
        playerPanelHealthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/{FullPlayerHealth}";

        int inventoryWeight = GetInventoryWeight(playerInventory);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanelWeightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
        
        agilityStatValueText.text = (player.agilityLevel + 1).ToString("0.0");
        healthStatValueText.text = (player.healthLevel + 1).ToString("0.0");
        luckStatValueText.text = (player.luckLevel + 1).ToString("0.0");
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
            if (curEyeItem == null) {
                equipedEye = new() { coreAttack = defaultAttack };
            }
            else {
                equipedEye = eyeInstanceFromItemId[curEyeItem.itemDataUuid];
            }
        }
        
        if (prevEquippedBackpackItem != curBackpackItem) {
            prevEquippedBackpackItem = curBackpackItem;
            if (curBackpackItem != null) {
                int backpackSize = 0;
                if (curBackpackItem.ItemRef == pouchItem) {
                    backpackSize = 8;
                }
                else if (curBackpackItem.ItemRef == ruckSackItem) {
                    backpackSize = 12;
                }
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
    
    public struct UIHoverInfo {
        public RectTransform hoveringTransform;
        public float timeSpentHovering;
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
        if (hoverInfo.inventory == traderInventoryPtr && !IsDraggingItem) {
            if (transactionState != TransactionState.Selling) {
                MoveItemBetweenInventories(traderInventoryPtr, transactionInventory, hoverInfo.slotIndex, MoveItemOption.Single);
            }
            return IsDraggingItem;
        }

        // If we are putting trader items back, then we also don't want to pick up the items
        if (!IsDraggingItem && hoverInfo.inventory == transactionInventory && transactionState == TransactionState.Buying) {
            MoveItemBetweenInventories(transactionInventory, traderInventoryPtr, hoverInfo.slotIndex, MoveItemOption.Single);
            return IsDraggingItem;
        }
        
        bool pickingUpItem = dragItem == null;
        if (pickingUpItem && pickupInputUsed) {
            if (!TryGetItemFromHoverInfo(hoverInfo, out InventoryItem item)) {
                return IsDraggingItem;
            }

            if (IsHoveredItemGrayedOut(hoverInfo)) {
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
        }

        bool placingItem = !pickingUpItem;
        if (placingItem && placeInputUsed) {
            bool droppingItemInHideout = hoverInfo.inventory == null && InHideout;
            bool tryingToPlaceItemToSellWhileBuying = hoverInfo.inventory == transactionInventory && transactionState == TransactionState.Buying;
            bool tryingToPlaceInTraderInventory = hoverInfo.inventory == traderInventoryPtr;
            
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
                bool itemsCanSwap = swapItem.itemDataUuid != dragItem.itemDataUuid || (swapItem.IsFullStack || dragItem.IsFullStack);
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
        
        bool allowInfiniteStacking = inventory == traderInventoryPtr;
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
            if (slot.item == null || slot.ui.disallowItemStacking || slot.item.IsFullStack || slot.item.itemDataUuid != item.itemDataUuid) continue;

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
        
        if (moveOption == MoveItemOption.Single) {
            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = 1;
            
            InventoryAddResult result = TryAddItemToInventory(toInventory, newItem);
            if (result.type is InventoryAddResult.ResultType.Success or InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - result.addedCount;
                AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
            }
            return;
        }

        MoveEntireItemStack(fromInventory, toInventory, slotIndex);
    }

    private bool MoveEntireItemStack(Inventory fromInventory, Inventory toInventory, int slotIndex) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, slotIndex);
        if (inventoryItem == null) {
            return false;
        }
        
        InventoryAddResult moveResult = TryAddItemToInventory(toInventory, inventoryItem);
        if (moveResult.type == InventoryAddResult.ResultType.Success) {
            RemoveItemFromInventory(fromInventory, slotIndex);
        }
        else if (moveResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
            int keepItemCount = inventoryItem.count - moveResult.addedCount;
            AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
        }
        
        return moveResult.type == InventoryAddResult.ResultType.Success;
    }

    private void MoveEntireInventory(Inventory fromInventory, Inventory toInventory) {
        for (int i = 0; i < fromInventory.slots.Length; i++) {
            if (fromInventory.slots[i].item == null) continue;
            MoveEntireItemStack(fromInventory, toInventory, i);
        }
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
            if (slot.item == null || slot.item.itemDataUuid != item.uuid) continue;
            
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
        if (OnTradingTab) {
            Trader curTrader = GetCurrentlySelectedTrader();
            foreach (InventorySlot slot in stashInventory.slots) {
                if (slot.item == null) continue;
                if (slot.item.ItemRef.associatedTrader != curTrader) {
                    slot.ui.itemUI.ToggleGray();
                }
            }
        }
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

    private bool MeetsSingleUpgradeRequirement(UpgradePath.Requirement req) {
        int itemCount = 0;
        itemCount += GetItemCountInInventory(stashInventory, req.item);
        itemCount += GetItemCountInInventory(playerInventory, req.item);
        return itemCount >= req.count; 
    }

    public bool CarryingPassiveItem(PassiveItem item, out int count) {
        count = GetItemCountInInventory(playerInventory, item);
        return count > 0;
    }

    private int GetInventoryWeight(Inventory inventory) {
        int weight = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            weight += slot.item.ItemRef.Weight * slot.item.count;
        }
        return weight;
    }
    
    private enum InventoryValueType { Buy, Sell, Xp }

    private int GetInventoryValue(Inventory inventory, InventoryValueType valueType) {
        int value = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            switch (valueType) {
                case InventoryValueType.Buy:
                    value += slot.item.ItemRef.buyPrice * slot.item.count;
                    break;
                case InventoryValueType.Sell:
                    value += slot.item.ItemRef.sellPrice * slot.item.count;
                    break;
                case InventoryValueType.Xp:
                    value += slot.item.ItemRef.traderXp * slot.item.count;
                    break;
            }
        }
        return value;
    }

    private void OpenPlayerInventory() {
        playerPanel.gameObject.SetActive(true);
        crosshairTrans.gameObject.SetActive(false);
        Cursor.visible = true;
    }

    private void ClosePlayerInventory() {
        playerPanel.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
        EndDragAndDropItem();
    }

    private void OpenLootInventory() {
        discoverLootIndex = -1;
        lootInventoryPanel.gameObject.SetActive(true);
        
        foreach (Transform child in lootInventoryParent.transform) {
            child.GetComponentInChildren<InventorySlotUI>()?.ClearItem();
        }
        
        for (int i = 0; i < lootInvetoryPtr.slots.Length; i++) {
            if (lootInvetoryPtr.slots[i].item == null) continue;
            if (lootInvetoryPtr.slots[i].item.notDiscovered) {
                discoverLootIndex = i;
                break;
            }
            InventoryItem item = lootInvetoryPtr.slots[i].item;
            lootInventoryParent.GetChild(i).GetComponentInChildren<InventorySlotUI>().SetItem(item.ItemRef, item.count);
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        discoverLootTimer.SetTime(1f);
        discoverLootTimer.EndAction ??= () => {
            InventoryItem item = lootInvetoryPtr.slots[discoverLootIndex].item;
            
            item.notDiscovered = false;
            lootInventoryParent.GetChild(discoverLootIndex).GetComponentInChildren<InventorySlotUI>().SetItem(item.ItemRef, item.count);
            
            discoverLootIndex++;
            if (discoverLootIndex < lootInvetoryPtr.slots.Length) {
                discoverLootTimer.SetTime(1f);
            }
        };
    }

    private void CloseLootInventory() {
        lootInventoryPanel.gameObject.SetActive(false);
        discoverLootTimer.Stop();
    }
    
    // **********************************
    // Player
    // **********************************

    public class Player : Entity {
        public bool bleeding;
        public Vector2 velocity;
        public int nextIdleAnimHash;
        public int nextIdleDir;
        public Limiter bleedLimiter;
        
        public int crucibleLevel;
        public int soulCurrency;
        public int coinCurrency;
        public int agilityLevel;
        public int luckLevel;
        public int healthLevel;
        public int strengthLevel;
    }

    private Player player;
    private int lastStepSmokeFrame = -1;
    
    private int PlayerRunSideHash = Animator.StringToHash("PlayerRunSide");
    private int PlayerRunUpHash = Animator.StringToHash("PlayerRunUp");
    private int PlayerRunDownHash = Animator.StringToHash("PlayerRunDown");
    private int PlayerIdleSide = Animator.StringToHash("PlayerIdleSide");
    private int PlayerIdleUp = Animator.StringToHash("PlayerIdleUp");
    private int PlayerIdleDown = Animator.StringToHash("PlayerIdleDown");
    private int PlayerDeathHash = Animator.StringToHash("PlayerDeath");
    
    private void UpdatePlayer() {
        if (player.bleeding && player.bleedLimiter.TimeHasPassed(3.5f)) {
            player.health -= 5;
        }

        if (InventoryIsOpen) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        
        float speed = GetPlayerSpeedBasedOnStats();
        player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (speed * Time.deltaTime);
        player.velocity = new Vector3(moveInput.x, moveInput.y, 0f) * speed;

        if (moveInput != Vector2.zero) {
            player.spriteRenderer.flipX = moveInput.x < 0;
            player.nextIdleDir = (int)Mathf.Sign(moveInput.x);
        }
        else {
            player.spriteRenderer.flipX = player.nextIdleDir < 0;
        }
        
        bool movingProdominatelyVertical = Mathf.Abs(Vector2.Dot(Vector2.up, moveInput)) > 0.9f;
        
        if (moveInput.magnitude > 0.1f && !movingProdominatelyVertical) {
            player.animator.Play(PlayerRunSideHash);
            player.nextIdleAnimHash = PlayerIdleSide;
        }
        else if (moveInput.y > 0) {
            player.animator.Play(PlayerRunUpHash);
            player.nextIdleAnimHash = PlayerIdleUp;
        }
        else if (moveInput.y < 0) {
            player.animator.Play(PlayerRunDownHash);
            player.nextIdleAnimHash = PlayerIdleDown;
        }
        else {
            player.animator.Play(player.nextIdleAnimHash);
        }
        
        if (moveInput != Vector2.zero) {
            int frameNumber = player.animator.CurrentFrameNumber();
            if (lastStepSmokeFrame != frameNumber && (frameNumber == 0 || frameNumber == 4)) {
                Entity runSmokeEntity = SpawnEntity(runSmokePool, OffsetY(player.position, 0.01f), Quaternion.identity);
                DestroyEntity(runSmokeEntity, CurrentClipLength(runSmokeEntity.animator));
                lastStepSmokeFrame = frameNumber;
            }
        }
        
        if (AimingWithController()) {
            Vector2 stick = lookInputAction.ReadValue<Vector2>();
            crosshairTrans.position = ScreenCenter + stick.normalized * 250f; 
        }
        else {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            crosshairTrans.position = mousePos;
        }

        bool shootInput = false;
        Vector2 targetScreenPos = Vector2.zero;
        
        if (AimingWithController()) {
            shootInput = lookInputAction.ReadValue<Vector2>().magnitude > 0.1f;
            targetScreenPos = crosshairTrans.position;
        }
        else {
            shootInput = attackInputAction.IsPressed();
            targetScreenPos = Mouse.current.position.ReadValue();
        }

        if (shootInput && CanShoot()) {
            PlayAudioClip(shootClip, player.position, 1f);
            ShootProjectile(targetScreenPos);
        }
    }

    private void HealPlayer(int healing) {
        player.health = Mathf.Clamp(player.health + healing, 0, FullPlayerHealth);
    }

    private void DamagePlayer(int damage, float chanceToBleed = 0f) {
        // if (RollProbability(chanceToBleed)) {
        //     player.bleeding = true;
        // }
        player.health -= damage;
        AddFlashHitEffect(player);
    }
    
    private const float defaultPlayerSpeed = 0.55f;
    private const float maxPlayerSpeed = 0.85f;

    private const int encumberingIncreasePerStrengthPoint = 50;
    private const int defaultStartingEncumberingWeight = 180;
    private const int maxEncumberedWeight = 280;
    private const float maxEncumberedSpeedReduction = 0.3f;

    private const int healthIncreasePerStatLevel = 10;
    private int FullPlayerHealth => 100 + (healthIncreasePerStatLevel * player.healthLevel);

    private const float luckPercentIncreasePerStatLevel = 0.01f;
    private float RaritySkewIncreaseFromLuck => luckPercentIncreasePerStatLevel * player.luckLevel;
    
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

    private float GetTotalWeightCompletion() {
        GetEncumberingWeightRange(out int _, out int endingEncumberingWeight);
        int inventoryWeight = GetInventoryWeight(playerInventory);
        return Mathf.Clamp01(inventoryWeight / (float)endingEncumberingWeight);
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
        public string GetDescriptionForEye() => Soulcard.GetStackDescription(stackCount);
    }

    public class DemonEyeInstance {
        public List<EquipedModInstance> modInstances = new();
        public CoreAttack coreAttack;
        
        public FirerateSoulcard.InstanceData? firerate;
        public TrishotSoulcard.InstanceData? trishot;
        public BleedCritSoulcard.InstanceData? bleedCrit;
        public RangeSoulcard.InstanceData? range;
        public FarDamageSoulcard.InstanceData? farDamage;
        public PenetrationSoulcard.InstanceData? penetration;
        public DoubleCritSoulcard.InstanceData? doubleCrit;
        public BackwardsShotSoulcard.InstanceData? backwardShot;
        public BackwardsShotMultiplierSoulcard.InstanceData? backwardsShotCrit;
        public PoisonSoulcard.InstanceData? poison;
        public ExplosionSoulcard.InstanceData? explosion;
        public StoppingPowerSoulcard.InstanceData? stoppingPower;
    }

    private Dictionary<int, DemonEyeInstance> eyeInstanceFromItemId = new();
    private DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    private DemonEyeInstance BuildAndRegisterEye(InventoryItem item) {
        item.itemDataUuid = GenerateNewItemUuid();
        item._itemRef = demonEyeItem;
        
        Dictionary<int, int> eyeModCountFromId = new();
        foreach (int modUuid in item.modifierUuids) {
            if (!eyeModCountFromId.TryAdd(modUuid, 1)) {
                eyeModCountFromId[modUuid]++;
            }
        }
        
        List<EquipedModInstance> eyeModifiers = new();
        foreach (KeyValuePair<int, int> pair in eyeModCountFromId) {
            eyeModifiers.Add(new() {
                modId = pair.Key,
                stackCount = pair.Value,
            });
        }
        
        DemonEyeInstance newDemonEye = new() {
            coreAttack = defaultAttack,
            modInstances = eyeModifiers,
        };
        
        foreach (EquipedModInstance modInstance in eyeModifiers) { 
            modInstance.ApplyToEye(newDemonEye); 
        }
        
        eyeInstanceFromItemId.Add(item.itemDataUuid, newDemonEye);
        return newDemonEye;
    }

    private bool CanShoot() {
        float attackDelay = equipedEye.coreAttack.attackDelay;
        if (equipedEye.firerate.TryGetValue(out var firerate)) {
            attackDelay -= attackDelay * firerate.rateIncrasePercentage;
            attackDelay = Mathf.Clamp(attackDelay, equipedEye.coreAttack.cappedMinAttackDelay, equipedEye.coreAttack.attackDelay);
        }
        return attackLimiter.TimeHasPassed(attackDelay);
    }

    private void ShootProjectile(Vector2 targetScreenPos) {
        Vector2 targetWorldPos = mainCamera.ScreenToWorldPoint(targetScreenPos);

        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - equipedEye.coreAttack.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = equipedEye.coreAttack.projectileSpeed;
        if (equipedEye.stoppingPower.TryGetValue(out var stoppingPower)) {
            projectileSpeed *= 1f - stoppingPower.percentSpeedReduction;
        }
        
        Vector2 dir = (targetWorldPos - player.trans.PositionV2()).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * projectileSpeed; 
        SpawnProjectile(velocity);

        if (equipedEye.trishot.TryGetValue(out var trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(secondShotVelocity);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(thirdShotVelocity);
        }

        if (equipedEye.backwardShot.TryGetValue(out var backShot) && RollProbability(backShot.probability)) {
            Projectile projectile = SpawnProjectile(-velocity);
            projectile.isBackwardsShot = true;
        }
    }
    
    private Projectile SpawnProjectile(Vector2 velocity) {
        float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
        Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        const float defaultTimeAlive = 0.65f;
        float projLifeTime = defaultTimeAlive;
        if (equipedEye.range.TryGetValue(out var rangeIncrease)) {
            projLifeTime += rangeIncrease.timeAliveIncrease;
        }

        EntityPool<Projectile> poolToSpawnFrom = equipedEye.stoppingPower.HasValue ? stoppingPowerProjectilePool : projectilePool;
        Projectile projectile = SpawnEntity(poolToSpawnFrom, player.position + new Vector3(0f, 0.13f, 0f), projectileRotation);
        projectile.lifeTimeDuration = projLifeTime;
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = equipedEye;
        projectiles.Add(projectile);

        projectile.trans.localScale = Vector3.zero;
        Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
        
        return projectile;
    }

    // *******************************
    // Interactions 
    // *******************************
    
    private void CheckForInteractions() { 
        interactPrompt.SetActive(false);
        
        Vector2 checkCenter = player.position + new Vector3(0f, 0.05f, 0f);
        
        ForCollidersInOverlapCircle(checkCenter, 0.1f, Masks.ItemMask, 10, col => {
            if (col.CompareTag(Tags.Pickup)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    ItemDrop itemDrop = col.GetComponent<ItemDrop>();
                    itemDrop.circleCollider.enabled = false;
                    
                    InventoryAddResult result = TryAddItemToInventory(playerInventory, itemDrop.item, itemDrop.dropCount);
                    if (result.type == InventoryAddResult.ResultType.Success) {
                        Entity droppedEntity = entityLookup[itemDrop.gameObject];
                        PickupDroppedItem(droppedEntity); 
                    }
                    else if (result.type == InventoryAddResult.ResultType.FailureToAddAll) {
                        itemDrop.dropCount -= result.addedCount;
                    }
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    lootInvetoryPtr.slots = deadBodySlotsLookup[col.gameObject];
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    gameStateMachine.SetStateIfNotCurrent(gameWinState);
                }
            }
        });
    }

    private void PickupDroppedItem(Entity droppedEntity) {
        Vector3 playerPickupTarget = new(0f, 0.07f, 0f);
        
        droppedEntity.bounceEffect = null;
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

    private void EnableInteractionPrompt(Vector3 position) {
        interactPrompt.SetActive(true);
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position + new Vector3(0f, 0.1f, 0f));
    }
    
    // *******************************
    // Projectiles
    // *******************************

    [NonSerialized] public List<Projectile> projectiles = new();
    
    public class Projectile : Entity {
        public float curTimeAlive;
        public float lifeTimeDuration;
        public float distTraveled;
        public bool isBackwardsShot;
        public Vector2 velocity;
        public DemonEyeInstance eyeInstanceSpawnedFrom;
        public List<Entity> ignoreEntities;
    }
    
    private static void OnSpawnProjectile(Projectile projectile) {
        projectile.curTimeAlive = default;
        projectile.lifeTimeDuration = default;
        projectile.distTraveled = default;
        projectile.isBackwardsShot = default;
        projectile.velocity = default;
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
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, projectileRadius, Masks.DamagableMask);
            if (!col) continue;
            
            Entity entity = entityLookup[col.gameObject];
                    
            if (proj.ignoreEntities == null || !proj.ignoreEntities.Contains(entity)) {
                HandleDamage(proj, entity);
            }

            if (entity is Enemy && ProjectileShouldPassThrough(proj, entity)) continue;
            
            Entity impact = SpawnEntity(projectileImpactPool, proj.position, RandomRotation());
            DestroyEntity(impact, CurrentClipLength(impact.animator));
            
            DestroyEntity(projectiles[i]);
            projectiles.RemoveAt(i);
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].curTimeAlive > projectiles[i].lifeTimeDuration) {
                const float despawnTime = 0.1f;
                Tween.Scale(projectiles[i].trans, 0f, despawnTime, Ease.OutBounce);
                DestroyEntity(projectiles[i], despawnTime);
                projectiles.RemoveAt(i);
            }
        }
    }

    private bool ProjectileShouldPassThrough(Projectile proj, Entity entity) {
        if (!proj.eyeInstanceSpawnedFrom.penetration.TryGetValue(out var pen)) {
            return false;
        }
        
        bool alreadyContainsEntity = proj.ignoreEntities?.Contains(entity) ?? false;
        if (entity.IsValid && !alreadyContainsEntity) {
            proj.ignoreEntities ??= ListPool<Entity>.Get();
            proj.ignoreEntities.Add(entity);
        }
        
        int alreadyPenetratedCount = proj.ignoreEntities?.Count ?? 0;
        return alreadyPenetratedCount <= pen.goThroughCount;
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
            Enemy enemy = entityLookup[entity.gameObject] as Enemy;
            
            bool isCriticalStrike = RollProbability(GetCriticalStrikeProbability(projectile, enemy));
            if (isCriticalStrike) {
                consecutiveCriticalHits++;
            }
            else {
                consecutiveCriticalHits = 0;
            }

            int damage = Mathf.RoundToInt(GetBaseDamage(projectile) * GetDamageMultiplier(projectile, isCriticalStrike));
            DamageEnemy(enemy, damage, isCriticalStrike);
            
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                modInstance.ApplyToEnemy(enemy);
            }
            
            if (eyeInstance.explosion.TryGetValue(out var explosion) && RollProbability(explosion.probability)) {
                Vector2 expSpawnPos = projectile.position + (enemy.position - projectile.position) / 2f;
                SpawnExplosion(explosion, expSpawnPos);
            }
        }
        else {
            entity.damageAccumilation += eyeInstance.coreAttack.damage;
            entity.health -= eyeInstance.coreAttack.damage;

            if (entity.damageAccumilation > 50) {
                entity.damageAccumilation = 0;
            }

            PlayAudioClip(stoneHitClip, entity.position, 1f);
                
            if (entity.health <= 0) {
                Entity smokeEntity = SpawnEntity<Entity>(rockSmokePrefab, entity.position, Quaternion.identity);
                DestroyEntity(smokeEntity, 0.417f);
                DestroyEntity(entity);
                
                PlayAudioClip(stoneBreakClip, entity.position, 1f);

                int dropCount = Random.Range(3, 6);
                float angleDeltaPerDrop = 360f / dropCount;
                float randomRangePerDrop = angleDeltaPerDrop * 0.25f;

                int upgradeDropIndex = Random.Range(0, 10);
                
                for (int i = 0; i < dropCount; i++) {
                    float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                    Vector3 endPos = entity.position + RotationVector(randomAngle, 0.18f, 0.25f);
                    Item dropItem = i == upgradeDropIndex ? GetItemFromDropPool(rockUpgradesDropPool) : GetItemFromDropPool(rockStonesDropPool);
                    Entity rockDrop = SpawnItemAsEntity(dropItem, 1, entity.position, Quaternion.identity);
                    AddBounceEffect(rockDrop, endPos, 0.8f);
                }
            }
            else {
                AddFlashHitEffect(entity);
                AddShakeEffect(entity, 8f, 0.038f, 0.35f, shakeCurve);
                AddScaleEffect(entity, 1.1f, 0.2f);
            }
        }
    }

    private float GetCriticalStrikeProbability(Projectile proj, Enemy enemy) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        float criticalStrikeProb = defaultCriticalStrikeChange;

        if (eyeInstance.bleedCrit.HasValue && enemy.bleed.HasValue) {
            criticalStrikeProb += eyeInstance.bleedCrit.Value.probability;
        }

        return criticalStrikeProb;
    }

    private int GetBaseDamage(Projectile proj) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        int damage = eyeInstance.coreAttack.damage;
        
        if (eyeInstance.farDamage.TryGetValue(out var farDamage)) {
            int increasedDamageFromDist = Mathf.RoundToInt(farDamage.damageIncreasePerUnitTraveled * proj.distTraveled);
            damage += increasedDamageFromDist;
        }

        if (eyeInstance.stoppingPower.TryGetValue(out var stoppingPower)) {
            damage += stoppingPower.extraDamage;
        }
        
        return damage;
    }
    
    private float GetDamageMultiplier(Projectile proj, bool isCriticalHit) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        float multiplier = isCriticalHit ? defaultCriticalStrikeMultiplier : 1f;
        
        if (eyeInstance.doubleCrit.TryGetValue(out var doubleCrit)) {
            if (consecutiveCriticalHits > 0 && consecutiveCriticalHits % 2 == 0) {
                multiplier += doubleCrit.damageMultiplier;
            }
        }

        if (proj.isBackwardsShot && eyeInstance.backwardsShotCrit.TryGetValue(out var backShotCrit)) {
            multiplier += backShotCrit.damageMultiplier;
        }

        return multiplier;
    }

    private void SpawnExplosion(ExplosionSoulcard.InstanceData explosion, Vector2 spawnPos) {
        Entity expEntity = SpawnEntity(explosionPool, spawnPos, Quaternion.identity); 
        DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
        
        ForCollidersInOverlapCircle(spawnPos, explosion.radius, Masks.EnemyMask, 30, col => {
            DamageEnemy(entityLookup[col.gameObject], explosion.damage, false);
        });
    }

    private enum DamageColor { Normal, Crit, Blood, Poison }

    private void SpawnDamageNumber(Vector3 spawnPos, int damage, DamageColor damageColor) {
        Entity damageNumber = SpawnEntity<Entity>(damageNumberPrefab, spawnPos, Quaternion.identity, damageNumbersParent);
        damageNumber.textMesh.text = damage.ToString();
        
        Vector3 startSize = Vector3.one * 0.8f;
        Vector3 endSize = Vector3.one * (damageColor == DamageColor.Crit ? 1.25f : 1f);
        Vector2 endDamageNumPos = OffsetY(OffsetX(spawnPos, Random.Range(-0.04f, 0.04f)), Random.Range(0.06f, 0.08f));
        
        switch (damageColor) {
            case DamageColor.Normal:
                damageNumber.textMesh.color = styles.normalDamageColor;
                break;
            case DamageColor.Crit:
                damageNumber.textMesh.color = styles.critDamageColor;
                break;
            case DamageColor.Blood:
                damageNumber.textMesh.color = styles.bleedDamageColor;
                break;
            case DamageColor.Poison:
                damageNumber.textMesh.color = styles.poisonDamageColor;
                break;
        }

        const float moveDuration = 0.3f;
        const float scaleUpDuration = 0.15f;
        const float popOutDuration = 0.09f;
        
        Tween.Position(damageNumber.trans, endDamageNumPos, moveDuration, Ease.OutCirc)
        .Group(Tween.Scale(damageNumber.trans, startSize, endSize, scaleUpDuration, Ease.InOutBounce))
        .Chain(Tween.Scale(damageNumber.trans, 0f, popOutDuration, Ease.InBounce));
        DestroyEntity(damageNumber, moveDuration + popOutDuration);
    }

    private Vector3 EnemyDamageNumberSpawnPos(Entity entity) {
        return OffsetY(entity.position, 0.28f);
    }
    
    // ***************************
    // Exit Portals 
    // ***************************
    
    private Transform activeExitPortal;

    private void InitExitPortals(Transform exitPortalParent, float timeBeforePortalsSpawn) {
        activeExitPortal = null;
        portalArrow.gameObject.SetActive(false);
        
        foreach (Transform portal in exitPortalParent) {
            portal.gameObject.SetActive(false);
        }
        
        exitPortalTimer.SetTime(timeBeforePortalsSpawn);
        
        exitPortalTimer.UpdateAction = () => {
            int totalSeconds = (int)exitPortalTimer.CurTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string formattedTime = $"{minutes}:{seconds:D2}";
            exitPortalStatusText.text = $"Exit Portal Countdown: {formattedTime}";
        };
        
        exitPortalTimer.EndAction = () => {
            int randomSpawnIndex = Random.Range(0, exitPortalParent.childCount);
            activeExitPortal = exitPortalParent.GetChild(randomSpawnIndex);
            activeExitPortal.gameObject.SetActive(true);
        };
    }

    private void UpdateExitPortalArrowUI() {
        if (!activeExitPortal) return;
        
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
        
        portalArrow.gameObject.SetActive(true);
        
        const float distFromScreenEdge = 50f;
        const float extraTopPadding = 0f;
        
        float minX = distFromScreenEdge;
        float maxX = Screen.width - distFromScreenEdge;
        float minY = distFromScreenEdge;
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
        
        int gemRocksToSpawn = Random.Range(6, 10);
        for (int i = 0; i < gemRocksToSpawn; i++) {
            Entity mineableRockEntity = SpawnResource<Entity>(gemRockPrefab, true);
            mineableRockEntity.health = 350;
        }
        
        int deadBodiesToSpawn = Random.Range(3, 5);
        InventorySlotUI[] lootInventorySlotUis = lootInventoryParent.GetComponentsInChildren<InventorySlotUI>(true);
        
        for (int i = 0; i < deadBodiesToSpawn; i++) {
            using var autoRelease = ListPool<Item>.Get(out List<Item> deadBodyItems);
            
            int maxDeadBodyItemCount = Random.Range(2, 6);
            GetUniqueItemsFromDropPool(bodyDropPool, maxDeadBodyItemCount, deadBodyItems);
            
            InventorySlot[] deadBodySlots = new InventorySlot[deadBodyItems.Count];
            for (int j = 0; j < deadBodyItems.Count; j++) {
                Item spawnItem = deadBodyItems[j];
                InventoryItem lootItem = new() {
                    itemDataUuid = spawnItem.uuid, 
                    count = Random.Range(1, spawnItem.MaxStackCount / 3),
                    notDiscovered = true,
                };
                deadBodySlots[j] = new() {
                    item = lootItem,
                    ui = lootInventorySlotUis[j],
                };
            }
            
            Entity body = SpawnResource<Entity>(deadBodyPrefab, false);
            deadBodySlotsLookup.Add(body.gameObject, deadBodySlots);
        }
        
        T SpawnResource<T>(GameObject resourcePrefab, bool cutsNavmesh) where T : Entity, new() {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnTrans = spawnPoints[randomIndex];
            spawnPoints.RemoveAt(randomIndex);
            
            T resource = SpawnEntity<T>(resourcePrefab, spawnTrans.position, spawnTrans.rotation);

            if (cutsNavmesh) {
                // AstarPath.active.UpdateGraphs(resource.collider.bounds);
            }

            return resource;
        }
    }

    private void DestroyLevelEntities() {
        for (int i = entities.Count - 1; i >= 0; i--) {
            if (entities[i].lifetime == EntityLifetime.Level) {
                DestroyEntityAtIndex(i);    
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
    }

    private string GetInventorySavePath(Inventory inventory) {
        if (inventory == playerInventory) return playerInventorySavePath;
        if (inventory == stashInventory) return stashSavePath;
        if (inventory == crucibleInventory) return crucibleSavePath;
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
        public int luckLevel;
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
            luckLevel = player.luckLevel,
            healthLevel = player.healthLevel,
            strengthLevel = player.strengthLevel,
        };
        SaveToFile(playerSavePath, data);
    }

    private void LoadAndAssignPlayerSaveData(Player instancedPlayer) {
        PlayerSaveData data = LoadFromFile<PlayerSaveData>(playerSavePath);
        if (data == null) return;
        instancedPlayer.health = data.health;
        instancedPlayer.crucibleLevel = data.crucibleLevel;
        instancedPlayer.soulCurrency = data.soulCurrency;
        instancedPlayer.coinCurrency = data.coinCurrency;
        instancedPlayer.agilityLevel = data.agilityLevel;
        instancedPlayer.luckLevel = data.luckLevel;
        instancedPlayer.healthLevel = data.healthLevel;
        instancedPlayer.strengthLevel = data.strengthLevel;
        
        // We want to make sure that the player health is never <= zero
        instancedPlayer.health = player.health <= 0f ? FullPlayerHealth : player.health;
    }

    // ************************************
    // UI 
    // ************************************

    private void OnGameStartInitUI() {
        CloseHideoutUI();
        CloseRaidUI();
        ShowMainMenuUI();

        // Set the stat upgrade info once at startup because each increase is the same
        {
            const float speedRange = maxPlayerSpeed - defaultPlayerSpeed;
            float speedPercentIncreasePerLevel = (speedRange / agilityUpgradePath.MaxLevel) / defaultPlayerSpeed;
            agilityUpgradeInfoText.text = $"+{(speedPercentIncreasePerLevel * 100f):0}% Speed";
            healthUpgradeInfoText.text = $"+{healthIncreasePerStatLevel} Health";
            luckUpgradeInfoText.text = $"+{(luckPercentIncreasePerStatLevel * 100f):0}% Luck";
            strengthUpgradeInfoText.text = $"+{encumberingIncreasePerStrengthPoint} Weight Carry Capacity";
        }
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
        hideoutHeaderParent.gameObject.SetActive(false);
        hideoutTabsParent.gameObject.SetActive(false);
        ToggleHideoutPanels(playerPanel, mapSelectionPanel);
    }

    private void CloseMapSelectionUI() {
        CloseHideoutUI();
    }
    
    private void ShowHideoutUI() {
        ToggleHideoutTab(characterTabButton, characterTabText);
        ToggleHideoutPanels(playerPanel, stashPanel);
        ToggleSlimPlayerPanel(false);
        currenciesParent.gameObject.SetActive(true);
        menuBackgroundImage.gameObject.SetActive(true);
        hideoutHeaderParent.gameObject.SetActive(true);
        hideoutTabsParent.gameObject.SetActive(true);
    }

    private void CloseHideoutUI() {
        ToggleHideoutPanels();
        HideItemDescPopup(); 
        HideUIElementPopup();
        currenciesParent.gameObject.SetActive(false);
        menuBackgroundImage.gameObject.SetActive(false);
        hideoutHeaderParent.gameObject.SetActive(false);
        hideoutTabsParent.gameObject.SetActive(false);
    }

    private void ShowRaidUI() {
        currenciesParent.gameObject.SetActive(true);
        playerBarsPanel.gameObject.SetActive(true);
        raidTimerText.gameObject.SetActive(true);
        crosshairTrans.gameObject.SetActive(true);
    }

    private void CloseRaidUI() {
        HideItemDescPopup(); 
        HideUIElementPopup();
        currenciesParent.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(false);
        interactPrompt.gameObject.SetActive(false);
        playerBarsPanel.gameObject.SetActive(false);
        raidTimerText.gameObject.SetActive(false);
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
    }
    
    private void InitButtonCallbacks() {
        mainMenuPlayButton.button.onClick.AddListener(() => {
            gameStateMachine.SetStateIfNotCurrent(mapSelectionState);
        });
        
        mainMenuHideoutButton.button.onClick.AddListener(() => {
            gameStateMachine.SetStateIfNotCurrent(hideoutState);
        });
        
        characterTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(characterTabButton, characterTabText);
            ToggleSlimPlayerPanel(false);
            ToggleHideoutPanels(playerPanel, stashPanel);
        });
        
        eyeForgeTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(eyeForgeTabButton, eyeForgeTabText);
            ToggleHideoutPanels(eyeForgePanel, stashPanel);
        });
        
        traderTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(traderTabButton, traderTabText);
            ToggleHideoutPanels(traderInventoryPanel, traderTransactionPanel, stashPanel);
        });
        
        questsTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(questsTabButton, questsTabText);
            ToggleHideoutPanels(questsPanel);
        });
        
        levelupTabButton.onClick.AddListener(() => {
            ToggleHideoutTab(levelupTabButton, levelupTabText);
            ToggleSlimPlayerPanel(false);
            ToggleHideoutPanels(playerPanel, levelupPanel);
        });

        potionManTraderButton.button.onClick.AddListener(() => OnTraderButtonPressed(potionManTrader));
        armsDealerTraderButton.button.onClick.AddListener(() => OnTraderButtonPressed(armsDealerTrader));
        hatManTraderButton.button.onClick.AddListener(() => OnTraderButtonPressed(hatManTrader));
        
        agilityUpgradeButton.button.onClick.AddListener(() => OnLevelupButtonPressed(agilityUpgradePath, player.agilityLevel));
        corruptionUpgradeButton.button.onClick.AddListener(() => OnLevelupButtonPressed(corruptionUpgradePath, player.luckLevel));
        healthUpgradeButton.button.onClick.AddListener(() => OnLevelupButtonPressed(healthUpgradePath, player.healthLevel));
        strengthUpgradeButton.button.onClick.AddListener(() => OnLevelupButtonPressed(strengthUpgradePath, player.strengthLevel));
        
        forgeEyeButton.button.onClick.AddListener(() => {
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
            
            DoEyeForgeAnimation(() => {
                InventoryItem newDemonEyeItem = new() {
                    modifierUuids = new(),
                };

                foreach (InventorySlot slot in crucibleInventory.slots) {
                    if (slot.item == null) continue;
                    
                    if (slot.ui.OnlyAcceptsType(soulcardType)) {
                        newDemonEyeItem.modifierUuids.Add(slot.item.ItemRef.uuid);
                    }
                    slot.item = null;
                }

                BuildAndRegisterEye(newDemonEyeItem);
                
                crucibleInventory.slots[eyeSlotIndex].item = newDemonEyeItem;
            });
        });
        
        upgradeForgeButton.button.onClick.AddListener(() => {
            if (upgradeForgeButton.isDisabled) return;
            
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
            
            ChangeInventorySize(crucibleInventory, crucibleInventory.slots.Length + 1);
            ArrangeEyeCrucibleInventorySlots();
        });
        
        // stashUpgradeButton.onClick.AddListener(() => {
            // UpgradePath.UpgradeRequirements requirements = stashUpgradePath.pathUpgrades[hideoutStateData.stashLevel];
            //
            // bool canUpgrade = true;
            // foreach (UpgradePath.Requirement requirement in requirements.requirements) {
            //     int itemCount = 0;
            //     itemCount += GetItemCountInInventory(stashInventory, requirement.item);
            //     itemCount += GetItemCountInInventory(playerInventory, requirement.item);
            //     
            //     if (itemCount < requirement.count) {
            //         canUpgrade = false;
            //         break;
            //     }
            // }
            //
            // if (!canUpgrade) return;
            //
            // foreach (UpgradePath.Requirement requirement in requirements.requirements) {
            //     int stashRemoveCount = RemoveNumberOfItemsFromInventory(stashInventory, requirement.item, requirement.count);
            //     if (stashRemoveCount == requirement.count) continue;
            //     RemoveNumberOfItemsFromInventory(playerInventory, requirement.item, requirement.count - stashRemoveCount);
            // }
            //
            // hideoutStateData.stashLevel++;
            // SaveToFile(hideoutDataSavePath, hideoutStateData);
            //
            // ChangeInventorySize(stashInventory, stashInventory.slots.Length + stashUpgradeSlotIncrease);
        // });
        
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
                foreach (InventorySlot slot in stashInventory.slots) {
                    if (slot.item == null) continue;
                    slot.item.traderOwned = false;
                }
                transactionState = TransactionState.Empty;
            }
            else if (transactionState == TransactionState.Selling) {
                player.coinCurrency += price;
                
                int xpGain = GetInventoryValue(transactionInventory, InventoryValueType.Xp);
                IncreaseTraderRep(GetCurrentlySelectedTrader(), xpGain);
                ClearInventory(transactionInventory);
                transactionState = TransactionState.Empty;
            }
            
            SavePlayerData();
            SaveInventory(stashInventory);

            RefreshTransactionUI();
        });
        
        easyMapButton.onClick.AddListener(() => {
            LoadMapAsync(lighthouseMap, () => {
                gameStateMachine.SetStateIfNotCurrent(raidState);
                CreateDropPoolsForMap(lighthouseMap);
            });
        });
        
        mediumMapButton.onClick.AddListener(() => {
            LoadMapAsync(customsMap, () => {
                gameStateMachine.SetStateIfNotCurrent(raidState);
                CreateDropPoolsForMap(customsMap);
            });
        });
    }
    
    private int fillParamProperty = Shader.PropertyToID("_Fill");
    
    private void DoEyeForgeAnimation(Action onAnimationEndCallback) {
        Tween.Custom(target: this, 0f, 1f, 5f, (target, val) => {
            target.pentagramFillImage.material.SetFloat(target.fillParamProperty, target.pentagramFillCurve.Evaluate(val));
        }, Ease.Linear)
        .OnComplete(onAnimationEndCallback);
        
        foreach (InventorySlot slot in crucibleInventory.slots) {
            if (slot.item == null) continue;
            
            RectTransform rectTransform = slot.ui.itemUI.rectTransform;
            
            // Use our own shake because prime tween shake's curve does not work
            rectTransform.DoTweenShake(10f, 3.3f, 5f, itemShakeCurve);
            
            Sequence sequence = Sequence.Create();
            sequence.ChainDelay(1f);
            
            if (slot.item.ItemRef.type == eyeType) {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 1.25f, new() {
                    duration = 4f,
                    ease = Ease.InCubic,
                }));
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 1.45f, Vector3.one, new() {
                    duration = 0.15f,
                    ease = Ease.InOutBounce,
                }));
            }
            else {
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one, Vector3.one * 0.87f, new() {
                    duration = 4f,
                    ease = Ease.InCubic,
                }));
                sequence.Chain(Tween.Scale(rectTransform, Vector3.one * 0.87f, Vector3.one, new() {
                    duration = 0.15f,
                    ease = Ease.InOutBounce,
                }));
                sequence.ChainCallback(() => {
                    slot.item = null;
                    slot.ui.ClearItem();
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.localScale = Vector3.one;
                });
            }
        }
    }
    
    private void UpdateInRaidUI() {
        healthBarFillImage.fillAmount = player.health / 100f;
        weightBarFillImage.fillAmount = GetTotalWeightCompletion();
        
        float overweightComp = GetOverweightCompletion();
        if (overweightComp > 0f) {
            weightBarFillImage.color = Color.Lerp(styles.startingOverWeightColor, styles.endingOverWeightColor, overweightComp);
        }
        else {
            weightBarFillImage.color = styles.underWeightColor;
        }

        
        float timeUntilFinalWave = Mathf.Clamp(spawnManager.timeUntilFinalWave, 0f, float.MaxValue);
        int minutesLeft = Mathf.FloorToInt(timeUntilFinalWave / 60f);
        int secondsLeft = Mathf.FloorToInt(timeUntilFinalWave % 60f);
        raidTimerText.text = $"{minutesLeft:0}:{secondsLeft:00}";
    }

    // Its better just to have these as constants because the canvas layout recalculates in LateUpdate
    private const float playerPanelWidth = 570f;
    private const float playerPocketsBackpackWidth = 221.55f;
    
    private void ToggleSlimPlayerPanel(bool toggle) {
        if (toggle) {
            playerPocketsBackpackParent.gameObject.SetActive(false);
            playerPanel.GetComponent<LayoutElement>().preferredWidth = playerPanelWidth - playerPocketsBackpackWidth;
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
            soulsCurrencyText.text = player.soulCurrency.ToString();
        }
        if (prevCoinCurrency != player.coinCurrency) {
            coinCurrencyText.text = player.coinCurrency.ToString();
        }
        prevSoulCurrency = player.soulCurrency;
        prevCoinCurrency = player.coinCurrency;
    }


    private enum CrucibleState { Upgrade, Forging }
    private CrucibleState crucibleState;

    private void UpdateCrucibleState() {
        if (!OnEyeForgeTab) return;
        
        bool forging = GetInventoryItemCount(crucibleInventory) > 0;
        crucibleState = forging ? CrucibleState.Forging : CrucibleState.Upgrade;

        if (forging && forgeEyeButton.isDisabled) {
            forgeEyeButton.Enable();
            upgradeForgeButton.Disable();
            hoverableUIElements.Add(forgeEyeButton.rectTransform);
            hoverableUIElements.Remove(upgradeForgeButton.rectTransform);
        }
        if (!forging && !forgeEyeButton.isDisabled) {
            forgeEyeButton.Disable();
            upgradeForgeButton.Enable();
            hoverableUIElements.Remove(forgeEyeButton.rectTransform);
            hoverableUIElements.Add(upgradeForgeButton.rectTransform);
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
            traderTransactionInfoText.text = string.Empty;
            return;
        }
        
        if (transactionState == TransactionState.Buying) {
            int buyPrice = GetInventoryValue(transactionInventory, InventoryValueType.Buy);
            string buyPriceString = ColorText(buyPrice.ToString(), styles.coinCurrencyColor);
            traderTransactionInfoText.text = $"Purchase for <sprite=0>{buyPriceString}";
        }
        else if (transactionState == TransactionState.Selling) {
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            int xpGain = GetInventoryValue(transactionInventory, InventoryValueType.Xp);
            string sellPriceString = ColorText(sellPrice.ToString(), styles.coinCurrencyColor);
            traderTransactionInfoText.text = $"Sell for <sprite=0>{sellPriceString}\n Gain {xpGain} trader experience";
        }
    }
    
    // ************************
    // Traders
    // ************************

    [Serializable]
    public class TradersSaveData {
        public int potionManRep;
        public int armsDealerRep;
        public int hatManRep;
    }
    
    private TradersSaveData traderSaveData;

    private void SaveTraders() {
        SaveToFile(traderSavePath, traderSaveData);
    }

    private InventorySlot[] potionManSlots;
    private InventorySlot[] armsDealerSlots;
    private InventorySlot[] hatManSlots;

    private void InitTraders() {
        traderSaveData = LoadFromFileOrCreateNew<TradersSaveData>(traderSavePath);

        const int traderInventorySize = traderInventoryRowCount * traderInventoryColCount;
        potionManSlots = new InventorySlot[traderInventorySize];
        armsDealerSlots = new InventorySlot[traderInventorySize];
        hatManSlots = new InventorySlot[traderInventorySize];
        
        potionManSlots.InitalizeWithDefault();
        armsDealerSlots.InitalizeWithDefault();
        hatManSlots.InitalizeWithDefault();

        for (int i = 0; i < traderInventorySize; i++) {
            potionManSlots[i].ui = traderInventoryPtr.slots[i].ui;
            armsDealerSlots[i].ui = traderInventoryPtr.slots[i].ui;
            hatManSlots[i].ui = traderInventoryPtr.slots[i].ui;
        }
        
        OnTraderButtonPressed(potionManTrader); // Toggle default trader

        RefillTraderSlotsWithItems(potionManTrader); 
        RefillTraderSlotsWithItems(armsDealerTrader); 
        RefillTraderSlotsWithItems(hatManTrader); 
    }
    
    private void IncreaseTraderRep(Trader trader, int repGain) {
        if (ReachedTraderMaxRep(trader)) return;

        if (AddToTraderRep(trader, repGain, out int repLevel)) {
            FillTraderRowWithItems(trader, repLevel - 1);
        }
        if (trader == GetCurrentlySelectedTrader()) { 
            SetTraderRepBar(trader);
        }
    }

    private void SetTraderRepBar(Trader trader) {
        int levelIndex = GetTraderRepLevel(trader);
        
        if (ReachedTraderMaxRep(trader)) {
            traderXpLevelFill.fillAmount = 1f;
            traderRemainingXpText.text = string.Empty;
            traderLevelText.text = $"Level {levelIndex} (Max)";
            return;
        }
        
        int prefixedSumAtCurLevel = traderLevels.prefixedSumRepForLevel[levelIndex];
        int prefixedSumAtPrevLevel = traderLevels.prefixedSumRepForLevel[levelIndex - 1];
        int repNeededForThisLevel = prefixedSumAtCurLevel - prefixedSumAtPrevLevel;

        int traderRep = GetTraderRep(trader);
        int repCompletedAtCurLevel = traderRep - prefixedSumAtPrevLevel;
        int repLeftToGo = prefixedSumAtCurLevel - traderRep;
        
        traderXpLevelFill.fillAmount = repCompletedAtCurLevel / (float)repNeededForThisLevel;
        traderRemainingXpText.text = $"{repLeftToGo} Rep Left";
        traderLevelText.text = $"Level {levelIndex}";
    }

    private int GetTraderRep(Trader trader) {
        if (trader == potionManTrader) {
            return traderSaveData.potionManRep;
        }
        if (trader == armsDealerTrader) {
            return traderSaveData.armsDealerRep;
        }
        if (trader == hatManTrader) {
            return traderSaveData.hatManRep;
        }
        return -1;
    }

    private bool AddToTraderRep(Trader trader, int repGain, out int repLevel) {
        int prevLevel = GetTraderRepLevel(trader);
        
        if (trader == potionManTrader) {
            traderSaveData.potionManRep += repGain;
        }
        if (trader == armsDealerTrader) {
            traderSaveData.armsDealerRep += repGain;
        }
        if (trader == hatManTrader) {
            traderSaveData.hatManRep += repGain;
        }
        SaveTraders();

        repLevel = GetTraderRepLevel(trader);
        return prevLevel < repLevel;
    }

    private int GetTraderRepLevel(Trader trader) {
        int rep = GetTraderRep(trader);
        for (int i = 0; i < traderLevels.prefixedSumRepForLevel.Length; i++) {
            if (rep < traderLevels.prefixedSumRepForLevel[i]) {
                return i;
            }
        }
        return traderLevels.prefixedSumRepForLevel.Length;
    }

    private Trader GetCurrentlySelectedTrader() {
        if (traderInventoryPtr.slots == potionManSlots) {
            return potionManTrader;
        }
        if (traderInventoryPtr.slots == armsDealerSlots) {
            return armsDealerTrader;
        }
        if (traderInventoryPtr.slots == hatManSlots) {
            return hatManTrader;
        }
        return null;
    }

    private InventorySlot[] GetTraderInventorySlots(Trader trader) {
        if (trader == potionManTrader) {
            return potionManSlots;
        }
        if (trader == armsDealerTrader) {
            return armsDealerSlots;
        }
        if (trader == hatManTrader) {
            return hatManSlots;
        }
        return null;
    }
    
    private void RefillTraderSlotsWithItems(Trader trader) {
        ClearInventory(GetTraderInventorySlots(trader));
        
        int traderRepLevel = GetTraderRepLevel(trader);
        for (int i = 0; i < traderRepLevel; i++) {
            FillTraderRowWithItems(trader, i);
        }
    }

    private void FillTraderRowWithItems(Trader trader, int rowIndex) {
        // We highjack the trader invetory temporarily to safely add items to it
        // but restore it at the end of this method so its as if nothing changed ;)
        InventorySlot[] slotsToRestore = traderInventoryPtr.slots;
        traderInventoryPtr.slots = GetTraderInventorySlots(trader);
        
        DropPool traderDropPool;
        if (trader == potionManTrader) {
            traderDropPool = potionManTraderDropPool;
        }
        else if (trader == armsDealerTrader) {
            traderDropPool = armsDealerTraderDropPool;
        }
        else {
            traderDropPool = hatManTraderDropPool;
        }
        
        Span<float> raritySkews = stackalloc float[] { 0f, 0.20f, 0.40f, 0.50f };
        
        using var _ = ListPool<Item>.Get(out List<Item> items);
        GetUniqueItemsFromDropPool(traderDropPool, traderInventoryColCount, items, raritySkews[rowIndex]);
        foreach (Item item in items) {
            TryAddItemToInventory(traderInventoryPtr, item, item.MaxStackCount);
        }
        
        // Mark all items as trader owned
        foreach (InventorySlot slot in traderInventoryPtr.slots) {
            if (slot.item == null) continue;
            slot.item.traderOwned = true;
        }
        
        traderInventoryPtr.slots = slotsToRestore;
    }
    
    private bool ReachedTraderMaxRep(Trader trader) {
        int rep = GetTraderRep(trader);
        return rep >= traderLevels.prefixedSumRepForLevel[^1];
    }
    
    private void OnTraderButtonPressed(Trader selectedTrader) {
        Trader prevTrader = GetCurrentlySelectedTrader();
        
        potionManTraderButton.Toggle(false);
        armsDealerTraderButton.Toggle(false);
        hatManTraderButton.Toggle(false);
        
        if (selectedTrader == potionManTrader) {
            traderInventoryPtr.slots = potionManSlots;
            potionManTraderButton.Toggle(true);
        }
        if (selectedTrader == armsDealerTrader) {
            traderInventoryPtr.slots = armsDealerSlots;
            armsDealerTraderButton.Toggle(true);
        }
        if (selectedTrader == hatManTrader) {
            traderInventoryPtr.slots = hatManSlots;
            hatManTraderButton.Toggle(true);
        }

        bool switchedTraders = GetCurrentlySelectedTrader() != prevTrader;
        if (switchedTraders) {
            if (transactionState == TransactionState.Buying) {
                MoveEntireInventory(transactionInventory, traderInventoryPtr);
            }
            else if (transactionState == TransactionState.Selling) {
                MoveEntireInventory(transactionInventory, stashInventory);
            }
            SetTraderRepBar(selectedTrader);
        }
    }
    
    // ************************
    // Quests 
    // ************************

    private const int activeQuestCount = 3;
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
            
            Quest.SaveState saveState = questlineState.questSaveStates[i];
            if (saveState != null) {
                quest.LoadSaveState(saveState);     
            }
            quest.Init(questLines[i].questGiver);

            QuestUI ui = Instantiate(questPrefab, questsParent).GetComponent<QuestUI>();
            questUIs[i] = ui;
            
            int callbackIndex = i;
            ui.completeButton.onClick.AddListener(() => OnQuestCompleteClicked(callbackIndex));
            ui.Set(quest);
        }
    }

    private void OnQuestCompleteClicked(int activeQuestIndex) {
        
    }

    private void UpdateQuests() {
        for (int i = 0; i < activeQuests.Length; i++) {
            activeQuests[i].UpdateQuest(this);
            questUIs[i].Set(activeQuests[i]);
        }
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
        else if (upgradePath == corruptionUpgradePath) {
            player.luckLevel++;
        }
        else if (upgradePath == healthUpgradePath) {
            player.healthLevel++;
        }
        else if (upgradePath == strengthUpgradePath) {
            player.strengthLevel++;
        }
        
        SavePlayerData();
        RefreshLevelUpPossibilities();
    }
    
    private void RefreshLevelUpPossibilities() {
        ToggleStatUpgradeButton(agilityUpgradeButton, agilityUpgradePath, player.agilityLevel);
        ToggleStatUpgradeButton(corruptionUpgradeButton, corruptionUpgradePath, player.luckLevel);
        ToggleStatUpgradeButton(healthUpgradeButton, healthUpgradePath, player.healthLevel);
        ToggleStatUpgradeButton(strengthUpgradeButton, strengthUpgradePath, player.strengthLevel);
    }

    private void ToggleStatUpgradeButton(ButtonFeel button, StatUpgradePath upgradePath, int playerStatLevel) {
        UpgradeStatResult result = CanUpgradeStat(upgradePath, playerStatLevel);
        switch (result) {
            case UpgradeStatResult.CantAfford:
                button.Disable();
                button.text.text = $"{upgradePath.soulsNeededPerLevel[playerStatLevel]} Souls";
                break;
            case UpgradeStatResult.Affordable:
                button.Enable();
                button.text.text = $"{upgradePath.soulsNeededPerLevel[playerStatLevel]} Souls";
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
    
    private Dictionary<int, List<DynamicClipRecord>> clipRecords;
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

    private void PlayAudioClip(DynamicClip dynamicClip, Vector2 position, float volumeScaler) {
        if (ClipIsViolatingLocalArea(dynamicClip, position)) return;
        
        AudioSource source = sources.Dequeue();
        sources.Enqueue(source);
        
        source.transform.position = position;
        source.rolloffMode = dynamicClip.rolloffMode;
        source.clip = dynamicClip.clips[Random.Range(0, dynamicClip.clips.Length)];
        source.outputAudioMixerGroup = dynamicClip.mixerGroup;
        source.volume = volumeScaler;
        source.pitch = Random.Range(dynamicClip.minPitch, dynamicClip.maxPitch);
        source.minDistance = dynamicClip.minDistance;
        source.maxDistance = dynamicClip.maxDistance;
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
    
    // ************************
    // Scene Management 
    // ************************

    private enum MapLoadingState { Unloaded, Loaded, Loading, Unloading }
    private MapLoadingState mapLoadingState;

    [NonSerialized] public MapData loadedMapData;
    [NonSerialized] public MapInstance currentMapInstance;

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
                currentMapInstance = map;
                map.gameObject.SetActive(false);
                break;
            }
            
            ListPool<GameObject>.Release(loadedMapRoots);
            onLoadedCallback?.Invoke();
        }
    }

    public void UnloadCurrentMapAsync() {
        if (UnloadingMapInProgress()) return;
            
        currentMapInstance.gameObject.SetActive(false); 
        
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

    private enum DropOrigin { Rock, Body, Trader, Enemy }

    private struct DropPool {
        public List<Item> items;
        public DropOrigin dropOrigin;
    }

    private DropPool rockStonesDropPool;
    private DropPool rockUpgradesDropPool;
    private DropPool bodyDropPool;
    private DropPool potionManTraderDropPool;
    private DropPool armsDealerTraderDropPool;
    private DropPool hatManTraderDropPool;
    private DropPool enemyDropPool;

    private void CreateDropPools() {
        rockStonesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        rockUpgradesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        bodyDropPool = new() { items = new(), dropOrigin = DropOrigin.Body };
        potionManTraderDropPool = new() { items = new(), dropOrigin = DropOrigin.Trader };
        armsDealerTraderDropPool = new() { items = new(), dropOrigin = DropOrigin.Trader };
        hatManTraderDropPool = new() { items = new(), dropOrigin = DropOrigin.Trader };
        enemyDropPool = new() { items = new(), dropOrigin = DropOrigin.Enemy };

        foreach ((int _, Item item) in itemLookup) {
            if (item.chanceToSpawnOnTrader > 0f) {
                if (item.associatedTrader == potionManTrader) {
                    potionManTraderDropPool.items.Add(item);
                }
                if (item.associatedTrader == armsDealerTrader) {
                    armsDealerTraderDropPool.items.Add(item);
                }
                if (item.associatedTrader == hatManTrader) {
                    hatManTraderDropPool.items.Add(item);
                }
            }

            if (item.chanceToSpawnFromEnemy > 0f) {
                enemyDropPool.items.Add(item);
            }
        }
    }
    
    private void CreateDropPoolsForMap(MapData map) { 
        rockStonesDropPool.items.Clear();
        rockUpgradesDropPool.items.Clear();
        bodyDropPool.items.Clear();
        
        foreach ((int _, Item item) in itemLookup) {
            bool spawnsOnCurrentMap = item.spawnsOnAllMaps || item.spawnsOnMaps.Contains(map);
            if (!spawnsOnCurrentMap) continue;
            
            if (item.chanceToSpawnFromRock > 0f) {
                List<Item> itemsForRock = item.type == soulcardType ? rockUpgradesDropPool.items : rockStonesDropPool.items;
                itemsForRock.Add(item);
            }
            
            if (item.chanceToSpawnOnBody > 0f) {
                bodyDropPool.items.Add(item);
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
        
        Item item = GetItemFromDropPool(tempEnemyPool);
        ListPool<Item>.Release(tempEnemyPool.items);
        return item;
    }

    private Item GetItemFromDropPool(DropPool dropPool) {
        Assert.IsFalse(dropPool.items == enemyDropPool.items, $"Use {nameof(GetItemFromEnemyDropPool)} for enemies");
        
        float dropTotal = 0f;
        foreach (Item drop in dropPool.items) {
            dropTotal += GetDropChanceOfItem(drop, dropPool.dropOrigin);
        }

        float randomChance = Random.Range(0f, dropTotal);
        float prefixSum = 0f;
        
        foreach (Item drop in dropPool.items) {
            prefixSum += GetDropChanceOfItem(drop, dropPool.dropOrigin);
            if (randomChance < prefixSum) {
                return drop;
            }
        }
        
        return dropPool.items[^1];
    }

    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, List<Item> items, float raritySkew = 0f) {
        foreach (Item item in dropPool.items) {
            float itemDropChance = GetDropChanceOfItem(item, dropPool.dropOrigin) + raritySkew;
            if (itemDropChance > 1f) continue;
            
            if (Random.value < itemDropChance) {
                items.Add(item);
            }
        }

        items.Shuffle();

        bool itemListNeedsTrimming = items.Count > maxCount;
        if (itemListNeedsTrimming) {
            items.RemoveRange(maxCount, items.Count - maxCount);
        }
    }

    private float GetDropChanceOfItem(Item item, DropOrigin origin) {
        float addChanceToSpawnFromLuck = 0f;
        
        if (origin != DropOrigin.Trader) {
            addChanceToSpawnFromLuck = item.GetRarity() switch {
                // Scaling the luck increase exponentionally (the adding/subtracting 1 is because rarity skew from luck is a decimal)
                Item.Rarity.Uncommon  => Mathf.Pow(1f + RaritySkewIncreaseFromLuck, 1.1f) - 1f,
                Item.Rarity.Rare      => Mathf.Pow(1f + RaritySkewIncreaseFromLuck, 1.2f) - 1f,
                Item.Rarity.Legendary => Mathf.Pow(1f + RaritySkewIncreaseFromLuck, 1.3f) - 1f,
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
    
    // ************************
    // Controls
    // ************************
    
    private InputDevice lastDeviceToGiveAimInput;
    
    public bool AimingWithController() {
        if (lookInputAction.activeControl != null) {
            lastDeviceToGiveAimInput = lookInputAction.activeControl.device;
        }
        
        if (!ControllerPluggedIn) {
            return false;
        }
        
        return lastDeviceToGiveAimInput is Gamepad;
    }
    
    public static bool RollProbability(float probability) {
        return Random.value < probability;
    }
    
    private Vector2 ScreenCenter => new(Screen.width / 2f, Screen.height / 2f);
    
    private bool InHideout => gameStateMachine.CurState == hideoutState;
    
    private bool InMapSelection => gameStateMachine.CurState == mapSelectionState;
    
    private bool InRaid => gameStateMachine.CurState == raidState;

    public bool ControllerPluggedIn => Gamepad.current != null;

    private Vector3 RotationVector360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
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

    private float CurrentClipLength(Animator anim) {
        return anim.GetCurrentAnimatorStateInfo(0).length;
    }

    private void ForCollidersInOverlapCircle(Vector2 center, float radius, LayerMask mask, int maxColliders, Action<Collider2D> perItemCallback) {
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        List<Collider2D> cols = ListPool<Collider2D>.Get();
        
        if (cols.Capacity < maxColliders) {
            cols.Capacity = maxColliders;
        }
        
        int count = Physics2D.OverlapCircle(center, radius, contactFilter, cols);
        for (int i = 0; i < count; i++) {
            perItemCallback?.Invoke(cols[i]);
        }
        
        ListPool<Collider2D>.Release(cols);
    }

    private static string SizeText(string text, int fontSize) {
        return $"<size={fontSize}>{text}</size>";
    }
    
    public static string ColorText(string text, Color color) {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }
    
    public static string DisplayProb(float probability) {
        return ColorText($"{Mathf.FloorToInt(probability * 100f)}%", instance.styles.increaseDescColor);
    }
    
    public static string DisplayProbIncrease(float probability) {
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", instance.styles.increaseDescColor);
    }
    
    public static string DisplayProbDecrease(float probability) {
        return ColorText($"-{Mathf.FloorToInt(probability * 100f)}%", instance.styles.decreaseDescColor);
    }

    
    public static string DisplayNumber(int number) {
        return ColorText(number.ToString(), instance.styles.increaseDescColor);
    }
    
    public static string DisplayNumber(float number) {
        return ColorText(number.ToString("0.00"), instance.styles.increaseDescColor);
    }


    public static string DisplayIncrease(int amount) {
        return ColorText($"+{amount}", instance.styles.increaseDescColor);
    }
    
    public static string DisplayIncrease(float amount) {
        return ColorText($"+{amount:0.00}", instance.styles.increaseDescColor);
    }
    
    public static string DisplayMultiplier(float multiplier) {
        return ColorText($"{multiplier:0.00}x", instance.styles.increaseDescColor);
    }

    private enum CardinalDir { Right, Left, Up, Down }

    private CardinalDir CardinalDirFromVector(Vector2 vector) {
        float dot = Vector2.Dot(Vector2.right, vector);
        if (Mathf.Abs(dot) >= 0.2f) {
            return vector.x > 0 ? CardinalDir.Right : CardinalDir.Left;
        } 
        return vector.y > 0 ? CardinalDir.Up : CardinalDir.Down;
    }

}