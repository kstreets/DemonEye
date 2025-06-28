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
    
    [NonSerialized] public List<EnemyPath> enemyPaths = new();
    
    private void Start() {
        Cursor.visible = false;
        
        moveInputAction = InputSystem.actions.FindAction("Move");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        
        Player.Init(this);
        
        ABPath abPath = ABPath.Construct(tempEnemy.position, new(4, 4, 0), path => {
            enemyPaths.Add(new() {path = path.vectorPath, trans = tempEnemy});
        });
        AstarPath.StartPath(abPath);
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

    public struct EnemyPath {
        public Transform trans;
        public List<Vector3> path;
        public int curPathIndex;
        public float curSegComp;
    }
    
    private void UpdateEnemies() {
        for (int i = 0; i < enemyPaths.Count; i++) {
            EnemyPath enemyPath = enemyPaths[i];
            enemyPath.curSegComp += Time.deltaTime;

            if (enemyPath.curSegComp >= 1f) {
                enemyPath.curSegComp -= 1f;
                enemyPath.curPathIndex++;
            }

            if (enemyPath.curPathIndex >= enemyPath.path.Count - 1) continue;

            Vector2 startSeg = enemyPath.path[enemyPath.curPathIndex];
            Vector2 endSeg = enemyPath.path[enemyPath.curPathIndex + 1];

            Vector2 pos = Vector2.Lerp(startSeg, endSeg, enemyPath.curSegComp);
            enemyPath.trans.position = pos;
            
            enemyPaths[i] = enemyPath;
        }
    }
    
}
