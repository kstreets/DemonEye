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
        public Collider2D enemySpacerCollider;
        public Vector2 separationOffset;
        public float fixedRandomSeparationAngle;
        public Vector2 separationVel;
        public EnemyData data;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public Limiter changeDirLimiter;
        public Tween attackDelay;
        public bool queuedForRangedAttack;
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
        CircleCollider2D spacerCircle = (CircleCollider2D)enemy.enemySpacerCollider;
        enemy.separationOffset = spacerCircle.transform.TransformPoint(spacerCircle.offset) - enemy.trans.position;
        enemy.fixedRandomSeparationAngle = Random.Range(0f, 360f);
        enemy.separationVel = Vector2.zero;
        enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
        enemy.rigidbody.mass = data.defaultMass;
        enemy.collider.enabled = true;
        enemy.attackDelay = Tween.Delay(data.projectileCooldownTime);
        enemy.queuedForRangedAttack = false;
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
        
        RaidSpawnPattern.Variant reteleportVariant = spawnManager.SpawningVariant;
        if (reteleportVariant != null && reteleportVariant.reteleportCount > 0 &&
            curRaid.data.reteleportLimitter.TimeHasPassed(reteleportVariant.reteleportInterval)) {
            ReteleportEnemies(reteleportVariant);
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
                enemy.prevAverageDistFromPlayer = enemy.curRunningSumDistFromPlayer / enemy.curRunningSumFrameCount;
                enemy.curRunningSumDistFromPlayer = 0f;
                enemy.curRunningSumFrameCount = 0;
            }

            switch (enemy.data.type) {
                case EnemyData.EnemyType.FireBoss:
                    FireBossLogic(enemy);
                    break;
                case EnemyData.EnemyType.SulfurBoss:
                    SulfurBossLogic(enemy);
                    break;
                case EnemyData.EnemyType.FireBender:
                    RangedLogic(enemy);
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
                if (deadEnemy == entities.boss) {
                    entities.boss = null;
                    foreach (Enemy otherEnemy in enemies) {
                        otherEnemy.health = 0;
                    }
                }
            
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

        UpdateRangedAttackManager();

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
    
    public class RangedAttackManager {
        public const float minTimeBetweenAttacks = 1.5f;
        public const float maxTimeBetweenAttacks = 3f;
        public float nextAttackAllowedTime;
        public readonly List<Enemy> queuedEnemies = new();
    }
    
    private RangedAttackManager rangedAttackManager = new();

    private void ClearRangedAttackManager() {
        foreach (Enemy enemy in rangedAttackManager.queuedEnemies) {
            enemy.queuedForRangedAttack = false;
        }
        rangedAttackManager.queuedEnemies.Clear();
        rangedAttackManager.nextAttackAllowedTime = 0f;
    }

    private void RangedLogic(Enemy enemy) {
        if (enemy.attackDelay.isAlive || enemy.queuedForRangedAttack) return;
        enemy.queuedForRangedAttack = true;
        rangedAttackManager.queuedEnemies.Add(enemy);
    }

    private void UpdateRangedAttackManager() {
        RangedAttackManager rm = rangedAttackManager;
        rm.queuedEnemies.RemoveAll(static enemy => enemy.health <= 0);

        if (Time.time < rm.nextAttackAllowedTime) return;

        for (int i = 0; i < rm.queuedEnemies.Count; i++) {
            Enemy enemy = rm.queuedEnemies[i];

            // Teleporting enemies keep their place in line until they're back
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (!TryPerformRangedAttack(enemy)) continue;

            enemy.queuedForRangedAttack = false;
            enemy.attackDelay = Tween.Delay(enemy.data.projectileCooldownTime);
            rm.queuedEnemies.RemoveAt(i);
            rm.nextAttackAllowedTime = Time.time + Random.Range(RangedAttackManager.minTimeBetweenAttacks, RangedAttackManager.maxTimeBetweenAttacks);
            return;
        }
    }

    private bool TryPerformRangedAttack(Enemy enemy) {
        switch (enemy.data.type) {
            case EnemyData.EnemyType.FireBender: {
                Vector2 spawnPos = enemy.position.Offset(y: 0.2f);
                if (!TryGetPlayerInterceptPoint(spawnPos, FireBoss.fireballSpeed, out Vector2 aimPoint)) return false;

                EnemyData data = enemy.data;
                Vector2 velocity = (aimPoint - spawnPos).normalized * FireBoss.fireballSpeed;
                SpawnProjectile(entityPools.smallFireballProjectile, spawnPos, velocity, data.projectileLifetime, enemy, flatDamage: data.damage, layermask: Masks.PlayerHurtMask);
                return true;
            }
            default:
                return false;
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
                gameInstance.PerformEnemyAttack(enemy); 
            });
        }
    }
    
    
    public class FireBoss : Enemy {
        public enum AttackTypes { Fireball, Meteor, DoubleFireball, MutliMeteor, Count }
        public AttackBrain<AttackTypes> attackBrain;
        
        public const float minAttackDelay = 0.65f;
        public const float maxAttackDelay = 1.75f;
        
        public const int fireballDamage = 10;
        public const float fireballSpeed = 1.5f;
        public const float fireballLifetime = 5f; 
        public Vector2 fireBallSpawnPos => position.Offset(y: 0.2f);
        
        public const int meteorDamage = 15;
        public const float meteorTravelTime = 0.6f;
        public const float meteorRadius = 0.2f;
        public const float meteorScatterRadiusPerMeteor = 0.3f;
        public const float delayBetweenMeteors = 0.12f;
        public const int multiMeteorCount = 4;
    }
    
    private Enemy InitFireBoss(EnemyData data, Vector2 spawnPos) { 
        FireBoss fireBoss = SpawnEntity<FireBoss>(data.enemyPrefab, spawnPos, Quaternion.identity);
        fireBoss.attackDelay = Tween.Delay(FireBoss.minAttackDelay, FireBoss.maxAttackDelay);
        
        fireBoss.attackBrain = new((int)FireBoss.AttackTypes.Count);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Fireball, level: 0, defaultProb: 1f, repeatedProbDecay: 0.25f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Meteor, level: 0, defaultProb: 0.5f, repeatedProbDecay: 0.1f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.DoubleFireball, level: 1, defaultProb: 1f, repeatedProbDecay: 0.25f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.MutliMeteor, level: 1, defaultProb: 0.5f, repeatedProbDecay: 0.1f);
        
        return fireBoss;
    }
    
    private void FireBossLogic(Enemy enemy) {
        if (enemy is not FireBoss fireBoss) return;
        
        bool canAtttack = !fireBoss.attackDelay.isAlive;
        if (!canAtttack) return;
        
        int attackLevel = (fireBoss.health / (float)fireBoss.data.health) > 0.5f ? 0 : 1;
        if (!fireBoss.attackBrain.TryChooseAttack(attackLevel, out FireBoss.AttackTypes attack)) return;

        fireBoss.attackDelay = Tween.Delay(FireBoss.minAttackDelay, FireBoss.maxAttackDelay);
        switch(attack) {
            case FireBoss.AttackTypes.Fireball:
                ShootFireball();
                break;
            case FireBoss.AttackTypes.Meteor:
                RainMeteors(1);
                break;
            case FireBoss.AttackTypes.DoubleFireball:
                ShootFireball();
                Tween.Delay(0.2f, ShootFireball);
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
            centeralAimPoint = ClampPositionToMap(player.position, centeralAimPoint, FireBoss.meteorRadius);

            using var _ = ListPool<Vector2>.Get(out var meteorPositions);
            meteorPositions.Add(centeralAimPoint); // First meteor is focused on the player

            // Grows with the count so the meteors are spread at a similar density no matter how many there are
            float scatterRadius = FireBoss.meteorScatterRadiusPerMeteor * Mathf.Sqrt(meteorCount);
            ScatterPositions(ref meteorPositions, meteorCount - 1, centeralAimPoint, scatterRadius, FireBoss.meteorRadius);

            for (int i = 0; i < meteorPositions.Count; i++) {
                Vector2 meteorPos = meteorPositions[i];
                // Tied to the boss so the remaining meteors are cancelled if it dies mid rain
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
                Entity explosion = SpawnEntityOneShot(entityPools.meteorExplosion, targetPos, Quaternion.identity);
                DealShapeDamage(explosion, FireBoss.meteorDamage, Masks.PlayerHurtMask);
                DestroyEntity(attackCircleEntity);
                DestroyEntity(meteorEntity);
            });
        }
    }
    
    private class SulfurBoss : Enemy {
        public enum AttackTypes { PoisonCloud, PoisonEruption, Count }
        public AttackBrain<AttackTypes> attackBrain;
        
        public const float minAttackDelay = 0.65f;
        public const float maxAttackDelay = 1.75f;
        
        public Vector2 projectileSpawnPos => position.Offset(y: 0.2f);
        
        public const int poisonCloudDamage = 10;
        public const float poisonCloudProjectileSpeed = 1.7f;
        public const float poisonCloudProjectileLifetime = 2f;
        public const float poisonCloudDetonateRadius = 0.15f;
        public const float poisonDamageMulti = 1.5f;
        public const float poisonDebuffDuration = 5f;
        
        public const float poisonEruptionSpeed = 1.5f;
        public const int poisonEruptionCount = 12;
        public const float delayBetweenPoisonLobs = 0.012f;
        public const float poisonEruptionScatterRadius = 1.5f;
        public const float poisonPoolSpacing = 0.12f;

        public const int poisonPoolDamage = 5;
        public const float poisonPoolDuration = 10f;
        public const float poisonPoolTickInterval = 0.5f;
        public const float poisonPoolWarningRadius = 0.25f;
    }
    
    private Enemy InitSulfurBoss(EnemyData data, Vector2 spawnPos) {
        SulfurBoss sulfurBoss = SpawnEntity<SulfurBoss>(data.enemyPrefab, spawnPos, Quaternion.identity);
        sulfurBoss.attackDelay = Tween.Delay(SulfurBoss.minAttackDelay, SulfurBoss.maxAttackDelay);
        
        sulfurBoss.attackBrain = new((int)SulfurBoss.AttackTypes.Count);
        sulfurBoss.attackBrain.AddAttack(SulfurBoss.AttackTypes.PoisonCloud, level: 0, defaultProb: 1f,  repeatedProbDecay: 0.1f);
        sulfurBoss.attackBrain.AddAttack(SulfurBoss.AttackTypes.PoisonEruption, level: 0, defaultProb: 0.35f, repeatedProbDecay: 1f, cooldown: 5f);
        
        return sulfurBoss;
    }
    
    private void SulfurBossLogic(Enemy enemy) {
        if (enemy is not SulfurBoss sulfurBoss) return;
        
        bool canAtttack = !sulfurBoss.attackDelay.isAlive;
        if (!canAtttack) return;
        
        // int attackLevel = (sulfurBoss.health / (float)sulfurBoss.data.health) > 0.5f ? 0 : 1;
        int attackLevel = 0;
        // Every attack is on cooldown, so try again next frame
        if (!sulfurBoss.attackBrain.TryChooseAttack(attackLevel, out SulfurBoss.AttackTypes attack)) return;

        sulfurBoss.attackDelay = Tween.Delay(SulfurBoss.minAttackDelay, SulfurBoss.maxAttackDelay);
        switch(attack) {
            case SulfurBoss.AttackTypes.PoisonCloud:
                ShootPoisonCloud();
                break;
            case SulfurBoss.AttackTypes.PoisonEruption:
                PoisonEruption(); 
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // ------------------------------
        // Nested Methods
        // ------------------------------
        
        void ShootPoisonCloud() {
            Vector2 spawnPos = sulfurBoss.projectileSpawnPos;
            if (!TryGetPlayerInterceptPoint(spawnPos, SulfurBoss.poisonCloudProjectileSpeed, out Vector2 aimPoint)) return;
            Vector2 velocity = (aimPoint - spawnPos).normalized * SulfurBoss.poisonCloudProjectileSpeed;
            Projectile projectile = SpawnProjectile(entityPools.gooProjectile, spawnPos, velocity, SulfurBoss.poisonCloudProjectileLifetime, sulfurBoss, layermask: 0);
            projectile.detonateNearPlayerRadius = SulfurBoss.poisonCloudDetonateRadius;
            projectile.onDetonate = static (detonatePos) => {
                Entity poisonCloud = gameInstance.SpawnEntityOneShot(gameInstance.entityPools.poisonCloud, detonatePos, Quaternion.identity);
                gameInstance.DealShapeDamage(poisonCloud, SulfurBoss.poisonCloudDamage, Masks.PlayerHurtMask);
                gameInstance.Delay(poisonCloud, DamageShapeDelay(poisonCloud), static cloud => {
                    if (cloud.damageShape.OverlapFirst(Masks.PlayerHurtMask)) {
                        gameInstance.PoisonPlayer(SulfurBoss.poisonDamageMulti, SulfurBoss.poisonDebuffDuration);
                    }
                });
            };
        }
        
        void PoisonEruption() {
            using var _ = ListPool<Vector2>.Get(out var poolPositions);
            ScatterPositions(ref poolPositions, SulfurBoss.poisonEruptionCount, player.position, SulfurBoss.poisonEruptionScatterRadius, SulfurBoss.poisonPoolSpacing);
            for (int i = 0; i < poolPositions.Count; i++) {
                Vector2 poolPos = poolPositions[i];
                // Tied to the boss so the remaining lobs are cancelled if it dies mid eruption
                Delay(sulfurBoss, i * SulfurBoss.delayBetweenPoisonLobs, _ => LobPoisonPool(poolPos));
            }
        }

        void LobPoisonPool(Vector2 targetPos) {
            Vector2 spawnPos = sulfurBoss.projectileSpawnPos;
            float gravity = config.gameplay.projectileGravity;
            Vector2 velocity = ProjectileLobVelocity(spawnPos, targetPos, gravity, SulfurBoss.poisonEruptionSpeed);
            SpawnProjectile(entityPools.gooProjectile, spawnPos, velocity, float.MaxValue, sulfurBoss, layermask: 0, gravity: gravity, floorY: targetPos.y);

            float travelTime = Vector2.Distance(spawnPos, targetPos) / SulfurBoss.poisonEruptionSpeed;
            Entity attackCircleEntity = SpawnEntity(entityPools.attackCircle, targetPos, Quaternion.identity);
            attackCircleEntity.attackCircle.Animate(SulfurBoss.poisonPoolWarningRadius, travelTime * 0.9f, startDelay: travelTime * 0.1f);

            Tween.Delay(travelTime, () => {
                DestroyEntity(attackCircleEntity);
                SpawnHazard(entityPools.poisonPool, targetPos, SulfurBoss.poisonPoolDamage, Masks.PlayerHurtMask, SulfurBoss.poisonPoolDuration, SulfurBoss.poisonPoolTickInterval);
            });
        }
        
    }
    
    private void ScatterPositions(ref List<Vector2> positions, int count, Vector2 center, float scatterRadius, float radius, int maxAttemptsPerPosition = 10) {
        float minSqrDist = (radius * 2f) * (radius * 2f);

        for (int i = 0; i < count; i++) {
            for (int attempt = 0; attempt < maxAttemptsPerPosition; attempt++) {
                Vector2 candidate = center + Random.insideUnitCircle * scatterRadius;
                candidate = ClampPositionToMap(center, candidate, radius);

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

    private struct SeparationAgent {
        public Enemy enemy;
        public Vector2 center;
        public float radius;
        public float mass;
        public int layer;
        public int nextEnemyInSameCellIndex;
    }

    private SeparationAgent[] separationAgents = new SeparationAgent[500];
    private readonly Dictionary<long, int> firstAgentIndexInSeparationCell = new();

    private void UpdateEnemySeparation(List<Enemy> enemies) {
        GameplayConfig gameplay = config.gameplay;

        if (separationAgents.Length < enemies.Count) {
            separationAgents = new SeparationAgent[Mathf.NextPowerOfTwo(enemies.Count)];
        }

        int agentCount = 0;
        float maxRadius = 0f;
        foreach (Enemy enemy in enemies) {
            if (!enemy.gameObject.activeInHierarchy || !enemy.collider.enabled) continue;

            float radius = enemy.data.separationRadius * gameplay.separationRadiusScale;
            maxRadius = Mathf.Max(maxRadius, radius);

            bool holdsGround = enemy.slow.HasValue || EnemyPlayingAttackAnimation(enemy);

            separationAgents[agentCount++] = new() {
                enemy = enemy,
                center = enemy.rigidbody.position + enemy.separationOffset,
                radius = radius,
                mass = holdsGround ? enemySlowedMass : enemy.rigidbody.mass,
                layer = enemy.data.separationLayer,
            };
        }
        if (agentCount <= 0 || maxRadius <= 0f) return;

        // Cells are as wide as the largest possible contact distance, so only the surrounding 3x3 cells need checking
        float cellSize = maxRadius * 2f;
        
        // Build a linked list of enemies for each cell
        firstAgentIndexInSeparationCell.Clear();
        for (int i = 0; i < agentCount; i++) {
            ref SeparationAgent agent = ref separationAgents[i];
            int cellX = Mathf.FloorToInt(agent.center.x / cellSize);
            int cellY = Mathf.FloorToInt(agent.center.y / cellSize);
            long key = CellKeyForCellPos(cellX, cellY);
            agent.nextEnemyInSameCellIndex = firstAgentIndexInSeparationCell.GetValueOrDefault(key, -1);
            firstAgentIndexInSeparationCell[key] = i;
        }

        float smoothing = 1f - Mathf.Exp(-gameplay.separationSmoothing * Time.fixedDeltaTime);
        for (int i = 0; i < agentCount; i++) {
            ref SeparationAgent curAgent = ref separationAgents[i];
            int cellX = Mathf.FloorToInt(curAgent.center.x / cellSize);
            int cellY = Mathf.FloorToInt(curAgent.center.y / cellSize);

            Vector2 push = Vector2.zero;
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    
                    long cellKey = CellKeyForCellPos(cellX + x, cellY + y);
                    if (!firstAgentIndexInSeparationCell.TryGetValue(cellKey, out int otherAgentIndex)) continue;
                    
                    while (otherAgentIndex != -1) {
                        
                        // Enemies only separate from others on the same separation layer
                        bool sameAgent = otherAgentIndex == i;
                        bool differentLayer = separationAgents[otherAgentIndex].layer != curAgent.layer;
                        if (sameAgent || differentLayer) {
                            otherAgentIndex = separationAgents[otherAgentIndex].nextEnemyInSameCellIndex;
                            continue;
                        }
                        
                        ref SeparationAgent otherAgent = ref separationAgents[otherAgentIndex];

                        float minSafeDist = curAgent.radius + otherAgent.radius;
                        Vector2 delta = curAgent.center - otherAgent.center;
                        float distBetweenAgents = delta.magnitude;
                        
                        if (distBetweenAgents < minSafeDist) {
                            Vector2 dir = delta.normalized;
                            
                            bool agentsOccupySamePos = distBetweenAgents <= 1e-5f;
                            if (agentsOccupySamePos) { 
                                // Each enemy has a consistent separation angle so there's no confusion on how to stop overlapping
                                Vector2 curDir = RotationVector(curAgent.enemy.fixedRandomSeparationAngle);
                                Vector2 otherDir = RotationVector(otherAgent.enemy.fixedRandomSeparationAngle);
                                dir = (curDir - otherDir).normalized;
                            }
                            
                            float overlap = 1f - (distBetweenAgents / minSafeDist);
                            float overlapStrength = overlap * overlap; // Squared so closer agents push harder and further push softer
                            float massScaler = (otherAgent.mass * 2f) / (curAgent.mass + otherAgent.mass);
                            push += dir * (overlapStrength * massScaler);
                        } 
                        
                        otherAgentIndex = separationAgents[otherAgentIndex].nextEnemyInSameCellIndex;
                    }
                }
            }

            Vector2 targetVel = Vector2.ClampMagnitude(push * gameplay.separationStrength, gameplay.maxSeparationSpeed);
            curAgent.enemy.separationVel = Vector2.Lerp(curAgent.enemy.separationVel, targetVel, smoothing);
        }
        
        static long CellKeyForCellPos(int x, int y) {
            return ((long)x << 32) | (uint)y;
        }
    }

    private void FixedUpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        UpdateEnemySeparation(enemies);
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
            
            // Since flow field is using predicted player position, close enemies might move away from the player.
            // If thats the case then we just make them move directly to the player.
            Vector2 dirToPlayer = (player.position - enemy.position).normalized;
            if (Vector2.Dot(targetDir, dirToPlayer) < 0f) {
                targetDir = dirToPlayer;
            }
        }
        if (targetDir == Vector3.zero) {
            targetDir = (player.position - enemy.position).normalized;
        }
            
        enemy.moveDir = Vector3.Lerp(enemy.moveDir, targetDir, enemy.flowFieldAcc * Time.fixedDeltaTime);
        Vector2 goalVel = enemy.moveDir * speed;

        // When the crowd is pushing back, stop walking into it so packed enemies settle instead of shaking.
        // Only the part of the movement heading into the crowd is removed, so they still slide around it.
        float separationSpeed = enemy.separationVel.magnitude;
        if (separationSpeed > 1e-4f) {
            Vector2 separationDir = enemy.separationVel / separationSpeed;
            float intoCrowd = Vector2.Dot(goalVel, separationDir);
            if (intoCrowd < 0f) {
                float blocked = Mathf.Clamp01(separationSpeed / config.gameplay.maxSeparationSpeed);
                goalVel -= separationDir * (intoCrowd * blocked);
            }
        }

        enemy.rigidbody.linearVelocity = goalVel + enemy.separationVel;
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
    
    private void PerformEnemyAttack(Enemy enemy) {
        if (enemy.data.type == EnemyData.EnemyType.Boomon) {
            const int projectileCount = 8;
            const float angleDeltaPerDrop = 360f /  projectileCount;
            const float randomRangePerDrop = angleDeltaPerDrop * 0.25f;

            for (int i = 0; i <  projectileCount; i++) {
                float randomAngle = (angleDeltaPerDrop * i) + Random.Range(-randomRangePerDrop, randomRangePerDrop);
                Vector3 velocity = RotationVector(randomAngle) * 0.7f;
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
    
    private void ReteleportEnemies(RaidSpawnPattern.Variant variant) {
        List<Enemy> enemies = entities.enemies;
        using var _ = ListPool<(Enemy, float)>.Get(out var reteleportCandidates);

        foreach (Enemy enemy in enemies) {
            if (!enemy.gameObject.activeInHierarchy || enemy == entities.boss) continue;
            
            const float enemyTeleportCooldown = 2f;
            if (Time.time - enemy.lastTeleportTime < enemyTeleportCooldown) continue;

            if (EnemyIsOffscreen(enemy)) {
                float distFromPlayer = Vector2.Distance(player.Center, enemy.Center);
                reteleportCandidates.Add((enemy, distFromPlayer));
            }
        }

        if (reteleportCandidates.Count <= 0) return;

        // Teleport top N by how long they've been waiting to reteleport
        reteleportCandidates.Sort(static (a, b) => {
            Enemy enemyA = a.Item1;
            Enemy enemyB = b.Item1;
            return enemyA.lastTeleportTime.CompareTo(enemyB.lastTeleportTime);
        });

        int teleportCount = Mathf.Min(reteleportCandidates.Count, variant.reteleportCount);

        for (int i = 0; i < teleportCount; i++) {
            (Enemy enemy, float distFromPlayer) = reteleportCandidates[i];
            Vector2Int repositionCellRange = variant.repositionCellRange;
            Vector2 spawnPos = curRaid.mapInstance.grid.GetSpawnPosition(
                player.position, repositionCellRange.x, repositionCellRange.y, predictPlayerPos: true, GatherEnemyPositions()
            );
            
            if (Vector2.Distance(spawnPos, player.position) < distFromPlayer) {
                TeleportEnemy(enemy, spawnPos, TeleportType.Reposition);
            }
        }
    }

    private bool EnemyIsOffscreen(Enemy enemy) {
        // Small margin so enemies right at the edge of the screen don't vanish while partially visible
        const float margin = 0.05f;
        // The width is forced to 16:9 based on the screen height, otherwise enemies on wide screens would rarely count as offscreen
        const float forcedAspect = 16f / 9f;
        Vector3 viewportPos = camera.main.WorldToViewportPoint(enemy.Center);
        // Horizontal distance from the center of the screen in units of screen height
        float xFromCenter = Mathf.Abs(viewportPos.x - 0.5f) * camera.main.aspect;
        bool offscreenX = xFromCenter > (forcedAspect * 0.5f) + margin;
        bool offscreenY = viewportPos.y < -margin || viewportPos.y > 1f + margin;
        return offscreenX || offscreenY;
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
        if (sm.spawnPattern.raidBoss == null) return;
        
        Vector2Int spawnCellRange = sm.CurPhase.spawnCellRange;
        Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, GatherEnemyPositions());

        EnemyData bossEnemy = sm.spawnPattern.raidBoss;
        Enemy enemy = bossEnemy.type switch {
            EnemyData.EnemyType.FireBoss => InitFireBoss(bossEnemy, randomSpawnPos),
            EnemyData.EnemyType.SulfurBoss => InitSulfurBoss(bossEnemy, randomSpawnPos),
            _ => throw new ArgumentOutOfRangeException(),
        };

        OnEnemySpawned(enemy, bossEnemy);
        entities.enemies.Add(enemy);
        entities.boss = enemy;
        TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
    }

    private void UpdateBossTrashSpawning(EnemySpawnManager sm) {
        if (!RaidBossIsAlive()) {
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

    private bool RaidBossIsAlive() {
        return entities.boss != null && entities.boss.health > 0;
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
