using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Random = UnityEngine.Random;

public partial class GameManager {
    
    public class Enemy : Entity {
        public EnemyData data;
        public PathData pathData = new();
        public Timer applyDamageTimer;
        public BleedModInstance? bleed;
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

            if (distFromPlayer < 0.35f && !enemy.animator.Playing("Attack")) {
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
            
            if (enemy.bleed.TryGetValue(out BleedModInstance bleed)) {
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
            if (enemy.defaultSlow.TryGetValue(out SlowInstance defaultSlow)) {
                totalSlowPercentage += defaultSlow.speedReductionPercent;
                if (Time.time > defaultSlow.activationTime + defaultSlow.duration) {
                    enemy.defaultSlow = null;
                }
            }
            if (enemy.slow.TryGetValue(out SlowInstance slow)) {
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
            enemyLookup.Add(enemy.gameObject, enemy);
            
            wm.spawnTimeIndex++;
        }

    }


}
