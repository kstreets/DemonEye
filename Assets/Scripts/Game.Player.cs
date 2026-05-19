using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
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
        
        public int soulCurrency;
        public int coinCurrency;
        
        public int hasteSkillLevel;
        public int intellectSkillLevel;
        public int lifeBloodSkillLevel;
        public int strengthSkillLevel;
        
        public readonly int runSideAnim = Animator.StringToHash("PlayerRunSide");
        public readonly int runUpAnim = Animator.StringToHash("PlayerRunUp");
        public readonly int runDownAnim = Animator.StringToHash("PlayerRunDown");
        public readonly int idleSideAnim = Animator.StringToHash("PlayerIdleSide");
        public readonly int idleUpAnim = Animator.StringToHash("PlayerIdleUp");
        public readonly int idleDownAnim = Animator.StringToHash("PlayerIdleDown");
        public readonly int deathAnim = Animator.StringToHash("PlayerDeath");
        public readonly int drinkAnim = Animator.StringToHash("PlayerDrink");
        public readonly int eatAnim = Animator.StringToHash("PlayerEat");
        public readonly int bandageAnim = Animator.StringToHash("PlayerBandage");

        public float lastShotTime;
        public int consecutiveShotCount;
        public float curStepDistance;
        
        public Tween consumingTween;
        public Inventory consumingInventory;
        public int consumingSlotIndex;

        public Sprite defaultPlayerPreviewSprite;

        public enum Stat {
            BleedResist, CarryCapacity, CritChance, CritMulti, DamageMulti, FireratePercentage, Health, 
            HealingAmount, HealingSpeed, LootingSpeed, MovementSpeedPercentage, ProjectileCount, RangePercentage,
        }
    }
    
    private Player MakePlayer() {
        Player newPlayer = SpawnEntity<Player>(prefabs.player, Vector3.zero, Quaternion.identity, null, EntityLifetime.Global);
        LoadAndAssignPlayerSaveData(newPlayer);
        newPlayer.hurtCollider = newPlayer.gameObject.GetComponentInChildren<CapsuleCollider2D>();
        newPlayer.defaultPlayerPreviewSprite = playerPanel.previewImage.sprite;
        return newPlayer;
    }
    
    private void InitPlayer() {
        player.animator.Play(player.idleDownAnim);
        player.nextIdleAnimHash = player.idleDownAnim;
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
        player.soulCurrency += enemy.data.soulWorthPerKill;
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
        
        bool consumingItem = player.consumingTween.isAlive;
        if (consumingItem) {
            player.nextIdleAnimHash = player.idleDownAnim;
            return;
        }

        if (InventoryIsOpen || InteractingWithPortal()) return;
        
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
            player.animator.Play(player.runSideAnim);
            player.nextIdleAnimHash = player.idleSideAnim;
        }
        else if (moveInput.y > 0) {
            player.animator.Play(player.runUpAnim);
            player.nextIdleAnimHash = player.idleUpAnim;
        }
        else if (moveInput.y < 0) {
            player.animator.Play(player.runDownAnim);
            player.nextIdleAnimHash = player.idleDownAnim;
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
        
        bool canShoot = player.attackLimiter.TimeHasPassed(GetFirerateDelayBasedOnStats());
        if (!canShoot) return;
        
        float projCount = GetAbsoluteStat(Player.Stat.ProjectileCount);
        int targetCount = Mathf.FloorToInt(projCount);
        float extraProjChance = projCount % 1;
        if (RollProbability(extraProjChance)) {
            targetCount++;
        }

        List<Vector3> attackTargets = GetAttackTargets(targetCount);
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
    
    internal List<Vector3> GetAttackTargets(int targetCount) {
        float overlapDist = config.gameplay.projectileSpeed * GetProjectileRangeInSeconds();
        List<Collider2D> cols = Physics.OverlapCircle(player.position, overlapDist, Masks.EnemyMask);
        
        if (cols.Count <= 0) {
            cols = Physics.OverlapCircle(player.position, overlapDist, Masks.MineableMask);
        }
        
        cols.Sort(static (a, b) => {
            float aScore = GetTargetScore(a);
            float bScore = GetTargetScore(b);
            return aScore.CompareTo(bScore);
        });

        int count = Mathf.Min(targetCount, cols.Count);
        List<Vector3> targets = new();
        for (int i = 0; i < count; i++) {
            targets.Add(entities.lookup[cols[i].gameObject].Center);
        }
        return targets;
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
            const float baseTriShotAngle = 8f;
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

    private void HavePlayerConsumeItem(Inventory fromInventory, int slotIndex) {
        if (playerIsHealingOverTime || player.consumingTween.isAlive) return;
        ConsumableItem item = fromInventory.slots[slotIndex].itemInstance.ItemRef as ConsumableItem;

        if (!item) return;
        
        bool itemHeals = item.healingAmount > 0;
        bool itemStopsBleeds = item.bandageAmount > 0;

        if (itemHeals && itemStopsBleeds) {
            if (player.health == FullPlayerHealth && !player.bleeding) return;
        }
        else if (itemHeals) {
            if (player.health == FullPlayerHealth) return;
        }
        else if (itemStopsBleeds) {
            if (!player.bleeding) return;
        }
        
        const float additionalConsumeDelay = 0.15f;
        const float performActionAtAnimationCompletion = 0.9f;
        
        int animationHash = item.animationType switch {
            ConsumableItem.AnimationType.Drink   => player.drinkAnim,
            ConsumableItem.AnimationType.Eat     => player.eatAnim,
            ConsumableItem.AnimationType.Bandage => player.bandageAnim,
        };

        player.animator.Play(animationHash);
        player.animator.Update(0);

        float animationLength = CurrentClipLength(player.animator); 
        float actionDelay = animationLength * performActionAtAnimationCompletion;
        float postActionDelay = animationLength * (1f - performActionAtAnimationCompletion);

        // So we don't allocate any memory for the closure
        player.consumingSlotIndex = slotIndex;
        player.consumingInventory = fromInventory;
        
        player.consumingTween = Tween.Delay(item, actionDelay, static (item) => {
            if (item.healingAmount > 0) {
                gameInstance.HealPlayer(item.healingAmount, item.healingDuration);
            }
            if (item.bandageAmount > 0) {
                player.bleeding = false;
            }
            gameInstance.ReduceItemCountInInventory(player.consumingInventory, player.consumingSlotIndex);
        });
        
        player.consumingTween.OnUpdate(this, static (_, _) => {
            if (gameInstance.playerPanel.previewImage.sprite != player.spriteRenderer.sprite) {
                gameInstance.playerPanel.previewImage.sprite = player.spriteRenderer.sprite;     
            }
        });
        
        player.consumingTween.Chain(Tween.Delay(postActionDelay, static () => {
            player.animator.Play(player.idleDownAnim);
            player.animator.Update(0f);
            if (gameInstance.playerPanel.previewImage.sprite != player.spriteRenderer.sprite) {
                gameInstance.playerPanel.previewImage.sprite = player.spriteRenderer.sprite;     
            }
        }))
        .Chain(Tween.Delay(additionalConsumeDelay));
    }
    
    public class HealingOverTimeData {
        public Tween tween;
        public int healingGiven;
        public int targetHealing;
        public float healingPerSecond;
    } 
    
    private HealingOverTimeData healingOverTimeData = new();
    private bool playerIsHealingOverTime => healingOverTimeData.tween.isAlive;

    private void HealPlayer(int healing, float duration = 0f) {
        if (duration <= 0f) {
            player.health = Mathf.Clamp(player.health + healing, 0, FullPlayerHealth);
            return;
        }
        
        Assert.IsFalse(playerIsHealingOverTime, "Player is already healing over time, only 1 healing over time can be active");
        
        var data = healingOverTimeData;
        data.healingGiven = 0;
        data.targetHealing = healing;
        data.healingPerSecond = healing / duration;
        
        data.tween = Tween.Delay(duration)
        .OnUpdate(data, static (data, tween) => {
            int fullPlayerHealth = gameInstance.FullPlayerHealth;
            float healingPerSecond = data.healingPerSecond;
            float elapsedTime = tween.elapsedTime;
            
            int curTotalHealing = Mathf.FloorToInt(healingPerSecond * elapsedTime);
            int healthToAdd = Mathf.Clamp(curTotalHealing - data.healingGiven, 0, int.MaxValue);
            gameInstance.HealPlayer(healthToAdd);
            data.healingGiven += healthToAdd;
            
            if (player.health == fullPlayerHealth) {
                tween.Complete();
            }
        })
        .OnComplete(data, static (data) => {
            int remainingHealthToGiveFromFloatingPointError = Mathf.Clamp(data.targetHealing - data.healingGiven, 0, int.MaxValue);
            gameInstance.HealPlayer(remainingHealthToGiveFromFloatingPointError);
        });
    }
    
    public enum PlayerDamageType { Normal, Collision }

    public void DamagePlayer(int damage, PlayerDamageType damageType, Entity sourceEntity, float chanceToBleed = 0f) {
        chanceToBleed -= GetAbsoluteStat(Player.Stat.BleedResist); 
        if (!player.bleeding && !curRaid.map.playerCantBleed && !PlayerHealthIsAtAutoBleedStop() && RollProbability(chanceToBleed)) {
            player.bleeding = true;
        }
        
        bool ignoreCollisionDamage = !player.enemyCollisionDamageLimiter.TimeHasPassed(config.gameplay.repeatCollisionDamageDelay);
        if (damageType == PlayerDamageType.Collision && ignoreCollisionDamage) return;
        
        player.health -= damage;
        AddFlashHitEffect(player);
        SpawnDamageNumber(player.position, damage, DamageColor.Blood);
        CancelPortalSummoning();
        
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
        return player.health <= FullPlayerHealth * percentageOfHealthBleedingStops;
    }
    
    private int GetPlayerStatLevel(Player.Stat stat) {
        return stat switch {
            Player.Stat.FireratePercentage      => player.hasteSkillLevel,
            Player.Stat.MovementSpeedPercentage => player.hasteSkillLevel,
            Player.Stat.LootingSpeed            => player.hasteSkillLevel,
        
            Player.Stat.CritChance      => player.intellectSkillLevel,
            Player.Stat.CritMulti       => player.intellectSkillLevel,
            Player.Stat.ProjectileCount => player.intellectSkillLevel,
            
            Player.Stat.Health        => player.lifeBloodSkillLevel,
            Player.Stat.HealingAmount => player.lifeBloodSkillLevel,
            Player.Stat.HealingSpeed  => player.lifeBloodSkillLevel,
            
            Player.Stat.BleedResist   => player.strengthSkillLevel,
            Player.Stat.DamageMulti        => player.strengthSkillLevel,
            Player.Stat.CarryCapacity => player.strengthSkillLevel,
            
            _ => 0,
        };
    }
    
    private float GetAbsoluteStat(Player.Stat stat) {
        return GetPlayerStat(stat) + GetEquipmentStatAdjustment(stat);
    }
    
    private float GetPlayerStat(Player.Stat stat) {
        float startingValue = stat switch {
            Player.Stat.CarryCapacity           => config.gameplay.defaultStartingEncumberingWeight,
            Player.Stat.CritChance              => config.gameplay.defaultCritChance,
            Player.Stat.CritMulti               => config.gameplay.defaultCritMulti,
            Player.Stat.DamageMulti             => 1f,
            Player.Stat.FireratePercentage      => 1f,
            Player.Stat.Health                  => 100f,
            Player.Stat.LootingSpeed            => 1f,
            Player.Stat.MovementSpeedPercentage => 1f,
            Player.Stat.ProjectileCount         => 1f,
            Player.Stat.RangePercentage         => 1f,
            _                                   => 0f, 
        };
        return startingValue + GetPlayerStatAdjustment(stat);
    }

    private float GetPlayerStatAdjustment(Player.Stat stat) {
        return stat switch {
            Player.Stat.CarryCapacity           => GetPlayerStatLevel(Player.Stat.CarryCapacity) * config.gameplay.carryCapacityIncPerLevel,
            Player.Stat.CritChance              => GetPlayerStatLevel(Player.Stat.CritChance) * config.gameplay.critChanceIncPerLevel,
            Player.Stat.CritMulti               => GetPlayerStatLevel(Player.Stat.CritMulti) * config.gameplay.critMultiplierIncPerLevel,
            Player.Stat.DamageMulti             => GetPlayerStatLevel(Player.Stat.DamageMulti) * config.gameplay.damageMultiplierIncPerLevel,
            Player.Stat.FireratePercentage      => GetPlayerStatLevel(Player.Stat.FireratePercentage) * config.gameplay.firerateIncPerLevel,
            Player.Stat.Health                  => GetPlayerStatLevel(Player.Stat.Health) * config.gameplay.healthIncPerLevel,
            Player.Stat.HealingAmount           => GetPlayerStatLevel(Player.Stat.HealingAmount) * config.gameplay.healingIncPerLevel,
            Player.Stat.HealingSpeed            => GetPlayerStatLevel(Player.Stat.HealingSpeed) * config.gameplay.healingSpeedIncPerLevel,
            Player.Stat.LootingSpeed            => GetPlayerStatLevel(Player.Stat.LootingSpeed) * config.gameplay.lootingSpeedIncPerLevel,
            Player.Stat.MovementSpeedPercentage => GetPlayerStatLevel(Player.Stat.MovementSpeedPercentage) * config.gameplay.movementSpeedIncPerLevel,
            Player.Stat.ProjectileCount         => GetPlayerStatLevel(Player.Stat.ProjectileCount) * config.gameplay.projectileCountIncPerLevel,
            _                                   => 0f,
        };
    }
    
    private float GetEquipmentStatAdjustment(Player.Stat stat) {
        float statSum = 0f;
        foreach (EquipedUpgradeInstance mod in demonEye.equiped.upgradeInstances) {
            EyeUpgradeItem eyeUpgradeItem = mod.EyeUpgradeItem;
            int stackCount = mod.stackCount;
            if (!eyeUpgradeItem.modifiesStats) continue;

            switch (stat) {
                case Player.Stat.CritChance:
                    statSum += eyeUpgradeItem.GetCritChance(stackCount); 
                    break;
                case Player.Stat.CritMulti:
                    statSum += eyeUpgradeItem.GetCritMultiplier(stackCount); 
                    break;
                case Player.Stat.DamageMulti:
                    statSum += eyeUpgradeItem.GetDamageMultiplier(stackCount); 
                    break;
                case Player.Stat.FireratePercentage:
                    statSum += eyeUpgradeItem.GetFireratePercentage(stackCount); 
                    break;
                case Player.Stat.ProjectileCount:
                    statSum += eyeUpgradeItem.GetProjectileCount(stackCount); 
                    break;
                case Player.Stat.RangePercentage:
                    statSum += eyeUpgradeItem.GetRangePercentage(stackCount);
                    break;
            }
        }
        return statSum;
    }

    private int FullPlayerHealth => 100 + (int)GetPlayerStatAdjustment(Player.Stat.Health);

    private float GetPlayerSpeed() {
        float playerSpeed = config.gameplay.baseSpeed * GetAbsoluteStat(Player.Stat.MovementSpeedPercentage);
        
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

    private float GetFirerateDelayBasedOnStats() {
        if (demonEye.equiped == demonEye.empty) {
            return config.gameplay.attackDelay;
        }

        float attackDelay = config.gameplay.attackDelay / GetAbsoluteStat(Player.Stat.FireratePercentage);
        return Mathf.Clamp(attackDelay, config.gameplay.cappedMinAttackDelay, config.gameplay.attackDelay);
    }
    
    private float GetProjectileRangeInSeconds() {
        return config.gameplay.rangeInSeconds * GetAbsoluteStat(Player.Stat.RangePercentage);
    }
    
    private void GetEncumberingWeightRange(out int startingWeight, out int endingWeight) {
        startingWeight = (int)GetPlayerStat(Player.Stat.CarryCapacity);
        int encumberingIncreaseFromStrength = (int)GetPlayerStatAdjustment(Player.Stat.CarryCapacity);
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
    
    private void UpdatePlayerPanelUI() {
        if (!InventoryIsOpen) return;
            
        playerPanel.healthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/{FullPlayerHealth}";

        int inventoryWeight = GetInventoryWeight(inventories.player);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanel.weightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
        
        Color boostedColor = Styles.instance.increaseDescColor;
        EquipedStatsPanel equipedStatsPanel = playerPanel.equipedStatsPanel;
        
        equipedStatsPanel.bleedResistText.text = Boosted(Player.Stat.BleedResist) ? 
            DisplayProb(GetAbsoluteStat(Player.Stat.BleedResist), boostedColor) : 
            DisplayProbNoColor(GetAbsoluteStat(Player.Stat.BleedResist));
        
        equipedStatsPanel.critChanceText.text = Boosted(Player.Stat.CritChance) ? 
            DisplayProb(GetAbsoluteStat(Player.Stat.CritChance), boostedColor) :
            DisplayProbNoColor(GetAbsoluteStat(Player.Stat.CritChance));
        
        equipedStatsPanel.critMultiText.text = Boosted(Player.Stat.CritMulti) ? 
            DisplayMultiplier(GetAbsoluteStat(Player.Stat.CritMulti), boostedColor) :
            DisplayMultiplierNoColor(GetAbsoluteStat(Player.Stat.CritMulti));
        
        equipedStatsPanel.damageText.text = Boosted(Player.Stat.DamageMulti) ? 
            DisplayMultiplier(GetAbsoluteStat(Player.Stat.DamageMulti), boostedColor) :
            DisplayMultiplierNoColor(GetAbsoluteStat(Player.Stat.DamageMulti));
        
        equipedStatsPanel.firerateText.text = Boosted(Player.Stat.FireratePercentage) ? 
            DisplayProb(GetAbsoluteStat(Player.Stat.FireratePercentage), boostedColor) :
            DisplayProbNoColor(GetAbsoluteStat(Player.Stat.FireratePercentage));
        
        equipedStatsPanel.projectileCountText.text = Boosted(Player.Stat.ProjectileCount) ? 
            DisplayNumber(GetAbsoluteStat(Player.Stat.ProjectileCount), boostedColor) :
            DisplayNumberNoColor(GetAbsoluteStat(Player.Stat.ProjectileCount));
        
        equipedStatsPanel.rangeText.text = Boosted(Player.Stat.RangePercentage) ? 
            DisplayProb(GetAbsoluteStat(Player.Stat.RangePercentage), boostedColor) :
            DisplayProbNoColor(GetAbsoluteStat(Player.Stat.RangePercentage));
        
        bool Boosted(Player.Stat stat) => GetEquipmentStatAdjustment(stat) > 0f; 
    }
    
    [Serializable]
    private class PlayerSaveData {
        public int health;
        public int crucibleLevel;
        public int soulCurrency;
        public int coinCurrency;
        
        public int hasteSkillLevel;
        public int intellectSkillLevel;
        public int lifeBloodSkillLevel;
        public int strengthSkillLevel;
    }

    private void SavePlayerData() {
        PlayerSaveData data = new() {
            health = player.health,
            soulCurrency = player.soulCurrency,
            coinCurrency = player.coinCurrency,
            hasteSkillLevel = player.hasteSkillLevel,
            intellectSkillLevel = player.intellectSkillLevel,
            lifeBloodSkillLevel = player.lifeBloodSkillLevel,
            strengthSkillLevel = player.strengthSkillLevel,
        };
        SaveToFile(savePaths.player, data);
    }

    private void LoadAndAssignPlayerSaveData(Player instancedPlayer) {
        PlayerSaveData data = LoadFromFile<PlayerSaveData>(savePaths.player);
        if (data != null) {
            instancedPlayer.health = data.health;
            instancedPlayer.soulCurrency = data.soulCurrency;
            instancedPlayer.coinCurrency = data.coinCurrency;
            instancedPlayer.hasteSkillLevel = data.hasteSkillLevel;
            instancedPlayer.intellectSkillLevel = data.intellectSkillLevel;
            instancedPlayer.lifeBloodSkillLevel = data.lifeBloodSkillLevel;
            instancedPlayer.strengthSkillLevel = data.strengthSkillLevel;
        }
        // We want to make sure that the player health is never <= zero
        instancedPlayer.health = instancedPlayer.health <= 0f ? FullPlayerHealth : instancedPlayer.health;
    }
    
}
