using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;

public partial class GameManager : MonoBehaviour {

    public List<ItemPool> traderLevelPools;

    [Foldout("Pooling Prefabs")]
    public GameObject bloodDropPrefab;
    public GameObject projectilePrefab;
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
    [NonSerialized] public Dictionary<GameObject, Enemy> enemyLookup = new();
    
    public static Dictionary<int, Item> itemLookup = new();
    public static Dictionary<int, Soulcard> eyeModifierLookup = new();

    private Timer exitPortalTimer;
    private int consecutiveCriticalHits;

    private EntityPool<Entity> bloodDropPool;
    private EntityPool<Projectile> projectilePool;
    
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
        projectilePool = CreateEntityPool<Projectile>(projectilePrefab, 20, OnSpawnProjectile);

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

        if (attackInputAction.IsPressed() && CanShootPrimary()) {
            PlayAudioClip(shootClip, player.position, 1f);
            ShootPrimary();
        }
    }
    
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

    public class Projectile : Entity {
        public float timeAlive;
        public float destroyTime;
        public float distTraveled;
        public Vector2 velocity;
        public DemonEyeInstance eyeInstanceSpawnedFrom;
        public List<Entity> ignoreEntities;
    }
    
    private static void OnSpawnProjectile(Projectile projectile) {
        projectile.timeAlive = default;
        projectile.destroyTime = default;
        projectile.distTraveled = default;
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
            proj.timeAlive += Time.deltaTime;
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
            if (projectiles[i].timeAlive > projectiles[i].destroyTime) {
                DestroyEntity(projectiles[i]);
                projectiles.RemoveAt(i);
            }
        }
    }

    private bool ProjectileShouldPassThrough(Projectile proj, Entity entity) {
        if (!proj.eyeInstanceSpawnedFrom.penetration.TryGetValue(out PenetrationInstance pen)) {
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

    private void DamagePlayer(int damage) { 
        player.health -= damage;
        AddFlashHitEffect(player);
    }
    
    private void HandleDamage(Projectile projectile, Entity entity) {
        if (entity == null) return;
        
        DemonEyeInstance eyeInstance = projectile.eyeInstanceSpawnedFrom;
        
        if (entity.gameObject.CompareTag(Tags.Enemy)) {
            Enemy enemy = enemyLookup[entity.gameObject];
            
            int damage = eyeInstance.coreAttack.damage;
            float criticalStrikeProb = defaultCriticalStrikeChange;

            if (eyeInstance.bleedCrit.HasValue && enemy.bleed.HasValue) {
                criticalStrikeProb += eyeInstance.bleedCrit.Value.probability;
            }
            
            bool isCriticalStrike = RollProbability(criticalStrikeProb);
            if (isCriticalStrike) {
                consecutiveCriticalHits++;
                damage = Mathf.RoundToInt(damage * defaultCriticalStrikeMultiplier);
            }
            else {
                consecutiveCriticalHits = 0;
            }

            if (projectile.eyeInstanceSpawnedFrom.farDamage.TryGetValue(out FarDamageInstance farDamage)) {
                int increasedDamageFromDist = Mathf.RoundToInt(farDamage.damageIncreasePerUnitTraveled * projectile.distTraveled);
                damage += increasedDamageFromDist;
            }

            if (projectile.eyeInstanceSpawnedFrom.doubleCrit.TryGetValue(out DoubleCritInstance doubleCrit)) {
                if (consecutiveCriticalHits > 0 && consecutiveCriticalHits % 2 == 0) {
                    damage = Mathf.RoundToInt(damage * doubleCrit.damageMultiplier);
                }
            }
            
            enemy.health -= damage;
            
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                modInstance.ApplyToEnemy(enemy);
            }
            
            enemy.defaultSlow = new() { activationTime = Time.time, duration = 0.1f, speedReductionPercent = eyeInstance.coreAttack.enemySpeedReductionPercent };
            AddFlashHitEffect(entity);

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
    
    
    private void LoadAllItems() {
        Item[] itemsFoundInFolder = Resources.LoadAll<Item>(string.Empty);
        foreach (Item item in itemsFoundInFolder) {
            if (item is Soulcard mod) {
                eyeModifierLookup.Add(mod.uuid, mod);
            }
            itemLookup.Add(item.uuid, item);
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
    
}