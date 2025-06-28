using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour {

    public Transform player;
    public Camera mainCamera;
    public RectTransform crosshairTrans;
    
    public InputAction moveInputAction;
    public InputAction attackInputAction;

    public Transform tempEnemy;
    
    public GameObject projectilePrefab; 
    [NonSerialized] public List<Projectile> projectiles = new();
    
    [NonSerialized] public List<Enemy> enemies = new();
    
    private void Start() {
        Cursor.visible = false;
        
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        
        Player.Init(this);
        enemies.Add(new() {trans = tempEnemy});
    }

    private void Update() {
        Player.Update();
        UpdateProjectiles();
        UpdateEnemies();
    }

    public struct Projectile {
        public Transform trans;
        public float timeAlive;
        public Vector2 velocity;
    }
    
    private void UpdateProjectiles() {
        for (int i = 0; i < projectiles.Count; i++) {
            Projectile proj = projectiles[i];
            proj.timeAlive += Time.deltaTime;
            proj.trans.position += proj.velocity.ToVector3() * Time.deltaTime;
            projectiles[i] = proj;
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
        public PathData pathData = new();
        
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
    
    private void UpdateEnemies() {
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
        
        foreach (Enemy enemy in enemies) {
            if (enemy.pathData.abPath == null) continue;
            
            PathData pathData = enemy.pathData;
            
            bool usingPath = enemy.pathData.abPath.vectorPath.Count >= 2 && pathData.waypointIndex < pathData.abPath.vectorPath.Count;
            
            if (usingPath && Vector2.Distance(enemy.position, pathData.abPath.vectorPath[pathData.waypointIndex].ToVector2()) < 0.5f) {
                pathData.waypointIndex++;
            }
            
            usingPath = usingPath && pathData.waypointIndex < pathData.abPath.vectorPath.Count;

            Vector2 targetPos = usingPath ? pathData.abPath.vectorPath[pathData.waypointIndex] : player.position;
            enemy.position = Vector2.MoveTowards(enemy.position, targetPos, 0.3f * Time.deltaTime);
        }
    }
    
}
