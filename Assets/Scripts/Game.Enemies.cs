using System;
using System.Collections.Generic;
using PrimeTween;
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
        public float curDistFromPlayer;
        public bool notProgressingTowardsPlayer;
        public Collider2D enemySpacerCollider;
        public EnemyData data;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public Limiter changeDirLimiter;
        public OptionalRef<BleedEyeUpgrade.InstanceData> bleed;
        public OptionalRef<HemorrhageAugment.InstanceData> hemorrhage;
        public OptionalRef<PoisonEyeUpgrade.InstanceData> poison;
        public OptionalRef<SlowEyeUpgrade.InstanceData> slow;
        public OptionalRef<KnockBackAugment.InstanceData> knockBack;
    }
    
    public static void ClearEnemyDebuffs(Enemy enemy) {
        enemy.bleed.ClearValue();
        enemy.poison.ClearValue();
        enemy.slow.ClearValue();
        enemy.knockBack.ClearValue();
        enemy.GetEffect(Entity.EffectsIndicies.Poisoned).Stop();
    }
    
    private void OnEnemySpawned(Enemy enemy, EnemyData data) {
        enemy.data = data;
        enemy.health = data.health;
        enemy.animator.runtimeAnimatorController = data.animatorOverride;
        enemy.enemySpacerCollider = enemy.trans.GetChild(0).GetComponent<Collider2D>();
        enemy.enemySpacerCollider.excludeLayers = data.excludeCollisionLayers;
        enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
        enemy.rigidbody.mass = data.defaultMass;
        enemy.collider.enabled = true;
    }
    
    private void OnEnemyDeath(Enemy deadEnemy) {
        Assert.IsNotNull(deadEnemy.data.dropPool, "Enemy needs to have a drop pool");
        
        if (deadEnemy.data.dropPool.HasItems && RollProbability(deadEnemy.data.chanceToDropItem)) {
            Item dropItem = GetItemFromDropPool(deadEnemy.data.dropPool);
            if (dropItem) {
                Entity itemDropEntity = SpawnItemAsEntity(dropItem, 1, deadEnemy.Center, Quaternion.identity);
                Vector3 endPos = deadEnemy.Center + RotationVector360(0.15f, 0.22f);
                AddBounceEffect(itemDropEntity, endPos, 0.8f);
            }
        }
                    
        PlayerOnEnemyDeath(deadEnemy);
        
        if (!thisFrame.enemyKillCount.TryAdd(deadEnemy.data, 1)) {
            thisFrame.enemyKillCount[deadEnemy.data]++;
        }
    }
    
    private void UpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        if (enemies.Count <= 0) return;
        
        bool reteleportWithThisSpawn = !spawnManager.FinishedSpawningThisWave && spawnManager.spawnedThisFrame;
        bool reteleportWithoutSpawn = spawnManager.FinishedSpawningThisWave && curRaid.data.reteleportLimitter.TimeHasPassed(1f);
        if (reteleportWithThisSpawn || reteleportWithoutSpawn) {
            // ReteleportEnemies();
        }
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            enemy.curDistFromPlayer = Vector2.Distance(player.Center, enemy.Center);
            enemy.curRunningSumFrameCount++;
            enemy.curRunningSumDistFromPlayer += enemy.curDistFromPlayer;
            
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

            switch (enemy.data.type) {
                case EnemyData.EnemyType.FireBoss:
                    FireBossLogic(enemy);
                    break;
                default:
                    BasicEnemyLogic(enemy);
                    break;
            }
            
            if (enemy.bleed.HasValue) {
                ref var bleed = ref enemy.bleed.GetValue();
                if (Time.time - bleed.lastBleedTime > bleed.bleedInterval) {
                
                    float bleedDamageMultiplier = bleed.damageMultiplier;
                    DamageColor bleedDamageColor = DamageColor.Blood;
                
                    if (enemy.hemorrhage.HasValue) {
                        bleedDamageMultiplier *= enemy.hemorrhage.GetValue().bleedDamageMulti;
                        enemy.hemorrhage.ClearValue(); // One time use
                        bleedDamageColor = DamageColor.Hemorrhage;
                    }
                
                    int bleedDamage = Mathf.RoundToInt(GetBaseDamage() * bleedDamageMultiplier);
                    SpawnDamageNumber(EnemyDamageNumberSpawnPos(enemy), bleedDamage, bleedDamageColor);
                
                    enemy.health -= bleedDamage;
                    bleed.lastBleedTime = Time.time;

                    Entity bloodDrop = SpawnEntity(entityPools.bloodDrop, OffsetY(enemy.position, 0.015f), Quaternion.identity);
                    AddParentEffect(bloodDrop, enemy, 0.4f);
                    DestroyEntity(bloodDrop, 0.8f);
                
                    PlantBloodMushroom(enemy.position);
                    thisFrame.data.enemyBloodDropped++;
                }
            }

            if (enemy.health <= 0) {
                Enemy deadEnemy = enemies[i];
            
                // It looks better if the enemies keep some momentum when dying
                deadEnemy.rigidbody.linearVelocity *= 0.3f;
                deadEnemy.collider.enabled = false;
            
                const float deathDelay = 0.03f;
                Delay(deadEnemy, deathDelay, static (deadEnemy) => {
                    Game game = gameInstance;
                    game.SpawnEntityOneShot(game.entityPools.bloodSplatter, deadEnemy.position, Quaternion.identity);
                    game.PlayAudioClip(game.audio.bloodBurstClip, deadEnemy.position);
                    game.OnEnemyDeath(deadEnemy);
                });
            
                DissolveAndDestroy(deadEnemy, 0.6f, config.enemyColors.enemyDissolveBloodColor);
                enemies.RemoveAt(i);
            }
        }
        
        Vector2 hurtColliderCenter = (Vector2)player.position + player.hurtCollider.offset;
        List<Collider2D> overlapedEnemiesWithPlayer = Physics.OverlapCapsule(hurtColliderCenter, player.hurtCollider, Masks.EnemyMask);
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
    
    private void BasicEnemyLogic(Enemy enemy) {
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
        bool withinAttackDist = enemy.curDistFromPlayer < enemy.data.attackDistance;
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
            Delay(enemy, enemy.data.attackDamageDelay, static (enemy) => {
                // Its possible the enemy died while waiting to apply the damage 
                if (enemy.health <= 0) return;
                PerformEnemyAttack(enemy); 
            });
        }
    }
    
    
    public class FireBoss : Enemy {
        public enum AttackTypes { Fireball, Meteor, DoubleFireball, MutliMeteor, Count }
        public AttackBrain<AttackTypes> attackBrain;
        
        public const float minAttackDelay = 0.65f;
        public const float maxAttackDelay = 1.75f;
        public Tween attackDelay;
        
        public const int fireballDamage = 10;
        public const float fireballSpeed = 1.5f;
        public const float fireballLifetime = 5f; 
        public Vector2 fireBallSpawnPos => position.Offset(y: 0.2f);
        
        public const float meteorTravelTime = 0.8f;
        public const float meteorRadius = 0.2f;
        public const float meteorScatterRadiusPerMeteor = 0.3f;
        public const float delayBetweenMeteors = 0.12f;
        public const int multiMeteorCount = 4;
    }
    
    private Enemy InitFireBoss(EnemyData data, Vector2 spawnPos) { 
        FireBoss fireBoss = SpawnEntity<FireBoss>(data.enemyPrefab, spawnPos, Quaternion.identity);
        fireBoss.attackDelay = Tween.Delay(FireBoss.minAttackDelay, FireBoss.maxAttackDelay);
        
        fireBoss.attackBrain = new((int)FireBoss.AttackTypes.Count);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Fireball, level: 0, defaultProb: 0.5f, repeatedProbDecay: 0.5f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Meteor, level: 0, defaultProb: 0.5f, repeatedProbDecay: 0.5f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.DoubleFireball, level: 1, defaultProb: 0.5f, repeatedProbDecay: 0.1f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.MutliMeteor, level: 1, defaultProb: 0.5f, repeatedProbDecay: 0.1f);
        
        return fireBoss;
    }
    
    private void FireBossLogic(Enemy enemy) {
        if (enemy is not FireBoss fireBoss) return;
        
        bool canAtttack = !fireBoss.attackDelay.isAlive;
        if (!canAtttack) return;
        
        fireBoss.attackDelay = Tween.Delay(FireBoss.minAttackDelay, FireBoss.maxAttackDelay);
        int attackLevel = (fireBoss.health / (float)fireBoss.data.health) > 0.5f ? 0 : 1;
        
        switch(fireBoss.attackBrain.ChooseAttack(attackLevel)) {
            case FireBoss.AttackTypes.Fireball:
                ShootFireball();
                break;
            case FireBoss.AttackTypes.Meteor:
                RainMeteors(1);
                break;
            case FireBoss.AttackTypes.DoubleFireball:
                Debug.Log("Double Fireball");
                break;
            case FireBoss.AttackTypes.MutliMeteor:
                RainMeteors(FireBoss.multiMeteorCount);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // ------------------------------
        // Nested Methods
        // ------------------------------
        
        void ShootFireball() {
            Vector2 spawnPos = fireBoss.fireBallSpawnPos;
            if (!TryGetPlayerInterceptPoint(spawnPos, FireBoss.fireballSpeed, out Vector2 aimPoint)) return;
            Vector2 velocity = (aimPoint - spawnPos).normalized * FireBoss.fireballSpeed;
            SpawnProjectile(entityPools.fireBossProjectile, spawnPos, velocity, FireBoss.fireballLifetime, fireBoss, flatDamage: FireBoss.fireballDamage, layermask: Masks.PlayerHurtMask);
        }
        
        void RainMeteors(int meteorCount) {
            const float predictionTime = FireBoss.meteorTravelTime * 0.85f;
            Vector2 centeralAimPoint = PredictedPlayerPosition(predictionTime);
            centeralAimPoint = ClampAttackToMap(player.position, centeralAimPoint, FireBoss.meteorRadius);

            // One meteor always goes on the player, the rest get scattered around it
            using var _ = ListPool<Vector2>.Get(out var meteorPositions);
            meteorPositions.Add(centeralAimPoint);

            // Grows with the count so the meteors are spread at a similar density no matter how many there are
            float scatterRadius = FireBoss.meteorScatterRadiusPerMeteor * Mathf.Sqrt(meteorCount);
            ScatterPositions(ref meteorPositions, meteorCount - 1, centeralAimPoint, scatterRadius, FireBoss.meteorRadius);

            // The one on the player drops first, tied to the boss so the remaining meteors are cancelled if it dies mid rain
            for (int i = 0; i < meteorPositions.Count; i++) {
                Vector2 meteorPos = meteorPositions[i];
                Delay(fireBoss, i * FireBoss.delayBetweenMeteors, _ => SpawnSingleMeteor(meteorPos, predictionTime));
            }
        }
        
        void SpawnSingleMeteor(Vector2 targetPos, float warningTime) {
            Vector2 meteorSpawnPos = new(targetPos.x, targetPos.y + 2);
            Entity attackCircleEntity = SpawnEntity(entityPools.attackCircle, targetPos, Quaternion.identity);
            attackCircleEntity.attackCircle.Animate(FireBoss.meteorRadius, warningTime);
            
            Entity meteorEntity = SpawnEntity(entityPools.meteor, meteorSpawnPos, Quaternion.identity);
            Tween.Custom(0f, 1f, FireBoss.meteorTravelTime, ease: Ease.InQuad, onValueChange: (comp) => {
                meteorEntity.position = Vector2.Lerp(meteorSpawnPos, targetPos, comp);    
            })
            .OnComplete(() => {
                DestroyEntity(attackCircleEntity);
                DestroyEntity(meteorEntity);
            });
        }
    }
    
    // Adds up to count random positions within scatterRadius of center, each a circle of the given radius that stays inside the map
    // and doesn't overlap any other position in the list (including ones already in it). A position that can't find a free spot
    // within the attempts is skipped, so fewer than count can get added
    private void ScatterPositions(ref List<Vector2> positions, int count, Vector2 center, float scatterRadius, float radius, int maxAttemptsPerPosition = 10) {
        float minSqrDist = (radius * 2f) * (radius * 2f);

        for (int i = 0; i < count; i++) {
            for (int attempt = 0; attempt < maxAttemptsPerPosition; attempt++) {
                Vector2 candidate = center + Random.insideUnitCircle * scatterRadius;
                candidate = ClampAttackToMap(center, candidate, radius);

                bool overlapsPlacedPos = false;
                foreach (Vector2 placedPos in positions) {
                    if ((placedPos - candidate).sqrMagnitude < minSqrDist) {
                        overlapsPlacedPos = true;
                        break;
                    }
                }
                if (overlapsPlacedPos) continue;

                positions.Add(candidate);
                break;
            }
        }
    }

    private Vector2 ClampAttackToMap(Vector2 origin, Vector2 targetPos, float radius) {
        Vector2 toTarget = targetPos - origin;
        float dist = toTarget.magnitude;
        if (dist > Mathf.Epsilon) {
            RaycastHit2D hit = Physics2D.Raycast(origin, toTarget / dist, dist, Masks.StaticLevelMask);
            if (hit) {
                targetPos = hit.point + hit.normal * radius;
            }
        }

        // The target can still be close enough to an edge for the circle to overlap it (no hit, or the offset ran into a corner), push it out
        Collider2D edge = Physics2D.OverlapCircle(targetPos, radius, Masks.StaticLevelMask);
        if (edge != null) {
            Vector2 closestPoint = edge.ClosestPoint(targetPos);
            Vector2 awayFromEdge = targetPos - closestPoint;
            float distFromEdge = awayFromEdge.magnitude;
            if (distFromEdge > Mathf.Epsilon) {
                targetPos = closestPoint + (awayFromEdge / distFromEdge) * radius;
            }
        }

        return targetPos;
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
        
        if (enemy.knockBack.HasValue) {
            ref var knockBack = ref enemy.knockBack.GetValue();
            knockBack.timeSpentInKnockBack += Time.fixedDeltaTime;
            
            float duration = knockBack.knockBackDist / knockBack.knockBackSpeed;
            float comp = Mathf.Clamp01(knockBack.timeSpentInKnockBack / duration);
            
            // Take the derivative of ease-out function (distance over time) which gives us the velocity at that point 
            float velocity = 2f * (1f - comp);
            enemy.rigidbody.linearVelocity = knockBack.dir * velocity;
            
            if (comp >= 1f) {
                enemy.knockBack.ClearValue();
            }
            return;
        }
            
        float totalSlowPercentage = 0f;
        if (enemy.slow.HasValue) {
            ref var slow = ref enemy.slow.GetValue();
            slow.timeSpentSlowed += Time.fixedDeltaTime;
            
            totalSlowPercentage += slow.speedReductionPercent;
            enemy.rigidbody.mass = enemySlowedMass;
            
            if (slow.timeSpentSlowed >= slow.duration) {
                enemy.rigidbody.mass = enemy.data.defaultMass;
                enemy.slow.ClearValue();
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
    
    private static void PerformEnemyAttack(Enemy enemy) {
        if (enemy.data.type == EnemyData.EnemyType.Boomon) {
            const int projectileCount = 3;
            const float angleDeltaPerDrop = 360f /  projectileCount;
            const float randomRangePerDrop = angleDeltaPerDrop * 0.25f;

            for (int i = 0; i <  projectileCount; i++) {
                float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                Vector3 velocity = RotationVector(randomAngle) * 0.62f;
                Vector3 position = gameInstance.OffsetY(enemy.position, 0.2f);
                const float lifetime = 2f;
                gameInstance.SpawnProjectile(
                    gameInstance.entityPools.gooProjectile, position, velocity, lifetime, enemy, 
                    flatDamage: enemy.data.damage, layermask: Masks.PlayerHurtMask
                );
            }
            
            enemy.health = 0;
            return;
        }
        
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
                player.position, repositionCellRange.x, repositionCellRange.y, predictPlayerPos: true, GatherEnemyPositions()
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
        });
    }
    
    private void SpawnTeleportOut(Vector3 position) {
        Entity outTeleportFxEntity = SpawnEntity(entityPools.teleportOut, position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);
    }
    
    public class EnemySpawnManager {
        public float timeInCurPhase;
        public float startNextWaveDelay;
        public float timeAddedThroughActions;
        public int prevEnemyCount;
        public int curPhaseIndex;
        public bool spawnedThisFrame;
        public bool waveStartedThisFrame;
        public RaidSpawnPattern spawnPattern;
        
        public readonly List<int> chosenVarientIndices = new();
        
        public const int prefixedSumResolution = 500;
        public readonly float[] prefixedSums = new float[prefixedSumResolution];

        public readonly List<(float time, EnemyData enemy)> spawnEvents = new();
        public int spawnTimeIndex;

        // Set once trash starts spawning alongside the boss after the last wave, the spawn events then belong to it instead of the current wave
        public RaidSpawnPattern.Variant bossTrashVariant;
        public bool BossTrashActive => bossTrashVariant != null;
        public RaidSpawnPattern.Variant SpawningVariant => bossTrashVariant ?? CurPhase;

        public float extraMaxDuration;
        public float extraEarlyDelay;
        public float CurMaxDuration => CurPhase.maxDuration + extraMaxDuration;
        public float CurEarlyDelay => CurPhase.startNextPhaseEarlyDelay + extraEarlyDelay;

        public int CurWaveNumber => Mathf.Clamp(curPhaseIndex + 1, 0, chosenVarientIndices.Count);
        public int TotalWaveCount => chosenVarientIndices.Count;
        public float TotalCompletion => CurWaveNumber / (float)TotalWaveCount;
        
        public bool BeforeLastWave => curPhaseIndex < chosenVarientIndices.Count - 1;
        public bool OnLastWave => curPhaseIndex == chosenVarientIndices.Count - 1;
        public bool FinishedSpawningThisWave => !spawnEvents.IndexInRange(spawnTimeIndex);
        public bool FinishedSpawningForRaid => OnLastWave && FinishedSpawningThisWave;
        
        public int CurVarientIndex => chosenVarientIndices.IndexInRange(curPhaseIndex) ? chosenVarientIndices[curPhaseIndex] : -1;
        public List<RaidSpawnPattern.PhasePool> PhasePools => spawnPattern.phasePools; 
        public RaidSpawnPattern.PhasePool CurPhasePool => PhasePools.SafeIndex(curPhaseIndex); 
        public RaidSpawnPattern.Variant CurPhase => CurPhasePool?.variants.SafeIndex(CurVarientIndex);
    }

    [NonSerialized] private EnemySpawnManager spawnManager = new();
    
    private void InitSpawnManager(RaidSpawnPattern pattern) {
        if (curRaid.mapInstance.isBossMap) return;
        
        spawnManager.spawnEvents.Clear();
        spawnManager.bossTrashVariant = null;
        spawnManager.spawnedThisFrame = false;
        spawnManager.spawnPattern = pattern;
        spawnManager.curPhaseIndex = -1;
        spawnManager.timeInCurPhase = 0f;
        spawnManager.startNextWaveDelay = 0f;
        spawnManager.timeAddedThroughActions = 0f;

        // The first couple of raids on the first map give new players more time per wave
        spawnManager.extraMaxDuration = 0f;
        spawnManager.extraEarlyDelay = 0f;
        if (curRaid.map == config.maps[0]) {
            if (!persistentFlags.HasFlag(PersistentFlags.FirstMapPlayedOnce)) {
                spawnManager.extraMaxDuration = config.gameplay.firstRaidExtraMaxTime;
                spawnManager.extraEarlyDelay = config.gameplay.firstRaidExtraEarlyDelay;
                persistentFlags |= PersistentFlags.FirstMapPlayedOnce;
            }
            else if (!persistentFlags.HasFlag(PersistentFlags.FirstMapPlayedTwice)) {
                spawnManager.extraMaxDuration = config.gameplay.secondRaidExtraMaxTime;
                spawnManager.extraEarlyDelay = config.gameplay.secondRaidExtraEarlyDelay;
                persistentFlags |= PersistentFlags.FirstMapPlayedTwice;
            }
        }

        spawnManager.chosenVarientIndices.Clear();
        foreach (RaidSpawnPattern.PhasePool pool in pattern.phasePools) {
            int randomVarientIndex = Random.Range(0, pool.variants.Count);
            spawnManager.chosenVarientIndices.Add(randomVarientIndex);
        }
    }
    
    private Limiter spawnLimiterForEnemyBatching;

    private void UpdateSpawnManager() {
        if (curRaid.mapInstance.isBossMap) return;

        EnemySpawnManager sm = spawnManager;
        sm.spawnedThisFrame = false;
        sm.waveStartedThisFrame = false;

        float prevTimeInCurPhase = sm.timeInCurPhase;
        sm.timeInCurPhase += Time.deltaTime;

        bool lastWaveSpawned = sm.OnLastWave && (sm.FinishedSpawningThisWave || sm.BossTrashActive);
        if (lastWaveSpawned) {
            UpdateBossTrashSpawning(sm);
        }
        else {
            UpdateWaveProgression(sm, prevTimeInCurPhase);
        }

        SpawnDueEnemies(sm);
    }

    private void UpdateWaveProgression(EnemySpawnManager sm, float prevTimeInCurPhase) {
        bool afterFirstWave = sm.curPhaseIndex >= 0;
        float waveDuration = afterFirstWave ? sm.CurMaxDuration : sm.spawnPattern.timeBeforeFirstPhase;
        bool timeLimitExceeded = sm.timeInCurPhase >= waveDuration;
        bool justHitTimeLimit = prevTimeInCurPhase < waveDuration && timeLimitExceeded;
        int curEnemyCount = entities.enemies.Count;
        bool waveCleared = afterFirstWave && sm.FinishedSpawningThisWave && curEnemyCount <= 0;

        if (afterFirstWave) {
            // Hitting the time limit with enemies still alive gives a buffer before the next wave, scaled by how much of the wave is left
            if (justHitTimeLimit && curEnemyCount > 0) {
                int totalEnemiesThisWave = 0;
                foreach (RaidSpawnPattern.EnemyBatch batch in sm.CurPhase.enemyBatches) {
                    totalEnemiesThisWave += batch.enemyCount;
                }
                float remainingOfTotalPercentage = curEnemyCount / (float)totalEnemiesThisWave;
                float addedTime = Mathf.Lerp(1f, 4f, remainingOfTotalPercentage);
                // The early delay is only for clearing the wave, which didn't happen, so the buffer replaces it
                sm.startNextWaveDelay += addedTime - sm.CurEarlyDelay;
            }

            bool justKilledLastEnemiesInWave = sm.FinishedSpawningThisWave && sm.prevEnemyCount > 0 && curEnemyCount <= 0;
            if (justKilledLastEnemiesInWave) {
                // If a wave's max duration is about the same time as the spawn duration, we can't assume the player struggled
                // because they would of had none or very little time to kill the last spawned enemies
                const float struggleEligiblePercentage = 1.2f;
                bool waveIsStruggleEligible = sm.CurMaxDuration >= (sm.CurPhase.spawnDuration * struggleEligiblePercentage);
                if (!waveIsStruggleEligible) { }
                else {
                    const float struggleThresholdPercentage = 0.8f;
                    float stuggleThreshold = sm.CurMaxDuration * struggleThresholdPercentage;
                    if (sm.timeInCurPhase < stuggleThreshold) { }
                    else {
                        float exceededTime = sm.timeInCurPhase - stuggleThreshold;
                        float maxPossibleExceededTime = sm.CurMaxDuration * (1f - struggleThresholdPercentage);
                        float addedTime = Mathf.Lerp(1f, 6f, exceededTime / maxPossibleExceededTime);
                        sm.startNextWaveDelay += addedTime;
                    }
                }
            }
            sm.prevEnemyCount = curEnemyCount;

            if (sm.FinishedSpawningThisWave) {
                const float maxTimeCanAddThroughActions = 13f;
                if (sm.timeAddedThroughActions >= maxTimeCanAddThroughActions) { }
                else {
                    float prevTime = sm.timeAddedThroughActions;

                    if (thisFrame.flags.HasFlag(FrameFlags.SearchingBody)) {
                        sm.timeAddedThroughActions += 2f;
                    }
                    if (thisFrame.flags.HasFlag(FrameFlags.SearchingBush)) {
                        sm.timeAddedThroughActions += 1f;
                    }
                    if (thisFrame.flags.HasFlag(FrameFlags.SummonedUpgrade)) {
                        sm.timeAddedThroughActions += 1f;
                    }
                    if (thisFrame.flags.HasFlag(FrameFlags.TookConsumable)) {
                        float timeToAdd = Mathf.Lerp(1f, 2f, 1f - CurPlayerHealthPercentage());
                        sm.timeAddedThroughActions += timeToAdd;
                    }
                    if (thisFrame.flags.HasFlag(FrameFlags.ShotRock)) {
                        sm.timeAddedThroughActions += 0.2f;
                    }

                    if (thisFrame.flags.HasFlag(FrameFlags.PickedUpLoot)) {
                        int nearbyItems = Physics.OverlapCircle(player.Center, 0.5f, Masks.ItemMask).Count;
                        if (nearbyItems >= 2) {
                            sm.timeAddedThroughActions += 1f;
                        }
                    }

                    // If the player is looking at their inventory or reading an item popup we give them some extra time
                    if ((PlayerInventoryIsOpen && !LootInventoryIsOpen) || ui.itemDescPopupPickup.IsShowing) {
                        sm.timeAddedThroughActions += Time.deltaTime + Mathf.Epsilon;
                    }

                    sm.timeAddedThroughActions = Mathf.Clamp(sm.timeAddedThroughActions, 0f, maxTimeCanAddThroughActions);
                    float timeAdded = sm.timeAddedThroughActions - prevTime;
                    sm.startNextWaveDelay += timeAdded;
                }
            }

            // Clearing the wave counts down the early start delay, and hitting the time limit counts down the buffer
            // for leftover enemies, so the next wave isn't held up by enemies the player hasn't killed
            if (waveCleared || timeLimitExceeded) {
                sm.startNextWaveDelay -= Time.deltaTime;
            }
        }

        bool startNextWave = sm.startNextWaveDelay <= 0f && (timeLimitExceeded || waveCleared);
        if (startNextWave && !sm.OnLastWave) {
            sm.curPhaseIndex++;
            sm.waveStartedThisFrame = true;
            sm.startNextWaveDelay = sm.CurEarlyDelay;
            if (sm.OnLastWave) {
                SpawnRaidBoss(sm);
            }
            BeginSpawningVariant(sm, sm.CurPhase);
        }
    }

    private void SpawnRaidBoss(EnemySpawnManager sm) {
        Vector2Int spawnCellRange = sm.CurPhase.spawnCellRange;
        Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, GatherEnemyPositions());

        EnemyData bossEnemy = sm.spawnPattern.raidBoss;
        Enemy enemy = bossEnemy.type switch {
            EnemyData.EnemyType.FireBoss => InitFireBoss(bossEnemy, randomSpawnPos),
            _ => throw new ArgumentOutOfRangeException(),
        };

        OnEnemySpawned(enemy, bossEnemy);
        entities.enemies.Add(enemy);
        TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
    }

    private void UpdateBossTrashSpawning(EnemySpawnManager sm) {
        if (!RaidBossIsAlive(sm)) {
            if (sm.BossTrashActive) {
                sm.bossTrashVariant = null;
                sm.spawnEvents.Clear();
                sm.spawnTimeIndex = 0;
            }
            return;
        }

        if (!sm.FinishedSpawningThisWave) return;
        if (entities.enemies.Count > sm.spawnPattern.maxEnemiesForBossTrash) return;

        List<RaidSpawnPattern.Variant> trashVariants = sm.spawnPattern.bossTrashPool?.variants;
        if (trashVariants == null || trashVariants.Count <= 0) return;

        sm.bossTrashVariant = trashVariants[Random.Range(0, trashVariants.Count)];
        BeginSpawningVariant(sm, sm.bossTrashVariant);
    }

    private bool RaidBossIsAlive(EnemySpawnManager sm) {
        foreach (Enemy enemy in entities.enemies) {
            if (enemy.data == sm.spawnPattern.raidBoss && enemy.health > 0) return true;
        }
        return false;
    }

    // Lays out when each of the variant's enemies spawn across its spawn duration, following its spawn rate curve
    private void BeginSpawningVariant(EnemySpawnManager sm, RaidSpawnPattern.Variant variant) {
#if UNITY_EDITOR
        foreach (RaidSpawnPattern.EnemyBatch batch in variant.enemyBatches) {
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
            float weight = Mathf.Clamp01(variant.spawnRateCurve.Evaluate(sliceIndex));
            totalWeight += weight;
            sm.prefixedSums[i] = totalWeight;
        }

        sm.spawnEvents.Clear();
        foreach (RaidSpawnPattern.EnemyBatch batch in variant.enemyBatches) {
            int enemySpawnCount = batch.enemyCount;
            for (int i = 0; i < enemySpawnCount; i++) {
                float targetWeight = (i / (float)(enemySpawnCount - 1)) * totalWeight;

                // Find the corresponding time using linear search
                int weightIndex = 0;
                while (weightIndex < EnemySpawnManager.prefixedSumResolution && sm.prefixedSums[weightIndex] < targetWeight) {
                    weightIndex++;
                }

                float normalizedTime = weightIndex / (float)(EnemySpawnManager.prefixedSumResolution - 1);
                sm.spawnEvents.Add((normalizedTime * variant.spawnDuration, batch.enemyData));
            }
        }

        // Due to the way we add elements we need to sort by time so its chronologically ordered
        sm.spawnEvents.Sort(static (x, y) => x.time.CompareTo(y.time));

        spawnLimiterForEnemyBatching.MakeCurrent();
    }

    private void SpawnDueEnemies(EnemySpawnManager sm) {
        RaidSpawnPattern.Variant variant = sm.SpawningVariant;
        if (variant == null) return;

        const float batchTime = 1f;
        if (!spawnLimiterForEnemyBatching.TimeHasPassed(batchTime) || sm.spawnEvents.Count <= 0) return;

        List<Vector2> enemyPositions = GatherEnemyPositions();

        while (sm.spawnEvents.IndexInRange(sm.spawnTimeIndex) && sm.spawnEvents[sm.spawnTimeIndex].time <= sm.timeInCurPhase) {
            Vector2Int spawnCellRange = variant.spawnCellRange;

            Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, enemyPositions);
            enemyPositions.Add(randomSpawnPos);

            EnemyData enemyToSpawn = sm.spawnEvents[sm.spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity);
            OnEnemySpawned(enemy, enemyToSpawn);
            entities.enemies.Add(enemy);

            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
            sm.spawnTimeIndex++;
            sm.spawnedThisFrame = true;
        }
    }

    private readonly List<Vector2> _gatheredEnemyPositions = new();

    private List<Vector2> GatherEnemyPositions() {
        _gatheredEnemyPositions.Clear();
        foreach (Enemy enemy in entities.enemies) {
            // Teleporting enemies are inactive and already in teleportingInPositions, dead ones have their collider disabled
            if (!enemy.gameObject.activeInHierarchy || !enemy.collider.enabled) continue;
            _gatheredEnemyPositions.Add(enemy.position);
        }
        _gatheredEnemyPositions.AddRange(curRaid.teleportingInPositions);
        return _gatheredEnemyPositions;
    }
}
