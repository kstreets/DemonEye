using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public partial class Game {
    
    private int walkSideAnim = Animator.StringToHash("WalkSide");
    private int walkUpAnim = Animator.StringToHash("WalkUp");
    private int walkDownAnim = Animator.StringToHash("WalkDown");

    private int attackSideAnim = Animator.StringToHash("AttackSide");
    private int attackUpAnim = Animator.StringToHash("AttackUp");
    private int attackDownAnim = Animator.StringToHash("AttackDown");
    
    private const float enemySlowedMass = 1e6f;
    
    public class Enemy : Entity {
        public float flowFieldAcc;
        public float prevAverageDistFromPlayer;
        public float curRunningSumDistFromPlayer;
        public int curRunningSumFrameCount;
        public float averageDistFromPlayerTime;
        public float lastTeleportTime;
        public bool notProgressingTowardsPlayer;
        public Collider2D enemySpacerCollider;
        public EnemyData data;
        public Timer applyDamageTimer;
        public BleedEyeUpgrade.InstanceData? bleed;
        public PoisonEyeUpgrade.InstanceData? poison;
        public SlowEyeUpgrade.InstanceData? slow;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public Limiter changeDirLimiter;
    }
    
    private void OnEnemySpawned(Enemy enemy, EnemyData data) {
        enemy.data = data;
        enemy.health = data.health;
        enemy.animator.runtimeAnimatorController = data.animatorOverride;
        enemy.enemySpacerCollider = enemy.trans.GetChild(0).GetComponent<Collider2D>();
        enemy.enemySpacerCollider.excludeLayers = data.excludeCollisionLayers;
        enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
        enemy.rigidbody.mass = data.defualtMass;
    }
    
    private void OnEnemyDeath(Enemy deadEnemy) {
        Assert.IsNotNull(deadEnemy.data.dropPool, "Enemy needs to have a drop pool");
        
        if (deadEnemy.data.dropPool.HasItems && RollProbability(deadEnemy.data.chanceToDropItem)) {
            Item dropItem = GetItemFromDropPool(deadEnemy.data.dropPool);
            if (dropItem) {
                SpawnItemAsEntity(dropItem, 1, deadEnemy.position, Quaternion.identity);
            }
        }
                    
        Entity bloodSplatterEntity = SpawnEntity(entityPools.bloodSplatter, deadEnemy.position, Quaternion.identity);
        DestroyEntity(bloodSplatterEntity, CurrentClipLength(bloodSplatterEntity.animator));
        PlayAudioClip(audio.bloodBurstClip, deadEnemy.position);
        
        PlayerOnEnemyDeath(deadEnemy);
        
        if (!thisFrame.enemyKillCount.TryAdd(deadEnemy.data, 1)) {
            thisFrame.enemyKillCount[deadEnemy.data]++;
        }
        
        DestroyEntity(deadEnemy);
    }
    
    private void UpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        if (enemies.Count <= 0) return;
        
        bool reteleportWithThisSpawn = !spawnManager.SpawningDoneInCurPhase && spawnManager.spawnedThisFrame;
        bool reteleportWithoutSpawn = spawnManager.SpawningDoneInCurPhase && curRaid.data.reteleportLimitter.TimeHasPassed(1f);
        if (reteleportWithThisSpawn || reteleportWithoutSpawn) {
            // ReteleportEnemies();
        }
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (enemy.data.type == EnemyData.EnemyType.Phantom) continue;
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
                BeginEnemyAttackDelay(enemy); 
            }
            
            if (enemy.bleed.TryGetValue(out var bleed)) {
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                    int bleedDamage = Mathf.RoundToInt(GetBaseDamage() * bleed.damageMultiplier);
                    SpawnDamageNumber(EnemyDamageNumberSpawnPos(enemy), bleedDamage, DamageColor.Blood);
                    
                    enemy.health -= bleedDamage;
                    bleed.lastBleedTime = Time.time;
                    enemy.bleed = bleed;
                    
                    Entity bloodDrop = SpawnEntity(entityPools.bloodDrop, OffsetY(enemy.position, 0.015f), Quaternion.identity);
                    AddParentEffect(bloodDrop, enemy, 0.4f);
                    DestroyEntity(bloodDrop, 0.8f);
                    
                    PlantBloodMushroom(enemy.position);
                    thisFrame.data.enemyBloodDropped++;
                }
            }

            if (enemy.health <= 0) {
                Enemy deadEnemy = enemies[i];
                const float deathDelay = 0.12f;
                Delay(deadEnemy, deathDelay, static (deadEnemy) => gameInstance.OnEnemyDeath(deadEnemy));
                enemies.RemoveAt(i);
            }
        }
        
        List<Collider2D> overlapedEnemiesWithPlayer = Physics.OverlapCapsule(player.position, player.hurtCollider, Masks.EnemyMask);
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
            
            Enemy collidedWithEnemy = entities.lookup[closestColToPlayer.gameObject] as Enemy;
            DamagePlayer(collidedWithEnemy.data.collisionDamage, PlayerDamageType.Collision, collidedWithEnemy);
        }
    }
    
    private void FixedUpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        foreach (Enemy enemy in enemies) {
            bool isAttacking = EnemyPlayingAttackAnimation(enemy);
            MoveEnemy(enemy, isAttacking);
            OrientEnemy(enemy, isAttacking);
        }
    }
    
    private void MoveEnemy(Enemy enemy, bool isAttacking) {
        float speed = enemy.data.speed;
            
        float totalSlowPercentage = 0f;
        if (enemy.slow.TryGetValue(out var slow)) {
            totalSlowPercentage += slow.speedReductionPercent;
            enemy.rigidbody.mass = enemySlowedMass;
            if (Time.time > slow.activationTime + slow.duration) {
                enemy.rigidbody.mass = enemy.data.defualtMass;
                enemy.slow = null;
            }
        }
        speed = Mathf.Clamp(speed * Mathf.Clamp01(1f - totalSlowPercentage), 0.05f, enemy.data.speed);

        if (isAttacking) {
            speed = 0f;
        }

        Vector3 targetDir = Vector3.zero;
        if (enemy.data.usesFlowField) {
            targetDir = curRaid.mapInstance.grid.GetFlowFieldDirection(enemy.position);
        }
        if (targetDir == Vector3.zero) {
            targetDir = (player.position - enemy.position).normalized;
        }
            
        enemy.moveDir = Vector3.Lerp(enemy.moveDir, targetDir, enemy.flowFieldAcc * Time.fixedDeltaTime);
        enemy.rigidbody.linearVelocity = enemy.moveDir * speed;
    }
    
    private void OrientEnemy(Enemy enemy, bool isAttacking) {
        if (isAttacking || enemy.changeDirLimiter.TimeHasPassed(0.15f)) return;
        
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
    
    private void BeginEnemyAttackDelay(Enemy enemy) {
        if (enemy.data.type == EnemyData.EnemyType.Boomon) {
            Delay(enemy, enemy.data.attackDamageDelay, static (enemy) => {
                const int projectileCount = 3;
                const float angleDeltaPerDrop = 360f /  projectileCount;
                const float randomRangePerDrop = angleDeltaPerDrop * 0.25f;

                for (int i = 0; i <  projectileCount; i++) {
                    float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                    Vector3 velocity = gameInstance.RotationVector(randomAngle) * 0.62f;
                    Vector3 position = gameInstance.OffsetY(enemy.position, 0.2f);
                    const float lifetime = 2f;
                    gameInstance.SpawnProjectile(
                        gameInstance.entityPools.gooProjectile, position, velocity, lifetime, enemy, 
                        flatDamage: enemy.data.damage, layermask: Masks.PlayerHurtMask
                    );
                }
                
                enemy.health = 0;
            });
            return;
        }
        
        if (enemy.data.type == EnemyData.EnemyType.Phantom) {
            float time = enemy.animator.TimeLeftInCurrentAnimation();
            Delay(enemy, time, static (enemy) => {
                gameInstance.SpawnTeleportOut(enemy.position);
                gameInstance.entities.enemies.Remove(enemy);
                gameInstance.DestroyEntity(enemy);
            });
        }
        
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
                Entity smokeSlam = gameInstance.SpawnEntity<Entity>(gameInstance.prefabs.slamSmoke, attackCheckPos, Quaternion.identity);
                gameInstance.DestroyEntity(smokeSlam, gameInstance.CurrentClipLength(smokeSlam.animator));
            }

            if (col) {
                gameInstance.DamagePlayer(enemy.data.damage, PlayerDamageType.Normal, enemy, enemy.data.changeToCauseBleed);
            }
        });
    }
    
    private void ReteleportEnemies() {
        List<Enemy> enemies = entities.enemies;
        using var _ = ListPool<(Enemy, float)>.Get(out var reteleportCandidates);
        
        foreach (Enemy enemy in enemies) {
            if (Time.time - enemy.lastTeleportTime < 2f) continue;
            
            float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);
            bool farFromPlayer = distFromPlayer > 1.1f;
            if (farFromPlayer && enemy.notProgressingTowardsPlayer) {
                reteleportCandidates.Add((enemy, distFromPlayer));
            }
        }
        
        int minCountNeeded = Mathf.Max(1, Mathf.RoundToInt(enemies.Count * 0.15f));
        if (reteleportCandidates.Count < minCountNeeded) {
            return;
        }
        
        // Teleport top N by how long they've been waiting to reteleport
        reteleportCandidates.Sort(static (a, b) => {
            Enemy enemyA = a.Item1;
            Enemy enemyB = b.Item1;
            return enemyA.lastTeleportTime.CompareTo(enemyB.lastTeleportTime);
        });

        int maxTeleportCount = curRaid.map.spawning.maxEnemyRepositionCount;
        int teleportCount = Mathf.Min(reteleportCandidates.Count, maxTeleportCount);
        
        for (int i = 0; i < teleportCount; i++) {
            if (spawnManager.CurPhase == null) continue;
            
            (Enemy enemy, float distFromPlayer) = reteleportCandidates[i];
            Vector2Int repositionCellRange = spawnManager.CurPhase.repositionCellRange;
            Vector2 spawnPos = curRaid.mapInstance.grid.GetSpawnPosition(
                player.position, repositionCellRange.x, repositionCellRange.y, predictPlayerPos: true, curRaid.teleportingInPositions
            );
            
            if (Vector2.Distance(spawnPos, player.position) < distFromPlayer) {
                TeleportEnemy(enemy, spawnPos, TeleportType.Reposition);
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
        curRaid.teleportingInPositions.Add(position);
        
        if (teleportType == TeleportType.Reposition) {
            SpawnTeleportOut(enemy.position);
        }
        
        enemy.position = position;
        enemy.gameObject.SetActive(false);
        enemy.lastTeleportTime = Time.time;
        
        Entity inTeleportFxEntity = SpawnEntity(entityPools.teleportIn, enemy.position, Quaternion.identity);
        float spawnAnimDuration = CurrentClipLength(inTeleportFxEntity.animator);
        DestroyEntity(inTeleportFxEntity, spawnAnimDuration);
        
        PlayAudioClip(audio.teleportInClip, inTeleportFxEntity.position);

        float spawnDelay = spawnAnimDuration * 0.7f;
        
        Delay(enemy, spawnDelay, static (enemy) => {
            enemy.gameObject.SetActive(true);
            gameInstance.curRaid.teleportingInPositions.Remove(enemy.position);
            gameInstance.OnEnemyTeleportEnd(enemy);
        });
    }
    
    private void OnEnemyTeleportEnd(Enemy enemy) {
        if (enemy.data.type == EnemyData.EnemyType.Phantom) {
            Vector2 dirToPlayer = (player.position - enemy.position).normalized;
            switch (CardinalDirFromVector(dirToPlayer)) {
                case CardinalDir.Right:
                    enemy.spriteRenderer.flipX = false;
                    enemy.animator.Play(attackSideAnim);
                    enemy.graphicalDir = Vector2.right;
                    break;
                case CardinalDir.Left:
                    enemy.spriteRenderer.flipX = true;
                    enemy.animator.Play(attackSideAnim);
                    enemy.graphicalDir = Vector2.left;
                    break;
                case CardinalDir.Up:
                    enemy.animator.Play(attackUpAnim);
                    enemy.graphicalDir = Vector2.up;
                    break;
                case CardinalDir.Down:
                    enemy.animator.Play(attackDownAnim);
                    enemy.graphicalDir = Vector2.down;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            BeginEnemyAttackDelay(enemy);
        }
    }
    
    private void SpawnTeleportOut(Vector3 position) {
        Entity outTeleportFxEntity = SpawnEntity(entityPools.teleportOut, position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);
    }
    
    public class EnemySpawnManager {
        public float timeInCurPhase;
        public float totalTimeLeft;
        public float timeUntilFinalPhase;
        public int curPhaseIndex;
        public RaidSpawnPattern spawnPattern;
        public bool isFinishedSpawning;
        public bool spawnedThisFrame;
        
        public readonly List<int> chosenVarientIndices = new();
        
        public const int prefixedSumResolution = 500;
        public readonly float[] prefixedSums = new float[prefixedSumResolution];

        public readonly List<(float time, EnemyData enemy)> spawnEvents = new();
        public int spawnTimeIndex;
       
        public int CurVarientIndex => chosenVarientIndices.IndexInRange(curPhaseIndex) ? chosenVarientIndices[curPhaseIndex] : -1;
        public List<RaidSpawnPattern.PhasePool> PhasePools => spawnPattern.phasePools; 
        public RaidSpawnPattern.PhasePool CurPhasePool => PhasePools.SafeIndex(curPhaseIndex); 
        public RaidSpawnPattern.Variant CurPhase => CurPhasePool?.variants.SafeIndex(CurVarientIndex);
        public bool SpawningDoneInCurPhase => !spawnEvents.IndexInRange(spawnTimeIndex);
    }

    [NonSerialized] private EnemySpawnManager spawnManager = new();
    
    private void InitSpawnManager(RaidSpawnPattern pattern) {
        spawnManager.spawnEvents.Clear();
        spawnManager.isFinishedSpawning = false;
        spawnManager.spawnedThisFrame = false;
        spawnManager.spawnPattern = pattern;
        spawnManager.curPhaseIndex = -1;
        spawnManager.timeInCurPhase = 0f;
        spawnManager.totalTimeLeft = pattern.timeBeforeFirstPhase;
        
        spawnManager.chosenVarientIndices.Clear();
        foreach (RaidSpawnPattern.PhasePool pool in pattern.phasePools) {
            int randomVarientIndex = Random.Range(0, pool.variants.Count);
            spawnManager.chosenVarientIndices.Add(randomVarientIndex);
            spawnManager.totalTimeLeft += pool.variants[randomVarientIndex].phaseDuration;
        }
        
        float lastPhaseDuration = pattern.phasePools[^1].variants[spawnManager.chosenVarientIndices[^1]].phaseDuration;
        spawnManager.timeUntilFinalPhase = spawnManager.totalTimeLeft - lastPhaseDuration;
    }
    
    private Limiter spawnLimiterForEnemyBatching;
    
    private void UpdateSpawnManager() {
        EnemySpawnManager sm = spawnManager;
        sm.spawnedThisFrame = false;
        
        if (sm.isFinishedSpawning) return;
        
        sm.timeInCurPhase += Time.deltaTime;
        sm.totalTimeLeft -= Time.deltaTime;
        sm.timeUntilFinalPhase -= Time.deltaTime;
        
        float waveDuration = sm.curPhaseIndex == -1 ? sm.spawnPattern.timeBeforeFirstPhase : sm.CurPhase.phaseDuration;
        bool startNextWave = sm.timeInCurPhase >= waveDuration;
        bool onLastPhase = sm.curPhaseIndex == sm.spawnPattern.phasePools.Count - 1;
        
        if (startNextWave && !onLastPhase) {
            sm.curPhaseIndex++;

#if UNITY_EDITOR
            foreach (RaidSpawnPattern.EnemyBatch batch in sm.CurPhase.enemyBatches) {
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
                float weight = Mathf.Clamp01(sm.CurPhase.spawnRateCurve.Evaluate(sliceIndex));
                totalWeight += weight;
                sm.prefixedSums[i] = totalWeight;
            }

            // Build spawntimes for this next wave
            {
                sm.spawnEvents.Clear();
                
                foreach (RaidSpawnPattern.EnemyBatch waveUnit in sm.CurPhase.enemyBatches) {
                    int enemySpawnCount = waveUnit.enemyCount;
                    for (int i = 0; i < enemySpawnCount; i++) {
                        float targetWeight = (i / (float)(enemySpawnCount - 1)) * totalWeight;

                        // Find the corresponding time using linear search
                        int weightIndex = 0;
                        while (weightIndex < EnemySpawnManager.prefixedSumResolution && sm.prefixedSums[weightIndex] < targetWeight) {
                            weightIndex++;
                        }

                        float normalizedTime = weightIndex / (float)(EnemySpawnManager.prefixedSumResolution - 1);
                        sm.spawnEvents.Add((normalizedTime * sm.CurPhase.spawnDuration, waveUnit.enemyData));
                    }
                }
                
                // Due to the way we add elements we need to sort by time so its chronologically ordered 
                sm.spawnEvents.Sort(static (x, y) => x.time.CompareTo(y.time));
            }
            
            spawnLimiterForEnemyBatching.MakeCurrent();
        }
        
        if (sm.CurPhase == null) return;

        const float batchTime = 1f;
        if (!spawnLimiterForEnemyBatching.TimeHasPassed(batchTime) || sm.spawnEvents.Count <= 0) return;
        
        while (sm.spawnEvents.IndexInRange(sm.spawnTimeIndex) && sm.spawnEvents[sm.spawnTimeIndex].time <= sm.timeInCurPhase) {
            Vector2Int spawnCellRange = sm.CurPhase.spawnCellRange;
            
            Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(
                player.position, spawnCellRange.x, spawnCellRange.y, predictPlayerPos: false, curRaid.teleportingInPositions
            );

            EnemyData enemyToSpawn = sm.spawnEvents[sm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity);
            OnEnemySpawned(enemy, enemyToSpawn);
            entities.enemies.Add(enemy);
            
            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
            sm.spawnTimeIndex++;
            sm.spawnedThisFrame = true;
        }
        
        if (sm.SpawningDoneInCurPhase && onLastPhase) {
            sm.isFinishedSpawning = true;
        }
        
    }
    
}
