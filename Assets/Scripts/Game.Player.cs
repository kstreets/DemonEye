using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public partial class Game {
    
    public class Player : Entity {
        public CapsuleCollider2D hurtCollider;
        public Vector3 velocity;
        
        public int nextIdleAnimHash;
        public int nextIdleDir;
        
        public bool bleeding;
        public Limiter bleedLimiter;
        
        public Limiter attackLimiter;
        public Limiter enemyCollisionDamageLimiter;
        
        public float lastShotTime;
        public int consecutiveShotCount;
        public float curStepDistance;
        
        public ItemConsumption consumption;
        public bool isConsumingItem => consumption.tween.isAlive;
        
        public HealingOverTime healing;
        public bool isHealingOverTime => healing.tween.isAlive;

        public Sprite defaultPlayerPreviewSprite;
        public PlayerState state;
    }

    public class PlayerState {
        public int initHealth;
        public int soulCurrency;
        public int coinCurrency;
        public int hasteSkillLevel;
        public int intellectSkillLevel;
        public int lifeBloodSkillLevel;
        public int strengthSkillLevel;
    }
    
    public enum PlayerStat {
        BleedResist, CarryCapacity, CritChance, CritMulti, DamageMulti, FireratePercentage, Health, 
        HealingAmount, HealingSpeed, LootingSpeed, MovementSpeedPercentage, ProjectileCount, RangePercentage,
    }
    
    private static class PlayerAnimations {
        public static int runSide = Animator.StringToHash("PlayerRunSide");
        public static int runUp = Animator.StringToHash("PlayerRunUp");
        public static int runDown = Animator.StringToHash("PlayerRunDown");
        public static int idleSide = Animator.StringToHash("PlayerIdleSide");
        public static int idleUp = Animator.StringToHash("PlayerIdleUp");
        public static int idleDown = Animator.StringToHash("PlayerIdleDown");
        public static int death = Animator.StringToHash("PlayerDeath");
        public static int drink = Animator.StringToHash("PlayerDrink");
        public static int eat = Animator.StringToHash("PlayerEat");
        public static int bandage = Animator.StringToHash("PlayerBandage");
    }
    
    public static Player player => gameInstance.entities.player;
    
    private Player MakePlayer() {
        Player newPlayer = SpawnEntity<Player>(prefabs.player, Vector3.zero, Quaternion.identity, null, EntityLifetime.Global);
        newPlayer.hurtCollider = newPlayer.gameObject.GetComponentInChildren<CapsuleCollider2D>();
        newPlayer.defaultPlayerPreviewSprite = playerPanel.previewImage.sprite;
        return newPlayer;
    }
    
    private void InitPlayerState(Player instancedPlayer, GameState gameState) {
        instancedPlayer.state = gameState?.playerState ?? new();
        // We want to make sure that the player health is never <= zero
        int startingHealth = instancedPlayer.state.initHealth;
        instancedPlayer.health = startingHealth <= 0f ? FullPlayerHealth() : startingHealth;
    }
    
    private void InitPlayer() {
        player.animator.Play(PlayerAnimations.idleDown);
        player.nextIdleAnimHash = PlayerAnimations.idleDown;
    }
    
    private void DeinitPlayer() {
        player.bleeding = false;
        playerPanel.previewImage.sprite = player.defaultPlayerPreviewSprite;
    }
    
    private void PlayerOnEnemyDeath(Enemy enemy) {
        if (trinkets.equiped is SpeedBoostTrinket speedBoost) {
            if (++trinkets.data.trackingCount >= speedBoost.killsPerBoost) {
                trinkets.data.trackingCount = 0;
                trinkets.data.activeDuration.Add(speedBoost.duration);
            }
        }
        player.state.soulCurrency += enemy.data.soulWorthPerKill;
    }
    
    private void UpdatePlayer() {
        if (raidEnterSequence.isAlive) return;
        
        if (player.bleeding && player.bleedLimiter.TimeHasPassed(3.5f)) {
            const int bleedDamage = 5;
            player.health -= bleedDamage;
            
            Entity bloodDrop = SpawnEntity(entityPools.bloodDrop, OffsetY(player.position, 0.11f), Quaternion.identity);
            AddParentEffect(bloodDrop, player, 0.4f);
            DestroyEntity(bloodDrop, 0.8f);
            
            SpawnDamageNumber(OffsetY(player.position, 0.05f), bleedDamage, DamageColor.Blood);
            AddFlashHitEffect(player);
            Tween.PunchScale(playerInfo.bleedDebuffIcon.transform, Vector3.one * 0.8f, 0.25f, 5f);

            if (PlayerHealthIsAtAutoBleedStop()) {
                player.bleeding = false;
            }
        }
        
        if (player.isConsumingItem) {
            player.nextIdleAnimHash = PlayerAnimations.idleDown;
            return;
        }

        if (PlayerInventoryIsOpen || InteractingWithPortal()) return;
        
        Vector2 moveInput = input.move.ReadValue<Vector2>();
        Vector2 prevPos = player.position;
        
        float speed = GetPlayerSpeed();
        Vector3 frameVelocity = new Vector3(moveInput.x, moveInput.y, 0f) * speed;

        const float acceleration = 18f;
        player.velocity = Vector3.Lerp(player.velocity, frameVelocity, acceleration * Time.deltaTime);
        
        player.position += player.velocity * Time.deltaTime;
        player.curStepDistance += Vector2.Distance(prevPos, player.position);

        if (moveInput != Vector2.zero) {
            player.spriteRenderer.flipX = moveInput.x < 0;
            player.nextIdleDir = (int)Mathf.Sign(moveInput.x);
        }
        else {
            player.spriteRenderer.flipX = player.nextIdleDir < 0;
        }
        
        bool movingProdominatelyVertical = Mathf.Abs(Vector2.Dot(Vector2.up, moveInput)) > 0.9f;
        
        if (moveInput.magnitude > 0.1f && !movingProdominatelyVertical) {
            player.animator.Play(PlayerAnimations.runSide);
            player.nextIdleAnimHash = PlayerAnimations.idleSide;
        }
        else if (moveInput.y > 0) {
            player.animator.Play(PlayerAnimations.runUp);
            player.nextIdleAnimHash = PlayerAnimations.idleUp;
        }
        else if (moveInput.y < 0) {
            player.animator.Play(PlayerAnimations.runDown);
            player.nextIdleAnimHash = PlayerAnimations.idleDown;
        }
        else {
            player.animator.Play(player.nextIdleAnimHash);
        }
        
        bool playerStepped = moveInput != Vector2.zero && player.curStepDistance > 0.18f;
        if (playerStepped) {
            Entity runSmokeEntity = SpawnEntity(entityPools.runSmoke, OffsetY(player.position, 0.01f), Quaternion.identity);
            DestroyEntity(runSmokeEntity, CurrentClipLength(runSmokeEntity.animator));
            PlayAudioClip(audio.footStepClip, player.position);
            player.curStepDistance = 0f;
            
            if (trinkets.equiped is HealingStepsTrinket healingSteps) {
                if (++trinkets.data.trackingCount >= healingSteps.stepsPerHeal) {
                    HealPlayer(healingSteps.healing);
                    trinkets.data.trackingCount = 0;
                }
            }
        }
        
        bool canShoot = player.attackLimiter.TimeHasPassed(GetFirerateDelay());
        if (!canShoot) return;
        
        float projCount = GetAbsoluteStat(PlayerStat.ProjectileCount);
        int targetCount = Mathf.FloorToInt(projCount);
        float extraProjChance = projCount % 1;
        if (RollProbability(extraProjChance)) {
            targetCount++;
        }

        using var _ = ListPool<Vector3>.Get(out var attackTargets);
        GetAttackTargets(targetCount, ref attackTargets);
        if (attackTargets.Count <= 0) return;
        
        PlayAudioClip(audio.shootClip, player.position);
        for (int i = 0; i < attackTargets.Count; i++) {
            Vector3 attackTarget = attackTargets[i];
            
            bool isPrimaryShot = i == 0;
            if (isPrimaryShot) {
                ShootProjectile(attackTarget);
                if (trinkets.equiped is CauterizingWoundsTrinket cauterizingWounds) {
                    if (player.bleeding && RollProbability(cauterizingWounds.chancePerShotToStopBleeding)) {
                        player.bleeding = false;
                        SpawnTrinketActivationText(cauterizingWounds.activationText);
                    }
                }
            }
            
            bool isAdditionalShot = i > 0;
            if (isAdditionalShot) {
                if (demonEye.equiped.multiProjectileCritAugment.TryGetValue(out var multiProjCrit)) {
                    ShootProjectile(attackTarget, flatCritChance: multiProjCrit.probability);
                }
                else {
                    ShootProjectile(attackTarget);
                }
            }

            if (demonEye.equiped.doubleTapAugment.TryGetValue(out var doubleTap) && RollProbability(doubleTap.probability)) { 
                ShootProjectile(attackTarget, spawnDelay: doubleTap.delayBetweenShots);
            }
        }

        float consecutiveShotDelay = config.gameplay.attackDelay * 1.5f;
        if (Time.time - player.lastShotTime <= consecutiveShotDelay) {
            player.consecutiveShotCount++;
        }
        else {
            player.consecutiveShotCount = 0;
        }
        
        if (demonEye.equiped.blast.TryGetValue(out var blast) && player.consecutiveShotCount > 0 && player.consecutiveShotCount % blast.numshotsUntilOverheat == 0) {
            Vector2 spawnPos = OffsetY(player.position, 0.1f);
            
            Entity expEntity = SpawnEntity(entityPools.blast, spawnPos, Quaternion.identity); 
            DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
            
            List<Collider2D> cols = Physics.OverlapCircle(spawnPos, blast.radius, Masks.EnemyMask);
            foreach (Collider2D col in cols) {
                Enemy enemy = entities.lookup[col.gameObject] as Enemy;
                int damage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * blast.damageMulti);
                DamageEnemyAfterDelay(enemy, damage, false, 0.15f);
            }
        }
            
        player.lastShotTime = Time.time;
    }
    
    private void GetAttackTargets(int targetCount, ref List<Vector3> targets) {
        float overlapDist = config.gameplay.projectileSpeed * GetProjectileRangeInSeconds();
        List<Collider2D> cols = Physics.OverlapCircle(player.position, overlapDist, Masks.TargetableEnemyMask);
        
        if (cols.Count <= 0) {
            cols = Physics.OverlapCircle(player.position, overlapDist, Masks.MineableMask);
        }
        
        cols.Sort(static (a, b) => {
            float aScore = GetTargetScore(a);
            float bScore = GetTargetScore(b);
            return aScore.CompareTo(bScore);
        });

        int count = Mathf.Min(targetCount, cols.Count);
        targets.Clear();
        for (int i = 0; i < count; i++) {
            targets.Add(entities.lookup[cols[i].gameObject].Center);
        }
    }

    private static float GetTargetScore(Collider2D col) {
        Entity entity = gameInstance.entities.lookup[col.gameObject];
        float dist = Vector2.Distance(col.transform.position, player.position);

        if (entity is Enemy enemy) {
            const float distWeight = 1f;
            const float healthWeight = -0.003f;
            float bleedingWeight = enemy.bleed.HasValue ? 0.2f : 0f;
            return (dist * distWeight) + (enemy.health * healthWeight) + bleedingWeight;
        }
        
        return dist;
    }

    private Vector3 PlayerEyePos => player.position + new Vector3(0f, 0.13f, 0f);
    
    private void ShootProjectile(Vector2 targetPos, float? spawnDelay = default, float? flatCritChance = default) {
        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - config.gameplay.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = config.gameplay.projectileSpeed;
        Vector2 dir = (targetPos - (Vector2)PlayerEyePos).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * projectileSpeed; 
        _SpawnProjectile(PlayerEyePos, velocity, entityPools.projectile);
        
        if (demonEye.equiped.trishot.TryGetValue(out var trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 10f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            _SpawnProjectile(PlayerEyePos, secondShotVelocity, entityPools.projectile, flgs: ProjectileTypeFlags.Trishot);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            _SpawnProjectile(PlayerEyePos, thirdShotVelocity, entityPools.projectile, flgs: ProjectileTypeFlags.Trishot);
        }

        if (demonEye.equiped.backwardShot.TryGetValue(out var backShot) && RollProbability(backShot.probability)) {
            const float backwardsShotSpeedScaler = 1.1f;
            EntityPool<Projectile> pool = demonEye.equiped.backwardsPiercingAugment.HasValue ? entityPools.piercingShotProjectile : entityPools.projectile; 
            _SpawnProjectile(PlayerEyePos, -velocity * backwardsShotSpeedScaler, pool, flgs: ProjectileTypeFlags.BackwardsShot);
        }
        
        // Helper method just to forward the passed in parameters
        void _SpawnProjectile(Vector2 pos, Vector2 vel, EntityPool<Projectile> pool, ProjectileTypeFlags flgs = ProjectileTypeFlags.None) {
            float lifeTime = GetProjectileRangeInSeconds();
            SpawnProjectile(pool, pos, vel, lifeTime, player, typeFlags: flgs, spawnDelay: spawnDelay, flatCritChance: flatCritChance);
        }
    }
    
    public struct ItemConsumption {
        public Tween tween;
        public Inventory inventory;
        public int slotIndex;
    }

    private void HavePlayerConsumeItem(Inventory fromInventory, int slotIndex) {
        if (player.isHealingOverTime || player.isConsumingItem) return;
        ConsumableItem item = fromInventory.slots[slotIndex].itemInstance.ItemRef as ConsumableItem;

        if (!item) return;
        
        bool itemHeals = item.healingAmount > 0;
        bool itemStopsBleeds = item.bandageAmount > 0;

        if (itemHeals && itemStopsBleeds) {
            if (player.health == FullPlayerHealth() && !player.bleeding) return;
        }
        else if (itemHeals) {
            if (player.health == FullPlayerHealth()) return;
        }
        else if (itemStopsBleeds) {
            if (!player.bleeding) return;
        }
        
        const float additionalConsumeDelay = 0.15f;
        const float performActionAtAnimationCompletion = 0.9f;
        
        int animationHash = item.animationType switch {
            ConsumableItem.AnimationType.Drink   => PlayerAnimations.drink,
            ConsumableItem.AnimationType.Eat     => PlayerAnimations.eat,
            ConsumableItem.AnimationType.Bandage => PlayerAnimations.bandage,
            _                                    => throw new ArgumentOutOfRangeException(),
        };

        player.animator.Play(animationHash);
        player.animator.Update(0);

        float animationLength = CurrentClipLength(player.animator); 
        float actionDelay = animationLength * performActionAtAnimationCompletion;
        float postActionDelay = animationLength * (1f - performActionAtAnimationCompletion);

        // So we don't allocate any memory for the closure
        player.consumption.slotIndex = slotIndex;
        player.consumption.inventory = fromInventory;
        
        player.consumption.tween = Tween.Delay(item, actionDelay, static (item) => {
            if (item.healingAmount > 0) {
                gameInstance.HealPlayer(item.healingAmount, item.healingDuration);
            }
            if (item.bandageAmount > 0) {
                player.bleeding = false;
                gameInstance.thisFrame.flags |= FrameFlags.BleedStopped;
            }
            gameInstance.ReduceItemCountInInventory(player.consumption.inventory, player.consumption.slotIndex);
        });
        
        player.consumption.tween.OnUpdate(this, static (_, _) => {
            if (gameInstance.playerPanel.previewImage.sprite != player.spriteRenderer.sprite) {
                gameInstance.playerPanel.previewImage.sprite = player.spriteRenderer.sprite;     
            }
        });
        
        player.consumption.tween.Chain(Tween.Delay(postActionDelay, static () => {
            player.animator.Play(PlayerAnimations.idleDown);
            player.animator.Update(0f);
            if (gameInstance.playerPanel.previewImage.sprite != player.spriteRenderer.sprite) {
                gameInstance.playerPanel.previewImage.sprite = player.spriteRenderer.sprite;     
            }
        }))
        .Chain(Tween.Delay(additionalConsumeDelay));
    }
    
    public struct HealingOverTime {
        public Tween tween;
        public int healingGiven;
        public int targetHealing;
        public float healingPerSecond;
    } 

    private void HealPlayer(int healing, float duration = 0f) {
        if (duration <= 0f) {
            int clampedHealing = Mathf.Clamp(player.health + healing, 0, FullPlayerHealth());
            player.health = clampedHealing;
            thisFrame.data.healing += clampedHealing;
            return;
        }
        
        Assert.IsFalse(player.isHealingOverTime, "Player is already healing over time, only 1 healing over time can be active");
        
        player.healing.healingGiven = 0;
        player.healing.targetHealing = healing;
        player.healing.healingPerSecond = healing / duration;
        
        player.healing.tween = Tween.Delay(duration)
        .OnUpdate(this, static (_, tween) => {
            int fullPlayerHealth = gameInstance.FullPlayerHealth();
            float healingPerSecond = player.healing.healingPerSecond;
            float elapsedTime = tween.elapsedTime;
            
            int curTotalHealing = Mathf.FloorToInt(healingPerSecond * elapsedTime);
            int healthToAdd = Mathf.Clamp(curTotalHealing - player.healing.healingGiven, 0, int.MaxValue);
            gameInstance.HealPlayer(healthToAdd);
            player.healing.healingGiven += healthToAdd;
            
            if (player.health == fullPlayerHealth) {
                tween.Complete();
            }
        })
        .OnComplete(static () => {
            int remainingHealthToGiveFromFloatingPointError = Mathf.Clamp(player.healing.targetHealing - player.healing.healingGiven, 0, int.MaxValue);
            gameInstance.HealPlayer(remainingHealthToGiveFromFloatingPointError);
        });
    }
    
    public enum PlayerDamageType { Normal, Collision }

    public void DamagePlayer(int damage, PlayerDamageType damageType, Entity sourceEntity, float chanceToBleed = 0f) {
        chanceToBleed -= GetAbsoluteStat(PlayerStat.BleedResist); 
        if (!player.bleeding && !curRaid.map.playerCantBleed && !PlayerHealthIsAtAutoBleedStop() && RollProbability(chanceToBleed)) {
            player.bleeding = true;
        }
        
        bool ignoreCollisionDamage = !player.enemyCollisionDamageLimiter.TimeHasPassed(config.gameplay.repeatCollisionDamageDelay);
        if (damageType == PlayerDamageType.Collision && ignoreCollisionDamage) return;
        
        player.health -= damage;
        AddFlashHitEffect(player);
        SpawnDamageNumber(player.position, damage, DamageColor.Blood);
        CancelPortalSummoning();
        
        float damageImpactScale = Mathf.Clamp01(damage / 65f);
        float damageShakeFreq = Mathf.Lerp(6f, 10f, damageImpactScale);
        float damageShakeMag = Mathf.Lerp(0.02f, 0.1f, damageImpactScale);
        camera.cameraShake.Shake(damageShakeFreq, damageShakeMag, 0.6f);
        
        if (trinkets.equiped is Thorns thorns && trinkets.data.cooldownDuration.HasPassed()) {
            Entity damageEntity = sourceEntity switch {
                Enemy enemy => enemy,
                Projectile proj => proj.sourceEntity, 
                _ => null,
            };
            if (damageEntity != null) {
                DamageEnemy(damageEntity, damage, isCriticalStrike: false);
                SpawnTrinketActivationText(thorns.activationPopUpText);
                trinkets.data.cooldownDuration.Reset(thorns.cooldownTime);
            }
        }
    }
    
    private bool PlayerHealthIsAtAutoBleedStop() {
        const float percentageOfHealthBleedingStops = 0.10f;
        return player.health <= FullPlayerHealth() * percentageOfHealthBleedingStops;
    }
    
    private int GetPlayerStatLevel(PlayerStat stat) {
        return stat switch {
            PlayerStat.FireratePercentage      => player.state.hasteSkillLevel,
            PlayerStat.MovementSpeedPercentage => player.state.hasteSkillLevel,
            PlayerStat.LootingSpeed            => player.state.hasteSkillLevel,
        
            PlayerStat.CritChance      => player.state.intellectSkillLevel,
            PlayerStat.CritMulti       => player.state.intellectSkillLevel,
            PlayerStat.ProjectileCount => player.state.intellectSkillLevel,
            
            PlayerStat.Health        => player.state.lifeBloodSkillLevel,
            PlayerStat.HealingAmount => player.state.lifeBloodSkillLevel,
            PlayerStat.HealingSpeed  => player.state.lifeBloodSkillLevel,
            
            PlayerStat.BleedResist   => player.state.strengthSkillLevel,
            PlayerStat.DamageMulti   => player.state.strengthSkillLevel,
            PlayerStat.CarryCapacity => player.state.strengthSkillLevel,
            
            _ => 0,
        };
    }
    
    private float GetAbsoluteStat(PlayerStat stat) {
        return GetPlayerStat(stat) + GetEquipmentStatAdjustment(stat);
    }
    
    private float GetPlayerStat(PlayerStat stat) {
        float startingValue = stat switch {
            PlayerStat.CarryCapacity           => config.gameplay.defaultStartingEncumberingWeight,
            PlayerStat.CritChance              => config.gameplay.defaultCritChance,
            PlayerStat.CritMulti               => config.gameplay.defaultCritMulti,
            PlayerStat.DamageMulti             => 1f,
            PlayerStat.FireratePercentage      => 1f,
            PlayerStat.Health                  => 100f,
            PlayerStat.LootingSpeed            => 1f,
            PlayerStat.MovementSpeedPercentage => 1f,
            PlayerStat.ProjectileCount         => 1f,
            PlayerStat.RangePercentage         => 1f,
            _                                   => 0f, 
        };
        return startingValue + GetPlayerStatAdjustment(stat);
    }

    private float GetPlayerStatAdjustment(PlayerStat stat) {
        return stat switch {
            PlayerStat.CarryCapacity           => GetPlayerStatLevel(PlayerStat.CarryCapacity) * config.gameplay.carryCapacityIncPerLevel,
            PlayerStat.CritChance              => GetPlayerStatLevel(PlayerStat.CritChance) * config.gameplay.critChanceIncPerLevel,
            PlayerStat.CritMulti               => GetPlayerStatLevel(PlayerStat.CritMulti) * config.gameplay.critMultiplierIncPerLevel,
            PlayerStat.DamageMulti             => GetPlayerStatLevel(PlayerStat.DamageMulti) * config.gameplay.damageMultiplierIncPerLevel,
            PlayerStat.FireratePercentage      => GetPlayerStatLevel(PlayerStat.FireratePercentage) * config.gameplay.firerateIncPerLevel,
            PlayerStat.Health                  => GetPlayerStatLevel(PlayerStat.Health) * config.gameplay.healthIncPerLevel,
            PlayerStat.HealingAmount           => GetPlayerStatLevel(PlayerStat.HealingAmount) * config.gameplay.healingIncPerLevel,
            PlayerStat.HealingSpeed            => GetPlayerStatLevel(PlayerStat.HealingSpeed) * config.gameplay.healingSpeedIncPerLevel,
            PlayerStat.LootingSpeed            => GetPlayerStatLevel(PlayerStat.LootingSpeed) * config.gameplay.lootingSpeedIncPerLevel,
            PlayerStat.MovementSpeedPercentage => GetPlayerStatLevel(PlayerStat.MovementSpeedPercentage) * config.gameplay.movementSpeedIncPerLevel,
            PlayerStat.ProjectileCount         => GetPlayerStatLevel(PlayerStat.ProjectileCount) * config.gameplay.projectileCountIncPerLevel,
            _                                   => 0f,
        };
    }
    
    private float GetEquipmentStatAdjustment(PlayerStat stat) {
        float statSum = 0f;
        foreach (EquipedUpgradeInstance mod in demonEye.equiped.upgradeInstances) {
            EyeUpgrade eyeUpgrade = mod.EyeUpgrade;
            int stackCount = mod.stackCount;
            if (!eyeUpgrade.modifiesStats) continue;

            switch (stat) {
                case PlayerStat.CritChance:
                    statSum += eyeUpgrade.GetCritChance(stackCount); 
                    break;
                case PlayerStat.CritMulti:
                    statSum += eyeUpgrade.GetCritMultiplier(stackCount); 
                    break;
                case PlayerStat.DamageMulti:
                    statSum += eyeUpgrade.GetDamageMultiplier(stackCount); 
                    break;
                case PlayerStat.FireratePercentage:
                    statSum += eyeUpgrade.GetFireratePercentage(stackCount); 
                    break;
                case PlayerStat.ProjectileCount:
                    statSum += eyeUpgrade.GetProjectileCount(stackCount); 
                    break;
                case PlayerStat.RangePercentage:
                    statSum += eyeUpgrade.GetRangePercentage(stackCount);
                    break;
            }
        }
        return statSum;
    }

    private int FullPlayerHealth() => 100 + (int)GetPlayerStatAdjustment(PlayerStat.Health);

    private float GetPlayerSpeed() {
        float playerSpeed = config.gameplay.baseSpeed * GetAbsoluteStat(PlayerStat.MovementSpeedPercentage);
        
        float speedReductionFromWeight = Mathf.Lerp(0f, config.gameplay.maxEncumberedSpeedReduction, GetOverweightCompletion());
        speedReductionFromWeight = Mathf.Clamp(speedReductionFromWeight, 0f, config.gameplay.maxEncumberedSpeedReduction);

        playerSpeed -= speedReductionFromWeight;
        
        if (trinkets.equiped is SpeedBoostTrinket speedBoost) {
            if (trinkets.data.activeDuration.IsAlive()) {
                playerSpeed += playerSpeed * speedBoost.percentSpeedIncrease;
            }
        }
        
        return playerSpeed;
    }

    private float GetFirerateDelay() {
        if (demonEye.equiped == demonEye.empty) {
            return config.gameplay.attackDelay;
        }

        float attackDelay = config.gameplay.attackDelay / GetAbsoluteStat(PlayerStat.FireratePercentage);
        return Mathf.Clamp(attackDelay, config.gameplay.cappedMinAttackDelay, config.gameplay.attackDelay);
    }
    
    private float GetProjectileRangeInSeconds() {
        return config.gameplay.rangeInSeconds * GetAbsoluteStat(PlayerStat.RangePercentage);
    }
    
    private void GetEncumberingWeightRange(out int startingWeight, out int endingWeight) {
        startingWeight = (int)GetPlayerStat(PlayerStat.CarryCapacity);
        int encumberingIncreaseFromStrength = (int)GetPlayerStatAdjustment(PlayerStat.CarryCapacity);
        endingWeight = config.gameplay.maxEncumberedWeight + encumberingIncreaseFromStrength;
    }

    private float GetOverweightCompletion() {
        GetEncumberingWeightRange(out int startingEncumberingWeight, out int endingEncumberingWeight);
        int inventoryWeight = GetInventoryWeight(inventories.player);
        int curOverweightAmount = Mathf.Clamp(inventoryWeight - startingEncumberingWeight, 0, int.MaxValue);
        float maxOverweightAmount = (float)endingEncumberingWeight - startingEncumberingWeight;
        float overweightComp = curOverweightAmount / maxOverweightAmount;
        return Mathf.Clamp01(overweightComp);
    }
    
}
