using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public partial class Game {
    
    private static class EnemyAnimations {
        public static readonly int walkSide = Animator.StringToHash("WalkSide");
        public static readonly int walkUp = Animator.StringToHash("WalkUp");
        public static readonly int walkDown = Animator.StringToHash("WalkDown");
        
        public static readonly int attackSide = Animator.StringToHash("AttackSide");
        public static readonly int attackUp = Animator.StringToHash("AttackUp");
        public static readonly int attackDown = Animator.StringToHash("AttackDown");
        
        public static readonly int spawn = Animator.StringToHash("Spawn");
        public static readonly int death = Animator.StringToHash("Death");
        
        public static readonly int shootSide = Animator.StringToHash("ShootSide");
        public static readonly int shootUp = Animator.StringToHash("ShootUp");
        public static readonly int shootDown = Animator.StringToHash("ShootDown");
        
        public static readonly int erupt = Animator.StringToHash("Erupt");
        
        public static readonly List<int> movementBlockingAnimations = new() {
            attackSide, attackUp, attackDown, shootSide, shootUp, shootDown,
            spawn, death, erupt,
        };
    }
    
    private const float enemySlowedMass = 1e6f;

    public class Enemy : Entity {
        public Action onDeathCallback;
        public float flowFieldAcc;
        public float lastTeleportTime;
        public float curDistFromPlayer;
        public CircleCollider2D worldCollider;
        public DamageShape sideAttackShape;
        public DamageShape upAttackShape;
        public DamageShape downAttackShape;
        public List<AttackSpawnPoint> attackSpawnPoints;
        public Vector2 separationOffset;
        public float fixedRandomSeparationAngle;
        public Vector2 separationVel;
        public EnemyData data;
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public CardinalDir pendingDir;
        public float pendingDirStartTime;
        public Tween attackDelay;
        public bool queuedForRangedAttack;
        public bool isBossTrash;
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
    
    private void OnEnemySpawned(Enemy enemy, object spawnData) {
        Assert.IsTrue(spawnData is EnemyData, "Enemies must be spawned with their EnemyData as the spawn data");

        entities.enemies.Add(enemy);
        
        EnemyData data = (EnemyData)spawnData;
        enemy.data = data;
        enemy.health = data.health;
        enemy.isBossTrash = false;
        
        if (data.animatorOverride) { 
            enemy.animator.runtimeAnimatorController = data.animatorOverride;
        }
        
        enemy.worldCollider = enemy.trans.GetChild(0).GetComponent<CircleCollider2D>();
        enemy.worldCollider.excludeLayers = data.excludeCollisionLayers;
        enemy.separationOffset = enemy.worldCollider.transform.TransformPoint(enemy.worldCollider.offset) - enemy.trans.position;
        enemy.fixedRandomSeparationAngle = Random.Range(0f, 360f);
        enemy.separationVel = Vector2.zero;
        
        enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
        enemy.rigidbody.mass = data.defaultMass;
        enemy.collider.enabled = true;
        enemy.attackDelay = Tween.Delay(data.projectileCooldownTime);
        enemy.queuedForRangedAttack = false;
        
        enemy.sideAttackShape = null;
        enemy.upAttackShape = null;
        enemy.downAttackShape = null;
        for (int i = 0; i < enemy.trans.childCount; i++) {
            if (enemy.trans.GetChild(i).TryGetComponent(out AttackSpawnPoint spawnPoint)) {
                enemy.attackSpawnPoints ??= ListPool<AttackSpawnPoint>.Get();
                enemy.attackSpawnPoints.Add(spawnPoint);
            }
            if (!enemy.trans.GetChild(i).TryGetComponent(out DamageShape shape)) continue;
            switch (shape.attackDir) {
                case DamageShape.AttackDir.Side: 
                    enemy.sideAttackShape = shape; 
                    break;
                case DamageShape.AttackDir.Up:   
                    enemy.upAttackShape = shape;   
                    break;
                case DamageShape.AttackDir.Down: 
                    enemy.downAttackShape = shape; 
                    break;
            }
        }
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
        
        if (deadEnemy.attackSpawnPoints != null) {
            foreach (AttackSpawnPoint spawnPoint in deadEnemy.attackSpawnPoints) {
                spawnPoint.pendingTween.Stop();
                spawnPoint.pendingCallback = null;
            }
            ListPool<AttackSpawnPoint>.Release(deadEnemy.attackSpawnPoints);
            deadEnemy.attackSpawnPoints = null;
        }
    }
    
    private void InitEnemies() {
        RegisterOnSpawnCallback<Enemy>(OnEnemySpawned);
        RegisterOnSpawnCallback<FireBoss>(InitFireBoss);
        RegisterOnSpawnCallback<SulfurBoss>(InitSulfurBoss);
    }
    
    private void UpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        if (enemies.Count <= 0) return;
        
        RaidSpawnPattern.Variant reteleportVariant = spawnManager.CurVariant;
        if (reteleportVariant != null && reteleportVariant.reteleportCount > 0 && curRaid.data.reteleportLimitter.TimeHasPassed(reteleportVariant.reteleportInterval)) {
            ReteleportEnemies(reteleportVariant);
        }
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            enemy.curDistFromPlayer = Vector2.Distance(player.Center, enemy.Center);

            switch (enemy.data.type) {
                case EnemyData.EnemyType.FireBoss:
                    FireBossLogic(enemy);
                    break;
                case EnemyData.EnemyType.SulfurBoss:
                    SulfurBossLogic(enemy);
                    break;
                case EnemyData.EnemyType.FireBender:
                    RangedEnemyLogic(enemy);
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
        List<Collider2D> overlapedEnemiesWithPlayer = Physics.OverlapCapsule(hurtColliderCenter, player.hurtCollider, Masks.EnemyWorldColliderMask);
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
            
            GameObject enemyGameobject = closestColToPlayer.transform.parent.gameObject;
            Enemy collidedWithEnemy = entities.lookup[enemyGameobject] as Enemy;
            Assert.IsNotNull(collidedWithEnemy, "Collider involved in player collision could not be mapped to a Enemy. Collider needs to child gameobject.");
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

    private void RangedEnemyLogic(Enemy enemy) {
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
                SpawnProjectile(entityPools.smallFireballProjectile, spawnPos, velocity, data.projectileLifetime, enemy, flatDamage: data.damage, layermask: Masks.PlayerProjectileHurtMask);
                return true;
            }
            default:
                return false;
        }
    }
    
    private void BasicEnemyLogic(Enemy enemy) {
        Vector2 dirToPlayer = (player.position - enemy.position).normalized;
        bool withinAttackDist = enemy.curDistFromPlayer < enemy.data.attackDistance;
        bool facingPlayer = Vector2.Dot(enemy.graphicalDir, dirToPlayer) >= 0.5f;

        // If the enemy can't move, then we assume it is currently attacking already
        if (!EnemyCanMove(enemy) || !withinAttackDist || !facingPlayer) return;
        
        switch (CardinalDirFromVector(enemy.graphicalDir)) {
            case CardinalDir.Right:
            case CardinalDir.Left:
                enemy.animator.Play(EnemyAnimations.attackSide);
                break;
            case CardinalDir.Up:
                enemy.animator.Play(EnemyAnimations.attackUp);
                break;
            case CardinalDir.Down:
                enemy.animator.Play(EnemyAnimations.attackDown);
                break;
        }

        switch (enemy.data.type) {
            case EnemyData.EnemyType.Boomon:
                PerformBoomonAttack(enemy);
                break;
            default:
                PerformMeleeAttack(enemy);
                break;
        }
    }
    
    private void PerformMeleeAttack(Enemy enemy) {
        DamageShape attackShape = EnemyAttackShape(enemy, out bool mirrored);
        Assert.IsNotNull(attackShape, "Enemy is missing or does not have an attack shape, we should not be here");
            
        Action<Vector2> onDamageFrame = null;
        if (enemy.data.type == EnemyData.EnemyType.Doughmon) {
            onDamageFrame = static (pos) => gameInstance.SpawnSlamSmoke(pos);
        }
        DealShapeDamage(enemy, attackShape, enemy.data.damage, Masks.PlayerMeleeHurtMask, mirrored, enemy.data.changeToCauseBleed, onDamageFrame, onHit: null);
    }
    
    private void SpawnSlamSmoke(Vector2 position) {
        Entity smokeSlam = SpawnEntity<Entity>(prefabs.slamSmoke, position, Quaternion.identity);
        DestroyEntity(smokeSlam, CurrentClipLength(smokeSlam.animator));
    }
    
    private void PerformBoomonAttack(Enemy enemy) {
        if (enemy.data.type != EnemyData.EnemyType.Boomon) return;
        
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
                flatDamage: enemy.data.damage, layermask: Masks.PlayerProjectileHurtMask
            );
        }
            
        enemy.health = 0;
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
    
    private void InitFireBoss(FireBoss fireBoss) { 
        fireBoss.attackDelay = Tween.Delay(FireBoss.minAttackDelay, FireBoss.maxAttackDelay);
        fireBoss.attackBrain = new((int)FireBoss.AttackTypes.Count);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Fireball, level: 0, defaultProb: 1f, repeatedProbDecay: 0.25f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Meteor, level: 0, defaultProb: 0.5f, repeatedProbDecay: 0.1f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.DoubleFireball, level: 1, defaultProb: 1f, repeatedProbDecay: 0.25f);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.MutliMeteor, level: 1, defaultProb: 0.5f, repeatedProbDecay: 0.1f);
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
            SpawnProjectile(entityPools.fireBossProjectile, spawnPos, velocity, FireBoss.fireballLifetime, fireBoss, flatDamage: FireBoss.fireballDamage, layermask: Masks.PlayerProjectileHurtMask);
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
                DealShapeDamage(explosion, FireBoss.meteorDamage, Masks.PlayerMeleeHurtMask);
                DestroyEntity(attackCircleEntity);
                DestroyEntity(meteorEntity);
            });
        }
    }
    
    private class SulfurBoss : Enemy {
        public enum AttackTypes { PoisonCloud, PoisonEruption, Count }
        public AttackBrain<AttackTypes> attackBrain;
        public AttackTypes? curAttackType;
        public readonly List<Vector2> eruptionTargets = new();

        public const float minAttackDelay = 1.2f;
        public const float maxAttackDelay = 3f;
        
        public const int poisonCloudDamage = 18;
        public const float poisonCloudProjectileSpeed = 1.7f;
        public const float poisonCloudProjectileLifetime = 2f;
        public const float poisonCloudDetonateRadius = 0.15f;
        public const float poisonDamageMulti = 1.5f;
        public const float poisonDebuffDuration = 5f;
        
        public const float poisonEruptionSpeed = 1.5f;
        public const int poisonEruptionCount = 30;
        public const float delayBetweenPoisonLobs = 0.012f;
        public const float poisonEruptionScatterRadius = 2.5f;
        public const float poisonEruptionJitter = 0.12f;

        public const int poisonPoolDamage = 10;
        public const float poisonPoolDuration = 10f;
        public const float poisonPoolTickInterval = 0.35f;
        public const float poisonPoolWarningRadius = 0.2f;
        public const float poisonPoolWarningTime = 0.4f;
    }
    
    private void InitSulfurBoss(SulfurBoss sulfurBoss) {
        sulfurBoss.attackDelay = Tween.Delay(SulfurBoss.minAttackDelay, SulfurBoss.maxAttackDelay);
        sulfurBoss.attackBrain = new((int)SulfurBoss.AttackTypes.Count);
        sulfurBoss.attackBrain.AddAttack(SulfurBoss.AttackTypes.PoisonCloud, level: 0, defaultProb: 1f,  repeatedProbDecay: 0.1f);
        sulfurBoss.attackBrain.AddAttack(SulfurBoss.AttackTypes.PoisonEruption, level: 0, defaultProb: 0.35f, repeatedProbDecay: 1f, cooldown: 5f);
        sulfurBoss.animator.Play(EnemyAnimations.spawn);
    }
    
    private void SulfurBossLogic(Enemy enemy) {
        if (enemy is not SulfurBoss sulfurBoss) return;
        
        if (sulfurBoss.curAttackType.TryGetValue(out SulfurBoss.AttackTypes curAttack)) {
            if (curAttack == SulfurBoss.AttackTypes.PoisonCloud) {
                if (!TryGetPlayerInterceptPoint(sulfurBoss.position, SulfurBoss.poisonCloudProjectileSpeed, out Vector2 aimPoint)) {
                    aimPoint = player.position;
                }
                FaceDirectionWhileShooting(sulfurBoss, CardinalDirFromVector(aimPoint - (Vector2)sulfurBoss.position));
            }
        }
        
        bool canAtttack = !sulfurBoss.attackDelay.isAlive;
        if (!canAtttack) return;
        
        int attackLevel = 0;
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
            int animHash = ShootAnimation(CardinalDirFromVector(sulfurBoss.graphicalDir));
            sulfurBoss.animator.Play(animHash);
            sulfurBoss.curAttackType = SulfurBoss.AttackTypes.PoisonCloud;

            WaitForAttackSpawnFrame(sulfurBoss, animHash, static (boss, spawnPos) => {
                var g = gameInstance;
                ((SulfurBoss)boss).curAttackType = null;
                if (!g.TryGetPlayerInterceptPoint(spawnPos, SulfurBoss.poisonCloudProjectileSpeed, out Vector2 aimPoint)) return;

                Vector2 velocity = (aimPoint - spawnPos).normalized * SulfurBoss.poisonCloudProjectileSpeed;
                Projectile projectile = g.SpawnProjectile(g.entityPools.gooProjectile, spawnPos, velocity, SulfurBoss.poisonCloudProjectileLifetime, boss, layermask: 0);
                projectile.detonateNearPlayerRadius = SulfurBoss.poisonCloudDetonateRadius;
                
                projectile.onDetonate = static (detonatePos) => {
                    Entity poisonCloud = gameInstance.SpawnEntityOneShot(gameInstance.entityPools.poisonCloud, detonatePos, Quaternion.identity);
                    gameInstance.DealShapeDamage(poisonCloud, SulfurBoss.poisonCloudDamage, Masks.PlayerMeleeHurtMask, onHit: static (hit) => {
                        if (hit is Player) {
                            gameInstance.PoisonPlayer(SulfurBoss.poisonDamageMulti, SulfurBoss.poisonDebuffDuration);
                        }
                    });
                };
            });
        }
        
        void PoisonEruption() {
            sulfurBoss.animator.Play(EnemyAnimations.erupt);
            sulfurBoss.animator.Update(0f);

            AttackSpawnPoint point = FindAttackSpawnPoint(sulfurBoss, EnemyAnimations.erupt);
            bool mirrored = point && point.mirrorForLeft && CardinalDirFromVector(sulfurBoss.graphicalDir) == CardinalDir.Left;
            Vector2 estimatedSpawnPos = point ? point.GetPosition(mirrored) : sulfurBoss.position;
            float lobSpawnDelay = point ? DamageFrameDelay(sulfurBoss.animator, point.spawnFrame) : 0f;

            // Centered on where the player will be when the pools land, not when the boss starts erupting
            Vector2 playerPos = player.hurtCollider.bounds.center;
            float landTime = lobSpawnDelay + Vector2.Distance(estimatedSpawnPos, playerPos) / SulfurBoss.poisonEruptionSpeed;
            Vector2 predictedPlayerPos = ClampPositionToMap(playerPos, PredictedPlayerPosition(landTime), SulfurBoss.poisonPoolWarningRadius);

            List<Vector2> targets = sulfurBoss.eruptionTargets;
            targets.Clear();
            SpreadPositionsInCircle(targets, SulfurBoss.poisonEruptionCount, predictedPlayerPos, SulfurBoss.poisonEruptionScatterRadius,
                SulfurBoss.poisonEruptionJitter, SulfurBoss.poisonPoolWarningRadius);

            WaitForAttackSpawnFrame(sulfurBoss, EnemyAnimations.erupt, static (boss, spawnPos) => {
                List<Vector2> targets = ((SulfurBoss)boss).eruptionTargets;
                for (int i = 0; i < targets.Count; i++) {
                    Vector2 poolPos = targets[i];
                    // Tied to the boss so the remaining lobs are cancelled if it dies mid eruption
                    Delay(boss, i * SulfurBoss.delayBetweenPoisonLobs, (_) => LobPoisonPool(boss, spawnPos, poolPos));
                }
            });

            // Each circle finishes filling right as its pool is lobbed, and stays until the pool lands
            for (int i = 0; i < targets.Count; i++) {
                Vector2 targetPos = targets[i];
                float lobTime = (i * SulfurBoss.delayBetweenPoisonLobs) + lobSpawnDelay;
                float travelTime = Vector2.Distance(estimatedSpawnPos, targetPos) / SulfurBoss.poisonEruptionSpeed;
                float warnStartTime = Mathf.Max(0f, lobTime + travelTime - SulfurBoss.poisonPoolWarningTime);
                
                const float attackCircleAnimInTime = SulfurBoss.poisonPoolWarningTime * 0.9f;
                Entity attackCircleEntity = SpawnEntity(entityPools.attackCircle, targetPos, Quaternion.identity);
                attackCircleEntity.attackCircle.Animate(SulfurBoss.poisonPoolWarningRadius, attackCircleAnimInTime, startDelay: warnStartTime);
                DestroyEntity(attackCircleEntity, lobTime + travelTime);
            }
        }

        static void LobPoisonPool(Enemy boss, Vector2 spawnPos, Vector2 targetPos) {
            var g = gameInstance;
            float travelTime = Vector2.Distance(spawnPos, targetPos) / SulfurBoss.poisonEruptionSpeed;
            float gravity = g.config.gameplay.projectileGravity;
            Vector2 velocity = ProjectileLobVelocity(spawnPos, targetPos, gravity, SulfurBoss.poisonEruptionSpeed);
            g.SpawnProjectile(g.entityPools.gooProjectile, spawnPos, velocity, float.MaxValue, boss, layermask: 0, gravity: gravity, lobTarget: targetPos, lobTime: travelTime);
            g.SpawnHazard(g.entityPools.poisonPool, targetPos, SulfurBoss.poisonPoolDamage, Masks.PlayerMeleeHurtMask,
                SulfurBoss.poisonPoolDuration, SulfurBoss.poisonPoolTickInterval, spawnDelay: travelTime);
        }
        
    }
    
    private const float goldenAngle = 137.50776f;

    // Sunflower spiral so the positions cover the circle evenly with no big gaps, then jittered so it doesn't look like a pattern.
    // Ordered outside in, the first position is the outer edge and the last one is the exact center
    private static void SpreadPositionsInCircle(List<Vector2> positions, int count, Vector2 center, float circleRadius, float jitter, float bufferRadius) {
        float startAngle = Random.Range(0f, 360f);
        for (int i = count - 1; i >= 0; i--) {
            if (i == 0) {
                positions.Add(center);
                break;
            }
            float dist = circleRadius * Mathf.Sqrt(i / (float)(count - 1));
            Vector2 candidate = center + (Vector2)RotationVector(startAngle + i * goldenAngle) * dist + Random.insideUnitCircle * jitter;
            positions.Add(ClampPositionToMap(center, candidate, bufferRadius));
        }
    }

    private static void ScatterPositions(ref List<Vector2> positions, int count, Vector2 center, float scatterRadius, float radius, int maxAttemptsPerPosition = 10) {
        float minDist = radius * 2f;
        for (int i = 0; i < count; i++) {
            for (int attempt = 0; attempt < maxAttemptsPerPosition; attempt++) {
                Vector2 candidate = center + Random.insideUnitCircle * scatterRadius;
                candidate = ClampPositionToMap(center, candidate, radius);

                bool overlapsPlacedPos = false;
                foreach (Vector2 placedPos in positions) {
                    if (Vector2.Distance(placedPos, candidate) < minDist) {
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

            bool holdsGround = enemy.slow.HasValue || !EnemyCanMove(enemy);

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
            bool canMove = EnemyCanMove(enemy);
            MoveEnemy(enemy, canMove);
            OrientEnemy(enemy, canMove);
        }
    }
    
    private void MoveEnemy(Enemy enemy, bool canMove) {
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

        if (!canMove) {
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
    
    private void OrientEnemy(Enemy enemy, bool canMove) {
        if (!canMove) return;

        CardinalDir moveDir = CardinalDirFromVector(enemy.moveDir);
        if (moveDir != enemy.pendingDir) {
            enemy.pendingDir = moveDir;
            enemy.pendingDirStartTime = Time.time;
        }

        // Only turn once the enemy has kept moving the same way for a bit, so it doesn't flicker between directions
        const float enemyTurnHoldTime = 0.05f;
        bool hasFacing = enemy.graphicalDir != Vector2.zero;
        bool heldLongEnough = Time.time - enemy.pendingDirStartTime >= enemyTurnHoldTime;
        CardinalDir facingDir = (!hasFacing || heldLongEnough) ? moveDir : CardinalDirFromVector(enemy.graphicalDir);

        switch (facingDir) {
            case CardinalDir.Right:
                enemy.animator.PlayIfNotAlready(EnemyAnimations.walkSide);
                enemy.spriteRenderer.flipX = false;
                enemy.graphicalDir = Vector2.right;
                break;
            case CardinalDir.Left:
                enemy.animator.PlayIfNotAlready(EnemyAnimations.walkSide);
                enemy.spriteRenderer.flipX = true;
                enemy.graphicalDir = Vector2.left;
                break;
            case CardinalDir.Up:
                enemy.animator.PlayIfNotAlready(EnemyAnimations.walkUp);
                enemy.spriteRenderer.flipX = false;
                enemy.graphicalDir = Vector2.up;
                break;
            case CardinalDir.Down:
                enemy.animator.PlayIfNotAlready(EnemyAnimations.walkDown);
                enemy.spriteRenderer.flipX = false;
                enemy.graphicalDir = Vector2.down;
                break;
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

    private bool EnemyCanMove(Enemy enemy) {
        var stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
        int animStateHash = stateInfo.shortNameHash;
        
        bool clipIsFinished = stateInfo.normalizedTime >= 1f;
        foreach (int blockingAnim in EnemyAnimations.movementBlockingAnimations) {
            if (animStateHash == blockingAnim) {
                return clipIsFinished;
            }
        }
        
        return true;
    }
    
    private DamageShape EnemyAttackShape(Enemy enemy, out bool mirrored) {
        CardinalDir dir = CardinalDirFromVector(enemy.graphicalDir);
        DamageShape shape = dir switch {
            CardinalDir.Right or CardinalDir.Left => enemy.sideAttackShape,
            CardinalDir.Up                        => enemy.upAttackShape,
            _                                     => enemy.downAttackShape,
        };
        mirrored = dir == CardinalDir.Left && shape && shape.mirrorForLeft;
        return shape;
    }

    private static int ShootAnimation(CardinalDir dir) {
        return dir switch {
            CardinalDir.Up   => EnemyAnimations.shootUp,
            CardinalDir.Down => EnemyAnimations.shootDown,
            _                => EnemyAnimations.shootSide,
        };
    }

    private void FaceDirectionWhileShooting(Enemy enemy, CardinalDir dir) {
        enemy.graphicalDir = dir switch {
            CardinalDir.Right => Vector2.right,
            CardinalDir.Left  => Vector2.left,
            CardinalDir.Up    => Vector2.up,
            _                 => Vector2.down,
        };
        enemy.spriteRenderer.flipX = dir == CardinalDir.Left;

        int animHash = ShootAnimation(dir);
        AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != animHash) {
            enemy.animator.Play(animHash, 0, stateInfo.normalizedTime);
        }
    }

    private static AttackSpawnPoint FindAttackSpawnPoint(Enemy enemy, int stateHash) {
        if (enemy.attackSpawnPoints == null) return null;
        foreach (AttackSpawnPoint spawnPoint in enemy.attackSpawnPoints) {
            if (spawnPoint.StateHash == stateHash) return spawnPoint;
        }
        return null;
    }

    // The delay comes from the starting state's spawn point, but the position comes from whatever state is playing
    // when it fires, since attacks like the Sulfur Boss shot can turn to a different direction's animation mid wind-up
    private Tween WaitForAttackSpawnFrame(Enemy enemy, int stateHash, Action<Enemy, Vector2> onSpawnFrame) {
        AttackSpawnPoint point = FindAttackSpawnPoint(enemy, stateHash);
        if (!point) {
            onSpawnFrame(enemy, enemy.position);
            return default;
        }

        // Applies the Play from this frame so the delay is measured on the clip that's about to play
        if (enemy.animator.isActiveAndEnabled) {
            enemy.animator.Update(0f);
        }

        point.pendingCallback = onSpawnFrame;
        point.pendingTween.Stop();
        
        point.pendingTween = Delay(enemy, DamageFrameDelay(enemy.animator, point.spawnFrame), static (enemy) => {
            if (enemy.attackSpawnPoints == null) return;
            AttackSpawnPoint point = null;
            foreach (AttackSpawnPoint spawnPoint in enemy.attackSpawnPoints) {
                if (spawnPoint.pendingCallback == null) continue;
                point = spawnPoint;
                break;
            }
            if (!point) return;

            Action<Enemy, Vector2> onSpawnFrame = point.pendingCallback;
            point.pendingCallback = null;
            if (enemy.health <= 0) return;

            int curStateHash = enemy.animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            AttackSpawnPoint curPoint = FindAttackSpawnPoint(enemy, curStateHash);
            if (!curPoint) {
                curPoint = point;
            }
            bool mirrored = curPoint.mirrorForLeft && CardinalDirFromVector(enemy.graphicalDir) == CardinalDir.Left;
            onSpawnFrame(enemy, curPoint.GetPosition(mirrored));
        });
        return point.pendingTween;
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
        public float nextTrashRefillTime; // In time in the boss phase

        // The swarm spawns on its own track so the waves keep running underneath it
        public const float swarmWarningTime = 30f;
        public SwarmState swarmState;
        public int swarmWaveIndex;
        public float swarmWarnAtTimeInWave;
        public float swarmCountdown;
        public float timeInSwarm;
        public bool swarmStartedThisFrame;
        public bool swarmWarningStartedThisFrame;
        public RaidSpawnPattern.Variant swarmVariant;
        public readonly List<(float time, EnemyData enemy)> swarmSpawnEvents = new();
        public int swarmSpawnTimeIndex;
        public bool SwarmPending => swarmState != SwarmState.None;

        // The pattern's phase pools, followed by its boss phase when it has a raid boss
        public readonly List<RaidSpawnPattern.Phase> phases = new();
        public bool OnBossWave => CurPhase is RaidSpawnPattern.BossPhase;

        public float extraMaxDuration;
        public float extraEarlyDelay;
        public float CurMaxDuration => CurVariant.maxDuration + extraMaxDuration;
        public float CurEarlyDelay => CurVariant.startNextPhaseEarlyDelay + extraEarlyDelay;

        public int CurWaveNumber => Mathf.Clamp(curPhaseIndex + 1, 0, phases.Count);
        public int TotalWaveCount => phases.Count;
        public float TotalCompletion => CurWaveNumber / (float)TotalWaveCount;

        public bool BeforeLastWave => curPhaseIndex < phases.Count - 1;
        public bool OnLastWave => curPhaseIndex == phases.Count - 1;
        public bool FinishedSpawningThisWave => !spawnEvents.IndexInRange(spawnTimeIndex);
        public bool FinishedSpawningForRaid => OnLastWave && FinishedSpawningThisWave;

        public int CurVarientIndex => chosenVarientIndices.IndexInRange(curPhaseIndex) ? chosenVarientIndices[curPhaseIndex] : -1;
        public RaidSpawnPattern.Phase CurPhase => phases.SafeIndex(curPhaseIndex);
        public RaidSpawnPattern.PhasePool CurPhasePool => CurPhase as RaidSpawnPattern.PhasePool;
        public RaidSpawnPattern.Variant CurVariant => CurPhasePool?.variants.SafeIndex(CurVarientIndex);

        public Vector2Int CurSpawnCellRange => CurVariant.spawnCellRange;
    }

    public enum SwarmState { None, Scheduled, Warning, Spawning }

    [NonSerialized] private EnemySpawnManager spawnManager = new();

    private void InitSpawnManager(RaidSpawnPattern pattern) {
        spawnManager.swarmState = SwarmState.None;
        spawnManager.swarmStartedThisFrame = false;
        spawnManager.swarmWarningStartedThisFrame = false;
        if (curRaid.mapInstance.isBossMap) return;

        spawnManager.spawnEvents.Clear();
        spawnManager.spawnedThisFrame = false;
        spawnManager.spawnPattern = pattern;
        spawnManager.curPhaseIndex = -1;
        spawnManager.timeInCurPhase = 0f;
        spawnManager.nextTrashRefillTime = 0f;
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

        spawnManager.phases.Clear();
        spawnManager.phases.AddRange(pattern.phasePools);
        if (pattern.bossPhase.raidBoss != null) {
            spawnManager.phases.Add(pattern.bossPhase);
        }

        spawnManager.chosenVarientIndices.Clear();
        foreach (RaidSpawnPattern.Phase phase in spawnManager.phases) {
            int randomVarientIndex = phase is RaidSpawnPattern.PhasePool pool ? Random.Range(0, pool.variants.Count) : -1;
            spawnManager.chosenVarientIndices.Add(randomVarientIndex);
        }

        RollSwarm(spawnManager, pattern);
    }

    // Picks the wave the swarm warning shows up in, and when during that wave's spawning it does
    private void RollSwarm(EnemySpawnManager sm, RaidSpawnPattern pattern) {
        RaidSpawnPattern.Swarm swarm = pattern.swarm;
        int waveCount = pattern.phasePools.Count;
        if (swarm.pool.variants.Count <= 0 || waveCount <= 0 || !RollProbability(swarm.chance)) return;

        int minWave = Mathf.Clamp(swarm.waveRange.x, 1, waveCount);
        int maxWave = Mathf.Clamp(swarm.waveRange.y, minWave, waveCount);
        sm.swarmWaveIndex = Random.Range(minWave, maxWave + 1) - 1;

        RaidSpawnPattern.Variant waveVariant = pattern.phasePools[sm.swarmWaveIndex].variants[sm.chosenVarientIndices[sm.swarmWaveIndex]];
        sm.swarmWarnAtTimeInWave = Random.Range(0f, waveVariant.spawnDuration);
        sm.swarmVariant = swarm.pool.variants[Random.Range(0, swarm.pool.variants.Count)];
        sm.swarmState = SwarmState.Scheduled;
    }
    
    private Limiter spawnLimiterForEnemyBatching;

    private void UpdateSpawnManager() {
        if (curRaid.mapInstance.isBossMap) return;

        EnemySpawnManager sm = spawnManager;
        sm.spawnedThisFrame = false;
        sm.waveStartedThisFrame = false;

        float prevTimeInCurPhase = sm.timeInCurPhase;
        sm.timeInCurPhase += Time.deltaTime;

        if (sm.CurPhase is RaidSpawnPattern.BossPhase bossPhase) {
            UpdateBossWave(sm, bossPhase);
        }
        else {
            UpdateWaveProgression(sm, prevTimeInCurPhase);
        }

        SpawnDueEnemies(sm);
        UpdateSwarm(sm);
    }

    private Limiter swarmSpawnLimiter;

    private void UpdateSwarm(EnemySpawnManager sm) {
        sm.swarmStartedThisFrame = false;
        sm.swarmWarningStartedThisFrame = false;
        switch (sm.swarmState) {
            case SwarmState.Scheduled: {
                bool reachedWarnTime = sm.curPhaseIndex == sm.swarmWaveIndex && sm.timeInCurPhase >= sm.swarmWarnAtTimeInWave;
                if (reachedWarnTime || sm.curPhaseIndex > sm.swarmWaveIndex) {
                    sm.swarmCountdown = EnemySpawnManager.swarmWarningTime;
                    sm.swarmState = SwarmState.Warning;
                    sm.swarmWarningStartedThisFrame = true;
                }
                break;
            }
            case SwarmState.Warning: {
                sm.swarmCountdown -= Time.deltaTime;
                if (sm.swarmCountdown <= 0f) {
                    LayOutSpawnEvents(sm, sm.swarmVariant, sm.swarmSpawnEvents);
                    sm.swarmSpawnTimeIndex = 0;
                    sm.timeInSwarm = 0f;
                    swarmSpawnLimiter.MakeCurrent();
                    sm.swarmStartedThisFrame = true;
                    sm.swarmState = SwarmState.Spawning;
                }
                break;
            }
            case SwarmState.Spawning: {
                sm.timeInSwarm += Time.deltaTime;
                const float batchTime = 1f;
                if (swarmSpawnLimiter.TimeHasPassed(batchTime)) {
                    SpawnDueEvents(sm, sm.swarmSpawnEvents, ref sm.swarmSpawnTimeIndex, sm.timeInSwarm, sm.swarmVariant.spawnCellRange, false);
                }
                if (!sm.swarmSpawnEvents.IndexInRange(sm.swarmSpawnTimeIndex)) {
                    sm.swarmState = SwarmState.None;
                }
                break;
            }
        }
    }

    private void UpdateWaveProgression(EnemySpawnManager sm, float prevTimeInCurPhase) {
        bool afterFirstWave = sm.curPhaseIndex >= 0;
        float waveDuration = afterFirstWave ? sm.CurMaxDuration : sm.spawnPattern.timeBeforeFirstPhase;
        bool timeLimitExceeded = sm.timeInCurPhase >= waveDuration;
        bool justHitTimeLimit = prevTimeInCurPhase < waveDuration && timeLimitExceeded;
        int curEnemyCount = entities.enemies.Count;
        bool waveCleared = afterFirstWave && sm.FinishedSpawningThisWave && curEnemyCount <= 0;

        if (afterFirstWave) {
            AddTimeToNextWaveDelay(sm, justHitTimeLimit, curEnemyCount);
            if (waveCleared || timeLimitExceeded) {
                sm.startNextWaveDelay -= Time.deltaTime;
            }
        }

        bool startNextWave = sm.startNextWaveDelay <= 0f && (timeLimitExceeded || waveCleared);
        if (startNextWave && !sm.OnLastWave) {
            sm.curPhaseIndex++;
            sm.waveStartedThisFrame = true;
            switch (sm.CurPhase) {
                case RaidSpawnPattern.BossPhase bossPhase:
                    sm.timeInCurPhase = 0f;
                    QueueBossTrashRefill(sm, bossPhase);
                    SpawnRaidBoss(bossPhase, sm.CurVariant);
                    break;
                case RaidSpawnPattern.PhasePool:
                    sm.startNextWaveDelay = sm.CurEarlyDelay;
                    BeginSpawningVariant(sm, sm.CurVariant);
                    break;
            }
        }
    }
    
    private void AddTimeToNextWaveDelay(EnemySpawnManager sm, bool justHitTimeLimit, int curEnemyCount) {
        // Hitting the time limit with enemies still alive gives a buffer before the next wave, scaled by how much of the wave is left
        if (justHitTimeLimit && curEnemyCount > 0) {
            int totalEnemiesThisWave = 0;
            foreach (RaidSpawnPattern.EnemyBatch batch in sm.CurVariant.enemyBatches) {
                totalEnemiesThisWave += batch.enemyCount;
            }
            float remainingOfTotalPercentage = curEnemyCount / (float)totalEnemiesThisWave;
            float addedTime = Mathf.Lerp(1f, 4f, remainingOfTotalPercentage);
            // The early delay is only for clearing the wave, which didn't happen, so the buffer replaces it
            sm.startNextWaveDelay += addedTime - sm.CurEarlyDelay;
        }

        bool justKilledLastEnemiesInWave = sm.FinishedSpawningThisWave && sm.prevEnemyCount > 0 && curEnemyCount <= 0;
        sm.prevEnemyCount = curEnemyCount;
        
        if (justKilledLastEnemiesInWave) {
            // If a wave's max duration is about the same time as the spawn duration, we can't assume the player struggled
            // because they would of had none or very little time to kill the last spawned enemies
            const float struggleEligiblePercentage = 1.2f;
            bool waveIsStruggleEligible = sm.CurMaxDuration >= (sm.CurVariant.spawnDuration * struggleEligiblePercentage);
            if (waveIsStruggleEligible) {
                const float struggleThresholdPercentage = 0.8f;
                float stuggleThreshold = sm.CurMaxDuration * struggleThresholdPercentage;
                if (sm.timeInCurPhase >= stuggleThreshold) {
                    float exceededTime = sm.timeInCurPhase - stuggleThreshold;
                    float maxPossibleExceededTime = sm.CurMaxDuration * (1f - struggleThresholdPercentage);
                    float addedTime = Mathf.Lerp(1f, 6f, exceededTime / maxPossibleExceededTime);
                    sm.startNextWaveDelay += addedTime;
                }
            }
        }

        bool notFinishedWithWave = !sm.FinishedSpawningThisWave;
        const float maxTimeCanAddThroughActions = 13f;
        bool alreadyAddedTheMaxAmountOfTime = sm.timeAddedThroughActions >= maxTimeCanAddThroughActions;
        if (notFinishedWithWave || alreadyAddedTheMaxAmountOfTime) return;
        
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

    private void UpdateBossWave(EnemySpawnManager sm, RaidSpawnPattern.BossPhase bossPhase) {
        bool bossAlive = entities.boss != null && entities.boss.health > 0;
        if (!bossAlive || !sm.FinishedSpawningThisWave || sm.timeInCurPhase < sm.nextTrashRefillTime) return;

        int totalTrash = 0;
        foreach (RaidSpawnPattern.EnemyBatch batch in sm.CurVariant.enemyBatches) {
            totalTrash += batch.enemyCount;
        }
        
        int trashRefillThreshold = Mathf.RoundToInt(totalTrash * bossPhase.trashRefillAtPercentage);
        if (AliveTrashCount(null) <= trashRefillThreshold) {
            QueueBossTrashRefill(sm, bossPhase);
        }
    }

    private void QueueBossTrashRefill(EnemySpawnManager sm, RaidSpawnPattern.BossPhase bossPhase) {
        sm.nextTrashRefillTime = sm.timeInCurPhase + bossPhase.minTrashRefillDelay;
        sm.spawnTimeIndex = 0;
        LayOutSpawnEvents(sm, sm.CurVariant, sm.spawnEvents, isBossTrash: true);
        spawnLimiterForEnemyBatching.MakeCurrent();
    }

    private int AliveTrashCount(EnemyData ofType) {
        int count = 0;
        foreach (Enemy enemy in entities.enemies) {
            if (!enemy.isBossTrash || enemy.health <= 0) continue;
            if (ofType != null && enemy.data != ofType) continue;
            count++;
        }
        return count;
    }

    private void SpawnRaidBoss(RaidSpawnPattern.BossPhase bossPhase, RaidSpawnPattern.Variant variant) {
        Vector2Int spawnCellRange = variant.spawnCellRange;
        Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, GatherEnemyPositions());

        EnemyData bossEnemy = bossPhase.raidBoss;
        Enemy enemy = bossEnemy.type switch {
            EnemyData.EnemyType.FireBoss => SpawnEntity<FireBoss>(bossEnemy.enemyPrefab, randomSpawnPos, Quaternion.identity, data: bossEnemy),
            EnemyData.EnemyType.SulfurBoss => SpawnEntity<SulfurBoss>(bossEnemy.enemyPrefab, randomSpawnPos, Quaternion.identity, data: bossEnemy),
            _ => throw new ArgumentOutOfRangeException(),
        };
        entities.boss = enemy;
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
        LayOutSpawnEvents(sm, variant, sm.spawnEvents);
        spawnLimiterForEnemyBatching.MakeCurrent();
    }

    private void LayOutSpawnEvents(EnemySpawnManager sm, RaidSpawnPattern.Variant variant, List<(float time, EnemyData enemy)> spawnEvents, bool isBossTrash = false) {
        float startTime = isBossTrash ? sm.timeInCurPhase : 0f;
        float totalWeight = 0f;
        for (int i = 0; i < EnemySpawnManager.prefixedSumResolution; i++) {
            float sliceIndex = i / (float)(EnemySpawnManager.prefixedSumResolution - 1);
            float weight = Mathf.Clamp01(variant.spawnRateCurve.Evaluate(sliceIndex));
            totalWeight += weight;
            sm.prefixedSums[i] = totalWeight;
        }

        spawnEvents.Clear();
        foreach (RaidSpawnPattern.EnemyBatch batch in variant.enemyBatches) {
            int enemySpawnCount = batch.enemyCount;
            if (isBossTrash) {
                enemySpawnCount = Mathf.Max(0, batch.enemyCount - AliveTrashCount(batch.enemyData));
            }
            for (int i = 0; i < enemySpawnCount; i++) {
                float targetWeight = (i / (float)(enemySpawnCount - 1)) * totalWeight;

                // Find the corresponding time using linear search
                int weightIndex = 0;
                while (weightIndex < EnemySpawnManager.prefixedSumResolution && sm.prefixedSums[weightIndex] < targetWeight) {
                    weightIndex++;
                }

                float normalizedTime = weightIndex / (float)(EnemySpawnManager.prefixedSumResolution - 1);
                spawnEvents.Add((startTime + normalizedTime * variant.spawnDuration, batch.enemyData));
            }
        }

        // Due to the way we add elements we need to sort by time so its chronologically ordered
        spawnEvents.Sort(static (x, y) => x.time.CompareTo(y.time));
    }

    private void SpawnDueEnemies(EnemySpawnManager sm) {
        if (sm.CurPhase == null) return;

        const float batchTime = 1f;
        if (!spawnLimiterForEnemyBatching.TimeHasPassed(batchTime) || sm.spawnEvents.Count <= 0) return;

        SpawnDueEvents(sm, sm.spawnEvents, ref sm.spawnTimeIndex, sm.timeInCurPhase, sm.CurSpawnCellRange, sm.OnBossWave);
    }

    private void SpawnDueEvents(EnemySpawnManager sm, List<(float time, EnemyData enemy)> spawnEvents, ref int spawnTimeIndex,
        float time, Vector2Int spawnCellRange, bool isBossTrash) 
    {
        List<Vector2> enemyPositions = GatherEnemyPositions();

        while (spawnEvents.IndexInRange(spawnTimeIndex) && spawnEvents[spawnTimeIndex].time <= time) {
            Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, enemyPositions);
            enemyPositions.Add(randomSpawnPos);

            EnemyData enemyToSpawn = spawnEvents[spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity, data: enemyToSpawn);
            enemy.isBossTrash = isBossTrash;

            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
            spawnTimeIndex++;
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
