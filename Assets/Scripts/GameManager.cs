using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {

    public List<ItemData> allItems;
    public Transform player;
    public Camera mainCamera;
    public RectTransform crosshairTrans;
    public Transform resourceSpawnParent;
    public Transform exitPortalSpawnParent;

    public Transform hideoutParent;
    public Transform hellRaidParent;
    
    public GameObject projectilePrefab;
    public GameObject enemyPrefab;
    public GameObject gemRockPrefab;
    public GameObject deadBodyPrefab;
    public GameObject exitPortalPrefab;
    
    public EnemyWaveManager waveManager;
    
    [Header("Spawn Positions")]
    public Vector3 hideoutSpawnPosition;
    public Vector3 hellSpawnPosition;

    [Header("UI")]
    public RectTransform playerInventoryParent;
    public RectTransform lootInventoryParent;
    public RectTransform stashInventoryParent;
    public GameObject inventorySlotPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject interactPrompt;
    public TextMeshProUGUI exitPortalStatusText;
    
    [Header("Controls")]
    public InputAction moveInputAction;
    public InputAction attackInputAction;
    public InputAction interactInputAction;
    public InputAction inventoryInputAction;
    public InputAction selectItemInputAction;
    public InputAction splitStackInputAction;
    
    [NonSerialized] public List<Projectile> projectiles = new();
    
    [NonSerialized] public List<Enemy> enemies = new();
    [NonSerialized] public Dictionary<GameObject, Enemy> enemyLookup = new();
    
    
    private static Dictionary<string, ItemData> itemDataLookup = new();

    private Timer exitPortalTimer;

    private State hideoutState;
    private State raidState;
    private StateMachine gameStateMachine = new();

    private void Start() {
        Cursor.visible = false;
        
        foreach (ItemData itemData in allItems) {
            itemDataLookup.Add(itemData.uuid, itemData);
        }
        
        BuildSavePaths();
        LoadInventory();
        LoadStash();
        
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        selectItemInputAction = InputSystem.actions.FindAction("SelectItem");
        splitStackInputAction = InputSystem.actions.FindAction("SplitStack");

        hideoutState = gameStateMachine.CreateState(OnHideoutStateUpdate, OnHideoutStateEnter, OnHideoutStateExit);
        raidState = gameStateMachine.CreateState(OnRaidStateUpdate, OnRaidStateEndter, OnRaidStateExit);
        
        InitInventory();
    }

    private void Update() {
        gameStateMachine.Tick();
    }

    private void FixedUpdate() {
        FixedUpdateEnemies();
    }

    private void OnApplicationQuit() {
        SaveInventory();
        SaveStash();
    }

    private void UpdateTimers() {
        exitPortalTimer.Tick();
        discoverLootTimer.Tick();
    }


    private void OnHideoutStateEnter() {
        hideoutParent.gameObject.SetActive(true);
        player.transform.position = hideoutSpawnPosition;
    }

    private void OnHideoutStateExit() {
        hideoutParent.gameObject.SetActive(false);
    }

    private void OnHideoutStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
    }

    private void OnRaidStateEndter() {
        hellRaidParent.gameObject.SetActive(true);
        player.transform.position = hellSpawnPosition;
        InitExitPortal();
        InitWave();
        SpawnResources();
        AstarPath.active.Scan();
    }

    private void OnRaidStateExit() {
        ClearResources();
        ClearEnemies();
        ClearProjectiles();
        hellRaidParent.gameObject.SetActive(false);
    }

    private void OnRaidStateUpdate() {
        UpdateTimers();
        CheckForInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        SpawnEnemies();
        UpdateEnemies();
        UpdateWave();
    }
    
    
    private const float playerSpeed = 1.55f;
    private const float attackCooldown = 0.1f;
    private const float interactionRadius = 0.1f;
    private Limitter attackLimiter;
    private List<Collider2D> playerContacts = new(10);
    
    private void UpdatePlayer() {
        if (InventoryIsOpen) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
        player.position += new Vector3(moveInput.x, moveInput.y, 0f) * (playerSpeed * Time.deltaTime);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        crosshairTrans.position = mousePos;

        if (attackInputAction.IsPressed() && attackLimiter.TimeHasPassed(attackCooldown)) {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);

            Vector2 velocity = (mouseWorldPos - player.PositionV2()).normalized * 2.1f;
            float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
            
            Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            GameObject projectile = Instantiate(projectilePrefab, player.position + new Vector3(0f, 0.1f, 0f), projectileRotation);
            
            projectiles.Add(new() {
                timeAlive = 0f,
                trans = projectile.transform,
                velocity = velocity 
            });
        }
    }
    
    private void CheckForInteractions() { 
        interactPrompt.SetActive(false);
        
        Collider2D playerCol = player.GetComponent<Collider2D>();
        int size = playerCol.GetContacts(playerContacts);
        
        for (int i = 0; i < size; i++) {
            Collider2D col = playerContacts[i];
            
            if (col.CompareTag(Tags.Pickup)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    AddItemToInventory(playerInventory, col.GetComponent<Item>().itemData); 
                    Destroy(col.gameObject);
                }
            }

            if (col.CompareTag(Tags.Crucible)) {
                EnableInteractionPrompt(col.transform.position);
            }
            
            if (col.CompareTag(Tags.Stash)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    OpenPlayerInventory();
                    OpenStashInventory();
                }
            }

            if (col.CompareTag(Tags.DeadBody)) {
                EnableInteractionPrompt(col.transform.position);
                if (interactInputAction.WasPressedThisFrame()) {
                    curLootInvetory = deadBodyInventoriesLookup[col.gameObject].items;
                    OpenPlayerInventory();
                    OpenLootInventory();
                }
            }

            if (col.CompareTag(Tags.ExitPortal)) {
                gameStateMachine.SetStateIfNotCurrent(hideoutState);
            }

            if (col.CompareTag(Tags.HellPortal)) {
                gameStateMachine.SetStateIfNotCurrent(raidState);
            }
        } 
    }

    private void EnableInteractionPrompt(Vector3 position) {
        interactPrompt.SetActive(true);
        interactPrompt.transform.position = mainCamera.WorldToScreenPoint(position + new Vector3(0f, 0.1f, 0f));
    }
    
    
    [Serializable]
    public class InventoryItem {
        public string itemDataUuid; // Store uuid for serialization purposes
        public int count;
        
        // Using NOT discovered because initializer values are ignored by deserialization, so this field will always be false
        [NonSerialized] public bool notDiscovered;
        public ItemData itemData => itemDataLookup[itemDataUuid];
    }
    
    [NonSerialized] public List<InventoryItem> playerInventory = new();
    [NonSerialized] public List<InventoryItem> stashInventory = new();
    [NonSerialized] public List<InventoryItem> curLootInvetory;

    private Timer discoverLootTimer;
    private int discoverLootIndex;
    
    private bool InventoryIsOpen => playerInventoryParent.gameObject.activeSelf;
    private bool StashIsOpen => stashInventoryParent.gameObject.activeSelf;
    private bool LootInventoryIsOpen => lootInventoryParent.gameObject.activeSelf;
    
    private void InitInventory() {
        const int inventorySlotSizeWithPadding = 110;
        
        const int playerInventoryWidth = 3;
        const int playerInventoryHeight = 4;
        InitInventoryWithSlots(playerInventoryParent, playerInventoryWidth, playerInventoryHeight); 
        
        const int cachedLootInventoryWidth = 3;
        const int cachedLootInventoryHeight = 4;
        InitInventoryWithSlots(lootInventoryParent, cachedLootInventoryWidth, cachedLootInventoryHeight); 
        
        const int stashInventoryWidth = 3;
        const int stashInventoryHeight = 4;
        InitInventoryWithSlots(stashInventoryParent, stashInventoryWidth, stashInventoryHeight); 

        void InitInventoryWithSlots(RectTransform parent, int width, int height) {
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    Vector3 pos = new(parent.position.x, parent.position.y, 0f);
                    Vector3 offset = new(inventorySlotSizeWithPadding * i, -(inventorySlotSizeWithPadding * j), 0f);
                    GameObject slot = Instantiate(inventorySlotPrefab, pos + offset, Quaternion.identity, parent);
                    Instantiate(inventoryItemPrefab, pos + offset, Quaternion.identity, slot.transform);
                }
            }
            parent.gameObject.SetActive(false);
        }
    }
    
    private void UpdateInventory() {
        if (inventoryInputAction.WasPressedThisFrame()) {
            if (!InventoryIsOpen) {
                OpenPlayerInventory();
            }
            else {
                ClosePlayerInventory();
            }

            if (StashIsOpen) {
                CloseStashInventory();
            }

            if (LootInventoryIsOpen) {
                CloseLootInventory();
            }

            // Add nearby items to loot inventory
            {
                // Collider2D[] cols = Physics2D.OverlapCircleAll(player.position, interactionRadius, Masks.ItemMask);
                // if (cols.Length <= 0) return;
                //
                // lootInventoryParent.gameObject.SetActive(true);
                //
                // for (int i = 0; i < cols.Length; i++) {
                //     Collider2D col = cols[i];
                //     ItemData itemData = col.GetComponent<Item>().itemData;
                //     lootInventoryParent.GetChild(i).GetChild(0).GetComponent<Image>().sprite = itemData.inventorySprite;
                // }
            }
        }

        if (!InventoryIsOpen && (!StashIsOpen || !LootInventoryIsOpen)) return;
        
        if (!selectItemInputAction.WasPressedThisFrame() && !splitStackInputAction.WasPressedThisFrame()) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        if (InventoryIsOpen && StashIsOpen) {
            if (InventoryIsHovered(playerInventoryParent, out int playerSlotIndex)) {
                MoveItemBetweenInventories(playerInventory, stashInventory, playerSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(stashInventory, stashInventoryParent);
            }
            else if (InventoryIsHovered(stashInventoryParent, out int stashSlotIndex)) {
                MoveItemBetweenInventories(stashInventory, playerInventory, stashSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(stashInventory, stashInventoryParent);
            }
            return;
        }

        if (InventoryIsOpen && LootInventoryIsOpen) {
            if (InventoryIsHovered(lootInventoryParent, out int lootSlotIndex)) {
                MoveItemBetweenInventories(curLootInvetory, playerInventory, lootSlotIndex);
                RefreshInventoryDisplay(playerInventory, playerInventoryParent);
                RefreshInventoryDisplay(curLootInvetory, lootInventoryParent);
            }
        }

        void MoveItemBetweenInventories(List<InventoryItem> fromInventory, List<InventoryItem> toInventory, int hoveredSlotIndex) {
            InventoryItem inventoryItem = GetInventoryItem(fromInventory, hoveredSlotIndex);
            if (inventoryItem == null || inventoryItem.notDiscovered) return;

            if (splitStackInputAction.WasPressedThisFrame() && inventoryItem.count > 1) {
                int firstHalf = inventoryItem.count / 2;
                int secondHalf = inventoryItem.count - firstHalf;
                AdjustItemCountInInventory(fromInventory, hoveredSlotIndex, firstHalf);
                AddItemToInventory(toInventory, inventoryItem.itemData, secondHalf);
                return;
            }
            
            RemoveItemFromInventory(fromInventory, hoveredSlotIndex);
            AddItemToInventory(toInventory, inventoryItem.itemData, inventoryItem.count);
        }

        bool InventoryIsHovered(Transform inventoryParent, out int hoveredSlotIndex) {
            hoveredSlotIndex = GetHoveredInventorySlot(inventoryParent);
            return hoveredSlotIndex >= 0;
        }

        int GetHoveredInventorySlot(Transform inventoryParent) {
            for (int i = 0; i < inventoryParent.childCount; i++) {
                RectTransform rectTrans = inventoryParent.GetChild(i).GetComponent<RectTransform>();
                bool mouseInRect = RectTransformUtility.RectangleContainsScreenPoint(rectTrans, mousePos);
                if (mouseInRect) {
                    return i;
                }
            }
            return -1;
        }
    }
    
    public void AddItemToInventory(List<InventoryItem> inventory, ItemData itemData, int count = 1) {
        int possibleInsertIndex = int.MaxValue;
        
        for (int i = 0; i < inventory.Count; i++) {
            InventoryItem item = inventory[i];
            if (item == null) {
                possibleInsertIndex = Mathf.Min(i, possibleInsertIndex);
                continue;
            }

            if (item.itemData == itemData) {
                item.count += count;
                return;
            }
        }

        InventoryItem newItem = new() {
            itemDataUuid = itemData.uuid,
            count = count,
        };
        
        if (possibleInsertIndex < int.MaxValue) {
            inventory.Insert(possibleInsertIndex, newItem);
            return;
        }
        
        inventory.Add(newItem);
    }
    
    private void RemoveItemFromInventory(List<InventoryItem> inventory, int slotIndex) {
        inventory[slotIndex] = null;
    }
    
    private InventoryItem GetInventoryItem(List<InventoryItem> inventory, int slotIndex) {
        if (slotIndex < 0 || slotIndex >= inventory.Count) {
            return null;
        }
        return inventory[slotIndex];
    }
    
    private void AdjustItemCountInInventory(List<InventoryItem> inventory, int slotIndex, int newCount) {
        InventoryItem item = GetInventoryItem(inventory, slotIndex);
        item.count = newCount;
    }

    private void RefreshInventoryDisplay(List<InventoryItem> inventory, Transform inventoryParent) {
        if (!inventoryParent.gameObject.activeSelf) return;
        
        foreach (Transform child in inventoryParent.transform) {
            child.GetComponentInChildren<InvetoryItemUI>().Clear();
        }

        for (int i = 0; i < inventory.Count; i++) {
            InventoryItem item = inventory[i];
            if (item == null || item.notDiscovered) continue;
            inventoryParent.GetChild(i).GetComponentInChildren<InvetoryItemUI>().Set(item.itemData, item.count);
        }
    }

    private void OpenPlayerInventory() {
        playerInventoryParent.gameObject.SetActive(true);
        crosshairTrans.gameObject.SetActive(false);
        Cursor.visible = true;
        RefreshInventoryDisplay(playerInventory, playerInventoryParent);
    }

    private void ClosePlayerInventory() {
        playerInventoryParent.gameObject.SetActive(false);
        crosshairTrans.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    private void OpenStashInventory() {
        stashInventoryParent.gameObject.SetActive(true);
        RefreshInventoryDisplay(stashInventory, stashInventoryParent);
    }

    private void CloseStashInventory() {
        stashInventoryParent.gameObject.SetActive(false);
    }

    private void OpenLootInventory() {
        discoverLootIndex = -1;
        lootInventoryParent.gameObject.SetActive(true);
        
        foreach (Transform child in lootInventoryParent.transform) {
            child.GetComponentInChildren<InvetoryItemUI>().Clear();
        }

        for (int i = 0; i < curLootInvetory.Count; i++) {
            if (curLootInvetory[i] == null) continue;
            if (curLootInvetory[i].notDiscovered) {
                discoverLootIndex = i;
                break;
            }
            InventoryItem item = curLootInvetory[i];
            lootInventoryParent.GetChild(i).GetComponentInChildren<InvetoryItemUI>().Set(item.itemData, item.count);
        }

        bool alreadyDiscoveredAll = discoverLootIndex == -1;
        if (alreadyDiscoveredAll) return;
        
        discoverLootTimer.SetTime(1f);
        discoverLootTimer.EndAction ??= () => {
            InventoryItem item = curLootInvetory[discoverLootIndex];
            item.notDiscovered = false;
            
            lootInventoryParent.GetChild(discoverLootIndex).GetComponentInChildren<InvetoryItemUI>().Set(item.itemData, item.count);
            
            discoverLootIndex++;
            if (discoverLootIndex < curLootInvetory.Count) {
                discoverLootTimer.SetTime(1f);
            }
        };
    }

    private void CloseLootInventory() {
        lootInventoryParent.gameObject.SetActive(false);
        discoverLootTimer.Stop();
    }
    
    
    public struct Projectile {
        public Transform trans;
        public float timeAlive;
        public Vector2 velocity;
    }
    
    private void UpdateProjectiles() {
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.timeAlive += Time.deltaTime;
            proj.trans.position += proj.velocity.ToVector3() * Time.deltaTime;
            projectiles[i] = proj;
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, 0.1f, Masks.DamagableMask);
            if (col != null) {

                if (col.CompareTag(Tags.Enemy)) {
                    // We damage all very close enemies to elimate long trains
                    Collider2D[] cols = Physics2D.OverlapCircleAll(proj.trans.position, 0.12f, Masks.EnemyMask);
                    foreach (Collider2D eCol in cols) {
                        Enemy enemy = enemyLookup[eCol.gameObject];
                        enemy.health -= 50;
                    }
                }
                else {
                    Mineable mineable = col.GetComponent<Mineable>();
                    mineable.health -= 50;

                    Vector3 spawnPos = col.transform.position + RandomOffset360(0.25f, 0.5f);
                    spawnedResources.Add(Instantiate(mineable.dropPrefab, spawnPos, Quaternion.identity));

                    if (mineable.health <= 0) {
                        Destroy(mineable.gameObject);
                    }
                    
                    AstarPath.active.UpdateGraphs(mineable.GetComponent<Collider2D>().bounds);
                }
                
                Destroy(projectiles[i].trans.gameObject);
                projectiles.RemoveAt(i);
            }
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].timeAlive > 5f) {
                Destroy(projectiles[i].trans.gameObject);
                projectiles.RemoveAt(i);
            }
        }
    }

    private void ClearProjectiles() {
        foreach (Projectile projectile in projectiles) {
            Destroy(projectile.trans.gameObject);
        }
        projectiles.Clear();
    }
    

    public class Enemy {
        public Transform trans;
        public Rigidbody2D rigidbody;
        public PathData pathData = new();
        public int health;
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }
    }
    
    public class PathData {
        public ABPath abPath;
        public int waypointIndex;
        public bool isBeingCalculated;
        public float lastUpdateTime;
        
        public bool HasPath => abPath != null;
    }

    private void SpawnEnemies() {
        if (waveManager.enemiesLeftToSpawn <= 0 || enemies.Count > waveManager.enemySpawnLimit) return;

        Vector2 randomSpawnPos = player.position + RandomOffset360(3f, 4f);
        
        NNInfo info = AstarPath.active.graphs[0].GetNearest(randomSpawnPos, NNConstraint.Walkable);
        GameObject enemy = Instantiate(enemyPrefab, info.position, Quaternion.identity);
        
        enemies.Add(new() {
            trans = enemy.transform,
            rigidbody = enemy.GetComponent<Rigidbody2D>(),
            health = 100,
        });
        enemyLookup.Add(enemy, enemies[^1]);

        waveManager.enemiesLeftToSpawn--;
    }
    
    private void UpdateEnemies() {
        for (int i = enemies.Count - 1; i >= 0; i--) {
            if (enemies[i].health <= 0) {
                enemyLookup.Remove(enemies[i].trans.gameObject);
                Destroy(enemies[i].trans.gameObject);
                enemies.RemoveAt(i);
            }
        }

        foreach (Enemy enemy in enemies) {
            if ((enemy.pathData.HasPath && Time.time - enemy.pathData.lastUpdateTime <= 0.5f) || enemy.pathData.isBeingCalculated) continue;
            
            ABPath abPath = ABPath.Construct(enemy.position, player.position, path => {
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

            Vector2 targetPos = usingPath ? pathData.abPath.vectorPath[pathData.waypointIndex] : player.position;
            Vector2 movePos = Vector2.MoveTowards(enemy.position, targetPos, 0.4f * Time.fixedDeltaTime);
            enemy.rigidbody.MovePosition(movePos);
        }
    }

    private void ClearEnemies() {
        foreach (Enemy enemy in enemies) {
            Destroy(enemy.trans.gameObject);
        }
        enemies.Clear();
        enemyLookup.Clear();
    }


    [Serializable]
    public class EnemyWaveManager {
        public float minTimeBetweenWaves;
        public float maxTimeBetweenWaves;
        public int startingWaveSize;
        public int startingSpawnLimit;
        public int waveSizeIncrement;

        public float curTimeBetweenWave;
        public int curWaveCount;
        public int enemiesLeftToSpawn;
        public int enemySpawnLimit;
    }

    private void InitExitPortal() {
        exitPortalTimer.SetTime(Random.Range(35f, 45f));
        
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
            Instantiate(exitPortalPrefab, exitPortalParent.position, Quaternion.identity, exitPortalParent);
            exitPortalStatusText.text = $"Exit Portal: { exitPortalParent.name }";
        };
    }
    
    private void InitWave() {
        waveManager.curTimeBetweenWave = Random.Range(waveManager.minTimeBetweenWaves, waveManager.maxTimeBetweenWaves);
    }
    
    private void UpdateWave() {
        EnemyWaveManager wm = waveManager;
        
        wm.curTimeBetweenWave -= Time.deltaTime;
        if (wm.curTimeBetweenWave > 0f) return;

        wm.curTimeBetweenWave = Random.Range(wm.minTimeBetweenWaves, wm.maxTimeBetweenWaves);
        wm.enemiesLeftToSpawn = wm.startingWaveSize + wm.waveSizeIncrement * wm.curWaveCount;
        wm.enemySpawnLimit = wm.startingSpawnLimit + wm.waveSizeIncrement * wm.curWaveCount;
        wm.curWaveCount++;
    }
    

    public Vector3 RandomOffset360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }


    private List<GameObject> spawnedResources = new();
    private Dictionary<GameObject, DeadBodyInventory> deadBodyInventoriesLookup = new();

    public struct DeadBodyInventory {
        public List<InventoryItem> items;
    }
    
    private void SpawnResources() {
        List<Transform> spawnPoints = resourceSpawnParent.GetComponentsInChildren<Transform>().ToList();
        spawnPoints.RemoveAt(0); // Remove resourceSpawnParent
        
        int gemRocksToSpawn = Random.Range(10, 13);
        for (int i = 0; i < gemRocksToSpawn; i++) {
            SpawnResource(gemRockPrefab, true);
        }
        
        int deadBodiesToSpawn = Random.Range(5, 10);
        for (int i = 0; i < deadBodiesToSpawn; i++) {
            DeadBodyInventory inventory = new() { items = new() };
            
            int randomInventorySize = Random.Range(2, 6);
            for (int j = 0; j < randomInventorySize; j++) {
                InventoryItem lootItem = new() {
                    itemDataUuid = allItems[0].uuid, 
                    count = Random.Range(1, 10),
                    notDiscovered = true,
                };
                inventory.items.Add(lootItem);
            }
            
            GameObject body = SpawnResource(deadBodyPrefab, false);
            deadBodyInventoriesLookup.Add(body, inventory);
        }

        GameObject SpawnResource(GameObject resourcePrefab, bool cutsNavmesh) {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnTrans = spawnPoints[randomIndex];
            spawnPoints.RemoveAt(randomIndex);
            
            GameObject resource = Instantiate(resourcePrefab, spawnTrans.position, resourcePrefab.transform.rotation);
            spawnedResources.Add(resource);

            if (cutsNavmesh) {
                AstarPath.active.UpdateGraphs(resource.GetComponent<Collider2D>().bounds);
            }

            return resource;
        }
    }

    private void ClearResources() {
        foreach (GameObject resource in spawnedResources) {
            Destroy(resource);
        }
        spawnedResources.Clear();
        deadBodyInventoriesLookup.Clear();
    }


    private string inventorySavePath;
    private string stashSavePath;

    private void BuildSavePaths() {
        inventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
    }
    
    private void SaveInventory() {
        SaveToFile(inventorySavePath, playerInventory);
    }

    private void LoadInventory() {
        List<InventoryItem> loadedItems = LoadFromFile<List<InventoryItem>>(inventorySavePath);
        if (loadedItems != null) {
            playerInventory = loadedItems;
        }
    }

    private void SaveStash() {
        SaveToFile(stashSavePath, stashInventory);
    }

    private void LoadStash() {
        List<InventoryItem> loadedItems = LoadFromFile<List<InventoryItem>>(stashSavePath);
        if (loadedItems != null) {
            stashInventory = loadedItems;
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
    
}
