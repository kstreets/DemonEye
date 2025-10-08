using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;

public class Game : MonoBehaviour {

    public static Game instance;
    
    public TraderConfig traderConfig;
    public StartingItemsConfig startingItems;
    public List<Scene> mapSequence;

    [Foldout("Pooling Prefabs")]
    public GameObject baseProjectilePrefab;
    public GameObject stoppingPowerProjectilePrefab;
    public GameObject bloodDropPrefab;
    public GameObject poisonDebuffPrefab;
    public GameObject explosionPrefab;
    [EndFoldout]
    
    [Foldout("Item Type Refs")]
    public ItemType consumableType;
    public ItemType backpackType;
    public ItemType eyeType;
    public ItemType demonEyeType;
    public ItemType trinketType;
    public ItemType soulcardType;
    [EndFoldout]

    [Foldout("Item Refs")]
    public Item bandageItem;
    public Item healthPotionItem;
    public Item demonSteakItem;
    public Item pouchItem;
    public Item ruckSackItem;
    [EndFoldout]
    
    [Foldout("Gameplay Variables")]
    [Range(0f, 1f)] public float defaultCriticalStrikeChange;
    public float defaultCriticalStrikeMultiplier;
    [EndFoldout]

    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public RectTransform crosshairTrans;
    public Transform exitPortalSpawnParent;

    public GameObject playerPrefab;
    public GameObject gemRockPrefab;
    public GameObject altarPrefab;
    public GameObject deadBodyPrefab;
    public GameObject exitPortalPrefab;

    public BaseCharacterStats baseStats;
    public CoreAttack defaultAttack;
    public Item demonEyeItem;
    
    public ItemPool deadBodyPool;
    public DropPool altarDropPool;
    public DropPool rockDropPool;

    [Foldout("Effects")]
    public AnimationCurve hitFlashCurve;
    public AnimationCurve bounceCurve;
    [EndFoldout]
    
    [Foldout("UI/Prefabs")]
    public GameObject inventoryItemPrefab;
    public GameObject inventorySlotPrefab;
    public GameObject rockSmokePrefab;
    public GameObject damageNumberPrefab;
    [EndFoldout]
    
    [Foldout("UI/MiscRefs")]
    public ItemDescPopup itemDescPopup;
    public MechanicDescPopup mechanicDescPopup;
    public Button enterNextRaidButton;
    public RectTransform hideoutHeaderParent;
    public ItemUI dragAndDropItemUI;
    [EndFoldout]
    
    [Foldout("UI/HideoutTabs")]
    public RectTransform hideoutTabsParent;
    public Sprite tabNonSelectedSprite;
    public Sprite tabSelectedSprite;
    public Button characterTabButton;
    public Button eyeForgeTabButton;
    public Button traderTabButton;
    [EndFoldout]

    [Foldout("UI/PlayerPanel")]
    public RectTransform playerPanel;
    public RectTransform playerPocketParent;
    public RectTransform playerBackpackParent;
    public RectTransform playerPocketsBackpackParent;
    public RectTransform playerInventoryParent;
    public TextMeshProUGUI playerPanelHealthText;
    public TextMeshProUGUI playerPanelWeightText;
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
    public Button crucibleForgeButton;
    public Button crucibleUpgradeButton;
    [EndFoldout]
    
    [Foldout("UI/TraderPanel")]
    public RectTransform traderTransactionPanel;
    public RectTransform traderInventoryPanel;
    public RectTransform traderInventoryParent;
    public RectTransform traderTransactionInventoryParent;
    public TextMeshProUGUI traderTransactionInfoText;
    public Image traderXpLevelFill;
    public Button traderDealButton;
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
    public Color criticalStrikeColor;
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
    private InputAction attackInputAction;
    private InputAction interactInputAction;
    private InputAction inventoryInputAction;
    private InputAction selectItemInputAction;
    private InputAction useItemInputAction;
    private InputAction moveStackInputAction;
    private InputAction splitStackInputAction;
    
    [NonSerialized] public List<Entity> entities = new();
    [NonSerialized] public Dictionary<GameObject, Entity> entityLookup = new();
    [NonSerialized] public List<Projectile> projectiles = new();
    [NonSerialized] public List<Enemy> enemies = new();
    
    public static Dictionary<int, Item> itemLookup = new();
    public static Dictionary<int, Soulcard> eyeModifierLookup = new();

    private Timer exitPortalTimer;
    private int consecutiveCriticalHits;

    private EntityPool<Entity> bloodDropPool;
    private EntityPool<Projectile> projectilePool;
    private EntityPool<Projectile> stoppingPowerProjectilePool;
    private EntityPool<Entity> poisonDebuffPool;
    private EntityPool<Entity> explosionPool;
    
    private State hideoutState;
    private State raidState;
    private State gameOverState;
    private StateMachine gameStateMachine = new();

    private HideoutStateData hideoutStateData;
    private RaidStateData raidStateData;
    
    private void Start() {
        instance = this;
        
        #if UNITY_EDITOR
        Application.targetFrameRate = 0;
        #endif
        
        LoadAllItems();
        InitAudio();
        InitHideoutUI();
        
        BuildSavePaths();
        hideoutStateData = LoadFromFile<HideoutStateData>(hideoutDataSavePath) ?? new HideoutStateData();
        // raidStateData = LoadFromFile<RaidStateData>(raidDataSavePath) ?? new RaidStateData();
        player = SpawnEntity<Player>(playerPrefab, Vector3.zero, Quaternion.identity, null, EntityLifetime.Global);
        player.gameObject.SetActive(false);
        LoadAndAssignPlayerSaveData(player);

        List<string> mapSceneNames = new();
        mapSceneNames.Add("Lighthouse");
        mapSceneNames.Add("Customs");
        
        // Temporary for now
        {
            raidStateData = new() {
                raidDifficulty = 0,
                mapSceneNames = mapSceneNames,
            };
        }
        
        InitInventory();
        LoadInventory(playerInventory);
        LoadInventory(stashInventory);
        InitButtonCallbacks();
        AddItemsToTraderInventory(hideoutStateData.traderLevel);
        SetStashValue(0);
        
        bloodDropPool = CreateEntityPool<Entity>(bloodDropPrefab, 10, null);
        projectilePool = CreateEntityPool<Projectile>(baseProjectilePrefab, 20, OnSpawnProjectile);
        stoppingPowerProjectilePool = CreateEntityPool<Projectile>(stoppingPowerProjectilePrefab, 20, OnSpawnProjectile);
        poisonDebuffPool = CreateEntityPool<Entity>(poisonDebuffPrefab, 10, null);
        explosionPool = CreateEntityPool<Entity>(explosionPrefab, 5, null);

        equipedEye = new() { coreAttack = defaultAttack };
        
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        selectItemInputAction = InputSystem.actions.FindAction("SelectItem");
        splitStackInputAction = InputSystem.actions.FindAction("SplitStack");
        moveStackInputAction = InputSystem.actions.FindAction("MoveStack");
        useItemInputAction = InputSystem.actions.FindAction("UseItem");

        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEnter, OnRaidStateExit);
        gameOverState = gameStateMachine.CreateState(null, OnGameOverEnter, OnGameOverExit);
        
        raidState.To(gameOverState).When(() => player.health <= 0);
        gameOverState.To(hideoutState).When(() => true).AfterSeconds(3f);
    }

    private void Update() {
        UpdateDelayedEntitiesToDestroy();
        gameStateMachine.Tick();
    }

    private void FixedUpdate() {
        if (!InRaid) return;
        raidStateData.currentMap.grid.CompleteFlowFieldCalculation();
        raidStateData.currentMap.grid.ScheduleFlowFieldCalculation(player.position);
        FixedUpdateEnemies();
    }

    private void LateUpdate() {
        UpdateDragAndDropItemToCursor();
    }

    private void OnApplicationQuit() {
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
        SavePlayerData();
        raidStateData.currentMap.grid.Deinit();
    }

    private void UpdateTimers() {
        exitPortalTimer.Tick();
        discoverLootTimer.Tick();
    }

    private void OnHideoutStateEnter() {
        string nextMapScene = raidStateData.mapSceneNames[raidStateData.raidDifficulty];
        LoadMapAsync(nextMapScene);
        
        if (raidStateData.raidDifficulty == 0 && GetInventoryWeight(playerInventory) == 0) {
            foreach (StartingItemsConfig.ItemConfig config in startingItems.configs) {
                TryAddItemToInventory(playerInventory, config.item, config.count);
            }
        }
        
        Cursor.visible = true;
        ShowRaidUI(false); 
        InitHideoutUI(); 
        RefreshInventoryDisplay(playerInventory);
        RefreshInventoryDisplay(stashInventory);
        RefreshInventoryDisplay(crucibleInventory);
        RefreshInventoryDisplay(transactionInventory);
    }

    private void OnHideoutStateExit() {
        CloseHideoutUI();
    }

    private void OnHideoutStateUpdate() {
        UpdateInventory();
    }

    private void OnRaidStateEnter() {
        ShowRaidUI(true);
        
        Map curMap = raidStateData.currentMap;
        curMap.gameObject.SetActive(true);
        curMap.grid.Init();

        int randomSpawnIndex = Random.Range(0, curMap.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = curMap.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.gameObject.SetActive(true);
        player.position = randomSpawnPos;
        cinemachineCamera.Follow = player.trans;
        
        InitExitPortal();
        InitSpawnManager(curMap.waves);
        SpawnResources(curMap.resourceParent);
    }

    private void OnRaidStateExit() {
        if (player.health > 0) {
            LeaveRaid();
            raidStateData.raidDifficulty++;
        }
        raidStateData.currentMap.grid.Deinit();
        UnloadCurrentMapAsync();
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
        UpdateInRaidUi();
        if (spawnManager.totalTimeLeft <= 0f) {
            gameStateMachine.SetState(hideoutState);
        }
    }

    private void OnGameOverEnter() { }

    private void OnGameOverExit() {
        player.health = 100;
        SavePlayerData();
        LeaveRaid();

        ClearInventory(playerInventory);
        ClearInventory(stashInventory);
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
        ResetCrucibleUpgrades();
        raidStateData.raidDifficulty = 0;
        hideoutStateData.stashLevel = 0;
        SaveToFile(hideoutDataSavePath, hideoutStateData);
    }

    private void LeaveRaid() {
        DestroyLevelEntities();
        ClearProjectiles();
        raidStateData.currentMap.gameObject.SetActive(false);
        playerBarsPanel.gameObject.SetActive(false);
        player.gameObject.SetActive(false);
    }

    private void ResetCrucibleUpgrades() {
        hideoutStateData.crucibleLevel = 0;
        for (int i = 1; i < crucibleInventory.slots.Length; i++) {
            crucibleInventory.slots[i].ui.MakeSlotInactive();
        }
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
        
        public SpringShake? springShake;
        public ScaleEffect? scaleEffect;
        public HitFlashEffect? hitFlashEffect;
        public PoisonedEffect? poisonedEffect;
        public BounceEffect? bounceEffect;
        public ParentToEntity? parentEffect;
        public TweenPosition? tweenPosition;
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }

        public Vector3 Center => collider.bounds.center;
        public bool IsValid => trans;
        public GameObject gameObject => trans.gameObject;
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
        entity.animator?.Rebind();
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
    
    private void DestroyEntity(Entity entity) {
        RemoveHitFlashEffect(entity);
        RemovePoisonedEffect(entity);
        entity.parentEffect = null;
        
        entityLookup.Remove(entity.gameObject);
        entities.Remove(entity);
        DestroyOrReleaseEntitysGameObject(entity);
    }
    
    private void DestroyEntityAtIndex(int entityIndex) {
        Entity entity = entities[entityIndex];
        entityLookup.Remove(entity.gameObject);
        entities.RemoveAt(entityIndex);
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

    private List<(Entity entity, float delay)> delayedEntitiesToDestroy = new();
    
    private void DestroyEntity(Entity entity, float delay) {
        delayedEntitiesToDestroy.Add((entity, delay));
    }

    private void UpdateDelayedEntitiesToDestroy() {
        for (int i = delayedEntitiesToDestroy.Count - 1; i >= 0; i--) {
            (Entity entity, float delay) tuple = delayedEntitiesToDestroy[i];
            tuple.delay -= Time.deltaTime;
            if (tuple.delay < 0) {
                if (tuple.entity.IsValid) { // Could already be cleaned up on gameover
                    DestroyEntity(tuple.entity);
                }
                delayedEntitiesToDestroy.RemoveAt(i);
            }
            else {
                delayedEntitiesToDestroy[i] = tuple;
            }
        }
    }
    
    private void UpdateEntityEffects() {
        foreach (Entity entity in entities) {
            UpdateShakeEffect(entity);
            UpdateScaleEffect(entity);
            UpdateHitFlashEffect(entity);
            UpdatePoisonedEffect(entity);
            UpdateBounceEffect(entity);
            UpdateParentEffect(entity);
            UpdateTweenPosition(entity);
        }
    }
    
    
    public struct SpringShake {
        public float stiffness;
        public float damping; 
        public Vector2 velocity;
        public Vector2 offset;
        public Vector2 targetPos;
    }

    private void AddSpringShakeEffect(Entity entity, Vector2 velocity) {
        const float stiffness = 2000f;
        const float damping = 4f;
        
        SpringShake shake = new() {
            stiffness = stiffness,
            damping = damping,
            targetPos = entity.trans.localPosition,
        };

        const float randomVelocityAngle = 15f;
        const float shakeMagnitude = 0.025f;
        shake.offset = (Quaternion.AngleAxis(Random.Range(-randomVelocityAngle, randomVelocityAngle), Vector3.forward) * velocity.normalized * shakeMagnitude).ToVector2();
        
        entity.springShake = shake;
    }

    private void UpdateShakeEffect(Entity entity) {
        if (!entity.springShake.TryGetValue(out var shake)) return;
        
        float dt = Time.deltaTime;
        float k = shake.stiffness;
        float c = shake.damping;

        Vector2 x = shake.offset;
        Vector2 v = shake.velocity;

        // Derived constants
        float omega = Mathf.Sqrt(k);
        float zeta = c / (2f * omega);

        if (zeta < 1f) // underdamped
        {
            float expTerm = Mathf.Exp(-zeta * omega * dt);
            float c1 = expTerm * (Mathf.Cos(omega * Mathf.Sqrt(1f - zeta * zeta) * dt));
            float c2 = expTerm * (Mathf.Sin(omega * Mathf.Sqrt(1f - zeta * zeta) * dt));

            // Combine into matrix form for performance
            Vector2 newX = c1 * x + (dt * c2) * v;
            Vector2 newV = (-omega * zeta * c1 * x) + (c1 * v - omega * c2 * x);
            shake.offset = newX;
            shake.velocity = newV;
        }
        else // critically/overdamped
        {
            float expTerm = Mathf.Exp(-omega * dt);
            shake.offset = expTerm * (x + v * dt);
            shake.velocity = expTerm * (v - omega * x * dt);
        }

        entity.trans.localPosition = shake.targetPos + shake.offset;
        entity.springShake = shake;
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


    public enum TweenCurve { Linear, EaseOut, EaseIn, EaseInOut }

    public struct TweenPosition {
        public Vector2 startPos;
        public Vector2 endPos;
        public Timer timer;
        public TweenCurve curve;
    }

    private void AddTweenPosition(Entity entity, Vector2 endPos, float duration, TweenCurve curve = TweenCurve.Linear) {
        TweenPosition tween = new() {
            startPos = entity.position,
            endPos = endPos,
            curve = curve
        };
        tween.timer.SetTime(duration);
        entity.tweenPosition = tween;
    }

    private void UpdateTweenPosition(Entity entity) {
        if (!entity.tweenPosition.TryGetValue(out var tween)) return;

        tween.timer.Tick();
        float comp = tween.timer.Comp();
        
        switch (tween.curve) {
            case TweenCurve.EaseOut:
                comp = 1 - Mathf.Pow(1 - comp, 3);
                break;
            case TweenCurve.EaseIn:
                comp = Mathf.Pow(comp, 3);
                break;
            case TweenCurve.EaseInOut:
                comp = Mathf.SmoothStep(0f, 1f, comp);
                break;
        }
        
        entity.position = Vector2.Lerp(tween.startPos, tween.endPos, comp);
        entity.tweenPosition = tween;
    }
    
    // *****************************
    // Enemy 
    // *****************************
    
    public class Enemy : Entity {
        public EnemyData data;
        public Timer applyDamageTimer;
        public BleedModInstance? bleed;
        public PoisonSoulcard.InstanceData? poisoned;
        public SlowInstance? defaultSlow;
        public SlowInstance? slow;
    }
    
    private void UpdateEnemies() {
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            enemy.applyDamageTimer.Tick();

            float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);

            if (!enemy.poisoned.HasValue && distFromPlayer < 0.35f && !enemy.animator.Playing("Attack")) {
                enemy.animator.Play("Attack");
                enemy.applyDamageTimer.SetTime(0.31f);
                enemy.applyDamageTimer.EndAction = () => {
                    Vector3 dirToPlayer = (player.Center - enemy.Center).normalized;
                    Vector2 attackCheckPos = enemy.Center + dirToPlayer * 0.15f;
                    Collider2D col = Physics2D.OverlapCircle(attackCheckPos, 0.15f, Masks.PlayerMask);
                    if (col != null) {
                        DamagePlayer(enemy.data.damage,enemy.data.changeToCauseBleed);
                    }
                };
            }
            
            if (enemy.bleed.TryGetValue(out var bleed)) {
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    enemy.health -= bleed.bleedDamage;
                    bleed.lastBleedTime = Time.time;
                    enemy.bleed = bleed;
                    Entity bloodDrop = SpawnEntity(bloodDropPool, OffsetY(enemy.position, 0.015f), Quaternion.identity);
                    AddParentEffect(bloodDrop, enemy, 0.4f);
                    DestroyEntity(bloodDrop, 0.8f);
                }
            }

            if (enemy.health <= 0) {
                // Drop items from enemy 
                {
                    EnemyData.ItemDrop[] itemDrops = enemy.data.itemDrops;
                    foreach (EnemyData.ItemDrop itemDrop in itemDrops) {
                        float randomChance = Random.value;
                        if (randomChance < itemDrop.dropChance) {
                            SpawnEntity<Entity>(itemDrop.itemPrefab, enemy.position, Quaternion.identity);
                            break;
                        }
                    }
                }

                // Add enemy soul to nearby altar
                {
                    Altar closestAltar = null;
                    float closestDistance = float.MaxValue;
                    foreach (Altar altar in activeAltars) {
                        float dist = Vector2.Distance(altar.gameObject.transform.position, enemy.position);
                        if (dist < closestDistance) {
                            closestDistance = dist;
                            closestAltar = altar;
                        }
                    }

                    const float maxSoulDistFromAltar = 3f;
                    if (closestAltar != null && closestDistance < maxSoulDistFromAltar) {
                        closestAltar.soulCompletion += 0.025f;
                        if (closestAltar.soulCompletion >= 1f) {
                            // SpawnLevelEntity<Entity>(altarDropPool.GetDropFromPool(), closestAltar.gameObject.transform.position + new Vector3(0f, 0.3f, 0f), Quaternion.identity);
                            activeAltars.Remove(closestAltar);
                        }
                    }
                    
                }

                DestroyEntity(enemies[i]);
                enemies.RemoveAt(i);
            }
        }
    }
    
    private void FixedUpdateEnemies() {
        foreach (Enemy enemy in enemies) {
            float speed = enemy.data.speed;
            
            float totalSlowPercentage = 0f;
            if (enemy.defaultSlow.TryGetValue(out var defaultSlow)) {
                totalSlowPercentage += defaultSlow.speedReductionPercent;
                if (Time.time > defaultSlow.activationTime + defaultSlow.duration) {
                    enemy.defaultSlow = null;
                }
            }
            if (enemy.slow.TryGetValue(out var slow)) {
                totalSlowPercentage += slow.speedReductionPercent;
                if (Time.time > slow.activationTime + slow.duration) {
                    enemy.slow = null;
                }
            }
            speed = Mathf.Clamp(speed * Mathf.Clamp01(1f - totalSlowPercentage), 0.05f, enemy.data.speed);
            
            AnimatorStateInfo animStateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (animStateInfo.IsName("Attack")) {
                if (animStateInfo.normalizedTime > 1f) {
                    enemy.animator.Play("Walk");        
                }
                else {
                    speed = 0f;
                }
            }
            
            Vector2 moveDir = raidStateData.currentMap.grid.GetFlowFieldDirection(enemy.position);
            enemy.rigidbody.linearVelocity = moveDir * speed;

            enemy.spriteRenderer.flipX = player.position.x < enemy.position.x;
        }
    }

    public class EnemySpawnManager {
        public float timeInPhase;
        public float totalTimeLeft;
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
    }
    
    private void UpdateSpawnManager() {
        EnemySpawnManager sm = spawnManager;
        
        if (sm.curPhaseIndex >= sm.spawnPattern.spawnPhases.Count) return;
        
        sm.timeInPhase += Time.deltaTime;
        sm.totalTimeLeft -= Time.deltaTime;
        
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
            CoolerGrid.GridCell randomSpawnGridPos = raidStateData.currentMap.grid.GetSpawnPosition(player.position);
            Vector2 randomSpawnPos = randomSpawnGridPos.position;

            EnemyData enemyToSpawn = sm.spawnEvents[sm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity);
            enemy.health = enemyToSpawn.health;
            enemy.data = enemyToSpawn;
            enemies.Add(enemy);
            
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
    [NonSerialized] private Inventory traderInventory;
    [NonSerialized] private Inventory transactionInventory;
    [NonSerialized] private Inventory lootInvetoryPtr;
    [NonSerialized] private List<Inventory> allInventories = new();
    
    private const int playerPocketSize = 12;
    private const int playerEquipmentSize = 3;
    private int DefaultPlayerInventorySize => playerPocketSize + playerEquipmentSize;

    private Timer discoverLootTimer;
    private int discoverLootIndex;

    private int stashValue;

    private enum TransactionInvetoryState { Empty, Buying, Selling }
    private TransactionInvetoryState transactionState;
    
    private bool InventoryIsOpen => playerPanel.gameObject.activeInHierarchy;
    private bool LootInventoryIsOpen => lootInventoryPanel.gameObject.activeInHierarchy;

    private bool OnCharacterTab => characterTabButton.image.sprite == tabSelectedSprite;
    private bool OnEyeForgeTab => eyeForgeTabButton.image.sprite == tabSelectedSprite;
    private bool OnTradingTab => traderTabButton.image.sprite == tabSelectedSprite;
    
    private void InitInventory() {
        SpawnUiSlots(playerPocketParent, playerPocketSize);
        SpawnUiSlots(playerBackpackParent, 20);
        playerInventory = CreateInventory(playerInventoryParent, DefaultPlayerInventorySize); 
        
        const int cachedLootInventorySize = 12;
        SpawnUiSlots(lootInventoryParent, cachedLootInventorySize); 
        lootInvetoryPtr = CreateInventory(lootInventoryParent, cachedLootInventorySize);

        int stashInventorySize = 40;
        SpawnUiSlots(stashInventoryParent, stashInventorySize);
        stashInventory = CreateInventory(stashInventoryParent, stashInventorySize);
        
        const int traderInventorySize = 15;
        SpawnUiSlots(traderInventoryParent, traderInventorySize);
        traderInventory = CreateInventory(traderInventoryParent, traderInventorySize);
        
        const int transactionInventorySize = 20;
        SpawnUiSlots(traderTransactionInventoryParent, transactionInventorySize);
        transactionInventory = CreateInventory(traderTransactionInventoryParent, transactionInventorySize);

        const int crucibleInventorySize = 9;
        // Spawn crucible slots
        { 
            const int crucibleVeinSize = crucibleInventorySize - 1;
            Vector2 crucibleCenter = crucibleParent.position;
            GameObject centerSlot = Instantiate(inventorySlotPrefab, crucibleCenter, Quaternion.identity, crucibleParent);

            InventorySlotUI centerSlotUi = centerSlot.GetComponent<InventorySlotUI>();
            centerSlotUi.disallowItemStacking = true;
            centerSlotUi.onlyAcceptedItemType = eyeType;
            
            for (int i = 0; i < crucibleVeinSize; i++) {
                float deg = 360f / crucibleVeinSize * i;
                Vector2 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 150f;
                GameObject slot = Instantiate(inventorySlotPrefab, crucibleCenter + spawnDir, Quaternion.identity, crucibleParent);
                InventorySlotUI veinSlot = slot.GetComponent<InventorySlotUI>();
                
                if (i != 0 && i > hideoutStateData.crucibleLevel) {
                    veinSlot.MakeSlotInactive();
                }
                
                veinSlot.disallowItemStacking = true;
                veinSlot.onlyAcceptedItemType = soulcardType;
            }
        }
        crucibleInventory = CreateInventory(crucibleParent, crucibleInventorySize);
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
        bool movingItem = UpdateInventoryDragAndDrop(invHoverInfo);

        if (!movingItem) {
            UpdateItemDescPopup(invHoverInfo);
            CheckToMoveItem(invHoverInfo);
            CheckToConsumeItem(invHoverInfo);
        } 
        
        UpdatePlayerPanelUI();
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
        
        InventorySlot hoveredSlot = info.hoveredInventory.slots[info.hoveredSlotIndex];
        TextMeshProUGUI nameText = itemDescPopup.nameText;
        TextMeshProUGUI descText = itemDescPopup.descText;

        nameText.text = hoveredSlot.item.ItemRef.displayName;
        
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
        Vector2 center = hoveredSlot.ui.rectTransform.WorldRect().center;
        Vector2 popupPos = center + new Vector2(40, 40);
        itemDescPopup.transform.position = popupPos;

        // Fit popup size to text elements
        itemDescPopup.nameContentFitter.ForceRecalculate();
        itemDescPopup.descContentFitter.ForceRecalculate();
        FitPopupSize(itemDescPopup.rectTransform, itemDescPopup.nameText, itemDescPopup.descText);
        
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
                FitPopupSize(mechanicDescPopup.rectTransform, mechanicDescPopup.nameText, mechanicDescPopup.descText);
            } 
        }
    }

    private void FitPopupSize(RectTransform popupRect, params TextMeshProUGUI[] texts) {
        float height = 0f;
        foreach (TextMeshProUGUI text in texts) {
            height += text.rectTransform.rect.height;
        }
        
        const int minHeight = 80;
        Rect rect = popupRect.rect;
        rect.height = Mathf.Clamp(height, minHeight, Mathf.Infinity);
        popupRect.sizeDelta = new(rect.width, rect.height);
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
    
    private void CheckToMoveItem(InventoryHoverInfo invHoverInfo) {
        if (!moveStackInputAction.WasPressedThisFrame() && !splitStackInputAction.WasPressedThisFrame()) return;

        Inventory hoveredInventory = invHoverInfo.hoveredInventory;
        if (hoveredInventory == null) return;

        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem hoveredItem)) return;

        bool clickedOnEquipedBackpack = hoveredInventory == playerInventory && hoveredItem.ItemRef.type == backpackType;
        if (clickedOnEquipedBackpack && EquipedBackpackHasItems()) return;
        
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
            if (transactionState == TransactionInvetoryState.Buying) {
                if (hoveredInventory == traderInventory) {
                    destinationInventory = transactionInventory;
                }
                else if (hoveredInventory == transactionInventory) {
                    destinationInventory = traderInventory;
                }
            }
            else if (transactionState == TransactionInvetoryState.Selling) {
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
                    transactionState = TransactionInvetoryState.Buying;
                }
                else if (hoveredInventory == stashInventory) {
                    destinationInventory = transactionInventory;
                    transactionState = TransactionInvetoryState.Selling;
                }
            }
        }
        
        if (destinationInventory == null) return;

        MoveItemBetweenInventories(hoveredInventory, destinationInventory, invHoverInfo.hoveredSlotIndex);
        RefreshInventoryDisplay(hoveredInventory);
        RefreshInventoryDisplay(destinationInventory);

        if (OnTradingTab) {
            if (GetInventoryItemCount(transactionInventory) <= 0) {
                transactionState = TransactionInvetoryState.Empty;
            }
            RefreshTransactionUI();
        }
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
        
        ReduceItemCountInInventory(invHoverInfo.hoveredInventory, invHoverInfo.hoveredSlotIndex);
        RefreshInventoryDisplay(invHoverInfo.hoveredInventory);
    }

    private void UpdatePlayerPanelUI() {
        playerPanelHealthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/100";

        int inventoryWeight = GetInventoryWeight(playerInventory);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanelWeightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
    }

    private bool TryGetItemFromHoverInfo(InventoryHoverInfo invHoverInfo, out InventoryItem hoveredItem) {
        hoveredItem = null;
        
        int hoveredSlot = invHoverInfo.hoveredSlotIndex;
        Inventory hoveredInventory = invHoverInfo.hoveredInventory;
        
        if (hoveredInventory == null) return false;
        if (!hoveredInventory.slots.IndexInRange(hoveredSlot)) return false;
        if (hoveredInventory.slots[hoveredSlot].item == null) return false;
        if (hoveredInventory.slots[hoveredSlot].item.notDiscovered) return false;
        
        hoveredItem = hoveredInventory.slots[hoveredSlot].item;
        return true;
    } 
    
    private void SpawnUiSlots(RectTransform parent, int numSlots) {
        for (int i = 0; i < numSlots; i++) {
            Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity, parent);
        }
    }
    
    private Inventory CreateInventory(RectTransform uiParent, int slotCount) {
        Inventory inventory = new() {
            parent = uiParent,
            slots = new InventorySlot[slotCount]
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

    private bool EquipedBackpackHasItems() {
        int startingIndex = DefaultPlayerInventorySize;
        for (int i = startingIndex; i < playerInventory.slots.Length; i++) {
            if (playerInventory.slots[i].item != null) {
                return true;
            }
        }
        return false;
    }

    
    private InventoryItem prevEquippedEyeItem;
    private InventoryItem prevEquippedBackpackItem;
    
    private void CheckForEquipmentChange() {
        InventoryItem curEyeItem = playerInventory.slots[0].item;
        InventoryItem curBackpackItem = playerInventory.slots[2].item;

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
                ChangeInventorySize(playerInventory, DefaultPlayerInventorySize + backpackSize);
            }
            else {
                ChangeInventorySize(playerInventory, DefaultPlayerInventorySize);
            }
            RefreshInventoryDisplay(playerInventory);
        }

    }

    private void AddItemsToTraderInventory(int traderLevel) {
        foreach (Item item in traderConfig.persistentItems) {
            TryAddItemToInventory(traderInventory, item, 99);
        }
        
        ItemPool itemPool = traderConfig.itemPool;
        for (int i = 0; i < 10; i++) {
            Item traderItem = itemPool.GetItemFromPool();
            TryAddItemToInventory(traderInventory, traderItem, traderItem.MaxStackCount);
        }
        
        RefreshInventoryDisplay(traderInventory);
    }

    public struct InventoryHoverInfo {
        public Inventory hoveredInventory;
        public int hoveredSlotIndex;
        public float timeSpentHovering;
    }

    private InventoryHoverInfo lastHoverInfo;
    
    private InventoryHoverInfo UpdateInventoryHover() {
        InventoryHoverInfo info = new();
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        foreach (Inventory inventory in allInventories) {
            if (!inventory.parent.gameObject.activeInHierarchy) continue;
            
            Vector2 localMousePos = inventory.parent.InverseTransformPoint(mousePos);
            Bounds localUiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(inventory.parent);
            if (!localUiBounds.Contains(localMousePos)) continue;
            
            info.hoveredInventory = inventory;
            info.hoveredSlotIndex = GetHoveredInventorySlot(inventory);
            
            if (info.hoveredInventory == lastHoverInfo.hoveredInventory && info.hoveredSlotIndex == lastHoverInfo.hoveredSlotIndex) {
                info.timeSpentHovering = lastHoverInfo.timeSpentHovering + Time.deltaTime;
            }
            else {
                info.timeSpentHovering = 0f;
            }
            
            break;
        }

        lastHoverInfo = info;
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


    private InventoryItem dragItem;

    private bool UpdateInventoryDragAndDrop(InventoryHoverInfo hoverInfo) {
        bool movingItem = dragItem != null;
        
        if (!selectItemInputAction.WasPressedThisFrame()) {
            return movingItem;
        }

        bool pickingUpItem = dragItem == null;
        if (pickingUpItem) {
            if (!TryGetItemFromHoverInfo(hoverInfo, out InventoryItem item)) {
                return movingItem;
            }
            
            dragItem = item;
            dragAndDropItemUI.gameObject.SetActive(true);
            dragAndDropItemUI.SetItem(dragItem.ItemRef, dragItem.count);
            
            RemoveItemFromInventory(hoverInfo.hoveredInventory, hoverInfo.hoveredSlotIndex);
        }

        if (!pickingUpItem) {
            if (hoverInfo.hoveredInventory != null) {
                TryAddItemToInventory(hoverInfo.hoveredInventory, dragItem);
                RefreshInventoryDisplay(hoverInfo.hoveredInventory);
                dragItem = null;
                dragAndDropItemUI.ClearItem();
                dragAndDropItemUI.gameObject.SetActive(false);
            }
        }

        return movingItem;
    }

    private void UpdateDragAndDropItemToCursor() {
        if (dragItem == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        dragAndDropItemUI.GetComponent<RectTransform>().position = mousePos;
    }

    public struct InventoryAddResult {
        public enum ResultType { Success, Failure, FailureToAddAll };
        public ResultType type;
        public int addedCount;
    }
    
    public InventoryAddResult TryAddItemToInventory(Inventory inventory, Item item, int count) {
        InventoryItem newInventoryItem = new(item, count);
        return TryAddItemToInventory(inventory, newInventoryItem);
    }

    public InventoryAddResult TryAddItemToInventory(Inventory inventory, InventoryItem item, int slotIndex = -1) {
        InventoryAddResult result = new() { type = InventoryAddResult.ResultType.Failure };

        bool allowInfiniteStacking = inventory == traderInventory;
        
        if (slotIndex != -1) {
                
        }

        int remainingItemCount = item.count;

        // If we can stack the item then we just do that
        foreach (InventorySlot slot in inventory.slots) {
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
                continue;
            }
            
            slot.item.count += remainingItemCount;
            result.addedCount += remainingItemCount;
            result.type = InventoryAddResult.ResultType.Success;
            return result;
        }

        // Otherwise add to empty inventory slot
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item != null || slot.ui.SlotIsInactive) continue;

            if (!slot.ui.AcceptsItem(item.ItemRef)) continue;

            int newItemCount = slot.ui.disallowItemStacking ? 1 : Mathf.Clamp(remainingItemCount, 0, item.ItemRef.MaxStackCount);
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

    private void MoveItemBetweenInventories(Inventory fromInventory, Inventory toInventory, int slotIndex) {
        InventoryItem inventoryItem = GetInventoryItem(fromInventory, slotIndex);
        if (inventoryItem == null || inventoryItem.notDiscovered) return;

        if (OnTradingTab) {
            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = 1;
            
            InventoryAddResult traderMoveResult = TryAddItemToInventory(toInventory, newItem);
            if (traderMoveResult.type is InventoryAddResult.ResultType.Success or InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - traderMoveResult.addedCount;
                AdjustItemCountInInventory(fromInventory, slotIndex, keepItemCount);
            }
            return;
        }

        if (splitStackInputAction.WasPressedThisFrame() && inventoryItem.count > 1) {
            int firstHalf = inventoryItem.count / 2;
            int secondHalf = inventoryItem.count - firstHalf;

            InventoryItem newItem = inventoryItem.Clone();
            newItem.count = secondHalf;
            
            InventoryAddResult splitResult = TryAddItemToInventory(toInventory, newItem);
            if (splitResult.type == InventoryAddResult.ResultType.Success) {
                AdjustItemCountInInventory(fromInventory, slotIndex, firstHalf);
            }
            else if (splitResult.type == InventoryAddResult.ResultType.FailureToAddAll) {
                int keepItemCount = inventoryItem.count - splitResult.addedCount;
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

    private void ClearInventory(Inventory inventory) {
        for (int i = 0; i < inventory.slots.Length; i++) {
            RemoveItemFromInventory(inventory, i);
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

    private int GetInventoryItemCount(Inventory inventory) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            count++;
        }
        return count;
    }

    private int GetItemCountInInventory(Inventory inventory, Item item) {
        int count = 0;
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null) continue;
            if (slot.item.ItemRef.uuid == item.uuid) {
                count += slot.item.count;
            }
        }
        return count;
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
        RefreshInventoryDisplay(playerInventory);
    }

    private void ClosePlayerInventory() {
        playerPanel.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
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
    
    private void SetStashValue(int value) {
        stashValue = value;
        stashValueText.text = stashValue.ToString();
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
    }

    private Player player;
    
    private int PlayerRunSideHash = Animator.StringToHash("PlayerRunSide");
    private int PlayerRunUpHash = Animator.StringToHash("PlayerRunUp");
    private int PlayerRunDownHash = Animator.StringToHash("PlayerRunDown");
    private int PlayerIdleSide = Animator.StringToHash("PlayerIdleSide");
    private int PlayerIdleUp = Animator.StringToHash("PlayerIdleUp");
    private int PlayerIdleDown = Animator.StringToHash("PlayerIdleDown");
    
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
        
        if (moveInput.x != 0) {
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
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        crosshairTrans.position = mousePos;

        if (attackInputAction.IsPressed() && CanShoot()) {
            PlayAudioClip(shootClip, player.position, 1f);
            ShootProjectile();
        }
    }

    private void HealPlayer(int healing) {
        player.health = Mathf.Clamp(player.health + healing, 0, 100);
    }

    private void DamagePlayer(int damage, float chanceToBleed = 0f) {
        return;
        if (RollProbability(chanceToBleed)) {
            player.bleeding = true;
        }
        player.health -= damage;
        AddFlashHitEffect(player);
    }
    
    private const float defaultPlayerSpeed = 0.55f;
    private const float maxPlayerSpeed = 0.85f;

    private const int encumberingIncreasePerStrengthPoint = 50;
    private const int defaultStartingEncumberingWeight = 180;
    private const int maxEncumberedWeight = 280;
    private const float maxEncumberedSpeedReduction = 0.3f;
    
    private float GetPlayerSpeedBasedOnStats() {
        int agilityStat = baseStats.agility;
        for (int i = 0; i < playerEquipmentSize; i++) {
            InventoryItem item = playerInventory.slots[i].item;
            if (item == null) continue;
            if (item.ItemRef.modifiesStats && item.ItemRef.agilityStatAdjustment != 0) {
                agilityStat += item.ItemRef.agilityStatAdjustment;
            }
        }
        float playerSpeed = Mathf.Lerp(defaultPlayerSpeed, maxPlayerSpeed, (float)agilityStat / BaseCharacterStats.maxStatValue);
        
        float speedReductionFromWeight = Mathf.Lerp(0f, maxEncumberedSpeedReduction, GetOverweightCompletion());
        speedReductionFromWeight = Mathf.Clamp(speedReductionFromWeight, 0f, maxEncumberedSpeedReduction);

        playerSpeed -= speedReductionFromWeight;
        return playerSpeed;
    }

    private int GetStrengthStat() {
        int strengthStat = baseStats.strength;
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
        int overWeightAmount = Mathf.Clamp(inventoryWeight - startingEncumberingWeight, 0, int.MaxValue);
        float overWeightComp = overWeightAmount / (float)endingEncumberingWeight;
        return Mathf.Clamp01(overWeightComp);
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
            attackDelay -= firerate.reduction;
            attackDelay = Mathf.Clamp(attackDelay, equipedEye.coreAttack.cappedMinAttackDelay, equipedEye.coreAttack.attackDelay);
        }
        return attackLimiter.TimeHasPassed(attackDelay);
    }

    private void ShootProjectile() {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);

        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - equipedEye.coreAttack.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = equipedEye.coreAttack.projectileSpeed;
        if (equipedEye.stoppingPower.TryGetValue(out var stoppingPower)) {
            projectileSpeed *= 1f - stoppingPower.percentSpeedReduction;
        }
        
        Vector2 dir = (mouseWorldPos - player.trans.PositionV2()).normalized;
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
                    TryAddItemToInventory(playerInventory, col.GetComponent<ItemReference>().item, 1); 
                    DestroyEntity(col.gameObject);
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
                gameStateMachine.SetStateIfNotCurrent(hideoutState);
            }
        });
    }

    private void EnableInteractionPrompt(Vector3 position) {
        interactPrompt.SetActive(true);
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position + new Vector3(0f, 0.1f, 0f));
    }
    
    // *******************************
    // Projectiles
    // *******************************

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
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.curTimeAlive += Time.deltaTime;
            proj.trans.position += proj.velocity.ToVector3() * Time.deltaTime;
            proj.distTraveled += proj.velocity.magnitude * Time.deltaTime;
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, 0.1f, Masks.DamagableMask);
            if (!col) continue;
            
            Entity entity = entityLookup[col.gameObject];
                    
            if (proj.ignoreEntities == null || !proj.ignoreEntities.Contains(entity)) {
                HandleDamage(proj, entity);
            }

            if (entity is Enemy && ProjectileShouldPassThrough(proj, entity)) continue;
            
            DestroyEntity(projectiles[i]);
            projectiles.RemoveAt(i);
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].curTimeAlive > projectiles[i].lifeTimeDuration) {
                DestroyEntity(projectiles[i]);
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

    private void ClearProjectiles() {
        foreach (Projectile projectile in projectiles) {
            DestroyEntity(projectile);
        }
        projectiles.Clear();
    }
    
    // ***********************************
    // Damage Handling 
    // ***********************************
    
    private void DamageEnemy(Entity enemy, int damage, bool isCriticalStrike) {
        enemy.health -= damage;
        AddFlashHitEffect(enemy);

        Vector2 startDamageNumPos = OffsetY(enemy.position, 0.28f);
        Vector2 endDamageNumPos = OffsetY(enemy.position, 0.36f);
        Entity damageNumber = SpawnEntity<Entity>(damageNumberPrefab, startDamageNumPos, Quaternion.identity, damageNumbersParent);
        damageNumber.textMesh.text = damage.ToString();
        if (isCriticalStrike) {
            damageNumber.textMesh.color = criticalStrikeColor;
        }
        AddTweenPosition(damageNumber, endDamageNumPos, 0.3f, TweenCurve.EaseOut); 
        DestroyEntity(damageNumber, 0.3f);
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
            
            enemy.defaultSlow = new() { activationTime = Time.time, duration = 0.1f, speedReductionPercent = eyeInstance.coreAttack.enemySpeedReductionPercent };
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
                
                for (int i = 0; i < dropCount; i++) {
                    float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                    Vector3 endPos = entity.position + RotationVector(0.18f, 0.25f, randomAngle);
                    Entity rockDrop = SpawnEntity<Entity>(rockDropPool.GetDropFromPool(), entity.position, Quaternion.identity);
                    AddBounceEffect(rockDrop, endPos, 0.8f);
                }
            }
            else {
                AddFlashHitEffect(entity);
                AddSpringShakeEffect(entity, projectile.velocity);
                AddScaleEffect(entity, 0.88f, 0.15f);
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
    
    // ***************************
    // Spawning Map Items
    // ***************************

    private void InitExitPortal() {
        exitPortalTimer.SetTime(Random.Range(1f, 2f));
        // exitPortalTimer.SetTime(Random.Range(35f, 45f));
        
        exitPortalTimer.UpdateAction ??= () => {
            int totalSeconds = (int)exitPortalTimer.CurTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string formattedTime = $"{minutes}:{seconds:D2}";
            exitPortalStatusText.text = $"Exit Portal Countdown: {formattedTime}";
        };
        
        exitPortalTimer.EndAction ??= () => {
            int randomSpawnIndex = Random.Range(0, exitPortalSpawnParent.childCount);
            Transform exitPortalParent = exitPortalSpawnParent.GetChild(randomSpawnIndex);
            SpawnEntity<Entity>(exitPortalPrefab, exitPortalParent.position, Quaternion.identity, exitPortalParent);
            exitPortalStatusText.text = $"Exit Portal: { exitPortalParent.name }";
        };
    }
    
    
    private class Altar : Entity {
        public float soulCompletion;
    }
   
    private List<Altar> activeAltars = new();
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
            List<Item> deadBodyItems = ListPool<Item>.Get();
            foreach (var itemPair in itemLookup) {
                if (Random.value <= itemPair.Value.chanceToSpawn) {
                    deadBodyItems.Add(itemPair.Value);
                }
            }
            
            int maxDeadBodyItemCount = Random.Range(2, 6);
            deadBodyItems = deadBodyItems.OrderBy(x => Random.value).Take(maxDeadBodyItemCount).ToList();
            
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
                    ui = lootInventorySlotUis[j]
                };
            }
            
            ListPool<Item>.Release(deadBodyItems);

            Entity body = SpawnResource<Entity>(deadBodyPrefab, false);
            deadBodySlotsLookup.Add(body.gameObject, deadBodySlots);
        }
        
        int altarsToSpawn = Random.Range(1, 2);
        for (int i = 0; i < altarsToSpawn; i++) {
            Altar altarEntity = SpawnResource<Altar>(altarPrefab, true);
            activeAltars.Add(altarEntity);
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
        activeAltars.Clear();
        enemies.Clear();
    }

    // ***************************
    // Saving and Loading
    // ***************************

    [Serializable]
    private class SaveData {
        
    }
    
    [Serializable]
    private class RaidStateData {
        public int raidDifficulty;
        public List<string> mapSceneNames;
        [NonSerialized] public Map currentMap;
    }
    
    [Serializable]
    private class HideoutStateData {
        public int crucibleLevel;
        public int stashLevel;
        public int traderLevel;
        public int curTraderXpForLevel;
    }
    
    private string inventorySavePath;
    private string stashSavePath;
    private string crucibleSavePath;
    private string hideoutDataSavePath;
    private string raidDataSavePath;
    private string playerSavePath;
    private List<InventoryItem> cachedInventoryForSaving = new(50);
    
    private void BuildSavePaths() {
        inventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
        crucibleSavePath = $"{Application.persistentDataPath}/crucible";
        hideoutDataSavePath = $"{Application.persistentDataPath}/hideoutData"; 
        raidDataSavePath = $"{Application.persistentDataPath}/raidStateData";
        playerSavePath = $"{Application.persistentDataPath}/player";
    }

    private string GetSavePath(Inventory inventory) {
        if (inventory == playerInventory)   return inventorySavePath;
        if (inventory == stashInventory)    return stashSavePath;
        if (inventory == crucibleInventory) return crucibleSavePath;
        return string.Empty;
    }
    
    private void SaveInventory(Inventory inventory) {
        cachedInventoryForSaving.Clear();
        foreach (InventorySlot slot in inventory.slots) {
            cachedInventoryForSaving.Add(slot.item); 
        }
        SaveToFile(GetSavePath(inventory), cachedInventoryForSaving);
    }

    private void LoadInventory(Inventory inventory) {
        List<InventoryItem> items = LoadFromFile<List<InventoryItem>>(GetSavePath(inventory));
        if (items == null) return;

        // Items can be null because we save all inventory slots, including empty ones
        foreach (InventoryItem item in items) {
            bool isDemonEye = item != null && item.modifierUuids != null;
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
    
    private void SaveToFile(string path, object obj) {
        BinaryFormatter bf = new();
        using FileStream file = File.Create(path);
        bf.Serialize(file, obj);
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
    }

    private void SavePlayerData() {
        PlayerSaveData data = new() {
            health = player.health,
        };
        SaveToFile(playerSavePath, data);
    }

    private void LoadAndAssignPlayerSaveData(Player instancedPlayer) {
        PlayerSaveData data = LoadFromFile<PlayerSaveData>(playerSavePath);
        if (data == null) return;
        instancedPlayer.health = data.health;
    }

    // ************************************
    // UI 
    // ************************************
    
    private void InitHideoutUI() {
        characterTabButton.image.sprite = tabSelectedSprite;
        eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
        traderTabButton.image.sprite = tabNonSelectedSprite;
        
        hideoutHeaderParent.gameObject.SetActive(true);
        hideoutTabsParent.gameObject.SetActive(true);
        playerPanel.gameObject.SetActive(true);
        stashPanel.gameObject.SetActive(true);
        eyeForgePanel.gameObject.SetActive(false);
        traderInventoryPanel.gameObject.SetActive(false);
        traderTransactionPanel.gameObject.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
    }

    private void CloseHideoutUI() {
        hideoutHeaderParent.gameObject.SetActive(false);
        hideoutTabsParent.gameObject.SetActive(false);
        playerPanel.gameObject.SetActive(false);
        stashPanel.gameObject.SetActive(false);
        eyeForgePanel.gameObject.SetActive(false);
        traderInventoryPanel.gameObject.SetActive(false);
        traderTransactionPanel.gameObject.SetActive(false);
        lootInventoryPanel.gameObject.SetActive(false);
    }

    private void ShowRaidUI(bool show) {
        if (!show) {
            interactPrompt.gameObject.SetActive(false);
        }
        playerBarsPanel.gameObject.SetActive(show);
        raidTimerText.gameObject.SetActive(show);
    }
    
    private void InitButtonCallbacks() {
        characterTabButton.onClick.AddListener(() => {
            characterTabButton.image.sprite = tabSelectedSprite;
            eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
            traderTabButton.image.sprite = tabNonSelectedSprite;
            
            ToggleSlimPlayerPanel(false);
            playerPanel.gameObject.SetActive(true);
            stashPanel.gameObject.SetActive(true);
            eyeForgePanel.gameObject.SetActive(false);
            traderInventoryPanel.gameObject.SetActive(false);
            traderTransactionPanel.gameObject.SetActive(false);
        });
        
        eyeForgeTabButton.onClick.AddListener(() => {
            characterTabButton.image.sprite = tabNonSelectedSprite;
            eyeForgeTabButton.image.sprite = tabSelectedSprite;
            traderTabButton.image.sprite = tabNonSelectedSprite;
            
            ToggleSlimPlayerPanel(true);
            playerPanel.gameObject.SetActive(true);
            stashPanel.gameObject.SetActive(true);
            eyeForgePanel.gameObject.SetActive(true);
            traderInventoryPanel.gameObject.SetActive(false);
            traderTransactionPanel.gameObject.SetActive(false);
        });
        
        traderTabButton.onClick.AddListener(() => {
            characterTabButton.image.sprite = tabNonSelectedSprite;
            eyeForgeTabButton.image.sprite = tabNonSelectedSprite;
            traderTabButton.image.sprite = tabSelectedSprite;
            
            playerPanel.gameObject.SetActive(false);
            stashPanel.gameObject.SetActive(true);
            eyeForgePanel.gameObject.SetActive(false);
            traderInventoryPanel.gameObject.SetActive(true);
            traderTransactionPanel.gameObject.SetActive(true);
        });
        
        crucibleForgeButton.onClick.AddListener(() => {
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
            RefreshInventoryDisplay(crucibleInventory);
        });
        
        crucibleUpgradeButton.onClick.AddListener(() => {
            UpgradePath.UpgradeRequirements requirements = crucibleUpgradePath.pathUpgrades[hideoutStateData.crucibleLevel];
            
            bool canUpgrade = true;
            foreach (UpgradePath.Requirement requirement in requirements.requirements) {
                int itemCount = 0;
                itemCount += GetItemCountInInventory(stashInventory, requirement.item);
                itemCount += GetItemCountInInventory(playerInventory, requirement.item);
                
                if (itemCount < requirement.count) {
                    canUpgrade = false;
                    break;
                }
            }

            if (!canUpgrade) return;

            foreach (UpgradePath.Requirement requirement in requirements.requirements) {
                int stashRemoveCount = RemoveNumberOfItemsFromInventory(stashInventory, requirement.item, requirement.count);
                if (stashRemoveCount == requirement.count) continue;
                RemoveNumberOfItemsFromInventory(playerInventory, requirement.item, requirement.count - stashRemoveCount);
            }
            
            hideoutStateData.crucibleLevel++;
            SaveToFile(hideoutDataSavePath, hideoutStateData);
            
            RefreshInventoryDisplay(playerInventory);
            RefreshInventoryDisplay(stashInventory);

            foreach (InventorySlot slot in crucibleInventory.slots) {
                if (slot.ui.SlotIsInactive) {
                    slot.ui.MakeSlotActive();
                    break;
                }
            }
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
            // RefreshInventoryDisplay(stashInventory);
        // });
        
        traderDealButton.onClick.AddListener(() => {
            InventoryValueType valueType = transactionState == TransactionInvetoryState.Buying ? InventoryValueType.Buy : InventoryValueType.Sell;
            int price = GetInventoryValue(transactionInventory, valueType);
            
            if (transactionState == TransactionInvetoryState.Buying && stashValue >= price) {
                SetStashValue(stashValue - price); 
                for (int i = 0; i < transactionInventory.slots.Length; i++) { 
                    MoveEntireItemStack(transactionInventory, stashInventory, i);
                }
                RefreshInventoryDisplay(transactionInventory);
                RefreshInventoryDisplay(stashInventory);
                transactionState = TransactionInvetoryState.Empty;
            }
            else if (transactionState == TransactionInvetoryState.Selling) {
                int xpGain = GetInventoryValue(transactionInventory, InventoryValueType.Xp);
                IncreaseTraderLevel(xpGain);
                SetStashValue(stashValue + price);
                ClearInventory(transactionInventory);
                RefreshInventoryDisplay(transactionInventory);
                transactionState = TransactionInvetoryState.Empty;
            }

            RefreshTransactionUI();
        });
        
        enterNextRaidButton.onClick.AddListener(() => {
            gameStateMachine.SetStateIfNotCurrent(raidState);
        });
    }

    private void UpdateInRaidUi() {
        healthBarFillImage.fillAmount = player.health / 100f;
        weightBarFillImage.fillAmount = GetTotalWeightCompletion();
        
        int minutesLeftInRaid = Mathf.FloorToInt(spawnManager.totalTimeLeft / 60f);
        int secondsLeftInRaid = Mathf.FloorToInt(spawnManager.totalTimeLeft % 60f);
        raidTimerText.text = $"{minutesLeftInRaid:0}:{secondsLeftInRaid:00}";
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

    private void RefreshTransactionUI() {
        if (transactionState == TransactionInvetoryState.Empty) {
            traderTransactionInfoText.text = string.Empty;
            return;
        }
        
        if (transactionState == TransactionInvetoryState.Buying) {
            int buyPrice = GetInventoryValue(transactionInventory, InventoryValueType.Buy);
            traderTransactionInfoText.text = $"Purchase for {buyPrice}";
        }
        else if (transactionState == TransactionInvetoryState.Selling) {
            int sellPrice = GetInventoryValue(transactionInventory, InventoryValueType.Sell);
            int xpGain = GetInventoryValue(transactionInventory, InventoryValueType.Xp);
            traderTransactionInfoText.text = $"Sell for {sellPrice}\n Gain {xpGain} trader experience";
        }
    }

    private void IncreaseTraderLevel(int xpGain) {
        int totalXp = traderLevels.totalXpToNextLevel[hideoutStateData.traderLevel];
        hideoutStateData.curTraderXpForLevel += xpGain;
        traderXpLevelFill.fillAmount = hideoutStateData.curTraderXpForLevel / (float)totalXp;
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

    [NonSerialized] public string loadedMapName;
    [NonSerialized] public string activelyLoadingMapName;
    [NonSerialized] public string activelyUnloadingMapName;

    public void LoadMapAsync(string sceneName) {
        if (LoadingMapInProgress()) return;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOperation == null) return;
        
        activelyLoadingMapName = sceneName;
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!loadOperation.isDone) {
                yield return null;
            }
            activelyLoadingMapName = string.Empty;
            loadedMapName = sceneName;

            List<GameObject> loadedMapRoots = ListPool<GameObject>.Get();
            
            Scene loadedMapScene = SceneManager.GetSceneByName(sceneName);
            loadedMapScene.GetRootGameObjects(loadedMapRoots);
            
            foreach (GameObject root in loadedMapRoots) {
                if (!root.TryGetComponent(out Map map)) continue;
                raidStateData.currentMap = map;
                map.gameObject.SetActive(false);
                break;
            }
            
            ListPool<GameObject>.Release(loadedMapRoots);
        }
    }

    public void UnloadCurrentMapAsync() {
        if (UnloadingMapInProgress()) return;
            
        raidStateData.currentMap.gameObject.SetActive(false); 
        
        Scene loadedMap = SceneManager.GetSceneByName(loadedMapName);
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedMap);

        if (unloadOperation == null) return;
        
        activelyUnloadingMapName = loadedMapName;
        loadedMapName = string.Empty;
        
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!unloadOperation.isDone) {
                yield return null;
            }
            activelyUnloadingMapName = string.Empty;
        }
    } 
    
    public bool LoadingMapInProgress() {
        return !string.IsNullOrEmpty(activelyLoadingMapName);
    }
    
    public bool UnloadingMapInProgress() {
        return !string.IsNullOrEmpty(activelyUnloadingMapName);
    }
    
    
    public static bool RollProbability(float probability) {
        return Random.value < probability;
    }

    private bool InRaid => gameStateMachine.CurState == raidState;
    
    private Vector3 RotationVector360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }
    
    private Vector3 RotationVector(float degrees, float minDist, float maxDist) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }
    
    private Vector2 OffsetY(Vector2 pos, float yOffset) {
        return new(pos.x, pos.y + yOffset);
    }
    
    private Vector2 OffsetX(Vector2 pos, float xOffset) {
        return new(pos.x, pos.y + xOffset);
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
    
}