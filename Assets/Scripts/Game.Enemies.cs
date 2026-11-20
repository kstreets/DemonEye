using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public static partial class Game {
    
    static class EnemyAnimations {
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
        public static readonly int summon = Animator.StringToHash("Summon");
        
        // Attacks block movement through their attack phase instead
        public static readonly List<int> movementBlockingAnimations = new() { spawn, death };
    }

    public enum EnemyAttackPhase { None, Windup, Recovery }
    public enum AttackAnimTracking { None, AttackAnims, ShootAnims }

    public class Enemy : Entity {
        public EnemyData data;
        public CircleCollider2D worldCollider;
        public DamageShape sideAttackShape;
        public DamageShape upAttackShape;
        public DamageShape downAttackShape;
        public List<AttackSpawnPoint> attackSpawnPoints;
        
        public float flowFieldAcc;
        public float lane; // -1 to 1, which side of the flow field direction this enemy walks
        public float lastTeleportTime;
        public float curDistFromPlayer;
        public float hitStopEndTime;
        
        public Vector2 separationOffset;
        public float fixedRandomSeparationAngle;
        public Vector2 separationVel;
        
        public Vector2 moveDir;
        public Vector2 graphicalDir;
        public CardinalDir pendingDir;
        public float pendingDirStartTime;
        
        public EnemyAttackPhase attackPhase;
        public Action<Enemy, Vector2> onAttackRelease;
        public AttackAnimTracking attackAnimTracking;
        public Tween attackDelay;
        public float attackTime;
        public float attackReleaseTime;
        public float attackEndTime;
        public float attackLeadSpeed;
        public int attackCount;
        public bool queuedForRangedAttack;
        public float lastRangedAttackTime;
        public float rangedAttackScore;
        
        public bool isBossTrash;
        public bool cannotBePushedByOthers;
        
        public OptionalRef<BleedEyeUpgrade.InstanceData> bleed;
        public OptionalRef<HemorrhageAugment.InstanceData> hemorrhage;
        public OptionalRef<PoisonEyeUpgrade.InstanceData> poison;
        public OptionalRef<SlowEyeUpgrade.InstanceData> slow;
        public OptionalRef<KnockBackAugment.InstanceData> knockBack;
        
        public bool IsHitStopped => Time.time < hitStopEndTime;
    }

    public static void ClearEnemyDebuffs(Enemy enemy) {
        enemy.bleed.ClearValue();
        enemy.poison.ClearValue();
        enemy.slow.ClearValue();
        enemy.knockBack.ClearValue();
        enemy.GetEffect(Entity.EffectsIndicies.Poisoned).Stop();
    }

    static void HitStopEnemy(Enemy enemy) {
        const float enemyHitStopDuration = 0.08f;
        enemy.hitStopEndTime = Time.time + enemyHitStopDuration;
        enemy.animator.speed = 0f;
    }

    static void OnEnemySpawned(Enemy enemy, object spawnData) {
        Assert.IsTrue(spawnData is EnemyData, "Enemies must be spawned with their EnemyData as the spawn data");

        entities.enemies.Add(enemy);
        
        EnemyData data = (EnemyData)spawnData;
        enemy.data = data;
        enemy.health = data.health;
        enemy.isBossTrash = false;
        enemy.hitStopEndTime = 0f;
        enemy.animator.speed = 1f;

        if (data.animatorOverride) { 
            enemy.animator.runtimeAnimatorController = data.animatorOverride;
        }
        
        enemy.worldCollider = enemy.trans.GetChild(0).GetComponent<CircleCollider2D>();
        enemy.worldCollider.excludeLayers = data.excludeCollisionLayers;
        enemy.separationOffset = enemy.worldCollider.transform.TransformPoint(enemy.worldCollider.offset) - enemy.trans.position;
        enemy.fixedRandomSeparationAngle = Random.Range(0f, 360f);
        enemy.separationVel = Vector2.zero;
        
        enemy.flowFieldAcc = Random.Range(2.5f, 3.5f);
        enemy.lane = Random.Range(-1f, 1f);
        enemy.rigidbody.mass = data.defaultMass;
        enemy.collider.enabled = true;
        enemy.attackDelay = data.attack ? Tween.Delay(data.attack.cooldown) : default;
        enemy.attackPhase = EnemyAttackPhase.None;
        enemy.queuedForRangedAttack = false;
        enemy.lastRangedAttackTime = Time.time;
        
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

    static void OnEnemyDeath(Enemy deadEnemy) {
        Assert.IsNotNull(deadEnemy.data.dropPool, "Enemy needs to have a drop pool");
        
        if (!deadEnemy.data.isBoss && deadEnemy.data.dropPool.HasItems && RollProbability(deadEnemy.data.chanceToDropItem)) {
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
            ListPool<AttackSpawnPoint>.Release(deadEnemy.attackSpawnPoints);
            deadEnemy.attackSpawnPoints = null;
        }
    }

    static void InitEnemies() {
        RegisterOnSpawnCallback<Enemy>(OnEnemySpawned);
        RegisterOnSpawnCallback<FireBoss>(InitFireBoss);
        RegisterOnSpawnCallback<SulfurBoss>(InitSulfurBoss);
    }

    static void UpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        if (enemies.Count <= 0) return;
        
        RaidSpawnPattern.PhasePool reteleportPhase = spawnManager.CurPhasePool;
        if (reteleportPhase != null && reteleportPhase.reteleportCount > 0 && curRaid.data.reteleportLimitter.TimeHasPassed(reteleportPhase.reteleportInterval)) {
            ReteleportEnemies(reteleportPhase);
        }
        
        for (int i = enemies.Count - 1; i >= 0; i--) {
            Enemy enemy = enemies[i];
            
            if (!enemy.gameObject.activeInHierarchy) continue;

            if (!enemy.IsHitStopped && enemy.animator.speed == 0f) {
                enemy.animator.speed = 1f;
            }
            
            enemy.cannotBePushedByOthers = enemy.slow.HasValue || !EnemyCanMove(enemy);
            enemy.curDistFromPlayer = Vector2.Distance(player.Center, enemy.Center);

            if (enemy.attackPhase == EnemyAttackPhase.None) {
                switch (enemy.data.type) {
                    case EnemyData.EnemyType.FireBoss:
                        FireBossLogic(enemy);
                        break;
                    case EnemyData.EnemyType.SulfurBoss:
                        SulfurBossLogic(enemy);
                        break;
                    default:
                        UpdateEnemyAttack(enemy);
                        break;
                }
            }
            else {
                UpdateEnemyAttackPhase(enemy);
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

                    Entity bloodDrop = SpawnEntityOneShot(entityPools.bloodDrop, OffsetY(enemy.position, 0.015f), Quaternion.identity);
                    AddParentEffect(bloodDrop, enemy, 0.4f);
                    
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
            
                deadEnemy.collider.enabled = false;
                deadEnemy.worldCollider.enabled = false;
                deadEnemy.animator.speed = 1f; // Dead enemies leave the update loop, so the hit stop would never end

                if (deadEnemy.data.isBoss) {
                    deadEnemy.rigidbody.linearVelocity = Vector2.zero;
                    deadEnemy.animator.Play(EnemyAnimations.death);
                    Delay(deadEnemy, deadEnemy.animator.CurrentClipLength(), static (deadEnemy) => DropBossChest(deadEnemy));
                    OnEnemyDeath(deadEnemy);
                }
                else {
                    // It looks better if the enemies keep some momentum when dying
                    deadEnemy.rigidbody.linearVelocity *= 0.3f;
                    DissolveAndDestroy(deadEnemy, 0.6f, config.enemyColors.enemyDissolveBloodColor);
                    StampBlood(deadEnemy.position.Offset(y: -0.01f), BloodSize.Medium, delay: 0.05f);
                    
                    const float deathDelay = 0.03f;
                    Delay(deadEnemy, deathDelay, static (deadEnemy) => {
                        SpawnEntityOneShot(entityPools.largeBloodSplatter, deadEnemy.position, Quaternion.identity);
                        PlayAudioClip(audio.bloodBurstClip, deadEnemy.position);
                        OnEnemyDeath(deadEnemy);
                    });
                }
                
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
        // Used on maps without a spawn pattern, like the boss map
        public const float defaultMinTimeBetweenAttacks = 1.5f;
        public const float defaultMaxTimeBetweenAttacks = 3f;

        // Score = type priority + closeness bonus + time since last attack
        public const float distanceScoreRange = 2f;
        public const float distanceScoreWeight = 1f;
        public const float scorePerSecondWaiting = 0.5f;

        public float nextAttackAllowedTime;
        public readonly List<Enemy> queuedEnemies = new();
    }

    static RangedAttackManager rangedAttackManager = new();

    static void ClearRangedAttackManager() {
        foreach (Enemy enemy in rangedAttackManager.queuedEnemies) {
            enemy.queuedForRangedAttack = false;
        }
        rangedAttackManager.queuedEnemies.Clear();
        rangedAttackManager.nextAttackAllowedTime = 0f;
    }

    static void UpdateRangedAttackManager() {
        RangedAttackManager rm = rangedAttackManager;
        rm.queuedEnemies.RemoveAll(static enemy => enemy.health <= 0);

        if (Time.time < rm.nextAttackAllowedTime) return;

        foreach (Enemy enemy in rm.queuedEnemies) {
            enemy.rangedAttackScore = RangedAttackScore(enemy);
        }
        rm.queuedEnemies.Sort(static (a, b) => b.rangedAttackScore.CompareTo(a.rangedAttackScore));

        for (int i = 0; i < rm.queuedEnemies.Count; i++) {
            Enemy enemy = rm.queuedEnemies[i];
            EnemyAttack attack = enemy.data.attack;

            // Teleporting enemies stay queued until they're back
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (!CanStartEnemyAttack(enemy, attack) || !TryStartEnemyAttack(enemy, attack)) continue;

            enemy.queuedForRangedAttack = false;
            enemy.lastRangedAttackTime = Time.time;
            rm.queuedEnemies.RemoveAt(i);

            RaidSpawnPattern pattern = curRaid.map.spawning;
            float minDelay = pattern ? pattern.minTimeBetweenRangedAttacks : RangedAttackManager.defaultMinTimeBetweenAttacks;
            float maxDelay = pattern ? pattern.maxTimeBetweenRangedAttacks : RangedAttackManager.defaultMaxTimeBetweenAttacks;
            rm.nextAttackAllowedTime = Time.time + Random.Range(minDelay, maxDelay);
            return;
        }
    }

    static float RangedAttackScore(Enemy enemy) {
        float closeness = 1f - Mathf.Clamp01(enemy.curDistFromPlayer / RangedAttackManager.distanceScoreRange);
        float timeWaiting = Time.time - enemy.lastRangedAttackTime;
        return enemy.data.attack.rangedPriority + closeness * RangedAttackManager.distanceScoreWeight + timeWaiting * RangedAttackManager.scorePerSecondWaiting;
    }

    static void UpdateEnemyAttack(Enemy enemy) {
        EnemyAttack attack = enemy.data.attack;
        if (!attack) return;

        if (enemy.attackDelay.isAlive || !CanStartEnemyAttack(enemy, attack)) return;

        // Ranged attacks take turns through the ranged attack manager
        if (attack.kind == EnemyAttack.AttackKind.Projectile && attack.usesRangedAttackManager) {
            if (!enemy.queuedForRangedAttack) {
                enemy.queuedForRangedAttack = true;
                rangedAttackManager.queuedEnemies.Add(enemy);
            }
            return;
        }

        TryStartEnemyAttack(enemy, attack);
    }

    static bool CanStartEnemyAttack(Enemy enemy, EnemyAttack attack) {
        if (!EnemyCanMove(enemy)) {
            return false;
        }
        if (attack.range > 0f && enemy.curDistFromPlayer > attack.range) {
            return false;
        }
        if (attack.requireFacingPlayer && !attack.snapFaceTargetOnStart) {
            Vector2 dirToPlayer = (player.position - enemy.position).normalized;
            if (Vector2.Dot(enemy.graphicalDir, dirToPlayer) < 0.5f) {
                return false;
            }
        }
        return true;
    }

    // Fails when the attack can't be aimed, like a leading projectile that can't reach the player
    static bool TryStartEnemyAttack(Enemy enemy, EnemyAttack attack) {
        float leadSpeed = AttackLeadSpeed(attack);
        CardinalDir dir = CardinalDirFromVector(enemy.graphicalDir);
        if (attack.snapFaceTargetOnStart) {
            if (!TryGetAimPoint(EnemyAttackOrigin(enemy), leadSpeed, out Vector2 aimPoint)) {
                return false;
            }
            dir = CardinalDirFromVector(aimPoint - (Vector2)enemy.position);
            SetFacing(enemy, dir);
        }

        AttackAnimTracking animTracking = attack.trackTargetUntilRelease ? AttackAnimTracking.AttackAnims : AttackAnimTracking.None;
        StartEnemyAttack(enemy, AttackAnimationFromDir(dir), static (enemy, spawnPos) => ReleaseEnemyAttack(enemy, enemy.data.attack, spawnPos), animTracking, leadSpeed);
        enemy.attackDelay = Tween.Delay(attack.cooldown);
        return true;
    }

    // Plays the attack clip and calls onRelease on its release frame, which comes from the prefab's AttackSpawnPoint or DamageShape
    static void StartEnemyAttack(Enemy enemy, int animHash, Action<Enemy, Vector2> onRelease, AttackAnimTracking animTracking = AttackAnimTracking.None, float leadSpeed = 0f) {
        enemy.animator.Play(animHash, 0, 0f);
        // Applies the Play so the timings below come from the attack clip
        enemy.animator.Update(0f);

        enemy.attackPhase = EnemyAttackPhase.Windup;
        enemy.attackTime = 0f;
        // Read from the clip, not the animator state, since the state's length is infinite while hit stop has the animator at speed 0.
        // The attack time is already scaled by the animator speed.
        AnimationClip clip = enemy.animator.CurrentClip();
        Assert.IsNotNull(clip, $"{enemy.data.name} has no clip for its attack state");
        enemy.attackReleaseTime = clip ? AttackReleaseFrame(enemy, animHash) / clip.frameRate : 0f;
        enemy.attackEndTime = clip ? clip.length : 0f;
        enemy.attackAnimTracking = animTracking;
        enemy.attackLeadSpeed = leadSpeed;
        enemy.onAttackRelease = onRelease;
    }

    static void UpdateEnemyAttackPhase(Enemy enemy) {
        // Scaled by the animator so hit stop pauses the attack along with its animation
        enemy.attackTime += Time.deltaTime * enemy.animator.speed;

        if (enemy.attackPhase == EnemyAttackPhase.Windup) {
            if (enemy.attackAnimTracking != AttackAnimTracking.None) {
                Vector2 aimPoint = EnemyAimPoint(EnemyAttackOrigin(enemy), enemy.attackLeadSpeed);
                CardinalDir dir = CardinalDirFromVector(aimPoint - (Vector2)enemy.position);
                int animHash = enemy.attackAnimTracking == AttackAnimTracking.ShootAnims ? ShootAnimationFromDir(dir) : AttackAnimationFromDir(dir);
                FaceDirectionWhileAttacking(enemy, dir, animHash);
            }
            if (enemy.attackTime >= enemy.attackReleaseTime) {
                enemy.attackPhase = EnemyAttackPhase.Recovery;
                enemy.onAttackRelease(enemy, CurrentAttackSpawnPos(enemy));
            }
        }
        else {
            if (enemy.attackTime >= enemy.attackEndTime) {
                enemy.attackPhase = EnemyAttackPhase.None;
            }
        }
    }

    // Steps the clip back so the attack winds up and releases again, for attacks that repeat within one clip
    static void RewindEnemyAttack(Enemy enemy, int frames) {
        AnimationClip clip = enemy.animator.CurrentClip();
        if (!clip) return;
        enemy.attackTime = Mathf.Max(0f, enemy.attackReleaseTime - frames / clip.frameRate);
        enemy.animator.SetFrame(Mathf.RoundToInt(enemy.attackTime * clip.frameRate));
        enemy.attackPhase = EnemyAttackPhase.Windup;
    }

    static void ReleaseEnemyAttack(Enemy enemy, EnemyAttack attack, Vector2 spawnPos) {
        switch (attack.kind) {
            case EnemyAttack.AttackKind.Melee: {
                DamageShape shape = EnemyAttackShape(enemy, out bool mirrored);
                Assert.IsNotNull(shape, $"{enemy.data.name} has a melee attack but no DamageShape for this direction");
                if (!shape) return;

                if (attack.damageFrameEffect == EnemyAttack.DamageFrameEffect.SlamSmoke) {
                    SpawnSlamSmoke(shape.GetCenter(mirrored));
                }
                DamageEntitiesInShape(enemy, shape, attack.damage, Masks.PlayerMeleeHurtMask, mirrored, attack.bleedChance);
                break;
            }
            case EnemyAttack.AttackKind.Projectile: {
                EntityPool<Projectile> pool = EnemyProjectilePool(attack.projectilePrefab);

                if (attack.projectilePattern == EnemyAttack.ProjectilePattern.Ring) {
                    float angleStep = 360f / attack.ringProjectileCount;
                    float jitter = angleStep * attack.ringAngleJitter;
                    for (int i = 0; i < attack.ringProjectileCount; i++) {
                        float angle = angleStep * i + Random.Range(-jitter, jitter);
                        Vector2 ringVelocity = RotationVector(angle) * attack.projectileSpeed;
                        SpawnProjectile(pool, spawnPos, ringVelocity, attack.projectileLifetime, enemy,
                            flatDamage: attack.damage, layermask: Masks.PlayerProjectileHurtMask);
                    }
                }
                else {
                    SpawnAimedProjectile(pool, enemy, spawnPos, attack.projectileSpeed, attack.projectileLifetime, attack.damage,
                        Masks.PlayerProjectileHurtMask, attack.leadTarget);
                }
                break;
            }
        }

        if (attack.diesOnRelease) {
            enemy.health = 0;
        }
    }

    static int AttackReleaseFrame(Enemy enemy, int animHash) {
        AttackSpawnPoint point = FindAttackSpawnPoint(enemy, animHash);
        if (point) {
            return point.spawnFrame;
        }
        DamageShape shape = EnemyAttackShape(enemy, out _);
        return shape ? shape.damageFrame : 0;
    }

    // The enemy may have turned since the attack started, so this uses the point for the clip playing now
    static Vector2 CurrentAttackSpawnPos(Enemy enemy) {
        int stateHash = enemy.animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        AttackSpawnPoint point = FindAttackSpawnPoint(enemy, stateHash);
        if (!point) {
            return EnemyAttackOrigin(enemy);
        }
        bool mirrored = point.mirrorForLeft && CardinalDirFromVector(enemy.graphicalDir) == CardinalDir.Left;
        return point.GetPosition(mirrored);
    }

    static Vector2 EnemyAttackOrigin(Enemy enemy) {
        return enemy.position.Offset(y: 0.1f);
    }

    static float AttackLeadSpeed(EnemyAttack attack) {
        return attack.kind == EnemyAttack.AttackKind.Projectile && attack.leadTarget ? attack.projectileSpeed : 0f;
    }

    // A lead speed of 0 aims straight at the player
    static bool TryGetAimPoint(Vector2 origin, float leadSpeed, out Vector2 aimPoint) {
        if (leadSpeed > 0f) {
            return TryGetPlayerInterceptPoint(origin, leadSpeed, out aimPoint);
        }
        aimPoint = player.position;
        return true;
    }

    static Vector2 EnemyAimPoint(Vector2 origin, float leadSpeed) {
        return TryGetAimPoint(origin, leadSpeed, out Vector2 aimPoint) ? aimPoint : player.position;
    }

    static Projectile SpawnAimedProjectile(EntityPool<Projectile> pool, Enemy source, Vector2 spawnPos, float speed, float lifetime, 
        int? damage, LayerMask layermask, bool leadTarget = true)
    {
        Vector2 aimPoint = EnemyAimPoint(spawnPos, leadTarget ? speed : 0f);
        Vector2 velocity = (aimPoint - spawnPos).normalized * speed;
        return SpawnProjectile(pool, spawnPos, velocity, lifetime, source, flatDamage: damage, layermask: layermask);
    }

    static bool TryChooseBossAttack<T>(Enemy boss, AttackBrain<T> brain, int level, float minDelay, float maxDelay, out T attack) where T : Enum {
        attack = default;
        if (boss.attackDelay.isAlive) {
            return false;
        }
        attack = brain.ChooseAttack(level);
        boss.attackDelay = Tween.Delay(minDelay, maxDelay);
        return true;
    }

    static EntityPool<Projectile> EnemyProjectilePool(GameObject prefab) {
        EntityPools pools = entityPools;
        if (prefab == pools.smallFireballProjectile.prefab) return pools.smallFireballProjectile;
        if (prefab == pools.fireBossProjectile.prefab) return pools.fireBossProjectile;
        if (prefab == pools.fireEmberProjectile.prefab) return pools.fireEmberProjectile;
        if (prefab == pools.gooProjectile.prefab) return pools.gooProjectile;

        Assert.IsTrue(false, $"No projectile pool for {(prefab ? prefab.name : "null")}, add it to {nameof(EnemyProjectilePool)}");
        return pools.smallFireballProjectile;
    }

    static void SpawnSlamSmoke(Vector2 position) {
        SpawnEntityOneShot<Entity>(prefabs.slamSmoke, position, Quaternion.identity);
    }
    
    public class FireBoss : Enemy {
        public enum AttackTypes { Fireball, Meteor, EmberScatter, DoubleEmberScatter, DoubleFireball, MutliMeteor, Count }
        public AttackBrain<AttackTypes> attackBrain;

        public const float minAttackDelay = 1f;
        public const float maxAttackDelay = 2.5f;

        public const int fireballDamage = 25;
        public const float fireballSpeed = 1.7f;
        public const float fireballLifetime = 5f;
        public const float delayBetweenFireballs = 0.2f;

        public const int emberCount = 15;
        public const int emberDamage = 10;
        public const float emberSpeed = 1f;
        public const float emberLifetime = 10f;
        public const float emberCastAngle = 60f;
        public const int emberRecastRewindFrames = 3;

        public const int meteorDamage = 15;
        public const float meteorTravelTime = 0.6f;
        public const float meteorWarningTime = meteorTravelTime * 0.5f;
        public const float meteorRadius = 0.2f;
        public const float meteorScatterRadiusPerMeteor = 0.3f;
        public const float delayBetweenMeteors = 0.12f;
        public const int multiMeteorCount = 4;
    }

    static void InitFireBoss(FireBoss fireBoss) {
        fireBoss.attackDelay = Tween.Delay(FireBoss.minAttackDelay, FireBoss.maxAttackDelay);
        fireBoss.attackBrain = new((int)FireBoss.AttackTypes.Count);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Fireball, level: 0, marbles: 3);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.Meteor, level: 0, marbles: 2);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.EmberScatter, level: 0, marbles: 1, preventConsecutive: true);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.DoubleFireball, level: 1, marbles: 3);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.MutliMeteor, level: 1, marbles: 2);
        fireBoss.attackBrain.AddAttack(FireBoss.AttackTypes.DoubleEmberScatter, level: 1, marbles: 1, preventConsecutive: true);
        fireBoss.animator.Play(EnemyAnimations.spawn);
    }

    static void FireBossLogic(Enemy enemy) {
        if (enemy is not FireBoss fireBoss) return;

        int attackLevel = (fireBoss.health / (float)fireBoss.data.health) > 0.5f ? 0 : 1;
        if (!TryChooseBossAttack(fireBoss, fireBoss.attackBrain, attackLevel, FireBoss.minAttackDelay, FireBoss.maxAttackDelay, out FireBoss.AttackTypes attack)) return;

        int shootAnim = ShootAnimationFromDir(CardinalDirFromVector(fireBoss.graphicalDir));
        switch(attack) {
            case FireBoss.AttackTypes.Fireball:
                CastFireballs(1);
                break;
            case FireBoss.AttackTypes.Meteor:
                SummonMeteors(1);
                break;
            case FireBoss.AttackTypes.EmberScatter:
                CastEmberScatter(1);
                break;
            case FireBoss.AttackTypes.DoubleFireball:
                CastFireballs(2);
                break;
            case FireBoss.AttackTypes.MutliMeteor:
                SummonMeteors(FireBoss.multiMeteorCount);
                break;
            case FireBoss.AttackTypes.DoubleEmberScatter:
                CastEmberScatter(2);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // ------------------------------
        // Nested Methods
        // ------------------------------

        void CastFireballs(int count) {
            fireBoss.attackCount = count;
            StartEnemyAttack(fireBoss, shootAnim, animTracking: AttackAnimTracking.ShootAnims, leadSpeed: FireBoss.fireballSpeed, onRelease: static (boss, spawnPos) => {
                for (int i = 0; i < boss.attackCount; i++) {
                    Delay(boss, i * FireBoss.delayBetweenFireballs, (_) => {
                        SpawnAimedProjectile(entityPools.fireBossProjectile, boss, spawnPos, FireBoss.fireballSpeed, FireBoss.fireballLifetime, FireBoss.fireballDamage, Masks.PlayerProjectileHurtMask);
                    });
                }
            });
        }

        void SummonMeteors(int meteorCount) {
            fireBoss.attackCount = meteorCount;
            StartEnemyAttack(fireBoss, EnemyAnimations.summon, static (boss, _) => RainMeteors(boss, boss.attackCount));
        }

        static void RainMeteors(Enemy fireBoss, int meteorCount) {

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
                Delay(fireBoss, i * FireBoss.delayBetweenMeteors, _ => SpawnSingleMeteor(meteorPos));
            }
        }

        static void SpawnSingleMeteor(Vector2 targetPos) {

            Vector2 meteorSpawnPos = new(targetPos.x, targetPos.y + 2);
            Entity attackCircleEntity = SpawnEntity(entityPools.attackCircle, targetPos, Quaternion.identity);
            attackCircleEntity.attackCircle.Animate(FireBoss.meteorRadius, FireBoss.meteorWarningTime);

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
        
        void CastEmberScatter(int count) {
            fireBoss.attackCount = count;
            StartEnemyAttack(fireBoss, shootAnim, animTracking: AttackAnimTracking.ShootAnims, leadSpeed: FireBoss.emberSpeed, onRelease: static (boss, spawnPos) => {

                Vector2 aimDir = (EnemyAimPoint(spawnPos, FireBoss.emberSpeed) - spawnPos).normalized;
                using var _ = ListPool<Vector2>.Get(out var shootDirections);
                ScatterAimForProjectiles(aimDir, FireBoss.emberCastAngle, FireBoss.emberCount, ref shootDirections);

                foreach (Vector2 shootDir in shootDirections) {
                    SpawnProjectile(entityPools.fireEmberProjectile, spawnPos, shootDir * FireBoss.emberSpeed, FireBoss.emberLifetime, boss, flatDamage: FireBoss.emberDamage, layermask: Masks.PlayerProjectileHurtMask);
                }

                boss.attackCount--;
                if (boss.attackCount > 0) {
                    RewindEnemyAttack(boss, FireBoss.emberRecastRewindFrames);
                }
            });
        }
    }

    class SulfurBoss : Enemy {
        public enum AttackTypes { PoisonCloud, PoisonEruption, Count }
        public AttackBrain<AttackTypes> attackBrain;
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

    static void InitSulfurBoss(SulfurBoss sulfurBoss) {
        sulfurBoss.attackDelay = Tween.Delay(SulfurBoss.minAttackDelay, SulfurBoss.maxAttackDelay);
        sulfurBoss.attackBrain = new((int)SulfurBoss.AttackTypes.Count);
        sulfurBoss.attackBrain.AddAttack(SulfurBoss.AttackTypes.PoisonCloud, level: 0, marbles: 3);
        sulfurBoss.attackBrain.AddAttack(SulfurBoss.AttackTypes.PoisonEruption, level: 0, marbles: 1, preventConsecutive: true);
        sulfurBoss.animator.Play(EnemyAnimations.spawn);
    }

    static void SulfurBossLogic(Enemy enemy) {
        if (enemy is not SulfurBoss sulfurBoss) return;

        if (!TryChooseBossAttack(sulfurBoss, sulfurBoss.attackBrain, 0, SulfurBoss.minAttackDelay, SulfurBoss.maxAttackDelay, out SulfurBoss.AttackTypes attack)) {
            return;
        }

        switch(attack) {
            case SulfurBoss.AttackTypes.PoisonCloud:
                int shootAnim = ShootAnimationFromDir(CardinalDirFromVector(sulfurBoss.graphicalDir));
                StartEnemyAttack(sulfurBoss, shootAnim, static (boss, spawnPos) => ShootPoisonCloud(boss, spawnPos),
                    AttackAnimTracking.ShootAnims, SulfurBoss.poisonCloudProjectileSpeed);
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

        static void ShootPoisonCloud(Enemy boss, Vector2 spawnPos) {
            Projectile projectile = SpawnAimedProjectile(entityPools.gooProjectile, boss, spawnPos, SulfurBoss.poisonCloudProjectileSpeed,
                SulfurBoss.poisonCloudProjectileLifetime, damage: null, layermask: 0);
            projectile.detonateNearPlayerRadius = SulfurBoss.poisonCloudDetonateRadius;

            projectile.onDetonate = static (detonatePos) => {
                Entity poisonCloud = SpawnEntityOneShot(entityPools.poisonCloud, detonatePos, Quaternion.identity);
                DealShapeDamage(poisonCloud, SulfurBoss.poisonCloudDamage, Masks.PlayerMeleeHurtMask, onHit: static (hit) => {
                    if (hit is Player) {
                        PoisonPlayer(SulfurBoss.poisonDamageMulti, SulfurBoss.poisonDebuffDuration);
                    }
                });
            };
        }

        void PoisonEruption() {
            StartEnemyAttack(sulfurBoss, EnemyAnimations.erupt, static (boss, spawnPos) => {
                List<Vector2> targets = ((SulfurBoss)boss).eruptionTargets;
                for (int i = 0; i < targets.Count; i++) {
                    Vector2 poolPos = targets[i];
                    // Tied to the boss so the remaining lobs are cancelled if it dies mid eruption
                    Delay(boss, i * SulfurBoss.delayBetweenPoisonLobs, (_) => LobPoisonPool(boss, spawnPos, poolPos));
                }
            });

            Vector2 estimatedSpawnPos = CurrentAttackSpawnPos(sulfurBoss);
            float lobSpawnDelay = sulfurBoss.attackReleaseTime;

            // Centered on where the player will be when the pools land, not when the boss starts erupting
            Vector2 playerPos = player.hurtCollider.bounds.center;
            float landTime = lobSpawnDelay + Vector2.Distance(estimatedSpawnPos, playerPos) / SulfurBoss.poisonEruptionSpeed;
            Vector2 predictedPlayerPos = ClampPositionToMap(playerPos, PredictedPlayerPosition(landTime), SulfurBoss.poisonPoolWarningRadius);

            List<Vector2> targets = sulfurBoss.eruptionTargets;
            targets.Clear();
            SpreadPositionsInCircle(targets, SulfurBoss.poisonEruptionCount, predictedPlayerPos, SulfurBoss.poisonEruptionScatterRadius,
                SulfurBoss.poisonEruptionJitter, SulfurBoss.poisonPoolWarningRadius);

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
            float travelTime = Vector2.Distance(spawnPos, targetPos) / SulfurBoss.poisonEruptionSpeed;
            float gravity = config.gameplay.projectileGravity;
            Vector2 velocity = ProjectileLobVelocity(spawnPos, targetPos, gravity, SulfurBoss.poisonEruptionSpeed);
            SpawnProjectile(entityPools.gooProjectile, spawnPos, velocity, float.MaxValue, boss, layermask: 0, gravity: gravity, lobTarget: targetPos, lobTime: travelTime);
            SpawnHazard(entityPools.poisonPool, targetPos, SulfurBoss.poisonPoolDamage, Masks.PlayerMeleeHurtMask,
                SulfurBoss.poisonPoolDuration, SulfurBoss.poisonPoolTickInterval, spawnDelay: travelTime);
        }

    }

    // Sunflower spiral so the positions cover the circle evenly with no big gaps, then jittered so it doesn't look like a pattern.
    // Ordered outside in, the first position is the outer edge and the last one is the exact center
    static void SpreadPositionsInCircle(List<Vector2> positions, int count, Vector2 center, float circleRadius, float jitter, float bufferRadius) {
        const float goldenAngle = 137.50776f;
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

    static void ScatterPositions(ref List<Vector2> positions, int count, Vector2 center, float scatterRadius, float radius, int maxAttemptsPerPosition = 10) {
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

    struct SeparationAgent {
        public Enemy enemy;
        public Vector2 center;
        public float radius;
        public float mass;
        public int layer;
        public int nextEnemyInSameCellIndex;
    }

    static SeparationAgent[] separationAgents = new SeparationAgent[500];
    static readonly Dictionary<long, int> firstAgentIndexInSeparationCell = new();

    // Bends the direction to the enemy's lane side, less the closer it is to the player, and not at all when that would walk into a wall
    static Vector3 ApplyEnemyLane(Enemy enemy, CoolerGrid grid, Vector3 dir) {
        GameplayConfig gameplay = config.gameplay;
        float strength = Mathf.Clamp01(enemy.curDistFromPlayer / gameplay.laneFullStrengthDistance);
        Vector3 bentDir = Quaternion.AngleAxis(enemy.lane * gameplay.laneMaxAngle * strength, Vector3.forward) * dir;
        if (!grid.IsWalkable((Vector2)enemy.position + (Vector2)bentDir * gameplay.laneWallCheckDistance)) {
            return dir;
        }
        return bentDir;
    }

    static void UpdateEnemySeparation(List<Enemy> enemies) {
        GameplayConfig gameplay = config.gameplay;

        if (separationAgents.Length < enemies.Count) {
            separationAgents = new SeparationAgent[Mathf.NextPowerOfTwo(enemies.Count)];
        }

        RaidSpawnPattern spawnPattern = curRaid.map.spawning;
        float radiusScale = gameplay.separationRadiusScale * (spawnPattern != null ? spawnPattern.enemySeparationMultiplier : 1f);

        int agentCount = 0;
        float maxRadius = 0f;
        foreach (Enemy enemy in enemies) {
            if (!enemy.gameObject.activeInHierarchy || !enemy.collider.enabled) continue;

            float radius = enemy.data.separationRadius * radiusScale;
            maxRadius = Mathf.Max(maxRadius, radius);

            separationAgents[agentCount++] = new() {
                enemy = enemy,
                center = enemy.rigidbody.position + enemy.separationOffset,
                radius = radius,
                mass = enemy.cannotBePushedByOthers ? 1e6f : enemy.rigidbody.mass,
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

    static void FixedUpdateEnemies() {
        List<Enemy> enemies = entities.enemies;
        UpdateEnemySeparation(enemies);
        foreach (Enemy enemy in enemies) {
            bool canMove = EnemyCanMove(enemy);
            MoveEnemy(enemy, canMove);
            OrientEnemy(enemy, canMove);
        }
    }

    static void MoveEnemy(Enemy enemy, bool canMove) {
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
            
            if (slow.timeSpentSlowed >= slow.duration) {
                enemy.slow.ClearValue();
            }
        }
        speed = Mathf.Clamp(speed * Mathf.Clamp01(1f - totalSlowPercentage), 0.05f, enemy.data.speed);

        if (!canMove) {
            speed = 0f;
        }

        Vector3 targetDir = Vector3.zero;
        if (enemy.data.usesFlowField) {
            CoolerGrid grid = curRaid.mapInstance.grid;
            targetDir = grid.GetFlowFieldDirection(enemy.position);

            // Since flow field is using predicted player position, close enemies might move away from the player.
            // If thats the case then we just make them move directly to the player, unless a rock or something is in the way
            Vector2 dirToPlayer = (player.position - enemy.position).normalized;
            if (Vector2.Dot(targetDir, dirToPlayer) < 0f) {
                bool straightPathIsBlocked = grid.LineCast(enemy.position, player.position, enemy.worldCollider.radius);
                if (!straightPathIsBlocked) {
                    targetDir = dirToPlayer;
                }
            }

            if (targetDir != Vector3.zero) {
                targetDir = ApplyEnemyLane(enemy, grid, targetDir);
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

    static void OrientEnemy(Enemy enemy, bool canMove) {
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

    static void ReteleportEnemies(RaidSpawnPattern.PhasePool phase) {
        List<Enemy> enemies = entities.enemies;
        using var _ = ListPool<(Enemy, float)>.Get(out var reteleportCandidates);

        foreach (Enemy enemy in enemies) {
            if (!enemy.gameObject.activeInHierarchy || enemy == entities.boss) continue;
            
            if (Time.time - enemy.lastTeleportTime < phase.reteleportInterval) continue;

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

        int teleportCount = Mathf.Min(reteleportCandidates.Count, phase.reteleportCount);

        for (int i = 0; i < teleportCount; i++) {
            (Enemy enemy, float distFromPlayer) = reteleportCandidates[i];
            Vector2Int repositionCellRange = phase.spawnCellRange;
            Vector2 spawnPos = curRaid.mapInstance.grid.GetSpawnPosition(
                player.position, repositionCellRange.x, repositionCellRange.y, predictPlayerPos: true, GatherEnemyPositions()
            );

            if (Vector2.Distance(spawnPos, player.position) < distFromPlayer) {
                TeleportEnemy(enemy, spawnPos, TeleportType.Reposition);
            }
        }
    }

    static bool EnemyIsOffscreen(Enemy enemy) {
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

    static bool EnemyCanMove(Enemy enemy) {
        if (enemy.IsHitStopped || enemy.attackPhase != EnemyAttackPhase.None) {
            return false;
        }

        // Spawn and death clips block movement until they finish
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

    static DamageShape EnemyAttackShape(Enemy enemy, out bool mirrored) {
        CardinalDir dir = CardinalDirFromVector(enemy.graphicalDir);
        DamageShape shape = dir switch {
            CardinalDir.Right or CardinalDir.Left => enemy.sideAttackShape,
            CardinalDir.Up                        => enemy.upAttackShape,
            _                                     => enemy.downAttackShape,
        };
        mirrored = dir == CardinalDir.Left && shape && shape.mirrorForLeft;
        return shape;
    }

    static int ShootAnimationFromDir(CardinalDir dir) {
        return dir switch {
            CardinalDir.Up   => EnemyAnimations.shootUp,
            CardinalDir.Down => EnemyAnimations.shootDown,
            _                => EnemyAnimations.shootSide,
        };
    }

    static int AttackAnimationFromDir(CardinalDir dir) {
        return dir switch {
            CardinalDir.Up   => EnemyAnimations.attackUp,
            CardinalDir.Down => EnemyAnimations.attackDown,
            _                => EnemyAnimations.attackSide,
        };
    }

    static void SetFacing(Enemy enemy, CardinalDir dir) {
        enemy.graphicalDir = dir switch {
            CardinalDir.Right => Vector2.right,
            CardinalDir.Left  => Vector2.left,
            CardinalDir.Up    => Vector2.up,
            _                 => Vector2.down,
        };
        enemy.spriteRenderer.flipX = dir == CardinalDir.Left;
    }

    static void FaceDirectionWhileAttacking(Enemy enemy, CardinalDir dir, int animHash) {
        SetFacing(enemy, dir);
        AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != animHash) {
            enemy.animator.Play(animHash, 0, stateInfo.normalizedTime);
        }
    }

    static AttackSpawnPoint FindAttackSpawnPoint(Enemy enemy, int stateHash) {
        if (enemy.attackSpawnPoints == null) return null;
        foreach (AttackSpawnPoint spawnPoint in enemy.attackSpawnPoints) {
            if (spawnPoint.StateHash == stateHash) return spawnPoint;
        }
        return null;
    }

    enum TeleportType { Spawn, Reposition }

    static void TeleportEnemy(Enemy enemy, Vector3 position, TeleportType teleportType) {
        curRaid.teleportingInPositions.Add(position);
        
        if (teleportType == TeleportType.Reposition) {
            SpawnTeleportOut(enemy.position);
        }
        
        enemy.position = position;
        enemy.gameObject.SetActive(false);
        enemy.lastTeleportTime = Time.time;

        Entity inTeleportFxEntity = SpawnEntity(entityPools.teleportIn, enemy.position, Quaternion.identity);
        float spawnAnimDuration = inTeleportFxEntity.animator.CurrentClipLength();
        DestroyEntity(inTeleportFxEntity, spawnAnimDuration);
        
        PlayAudioClip(audio.teleportInClip, inTeleportFxEntity.position);

        float spawnDelay = spawnAnimDuration * 0.7f;
        
        Delay(enemy, spawnDelay, static (enemy) => {
            enemy.gameObject.SetActive(true);
            curRaid.teleportingInPositions.Remove(enemy.position);
        });
    }

    static void SpawnTeleportOut(Vector3 position) {
        Entity outTeleportFxEntity = SpawnEntityOneShot(entityPools.teleportOut, position, Quaternion.identity);
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);
    }
    
    public class EnemySpawnManager {
        public float timeInCurPhase;
        public float startNextWaveDelay;
        public float timeAddedThroughActions;
        public int prevEnemyCount;
        public int curPhaseIndex;
        public bool waveStartedThisFrame;
        public bool struggledLastWave;
        public RaidSpawnPattern spawnPattern;
        
        public float timeAccruedDuringWave;
        public Limiter accruedTimeLimitter;
        
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
        public float CurMaxDuration => CurPhasePool.maxDuration + extraMaxDuration;
        public float CurEarlyDelay => CurPhasePool.startNextPhaseEarlyDelay + extraEarlyDelay;

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
    }

    public enum SwarmState { None, Scheduled, Warning, Spawning }

    [NonSerialized] static EnemySpawnManager spawnManager = new();

    static void InitSpawnManager(RaidSpawnPattern pattern) {
        spawnManager.swarmState = SwarmState.None;
        spawnManager.swarmStartedThisFrame = false;
        spawnManager.swarmWarningStartedThisFrame = false;
        if (curRaid.mapInstance.isBossMap) return;

        spawnManager.spawnEvents.Clear();
        spawnManager.spawnPattern = pattern;
        spawnManager.curPhaseIndex = -1;
        spawnManager.timeInCurPhase = 0f;
        spawnManager.nextTrashRefillTime = 0f;
        spawnManager.startNextWaveDelay = 0f;
        spawnManager.timeAddedThroughActions = 0f;
        spawnManager.struggledLastWave = false;
        spawnManager.timeAccruedDuringWave = 0f;

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
    static void RollSwarm(EnemySpawnManager sm, RaidSpawnPattern pattern) {
        RaidSpawnPattern.Swarm swarm = pattern.swarm;
        int waveCount = pattern.phasePools.Count;
        if (swarm.pool.variants.Count <= 0 || waveCount <= 0 || !RollProbability(swarm.chance)) return;

        int minWave = Mathf.Clamp(swarm.waveRange.x, 1, waveCount);
        int maxWave = Mathf.Clamp(swarm.waveRange.y, minWave, waveCount);
        sm.swarmWaveIndex = Random.Range(minWave, maxWave + 1) - 1;

        sm.swarmWarnAtTimeInWave = Random.Range(0f, pattern.phasePools[sm.swarmWaveIndex].spawnDuration);
        sm.swarmVariant = swarm.pool.variants[Random.Range(0, swarm.pool.variants.Count)];
        sm.swarmState = SwarmState.Scheduled;
    }

    static Limiter spawnLimiterForEnemyBatching;

    static void UpdateSpawnManager() {
        if (curRaid.mapInstance.isBossMap) return;

        EnemySpawnManager sm = spawnManager;
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

    static Limiter swarmSpawnLimiter;

    static void UpdateSwarm(EnemySpawnManager sm) {
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
                    LayOutSpawnEvents(sm, sm.spawnPattern.swarm.pool, sm.swarmVariant, sm.swarmSpawnEvents);
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
                    SpawnDueEvents(sm.swarmSpawnEvents, ref sm.swarmSpawnTimeIndex, sm.timeInSwarm, sm.spawnPattern.swarm.pool.spawnCellRange, false);
                }
                if (!sm.swarmSpawnEvents.IndexInRange(sm.swarmSpawnTimeIndex)) {
                    sm.swarmState = SwarmState.None;
                }
                break;
            }
        }
    }

    static void UpdateWaveProgression(EnemySpawnManager sm, float prevTimeInCurPhase) {
        bool afterFirstWave = sm.curPhaseIndex >= 0;
        float waveDuration = afterFirstWave ? sm.CurMaxDuration : sm.spawnPattern.timeBeforeFirstPhase;
        bool timeLimitExceeded = sm.timeInCurPhase >= waveDuration;
        bool justHitTimeLimit = prevTimeInCurPhase < waveDuration && timeLimitExceeded;
        int curEnemyCount = entities.enemies.Count;
        bool waveCleared = afterFirstWave && sm.FinishedSpawningThisWave && curEnemyCount <= 0;

        if (afterFirstWave) {
            AddTimeToNextWaveDelay(sm, timeLimitExceeded, justHitTimeLimit, curEnemyCount);
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
                    SpawnRaidBoss(bossPhase);
                    break;
                case RaidSpawnPattern.PhasePool phasePool:
                    // sm.startNextWaveDelay = sm.CurEarlyDelay;
                    sm.startNextWaveDelay = 0f;
                    sm.timeAddedThroughActions = 0f;
                    BeginSpawningVariant(sm, phasePool, sm.CurVariant);
                    break;
            }
        }
    }

    static void AddTimeToNextWaveDelay(EnemySpawnManager sm, bool timeLimitExceeded, bool justHitTimeLimit, int curEnemyCount) {
        // Hitting the time limit with enemies still alive gives a buffer before the next wave, scaled by how much of the wave is left
        // if (justHitTimeLimit && curEnemyCount > 0) {
        //     int totalEnemiesThisWave = 0;
        //     foreach (RaidSpawnPattern.EnemyBatch batch in sm.CurVariant.enemyBatches) {
        //         totalEnemiesThisWave += batch.enemyCount;
        //     }
        //     const float totalEnemyPercentageForFullDelay = 0.5f;
        //     float struggleComp = Mathf.Clamp01(curEnemyCount / (totalEnemiesThisWave * totalEnemyPercentageForFullDelay));
        //     float addedTime = Mathf.Lerp(1f, 15f, struggleComp);
        //     sm.startNextWaveDelay = addedTime; // Override delay because we did not finish the raid early
        // }
        
        // if (justKilledLastEnemiesInWave) {
        //     // If a wave's max duration is about the same time as the spawn duration, we can't assume the player struggled
        //     // because they would of had none or very little time to kill the last spawned enemies
        //     const float struggleEligiblePercentage = 1.2f;
        //     bool waveIsStruggleEligible = sm.CurMaxDuration >= (sm.CurPhasePool.spawnDuration * struggleEligiblePercentage);
        //     if (waveIsStruggleEligible) {
        //         const float struggleThresholdPercentage = 0.8f;
        //         float stuggleThreshold = sm.CurMaxDuration * struggleThresholdPercentage;
        //         if (sm.timeInCurPhase >= stuggleThreshold) {
        //             float exceededTime = sm.timeInCurPhase - stuggleThreshold;
        //             float maxPossibleExceededTime = sm.CurMaxDuration * (1f - struggleThresholdPercentage);
        //             float addedTime = Mathf.Lerp(1f, 6f, exceededTime / maxPossibleExceededTime);
        //             sm.startNextWaveDelay += addedTime;
        //             sm.struggledLastWave = true;
        //         }
        //     }
        // }
        
        if (!timeLimitExceeded && curEnemyCount > 0) {
            const int maxAccruedChecksPerWave = 10;
            float accruedCheckDelay = sm.CurMaxDuration / maxAccruedChecksPerWave;
            bool addAccruedTime = sm.accruedTimeLimitter.TimeHasPassed(accruedCheckDelay);
            if (addAccruedTime) {
                sm.timeAccruedDuringWave += curEnemyCount * sm.spawnPattern.accrueTimePerEnemy;
                sm.timeAccruedDuringWave = Mathf.Clamp(sm.timeAccruedDuringWave, 0f, sm.spawnPattern.maxAccrueTime);
            }
        }
        
        bool justKilledLastEnemiesInWave = sm.FinishedSpawningThisWave && sm.prevEnemyCount > 0 && curEnemyCount <= 0;
        sm.prevEnemyCount = curEnemyCount;
        
        if (justHitTimeLimit || justKilledLastEnemiesInWave) {
            sm.startNextWaveDelay += sm.timeAccruedDuringWave;
            sm.timeAccruedDuringWave = 0f;
        }

        bool notFinishedWithWave = !sm.FinishedSpawningThisWave;
        bool alreadyAddedTheMaxAmountOfTime = sm.timeAddedThroughActions >= sm.spawnPattern.maxTimeFromActions;
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

        sm.timeAddedThroughActions = Mathf.Clamp(sm.timeAddedThroughActions, 0f, sm.spawnPattern.maxTimeFromActions);
        float timeAdded = sm.timeAddedThroughActions - prevTime;
        sm.startNextWaveDelay += timeAdded;
    }

    static void UpdateBossWave(EnemySpawnManager sm, RaidSpawnPattern.BossPhase bossPhase) {
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

    static void QueueBossTrashRefill(EnemySpawnManager sm, RaidSpawnPattern.BossPhase bossPhase) {
        sm.nextTrashRefillTime = sm.timeInCurPhase + bossPhase.minTrashRefillDelay;
        sm.spawnTimeIndex = 0;
        LayOutSpawnEvents(sm, bossPhase, sm.CurVariant, sm.spawnEvents, isBossTrash: true);
        spawnLimiterForEnemyBatching.MakeCurrent();
    }

    static int AliveTrashCount(EnemyData ofType) {
        int count = 0;
        foreach (Enemy enemy in entities.enemies) {
            if (!enemy.isBossTrash || enemy.health <= 0) continue;
            if (ofType != null && enemy.data != ofType) continue;
            count++;
        }
        return count;
    }

    static void SpawnRaidBoss(RaidSpawnPattern.BossPhase bossPhase) {
        Vector2Int spawnCellRange = bossPhase.spawnCellRange;
        Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, GatherEnemyPositions());

        EnemyData bossEnemy = bossPhase.raidBoss;
        Enemy enemy = bossEnemy.type switch {
            EnemyData.EnemyType.FireBoss => SpawnEntity<FireBoss>(bossEnemy.enemyPrefab, randomSpawnPos, Quaternion.identity, data: bossEnemy),
            EnemyData.EnemyType.SulfurBoss => SpawnEntity<SulfurBoss>(bossEnemy.enemyPrefab, randomSpawnPos, Quaternion.identity, data: bossEnemy),
            _ => throw new ArgumentOutOfRangeException(),
        };
        entities.boss = enemy;
    }

    // Lays out when each of the variant's enemies spawn across the phase's spawn duration, following its spawn rate curve
    static void BeginSpawningVariant(EnemySpawnManager sm, RaidSpawnPattern.PhasePool phase, RaidSpawnPattern.Variant variant) {
#if UNITY_EDITOR
        foreach (RaidSpawnPattern.EnemyBatch batch in variant.enemyBatches) {
            if (batch.enemyCount >= EnemySpawnManager.prefixedSumResolution) {
                Debug.LogError($"Wave cannot have more enemies than {nameof(EnemySpawnManager.prefixedSumResolution)}");
            }
        }
#endif

        sm.timeInCurPhase = 0f;
        sm.spawnTimeIndex = 0;
        LayOutSpawnEvents(sm, phase, variant, sm.spawnEvents);
        spawnLimiterForEnemyBatching.MakeCurrent();
    }

    static void LayOutSpawnEvents(EnemySpawnManager sm, RaidSpawnPattern.PhasePool phase, RaidSpawnPattern.Variant variant, List<(float time, EnemyData enemy)> spawnEvents, bool isBossTrash = false) {
        float startTime = isBossTrash ? sm.timeInCurPhase : 0f;
        float totalWeight = 0f;
        for (int i = 0; i < EnemySpawnManager.prefixedSumResolution; i++) {
            float sliceIndex = i / (float)(EnemySpawnManager.prefixedSumResolution - 1);
            float weight = Mathf.Clamp01(phase.spawnRateCurve.Evaluate(sliceIndex));
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
                spawnEvents.Add((startTime + normalizedTime * phase.spawnDuration, batch.enemyData));
            }
        }

        // Due to the way we add elements we need to sort by time so its chronologically ordered
        spawnEvents.Sort(static (x, y) => x.time.CompareTo(y.time));
    }

    static void SpawnDueEnemies(EnemySpawnManager sm) {
        if (sm.CurPhase == null) return;

        const float batchTime = 1f;
        if (!spawnLimiterForEnemyBatching.TimeHasPassed(batchTime) || sm.spawnEvents.Count <= 0) return;

        SpawnDueEvents(sm.spawnEvents, ref sm.spawnTimeIndex, sm.timeInCurPhase, sm.CurPhasePool.spawnCellRange, sm.OnBossWave);
    }

    static void SpawnDueEvents(List<(float time, EnemyData enemy)> spawnEvents, ref int spawnTimeIndex, float time, Vector2Int spawnCellRange, bool isBossTrash) {
        List<Vector2> enemyPositions = GatherEnemyPositions();

        while (spawnEvents.IndexInRange(spawnTimeIndex) && spawnEvents[spawnTimeIndex].time <= time) {
            Vector2 randomSpawnPos = curRaid.mapInstance.grid.GetSpawnPosition(player.position, spawnCellRange.x, spawnCellRange.y, false, enemyPositions);
            enemyPositions.Add(randomSpawnPos);

            EnemyData enemyToSpawn = spawnEvents[spawnTimeIndex].enemy;
            Enemy enemy = SpawnEntity<Enemy>(enemyToSpawn.enemyPrefab, randomSpawnPos, Quaternion.identity, data: enemyToSpawn);
            enemy.isBossTrash = isBossTrash;

            TeleportEnemy(enemy, randomSpawnPos, TeleportType.Spawn);
            spawnTimeIndex++;
        }
    }

    static readonly List<Vector2> _gatheredEnemyPositions = new();

    static List<Vector2> GatherEnemyPositions() {
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
