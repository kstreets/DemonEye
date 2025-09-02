using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;
using Pathfinding;
using Random = UnityEngine.Random;
using VInspector;

public class Game : MonoBehaviour {

    public static Game instance;
    
    public List<ItemPool> traderLevelPools;

    [Foldout("Pooling Prefabs")]
    public GameObject baseProjectilePrefab;
    public GameObject stoppingPowerProjectilePrefab;
    public GameObject bloodDropPrefab;
    public GameObject poisonDebuffPrefab;
    public GameObject explosionPrefab;
    [EndFoldout]
    
    [Foldout("Gameplay Variables")]
    [Range(0f, 1f)] public float defaultCriticalStrikeChange;
    public float defaultCriticalStrikeMultiplier;
    [EndFoldout]

    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public RectTransform crosshairTrans;
    public Transform exitPortalSpawnParent;

    public Transform smallMapParent;

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

    [Header("Spawn Positions")]
    public Vector3 hellSpawnPosition;
    
    [Foldout("UI/Prefabs")]
    public GameObject inventorySlotPrefab;
    public GameObject rockSmokePrefab;
    public GameObject damageNumberPrefab;
    [EndFoldout]

    [Foldout("Effects")]
    public AnimationCurve hitFlashCurve;
    public AnimationCurve bounceCurve;
    [EndFoldout]
    
    [Foldout("UI/MiscRefs")]
    public GameObject itemDescPopup;
    public Button enterNextRaidButton;
    public RectTransform hideoutHeaderParent;
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
    public GameObject interactPrompt;
    public TextMeshProUGUI exitPortalStatusText;
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
    
    [Header("Controls")]
    public InputAction moveInputAction;
    public InputAction attackInputAction;
    public InputAction interactInputAction;
    public InputAction inventoryInputAction;
    public InputAction selectItemInputAction;
    public InputAction splitStackInputAction;
    
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
    private StateMachine gameStateMachine = new();

    [Serializable]
    private class HideoutStateData {
        public int crucibleLevel;
        public int stashLevel;
        public int traderLevel;
        public int curTraderXpForLevel;
    }
    
    private HideoutStateData hideoutStateData;
    
    private void Start() {
        instance = this;
        
        LoadAllItems();
        InitAudio();
        InitHideoutUI();
        BuildSavePaths();
        hideoutStateData = LoadFromFile<HideoutStateData>(hideoutDataSavePath) ?? new HideoutStateData();
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

        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEnter, OnRaidStateExit);
    }

    private void Update() {
        UpdateDelayedEntitiesToDestroy();
        gameStateMachine.Tick();
    }

    private void FixedUpdate() {
        FixedUpdateEnemies();
    }

    private void OnApplicationQuit() {
        SaveInventory(playerInventory);
        SaveInventory(stashInventory);
    }

    private void UpdateTimers() {
        exitPortalTimer.Tick();
        discoverLootTimer.Tick();
    }


    private void OnHideoutStateEnter() {
        Cursor.visible = true;
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
        playerBarsPanel.gameObject.SetActive(true);

        smallMapParent.gameObject.SetActive(true);
        Map map = smallMapParent.GetComponent<Map>();
        player = SpawnEntity<Entity>(playerPrefab, hellSpawnPosition, Quaternion.identity);
        cinemachineCamera.Follow = player.trans;
        
        AstarPath.active.Scan();
        InitExitPortal();
        InitWave(map.waves);
        SpawnResources(map.resourceParent);
    }

    private void OnRaidStateExit() {
        DestroyLevelEntities();
        ClearProjectiles();
        smallMapParent.gameObject.SetActive(false);
        playerBarsPanel.gameObject.SetActive(false);
    }

    private void OnRaidStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        UpdateWave();
        UpdateEnemies();
        UpdateEntityEffects();
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
            T entity = SpawnEntity<T>(gameObj, Vector3.zero, Quaternion.identity, transform);
            entity.entityPool = pool;
            entity.gameObject.SetActive(false);
            pool.standbyList.Add(entity);
        }
        
        return pool;
    }
    
    
    public class Entity {
        public Transform trans;
        public Collider2D collider;
        public Rigidbody2D rigidbody;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public TextMeshProUGUI textMesh;
        
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

        public bool IsValid => trans;
        public GameObject gameObject => trans.gameObject;
    }
    
    private T SpawnEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        return InitializeEntity<T>(obj, position, rotation, parent);
    }

    private T SpawnEntity<T>(EntityPool<T> pool, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        T entity;
        if (pool.standbyList.Count > 0) {
            entity = pool.standbyList.PopLast();
            entity.trans.SetPositionAndRotation(position, rotation);
            entity.trans.SetParent(parent);
            ResetEntity(entity);
        }
        else { 
            entity = SpawnEntity<T>(pool.prefab, position, rotation, parent);
            entity.entityPool = pool;
        }
        entity.gameObject.SetActive(true);
        pool.inUseList.Add(entity);
        pool.OnSpawnCallback?.Invoke(entity);
        return entity;
    }
    
    private T InitializeEntity<T>(GameObject objInstance, Vector3 position, Quaternion rotation, Transform parent) where T : Entity, new() {
        objInstance.transform.SetPositionAndRotation(position, rotation);
        objInstance.transform.SetParent(parent);
        T newEntity = new() {
            trans = objInstance.transform,
            collider = objInstance.TryGetComponent(out Collider2D col) ? col : null,
            rigidbody = objInstance.TryGetComponent(out Rigidbody2D rbody) ? rbody : null,
            spriteRenderer = objInstance.TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer : null,
            animator = objInstance.TryGetComponent(out Animator anim) ? anim : null,
            textMesh = objInstance.TryGetComponent(out TextMeshProUGUI text) ? text : null,
        };
        ResetEntity(newEntity);
        entities.Add(newEntity);
        entityLookup.Add(objInstance, newEntity);
        return newEntity;
    }

    private void ResetEntity<T>(T entity) where T : Entity {
        entity.health = 100;
        entity.animator?.Rebind();
        entity.animator?.Update(0);
    }
    
    private void DestroyEntity(GameObject gameObj) {
        DestroyEntity(entityLookup[gameObj]);
    }
    
    private void DestroyEntity(Entity entity) {
        RemoveHitFlashEffect(entity);
        RemovePoisonedEffect(entity);
        
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
        entity.gameObject.transform.SetParent(transform);
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
        const float stiffness = 1000f;
        const float damping = 15f;
        
        SpringShake shake = new() {
            stiffness = stiffness,
            damping = damping,
            targetPos = entity.trans.localPosition,
        };

        const float randomVelocityAngle = 15f;
        const float shakeMagnitude = 0.017f;
        shake.offset = (Quaternion.AngleAxis(Random.Range(-randomVelocityAngle, randomVelocityAngle), Vector3.forward) * velocity.normalized * shakeMagnitude).ToVector2();
        
        entity.springShake = shake;
    }

    private void UpdateShakeEffect(Entity entity) {
        if (!entity.springShake.TryGetValue(out var shake)) return;
        
        Vector2 displacement = shake.offset;
        Vector2 acceleration = -shake.stiffness * displacement - shake.damping * shake.velocity;
        shake.velocity += acceleration * Time.deltaTime;
        shake.offset += shake.velocity * Time.deltaTime;
        
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
        public PathData pathData = new();
        public Timer applyDamageTimer;
        public BleedModInstance? bleed;
        public PoisonSoulcard.InstanceData? poisoned;
        public SlowInstance? defaultSlow;
        public SlowInstance? slow;
    }
    
    public class PathData {
        public ABPath abPath;
        public int waypointIndex;
        public bool isBeingCalculated;
        public float lastUpdateTime;
        
        public bool HasPath => abPath != null;
    }
    
    private void UpdateEnemies() {
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            enemy.applyDamageTimer.Tick();

            float distFromPlayer = Vector2.Distance(player.position, enemy.position);

            if (!enemy.poisoned.HasValue && distFromPlayer < 0.35f && !enemy.animator.Playing("Attack")) {
                enemy.animator.Play("Attack");
                enemy.applyDamageTimer.SetTime(0.31f);
                enemy.applyDamageTimer.EndAction = () => {
                    Vector3 dirToPlayer = (player.position - enemy.position).normalized;
                    Vector2 attackCheckPos = enemy.position + dirToPlayer * 0.15f;
                    Collider2D col = Physics2D.OverlapCircle(attackCheckPos, 0.15f, Masks.PlayerMask);
                    if (col != null) {
                        DamagePlayer(enemy.data.damage);
                    }
                };
            }
            
            if (enemy.bleed.TryGetValue(out var bleed)) {
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    enemy.health -= bleed.bleedDamage;
                    bleed.lastBleedTime = Time.time;
                    enemy.bleed = bleed;
                    Entity bloodDrop = SpawnEntity(bloodDropPool, enemy.position, Quaternion.identity);
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

        foreach (Enemy enemy in enemies) {
            if ((enemy.pathData.HasPath && Time.time - enemy.pathData.lastUpdateTime <= 0.5f) || enemy.pathData.isBeingCalculated) continue;

            float dist = Vector2.Distance(enemy.position, player.position);
            float time = dist / enemy.data.speed;
            
            Vector2 estimatedPlayerPos = player.position + playerVelocity.ToVector3() * time;
            Vector2 conservativeEstimatedPlayerPos = Vector2.Lerp(player.position, estimatedPlayerPos, 0.5f);
            ABPath abPath = ABPath.Construct(enemy.position, conservativeEstimatedPlayerPos, path => {
                path.Claim(this);
                enemy.pathData.abPath?.Release(this);
                enemy.pathData.abPath = path as ABPath;
                enemy.pathData.waypointIndex = 1;
                enemy.pathData.isBeingCalculated = false;
                enemy.pathData.lastUpdateTime = Time.time;
            });
            
            AstarPath.StartPath(abPath);
            enemy.pathData.isBeingCalculated = true;
        }
    }

    private void FixedUpdateEnemies() {
        foreach (Enemy enemy in enemies) {
            if (enemy.pathData.abPath == null) continue;
            
            PathData pathData = enemy.pathData;
            
            bool usingPath = enemy.pathData.abPath.vectorPath.Count >= 2 && pathData.waypointIndex < pathData.abPath.vectorPath.Count;
            
            if (usingPath && Vector2.Distance(enemy.position, pathData.abPath.vectorPath[pathData.waypointIndex].ToVector2()) < 0.5f) {
                pathData.waypointIndex++;
            }
            
            usingPath = usingPath && pathData.waypointIndex < pathData.abPath.vectorPath.Count;

            
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
            
            /*
                The below separation method causes jitter in big pools of enemies because center enemies are bouncing back and forth
                Todo: Make the separation logic start from the center of a crowd and work its way out to prevent this jitter
            */

            const float targetSeparationDist = 0.15f;
            Vector2 separation = Vector2.zero;
            foreach (Enemy avoidEnemy in enemies) {
                if (avoidEnemy == enemy) continue;
                
                Vector2 diff = enemy.position - avoidEnemy.position;
                float dist = diff.magnitude;

                if (dist < targetSeparationDist)
                    separation += diff.normalized / dist; // Stronger repulsion if closer
            }

            Vector2 targetPos = usingPath ? pathData.abPath.vectorPath[pathData.waypointIndex] : player.position;
            Vector2 dirToTarget = (targetPos - enemy.position.ToVector2()).normalized;
            Vector2 finalDirection = (dirToTarget + separation.normalized * 0.5f).normalized;
            enemy.rigidbody.linearVelocity = finalDirection * speed;

            enemy.spriteRenderer.flipX = player.position.x < enemy.position.x;
        }
    }
    
    
    public class EnemyWaveManager {
        public float timeInCurWave;
        public int curWaveIndex;
        public EnemyWaves waves;
        public EnemyWaves.WaveData CurWaveData;
        
        public const int prefixedSumResolution = 500;
        public float[] prefixedSums = new float[prefixedSumResolution];

        public List<(float time, EnemyData enemy)> spawnEvents = new();
        public int spawnTimeIndex;
    }

    [NonSerialized] private EnemyWaveManager waveManager = new();
    
    private void InitWave(EnemyWaves waves) {
        waveManager.waves = waves;
        waveManager.curWaveIndex = -1;
    }
    
    private void UpdateWave() {
        EnemyWaveManager wm = waveManager;
        if (wm.curWaveIndex >= wm.waves.waves.Count) return;
        
        wm.timeInCurWave += Time.deltaTime;
        float waveDuration = wm.curWaveIndex == -1 ? wm.waves.timeBeforeFirstWave : wm.CurWaveData.waveDuration;

        bool startNextWave = wm.timeInCurWave >= waveDuration;
        if (startNextWave) {
            wm.curWaveIndex++;
            if (!wm.waves.waves.IndexInRange(wm.curWaveIndex)) return;

            EnemyWaves.WaveData newUnitWave = wm.waves.waves[wm.curWaveIndex];
            wm.CurWaveData = newUnitWave;

            foreach (EnemyWaves.UnitWave waveUnit in newUnitWave.waveUnits) {
                if (waveUnit.enemyCount >= EnemyWaveManager.prefixedSumResolution) {
                    Debug.LogError($"Wave cannot have more enemies than {nameof(EnemyWaveManager.prefixedSumResolution)}");
                }
            }
            
            wm.timeInCurWave = 0f;
            wm.spawnTimeIndex = 0;

            float totalWeight = 0f;
            for (int i = 0; i < EnemyWaveManager.prefixedSumResolution; i++) {
                float sliceIndex = i / (float)(EnemyWaveManager.prefixedSumResolution - 1);
                float weight = Mathf.Clamp01(newUnitWave.spawnRateCurve.Evaluate(sliceIndex));
                totalWeight += weight;
                wm.prefixedSums[i] = totalWeight;
            }

            // Build spawntimes for this next wave
            {
                wm.spawnEvents.Clear();
                
                foreach (EnemyWaves.UnitWave waveUnit in newUnitWave.waveUnits) {
                    int enemySpawnCount = waveUnit.enemyCount;
                    for (int i = 0; i < enemySpawnCount; i++) {
                        float targetWeight = (i / (float)(enemySpawnCount - 1)) * totalWeight;

                        // Find the corresponding time using linear search
                        int weightIndex = 0;
                        while (weightIndex < EnemyWaveManager.prefixedSumResolution && wm.prefixedSums[weightIndex] < targetWeight) {
                            weightIndex++;
                        }

                        float normalizedTime = weightIndex / (float)(EnemyWaveManager.prefixedSumResolution - 1);
                        wm.spawnEvents.Add((normalizedTime * newUnitWave.spawnDuration, waveUnit.enemyData));
                    }
                }
                
                // Due to the way we add elements we need to sort by time so its chronologically ordered 
                wm.spawnEvents.Sort((x, y) => x.time.CompareTo(y.time));
            }
        }

        if (wm.spawnEvents.Count <= 0) return;
        
        while (wm.spawnEvents.IndexInRange(wm.spawnTimeIndex) && wm.spawnEvents[wm.spawnTimeIndex].time <= wm.timeInCurWave) {
            Vector2 randomSpawnPos = player.position + RandomOffset360(3f, 4f);
            NNInfo info = AstarPath.active.graphs[0].GetNearest(randomSpawnPos, NNConstraint.Walkable);

            EnemyData enemyToSpawn = wm.spawnEvents[wm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, info.position, Quaternion.identity);
            enemy.health = enemyToSpawn.health;
            enemy.data = enemyToSpawn;
            enemies.Add(enemy);
            
            wm.spawnTimeIndex++;
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
    
    private const int playerPocketSize = 6;
    private const int playerEquipmentSize = 3;
    private int DefaultPlayerInventorySize => playerPocketSize + playerEquipmentSize;

    private const int stashUpgradeSlotIncrease = 4;
    
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

        int stashInventorySize = 12 + hideoutStateData.stashLevel * stashUpgradeSlotIncrease;
        SpawnUiSlots(stashInventoryParent, 40);
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
            centerSlotUi.acceptsAllTypes = false;
            centerSlotUi.onlyAcceptedItemType = Item.ItemType.Eye;
            
            for (int i = 0; i < crucibleVeinSize; i++) {
                float deg = 360f / crucibleVeinSize * i;
                Vector2 spawnDir = (Quaternion.AngleAxis(deg, Vector3.forward) * Vector2.up) * 150f;
                GameObject slot = Instantiate(inventorySlotPrefab, crucibleCenter + spawnDir, Quaternion.identity, crucibleParent);
                InventorySlotUI veinSlot = slot.GetComponent<InventorySlotUI>();
                
                if (i != 0 && i > hideoutStateData.crucibleLevel) {
                    veinSlot.MakeSlotInactive();
                }
                
                veinSlot.disallowItemStacking = true;
                veinSlot.acceptsAllTypes = false;
                veinSlot.onlyAcceptedItemType = Item.ItemType.Soulcard;
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
                HideItemTooltip();
                return;
            }
        }

        InventoryHoverInfo invHoverInfo = UpdateInventoryHover();
        UpdateItemtooltip(invHoverInfo);
        HandleItemClicked(invHoverInfo);
        CheckForEquipmentChange();
    }
    
    private void UpdateItemtooltip(InventoryHoverInfo invHoverInfo) {
        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem _)) {
            HideItemTooltip();
            return;
        }
         
        const float hoverTimeUntilTooltip = 0.32f;
        bool spentEnoughTimeHovering = invHoverInfo.timeSpentHovering >= hoverTimeUntilTooltip;
        if (spentEnoughTimeHovering) {
            ShowItemTooltip(invHoverInfo);
        }
        else {
            HideItemTooltip();
        }
    }

    private void HandleItemClicked(InventoryHoverInfo invHoverInfo) {
        if (!selectItemInputAction.WasPressedThisFrame() && !splitStackInputAction.WasPressedThisFrame()) return;

        Inventory hoveredInventory = invHoverInfo.hoveredInventory;
        if (hoveredInventory == null) return;

        if (!TryGetItemFromHoverInfo(invHoverInfo, out InventoryItem hoveredItem)) return;

        bool clickedOnEquipedBackpack = hoveredInventory == playerInventory && hoveredItem.ItemRef.type == Item.ItemType.Backpack;
        if (clickedOnEquipedBackpack && EquipedBackpackHasItems()) {
            return;
        }
        
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
                bool hoveredItemIsDemonEye = hoveredItem.ItemRef.type == Item.ItemType.DemonEye;
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
                ChangeInventorySize(playerInventory, DefaultPlayerInventorySize + 9);
            }
            else {
                ChangeInventorySize(playerInventory, DefaultPlayerInventorySize);
            }
            RefreshInventoryDisplay(playerInventory);
        }

    }

    private void AddItemsToTraderInventory(int traderLevel) {
        ItemPool itemPool = traderLevelPools[traderLevel];
        for (int i = 0; i < 5; i++) {
            Item traderItem = itemPool.GetItemFromPool();
            TryAddItemToInventory(traderInventory, traderItem, traderItem.MaxStackCount);
            RefreshInventoryDisplay(traderInventory);
        }
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
            RectTransform rectTrans = inventory.slots[i].ui.GetComponent<RectTransform>();
            bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
            if (mouseInRect) {
                return i;
            }
        }
        return -1;
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

    public InventoryAddResult TryAddItemToInventory(Inventory inventory, InventoryItem item) {
        InventoryAddResult result = new() {
            type = InventoryAddResult.ResultType.Failure
        };

        int count = item.count;

        // If we can stack the item then we just do that
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item == null || slot.ui.disallowItemStacking || slot.item.IsFullStack || slot.item.itemDataUuid != item.itemDataUuid) continue;

            int overflowAmount = (count + slot.item.count) - slot.item.ItemRef.MaxStackCount;
            if (overflowAmount > 0) {
                int addCount = slot.item.ItemRef.MaxStackCount - slot.item.count;
                
                slot.item.count += addCount;
                count = overflowAmount;
                
                result.addedCount += addCount;
                result.type = InventoryAddResult.ResultType.FailureToAddAll;
                continue;
            }
            
            slot.item.count += count;
            result.addedCount += count;
            result.type = InventoryAddResult.ResultType.Success;
            return result;
        }

        // Otherwise add to empty inventory slot
        foreach (InventorySlot slot in inventory.slots) {
            if (slot.item != null || slot.ui.SlotIsInactive) continue;
            
            bool slotCanAcceptItemType = slot.ui.acceptsAllTypes || slot.ui.onlyAcceptedItemType == item.ItemRef.type;
            if (!slotCanAcceptItemType) continue;

            int addCount = slot.ui.disallowItemStacking ? 1 : Mathf.Clamp(count, 0, item.ItemRef.MaxStackCount);
            bool canMoveCleanly = addCount == count;
            
            if (canMoveCleanly) {
                slot.item = item;
                result.type = InventoryAddResult.ResultType.Success;
                result.addedCount = count;
                return result;
            }

            InventoryItem newItem = item.Clone();
            newItem.count = addCount;
            slot.item = newItem;
            
            result.type = InventoryAddResult.ResultType.FailureToAddAll;
            result.addedCount = addCount;
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

    private void ShowItemTooltip(InventoryHoverInfo info) {
        InventorySlot hoveredSlot = info.hoveredInventory.slots[info.hoveredSlotIndex];
        TextMeshProUGUI tooltipText = itemDescPopup.GetComponentInChildren<TextMeshProUGUI>();
        
        if (tooltipText.text != string.Empty) {
            itemDescPopup.SetActive(true);
        }
        
        if (hoveredSlot.item.ItemRef.type == Item.ItemType.DemonEye) {
            DemonEyeInstance eyeInstance = eyeInstanceFromItemId[hoveredSlot.item.itemDataUuid];
            string eyeDescription = "";
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += modInstance.GetDescriptionForEye() + "\n";
            }
            tooltipText.text = eyeDescription;
        }
        else {
            tooltipText.text = hoveredSlot.item.ItemRef.GetDescription();
        }
        
        Vector2 toolTipPos = hoveredSlot.ui.transform.position;
        float slotWidth = hoveredSlot.ui.GetComponent<RectTransform>().rect.width;
        float slotHeight = hoveredSlot.ui.GetComponent<RectTransform>().rect.height;
        toolTipPos += new Vector2(slotWidth / 2 + 20, slotHeight / 2 + 20);
        itemDescPopup.transform.position = toolTipPos;

        Rect rect = itemDescPopup.GetComponent<RectTransform>().rect;
        int minHeight = 80;
        rect.height = Mathf.Clamp(tooltipText.GetComponent<RectTransform>().rect.height, minHeight, Mathf.Infinity);
        itemDescPopup.GetComponent<RectTransform>().sizeDelta = new(rect.width, rect.height);
    }

    private void HideItemTooltip() {
        itemDescPopup.GetComponentInChildren<TextMeshProUGUI>().text = string.Empty;
        itemDescPopup.SetActive(false);
    }
    
    private void RemoveItemFromInventory(Inventory inventory, int slotIndex) {
        inventory.slots[slotIndex].item = null;
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

    private Entity player;
    private List<Collider2D> playerContacts = new(10);
    private Vector2 playerVelocity;
    
    private void UpdatePlayer() {
        if (player.health <= 0f) {
            ClearInventory(playerInventory);
            gameStateMachine.SetState(hideoutState);
            return;
        }
        
        healthBarFillImage.fillAmount = player.health / 100f;
        
        if (InventoryIsOpen) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        
        float speed = GetPlayerSpeedBasedOnStats();
        player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (speed * Time.deltaTime);
        playerVelocity = new Vector3(moveInput.x, moveInput.y, 0f) * speed;

        if (moveInput.x < 0) {
            player.spriteRenderer.flipX = true;
        }
        else {
            player.spriteRenderer.flipX = false;
        }
        
        if (moveInput.x != 0) {
            player.animator.Play("PlayerRun");
        }
        else if (moveInput.y > 0) {
            player.animator.Play("PlayerRunUp");
        }
        else if (moveInput.y < 0) {
            player.animator.Play("PlayerRunDown");
        }
        else {
            player.animator.Play("PlayerIdle");
        }
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        crosshairTrans.position = mousePos;

        if (attackInputAction.IsPressed() && CanShoot()) {
            PlayAudioClip(shootClip, player.position, 1f);
            ShootProjectile();
        }
    }
    
    private const float defaultPlayerSpeed = 0.55f;
    private const float maxPlayerSpeed = 0.85f;

    private const int encumberingIncreasePerStrengthPoint = 50;
    private const int defaultStartingEncumberingWeight = 600;
    private const int maxEncumberedWeight = 700;
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
        
        int strengthStat = baseStats.strength;
        for (int i = 0; i < playerEquipmentSize; i++) {
            InventoryItem item = playerInventory.slots[i].item;
            if (item == null) continue;
            if (item.ItemRef.modifiesStats && item.ItemRef.strengthStatAdjustment != 0) {
                strengthStat += item.ItemRef.strengthStatAdjustment;
            }
        }

        int encumberingIncreaseFromStrength = strengthStat * encumberingIncreasePerStrengthPoint;
        int startingEncumberingWeight = defaultStartingEncumberingWeight + encumberingIncreaseFromStrength;
        int endingEncumberingWeight = maxEncumberedWeight + encumberingIncreaseFromStrength;

        int inventoryWeight = GetInventoryWeight(playerInventory);
        int overWeightAmount = Mathf.Clamp(inventoryWeight - startingEncumberingWeight, 0, int.MaxValue);
        float overWeightComp = overWeightAmount / (float)endingEncumberingWeight;

        float speedReductionFromWeight = Mathf.Lerp(0f, maxEncumberedSpeedReduction, overWeightComp);
        speedReductionFromWeight = Mathf.Clamp(speedReductionFromWeight, 0f, maxEncumberedSpeedReduction);

        playerSpeed -= speedReductionFromWeight;
        return playerSpeed;
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
        
        const float defaultTimeAlive = 1.2f;
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
        ContactFilter2D contactFilter = new() { layerMask = Masks.ItemMask };
        int size = Physics2D.OverlapCircle(checkCenter, 0.1f, contactFilter, playerContacts);
        
        for (int i = 0; i < size; i++) {
            Collider2D col = playerContacts[i];
            
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
        } 
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
    
    private void DamagePlayer(int damage) { 
        player.health -= damage;
        AddFlashHitEffect(player);
    }

    private void DamageEnemy(Entity enemy, int damage, bool isCriticalStrike) {
        enemy.health -= damage;
        AddFlashHitEffect(enemy);

        Vector2 startDamageNumPos = OffsetY(enemy.position, 0.15f);
        Vector2 endDamageNumPos = OffsetY(enemy.position, 0.22f);
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
                AstarPath.active.UpdateGraphs(entity.collider.bounds);
                DestroyEntity(entity);
                
                PlayAudioClip(stoneBreakClip, entity.position, 1f);

                for (int i = 0; i < 6; i++) {
                    Vector3 spawnPos = entity.position + RandomOffset360(0.18f, 0.25f);
                    Entity rockDrop = SpawnEntity<Entity>(rockDropPool.GetDropFromPool(), entity.position, Quaternion.identity);
                    AddBounceEffect(rockDrop, spawnPos, 0.8f);
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
        Entity exp = SpawnEntity(explosionPool, spawnPos, Quaternion.identity); 
        DestroyEntity(exp, CurrentClipLength(exp.animator));

        ContactFilter2D contactFilter = new() {
            layerMask = Masks.EnemyMask,
            useLayerMask = true,
        };

        List<Collider2D> cols = ListPool<Collider2D>.Get();
        int count = Physics2D.OverlapCircle(spawnPos, explosion.radius, contactFilter, cols);
        for (int i = 0; i < count; i++) {
            DamageEnemy(entityLookup[cols[i].gameObject], explosion.damage, false);
        }
        ListPool<Collider2D>.Release(cols);
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
            int randomInventorySize = Random.Range(2, 6);
            InventorySlot[] deadBodySlots = new InventorySlot[randomInventorySize];

            for (int j = 0; j < randomInventorySize; j++) {
                Item spawnItem = deadBodyPool.GetItemFromPool();
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
                AstarPath.active.UpdateGraphs(resource.collider.bounds);
            }

            return resource;
        }
    }

    private void DestroyLevelEntities() {
        for (int i = entities.Count - 1; i >= 0; i--) {
            DestroyEntityAtIndex(i);    
        }

        deadBodySlotsLookup.Clear();
        activeAltars.Clear();
        enemies.Clear();
    }

    // ***************************
    // Saving and Loading
    // ***************************
    
    private string inventorySavePath;
    private string stashSavePath;
    private string crucibleSavePath;
    private string hideoutDataSavePath;
    private List<InventoryItem> cachedInventoryForSaving = new(50);

    private void BuildSavePaths() {
        inventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
        crucibleSavePath = $"{Application.persistentDataPath}/crucible";
        hideoutDataSavePath = $"{Application.persistentDataPath}/hideoutData";
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
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Eye) {
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
                
                if (slot.ui.onlyAcceptedItemType == Item.ItemType.Soulcard) {
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
        
        stashUpgradeButton.onClick.AddListener(() => {
            UpgradePath.UpgradeRequirements requirements = stashUpgradePath.pathUpgrades[hideoutStateData.stashLevel];
            
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
            
            hideoutStateData.stashLevel++;
            SaveToFile(hideoutDataSavePath, hideoutStateData);
            
            ChangeInventorySize(stashInventory, stashInventory.slots.Length + stashUpgradeSlotIncrease);
            RefreshInventoryDisplay(stashInventory);
        });
        
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

    // Its better just to have these as constants because the canvas layout recalculates in LateUpdate
    private const float playerPanelWidth = 500f;
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
    
    
    
    public static bool RollProbability(float probability) {
        return Random.value < probability;
    }

    private bool InRaid => gameStateMachine.CurState == raidState;
    
    private Vector3 RandomOffset360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
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
    
}