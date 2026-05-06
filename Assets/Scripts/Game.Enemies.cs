using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    [NonSerialized] public List<Enemy> enemies = new();
    public static Action<Enemy> onEnemyDeath;
    
    private int walkSideAnim = Animator.StringToHash("WalkSide");
    private int walkUpAnim = Animator.StringToHash("WalkUp");
    private int walkDownAnim = Animator.StringToHash("WalkDown");

    private int attackSideAnim = Animator.StringToHash("AttackSide");
    private int attackUpAnim = Animator.StringToHash("AttackUp");
    private int attackDownAnim = Animator.StringToHash("AttackDown");
    
    private const float enemySlowedMass = 1e6f;
    private float lastReteleportTime;
    
    public class Enemy : Entity {
        public float flowFieldAcc;
        public float prevAverageDistFromPlayer;
        public float curRunningSumDistFromPlayer;
        public int curRunningSumFrameCount;
        public float averageDistFromPlayerTime;
        public float postSlowMass;
        public bool notProgressingTowardsPlayer;
        public Collider2D enemySpacerCollider;
        public EnemyData data;
        public Timer applyDamageTimer;
        public BleedEyeUpgradeItem.InstanceData? bleed;
        public PoisonEyeUpgradeItem.InstanceData? poison;
        public SlowEyeUpgradeItem.InstanceData? slow;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public Limiter changeDirLimiter;
    }
    
    private void UpdateEnemies() {
        RaidSpawnPattern curSpawnPattern = loadedMapData.waves;
        bool reteleportTimeHasPassed = Time.time - lastReteleportTime >= curSpawnPattern.delayBetweenEnemyRepositions;
        
        if (reteleportTimeHasPassed) {
            int maxTeleportCount = curSpawnPattern.maxEnemyRepositionCount;
            using var _ = ListPool<(Enemy, float)>.Get(out var reteleportCandidates);
            
            foreach (Enemy enemy in enemies) {
                float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);
                bool farFromPlayer = distFromPlayer > 1f;
                if (farFromPlayer && enemy.notProgressingTowardsPlayer) {
                    reteleportCandidates.Add((enemy, distFromPlayer));
                }
            }
            
            int minCountNeeded = Mathf.Max(1, Mathf.RoundToInt(enemies.Count * 0.15f));
            if (reteleportCandidates.Count >= minCountNeeded) {
                // Teleport top N by distance
                reteleportCandidates.Sort(static (a, b) => b.Item2.CompareTo(a.Item2));

                int teleportCount = Mathf.Min(reteleportCandidates.Count, maxTeleportCount);
                for (int i = 0; i < teleportCount; i++) {
                    (Enemy enemy, float distFromPlayer) = reteleportCandidates[i];
                    Vector2Int repositionCellRange = spawnManager.CurSpawnPhase.repositionCellRange;
                    Vector2 spawnPos = loadedMapInst.grid.GetSpawnPosition(player.position, repositionCellRange.x, repositionCellRange.y);

                    if (Vector2.Distance(spawnPos, player.position) < distFromPlayer) {
                        TeleportEnemy(enemy, spawnPos, TeleportType.Reposition);
                        lastReteleportTime = Time.time; // Only reset the time if we actually teleport an enemy
                    }
                } 
            }
        }
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            enemy.applyDamageTimer.Tick();
            
            float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);

            enemy.curRunningSumFrameCount++;
            enemy.curRunningSumDistFromPlayer += distFromPlayer;
            
            enemy.averageDistFromPlayerTime += Time.deltaTime;
            if (enemy.averageDistFromPlayerTime > 1f) {
                enemy.averageDistFromPlayerTime = 0f;

                if (enemy.prevAverageDistFromPlayer != 0f) {
                    float curAverage = enemy.curRunningSumDistFromPlayer / enemy.curRunningSumFrameCount;
                    bool furtherAway = curAverage >= enemy.prevAverageDistFromPlayer;
                    bool aboutTheSameAway = Mathf.Abs(curAverage - enemy.prevAverageDistFromPlayer) < 0.01f;
                    enemy.notProgressingTowardsPlayer = furtherAway || aboutTheSameAway;
                }
                
                enemy.prevAverageDistFromPlayer = enemy.curRunningSumDistFromPlayer / enemy.curRunningSumFrameCount;
                enemy.curRunningSumDistFromPlayer = 0f;
                enemy.curRunningSumFrameCount = 0;
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
                            Vector3 velocity = gameInstance.RotationVector(randomAngle) * 0.62f;
                            gameInstance.SpawnProjectile(gameInstance.OffsetY(enemy.position, 0.2f), velocity, gameInstance.gooProjectilePool, 
                                flatDamage: enemy.data.damage, lifetime: 2f, layermask: Masks.PlayerHurtMask);
                        }
                        
                        enemy.health = 0;
                    });
                }
                else {
                    Delay(enemy, enemy.data.attackDamageDelay, static (enemy) => {
                        Vector2 attackCheckPos = enemy.position;
                        switch (gameInstance.CardinalDirFromVector(enemy.graphicalDir)) {
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
                            Entity smokeSlam = gameInstance.SpawnEntity<Entity>(gameInstance.slamSmokePrefab, attackCheckPos, Quaternion.identity);
                            gameInstance.DestroyEntity(smokeSlam, gameInstance.CurrentClipLength(smokeSlam.animator));
                        }

                        if (col) {
                            gameInstance.DamagePlayer(enemy.data.damage, PlayerDamageType.Normal, enemy.data.changeToCauseBleed);
                        }
                    });
                }
            }
            
            if (enemy.bleed.TryGetValue(out var bleed)) {
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    int bleedDamage = Mathf.RoundToInt(GetBaseDamage() * bleed.damageMultiplier);
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
                        Item dropItem = gameInstance.GetItemFromEnemyDropPool(deadEnemy.data);
                        if (dropItem) {
                            gameInstance.SpawnItemAsEntity(dropItem, 1, deadEnemy.position, Quaternion.identity);
                        }
                    }

                    gameInstance.player.soulCurrency += deadEnemy.data.soulWorthPerKill;
                    onEnemyDeath?.Invoke(deadEnemy);
                    
                    Entity bloodSplatterEntity = gameInstance.SpawnEntity(gameInstance.bloodSplatterPool, deadEnemy.position, Quaternion.identity);
                    gameInstance.DestroyEntity(bloodSplatterEntity, gameInstance.CurrentClipLength(bloodSplatterEntity.animator));
                    
                    gameInstance.PlayAudioClip(gameInstance.bloodBurstClip, deadEnemy.position);

                    gameInstance.DestroyEntity(deadEnemy);
                });
                enemies.RemoveAt(i);
            }
        }
        
        List<Collider2D> overlapedEnemiesWithPlayer = OverlapCapsule(player.position, player.hurtCollider, Masks.EnemyMask);
        bool someEnemyOverlapsPlayer = overlapedEnemiesWithPlayer.Count > 0;
        
        if (someEnemyOverlapsPlayer) {
            Vector2 playerPos = player.position;
            
            Collider2D closestColToPlayer = overlapedEnemiesWithPlayer[0];
            float closestDistToPlayer = Vector2.Distance(closestColToPlayer.transform.position, playerPos);
            
            foreach (Collider2D col in overlapedEnemiesWithPlayer) {
                float dist = Vector2.Distance(col.transform.position, playerPos);
                if (dist < closestDistToPlayer) {
                    closestDistToPlayer = dist;
                    closestColToPlayer = col;
                }
            }
            
            Enemy collidedWithEnemy = entityLookup[closestColToPlayer.gameObject] as Enemy;
            DamagePlayer(collidedWithEnemy.data.collisionDamage, PlayerDamageType.Collision);
        }
    }
    
    private void FixedUpdateEnemies() {
        foreach (Enemy enemy in enemies) {
            float speed = enemy.data.speed;
            
            float totalSlowPercentage = 0f;
            if (enemy.slow.TryGetValue(out var slow)) {
                totalSlowPercentage += slow.speedReductionPercent;
                
                bool enemyNeedsLargerMass = Mathf.Approximately(enemy.rigidbody.mass, enemySlowedMass);
                if (enemyNeedsLargerMass) {
                    enemy.postSlowMass = enemy.rigidbody.mass;
                    enemy.rigidbody.mass = enemySlowedMass;
                }
                
                if (Time.time > slow.activationTime + slow.duration) {
                    enemy.rigidbody.mass = enemy.postSlowMass;
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
        });
    }
    
    public class EnemySpawnManager {
        public float timeInCurPhase;
        public float totalTimeLeft;
        public float timeUntilFinalPhase;
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
        spawnManager.timeInCurPhase = 0f;
        spawnManager.totalTimeLeft = pattern.timeBeforeFirstPhase;
        foreach (RaidSpawnPattern.SpawnPhase phase in spawnManager.spawnPattern.spawnPhases) {
            spawnManager.totalTimeLeft += phase.phaseDuration;
        }
        spawnManager.timeUntilFinalPhase = spawnManager.totalTimeLeft - spawnManager.spawnPattern.spawnPhases[^1].phaseDuration;
    }
    
    private Limiter spawnLimiterForEnemyBatching;
    
    private void UpdateSpawnManager() {
        EnemySpawnManager sm = spawnManager;
        if (sm.isFinishedSpawning) return;
        
        sm.timeInCurPhase += Time.deltaTime;
        sm.totalTimeLeft -= Time.deltaTime;
        sm.timeUntilFinalPhase -= Time.deltaTime;
        
        float waveDuration = sm.curPhaseIndex == -1 ? sm.spawnPattern.timeBeforeFirstPhase : sm.spawnPattern.spawnPhases[sm.curPhaseIndex].phaseDuration;
        bool startNextWave = sm.timeInCurPhase >= waveDuration;
        bool onLastPhase = sm.curPhaseIndex == sm.spawnPattern.spawnPhases.Count - 1;
        
        if (startNextWave && !onLastPhase) {
            sm.curPhaseIndex++;
            RaidSpawnPattern.SpawnPhase curPhase = sm.spawnPattern.spawnPhases[sm.curPhaseIndex];

#if UNITY_EDITOR
            foreach (RaidSpawnPattern.EnemyBatch batch in curPhase.enemyBatches) {
                if (batch.enemyCount >= EnemySpawnManager.prefixedSumResolution) {
                    Debug.LogError($"Wave cannot have more enemies than {nameof(EnemySpawnManager.prefixedSumResolution)}");
                }
            }
#endif
            
            sm.timeInCurPhase = 0f;
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

        if (!spawnLimiterForEnemyBatching.TimeHasPassed(1f) || sm.spawnEvents.Count <= 0) return;
        
        while (sm.spawnEvents.IndexInRange(sm.spawnTimeIndex) && sm.spawnEvents[sm.spawnTimeIndex].time <= sm.timeInCurPhase) {
            Vector2Int spawnCellRange = spawnManager.CurSpawnPhase.spawnCellRange;
            Vector2 randomSpawnPos = loadedMapInst.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y);

            EnemyData enemyToSpawn = sm.spawnEvents[sm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity);
            {
                enemy.health = enemyToSpawn.health;
                enemy.data = enemyToSpawn;
                enemy.animator.runtimeAnimatorController = enemyToSpawn.animatorOverride;
                enemy.enemySpacerCollider = enemy.trans.GetChild(0).GetComponent<Collider2D>();
                enemy.enemySpacerCollider.excludeLayers = enemyToSpawn.excludeCollisionLayers;
                enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
            }
            enemies.Add(enemy);
            
            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
            sm.spawnTimeIndex++;
        }
        
        bool outOfSpawnsInPhase = !sm.spawnEvents.IndexInRange(sm.spawnTimeIndex);
        if (outOfSpawnsInPhase && onLastPhase) {
            sm.isFinishedSpawning = true;
        }
        
    }
    
}
