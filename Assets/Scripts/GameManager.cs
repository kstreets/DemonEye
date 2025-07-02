using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {

    public Transform player;
    public Camera mainCamera;
    public RectTransform crosshairTrans;
    
    public InputAction moveInputAction;
    public InputAction attackInputAction;
    public InputAction interactInputAction;
    public InputAction inventoryInputAction;

    public GameObject projectilePrefab;
    public GameObject enemyPrefab;
    
    public EnemyWaveManager waveManager;

    [Header("UI")]
    public RectTransform inventoryParent;
    public RectTransform lootInventoryParent;
    public GameObject inventorySlotPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject interactPrompt;

    [NonSerialized] public List<Projectile> projectiles = new();
    
    [NonSerialized] public List<Enemy> enemies = new();
    [NonSerialized] public Dictionary<GameObject, Enemy> enemyLookup = new();

    [NonSerialized] public bool inventoryIsOpen;
    

    private void Start() {
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        inventoryInputAction = InputSystem.actions.FindAction("Inventory");
        
        Player.Init(this);
    }

    private void Update() {
        Player.Update();
        UpdateProjectiles();
        // SpawnEnemies();
        UpdateEnemies();
        UpdateWave();
    }

    private void FixedUpdate() {
        FixedUpdateEnemies();
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
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, 0.1f, Masks.EnemyMask);
            if (col != null) {
                // We damage all very close enemies to elimate long trains
                Collider2D[] cols = Physics2D.OverlapCircleAll(proj.trans.position, 0.12f, Masks.EnemyMask);
                foreach (Collider2D eCol in cols) {
                    Enemy enemy = enemyLookup[eCol.gameObject];
                    enemy.health -= 50;
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
        if (waveManager.enemiesLeftToSpawn <= 0) return;
        
        Vector2 randomSpawnPos = player.position + Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(3f, 4f);
        
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


    [Serializable]
    public class EnemyWaveManager {
        public float minTimeBetweenWaves;
        public float maxTimeBetweenWaves;
        public int startingWaveSize;
        public int waveSizeIncrement;

        public float curTimeBetweenWave;
        public int curWaveCount;
        public int enemiesLeftToSpawn;
    }
    
    private void UpdateWave() {
        EnemyWaveManager wm = waveManager;

        wm.curTimeBetweenWave -= Time.deltaTime;
        if (wm.curTimeBetweenWave > 0f) return;

        wm.curTimeBetweenWave = Random.Range(wm.minTimeBetweenWaves, wm.maxTimeBetweenWaves);
        wm.enemiesLeftToSpawn = wm.startingWaveSize + wm.waveSizeIncrement * wm.curWaveCount;
        wm.curWaveCount++;
    }


    [Serializable]
    public class InventoryItem {
        public ItemData itemData;
        public int count;
    }

    [NonSerialized] public List<InventoryItem> playerInventory = new();

    public void AddItemToPlayerInventory(ItemData itemData) {
        foreach (InventoryItem item in playerInventory) {
            if (item.itemData == itemData) {
                item.count += 1;
                return;
            }
        }

        playerInventory.Add(new() { itemData = itemData, count = 1 });
    }

}
