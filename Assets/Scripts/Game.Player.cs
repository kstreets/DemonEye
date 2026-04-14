using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class Game {
    
    public class Player : Entity {
        public CapsuleCollider2D hurtCollider;
        public Vector3 velocity;
        
        public int nextIdleAnimHash;
        public int nextIdleDir;
        
        public bool bleeding;
        public Limiter bleedLimiter;
        
        public Limiter enmeyCollisionDamageLimiter;
        
        public int crucibleLevel;
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

        public Sprite defaultPlayerPreviewSprite;

        public enum Stat {
            BleedResist, CarryCapacity, CritChance, CritMulti, DamageMulti, FireratePercentage, Health, 
            HealingAmount, HealingSpeed, LootingSpeed, MovementSpeedPercentage, ProjectileCount, RangePercentage,
        }
    }
    
    public Player player;

    private void OnPlayerCreated() {
        player.hurtCollider = player.gameObject.GetComponentInChildren<CapsuleCollider2D>();
        player.defaultPlayerPreviewSprite = playerPreviewImage.sprite;
    }
    
    private void InitPlayer() {
        player.animator.Play(player.idleDownAnim);
        player.nextIdleAnimHash = player.idleDownAnim;
    }
    
    private void DeinitPlayer() {
        player.bleeding = false;
        playerPreviewImage.sprite = player.defaultPlayerPreviewSprite;
    }
    
    private void UpdatePlayer() {
        bleedDebuffIcon.gameObject.SetActive(player.bleeding);
        
        if (raidEnterSequence.isAlive) return;
        
        if (player.bleeding && player.bleedLimiter.TimeHasPassed(3.5f)) {
            const int bleedDamage = 5;
            player.health -= bleedDamage;
            
            Entity bloodDrop = SpawnEntity(bloodDropPool, OffsetY(player.position, 0.11f), Quaternion.identity);
            AddParentEffect(bloodDrop, player, 0.4f);
            DestroyEntity(bloodDrop, 0.8f);
            
            SpawnDamageNumber(OffsetY(player.position, 0.05f), bleedDamage, DamageColor.Blood);
            AddFlashHitEffect(player);
            Tween.PunchScale(bleedDebuffIcon.transform, Vector3.one * 0.8f, 0.25f, 5f);

            if (PlayerHealthIsAtAutoBleedStop()) {
                player.bleeding = false;
            }
        }
        
        bool consumingItem = playerConsumingTween.isAlive;
        if (consumingItem) {
            player.nextIdleAnimHash = player.idleDownAnim;
            return;
        }

        bool interactingWithPortal = interactionVars.timeSpentSummoningPortal > Mathf.Epsilon;
        if (InventoryIsOpen || interactingWithPortal) return;
        
        Vector2 moveInput = moveInputAction.ReadValue<Vector2>();
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
        
        if (moveInput != Vector2.zero && player.curStepDistance > 0.18f) {
            Entity runSmokeEntity = SpawnEntity(runSmokePool, OffsetY(player.position, 0.01f), Quaternion.identity);
            DestroyEntity(runSmokeEntity, CurrentClipLength(runSmokeEntity.animator));
            PlayAudioClip(footStepClip, player.position);
            player.curStepDistance = 0f;
        }
        
        bool canShoot = attackLimiter.TimeHasPassed(GetFirerateDelayBasedOnStats());
        if (!canShoot) return;
        
        float projCount = GetAbsoluteStat(Player.Stat.ProjectileCount);
        int targetCount = Mathf.FloorToInt(projCount);
        float extraProjChance = projCount % 1;
        if (RollProbability(extraProjChance)) {
            targetCount++;
        }

        List<Vector3> attackTargets = GetAttackTargets(targetCount);
        if (attackTargets.Count <= 0) return;
        
        PlayAudioClip(shootClip, player.position);
        for (int i = 0; i < attackTargets.Count; i++) {
            Vector3 attackTarget = attackTargets[i];
            
            bool isPrimaryShot = i == 0;
            if (isPrimaryShot) {
                ShootProjectile(attackTarget);
            }
            
            bool isAdditionalShot = i > 0;
            if (isAdditionalShot) {
                if (equipedEye.multiProjectileCritAugment.TryGetValue(out var multiProjCrit)) {
                    ShootProjectile(attackTarget, flatCritChance: multiProjCrit.probability);
                }
                else {
                    ShootProjectile(attackTarget);
                }
            }

            if (equipedEye.doubleTapAugment.TryGetValue(out var doubleTap) && RollProbability(doubleTap.probability)) { 
                ShootProjectile(attackTarget, spawnDelay: doubleTap.delayBetweenShots);
            }
        }

        float consecutiveShotDelay = gameplayConfig.attackDelay * 1.5f;
        if (Time.time - player.lastShotTime <= consecutiveShotDelay) {
            player.consecutiveShotCount++;
        }
        else {
            player.consecutiveShotCount = 0;
        }
        
        if (equipedEye.blast.TryGetValue(out var blast) && player.consecutiveShotCount > 0 && player.consecutiveShotCount % blast.numshotsUntilOverheat == 0) {
            Vector2 spawnPos = OffsetY(player.position, 0.1f);
            
            Entity expEntity = SpawnEntity(blastPool, spawnPos, Quaternion.identity); 
            DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
            
            List<Collider2D> cols = OverlapCircle(spawnPos, blast.radius, Masks.EnemyMask);
            foreach (Collider2D col in cols) {
                Enemy enemy = entityLookup[col.gameObject] as Enemy;
                int damage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * blast.damageMulti);
                DamageEnemyAfterDelay(entityLookup[col.gameObject], damage, false, 0.15f);
            }
        }
            
        player.lastShotTime = Time.time;
    }
    
    private List<Vector3> GetAttackTargets(int targetCount) {
        float overlapDist = gameplayConfig.projectileSpeed * GetProjectileRangeInSeconds();
        List<Collider2D> cols = OverlapCircle(player.position, overlapDist, Masks.EnemyMask);
        
        if (cols.Count <= 0) {
            cols = OverlapCircle(player.position, overlapDist, Masks.MineableMask);
        }
        
        cols.Sort(static (a, b) => {
            float aScore = GetTargetScore(a);
            float bScore = GetTargetScore(b);
            return aScore.CompareTo(bScore);
        });

        int count = Mathf.Min(targetCount, cols.Count);
        List<Vector3> targets = new();
        for (int i = 0; i < count; i++) {
            targets.Add(entityLookup[cols[i].gameObject].Center);
        }
        return targets;
    }

    private static float GetTargetScore(Collider2D col) {
        Entity entity = gameInstance.entityLookup[col.gameObject];
        float dist = Vector2.Distance(col.transform.position, gameInstance.player.position);

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
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - gameplayConfig.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = gameplayConfig.projectileSpeed;
        Vector2 dir = (targetPos - PlayerEyePos.ToVector2()).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * projectileSpeed; 
        _SpawnProjectile(PlayerEyePos, velocity, projectilePool);
        
        if (equipedEye.trishot.TryGetValue(out var trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            _SpawnProjectile(PlayerEyePos, secondShotVelocity, projectilePool, flgs: ProjectileTypeFlags.Trishot);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            _SpawnProjectile(PlayerEyePos, thirdShotVelocity, projectilePool, flgs: ProjectileTypeFlags.Trishot);
        }

        if (equipedEye.backwardShot.TryGetValue(out var backShot) && RollProbability(backShot.probability)) {
            const float backwardsShotSpeedScaler = 1.1f;
            EntityPool<Projectile> pool = equipedEye.backwardsPiercingAugment.HasValue ? piercingShotProjectilePool : projectilePool; 
            _SpawnProjectile(PlayerEyePos, -velocity * backwardsShotSpeedScaler, pool, flgs: ProjectileTypeFlags.BackwardsShot);
        }
        
        // Helper method just to forward the passed in parameters
        void _SpawnProjectile(Vector2 pos, Vector2 vel, EntityPool<Projectile> pool, ProjectileTypeFlags flgs = ProjectileTypeFlags.None) {
            SpawnProjectile(pos, vel, pool, typeFlags: flgs, spawnDelay: spawnDelay, flatCritChance: flatCritChance);
        }
    }

    private Projectile SpawnProjectile(Vector2 spawnPos, Vector2 velocity, EntityPool<Projectile> pool, 
        Quaternion? rotation = default, int? flatDamage = default, float? spawnDelay = default, float? lifetime = default, 
        float? flatCritChance = default, LayerMask? layermask = default, ProjectileTypeFlags typeFlags = ProjectileTypeFlags.None) 
    {
        Quaternion projectileRotation = rotation ?? Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, velocity.normalized), Vector3.forward);
        Projectile projectile = SpawnEntity(pool, spawnPos, projectileRotation);
        
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = equipedEye;
        projectile.flatDamage = flatDamage;
        projectile.flatCritChance = flatCritChance;
        projectile.lifeTimeDuration = lifetime ?? GetProjectileRangeInSeconds();
        projectile.layerMask = layermask ?? Masks.DamagableMask;
        projectile.typeFlags = typeFlags;

        if (!spawnDelay.HasValue) {
            projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
            return projectile;
        }

        projectile.gameObject.SetActive(false);
        Delay(projectile, spawnDelay.Value, static (projectile) => {
            projectile.gameObject.SetActive(true);
            gameInstance.projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
        });

        return projectile;
    }
    
    private Tween playerConsumingTween;
    private Inventory consumingInventory;
    private int consumingSlotIndex;
    
    private void HavePlayerConsumeItem(Inventory fromInventory, int slotIndex) {
        if (playerConsumingTween.isAlive) return;
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
        consumingSlotIndex = slotIndex;
        consumingInventory = fromInventory;
        
        playerConsumingTween = Tween.Delay(item, actionDelay, static (item) => {
            if (item.healingAmount > 0) {
                gameInstance.HealPlayer(item.healingAmount);
            }
            if (item.bandageAmount > 0) {
                gameInstance.player.bleeding = false;
            }
            gameInstance.ReduceItemCountInInventory(gameInstance.consumingInventory, gameInstance.consumingSlotIndex);
        });
        
        playerConsumingTween.OnUpdate(this, static (_, _) => {
            if (gameInstance.playerPreviewImage.sprite != gameInstance.player.spriteRenderer.sprite) {
                gameInstance.playerPreviewImage.sprite = gameInstance.player.spriteRenderer.sprite;     
            }
        });
        
        playerConsumingTween.Chain(Tween.Delay(postActionDelay, static () => {
            gameInstance.player.animator.Play(gameInstance.player.idleDownAnim);
            gameInstance.player.animator.Update(0f);
            if (gameInstance.playerPreviewImage.sprite != gameInstance.player.spriteRenderer.sprite) {
                gameInstance.playerPreviewImage.sprite = gameInstance.player.spriteRenderer.sprite;     
            }
        }))
        .Chain(Tween.Delay(additionalConsumeDelay));
    }

    private void HealPlayer(int healing) {
        player.health = Mathf.Clamp(player.health + healing, 0, FullPlayerHealth);
    }
    
    private enum PlayerDamageType { Normal, Collision }

    private void DamagePlayer(int damage, PlayerDamageType damageType, float chanceToBleed = 0f) {
        // if (!player.bleeding && !PlayerHealthIsAtAutoBleedStop() && RollProbability(chanceToBleed)) {
        //     player.bleeding = true;
        // }
        
        if (interactionVars.timeSpentSummoningPortal < gameplayConfig.portalSummonTime) {
            interactionVars.timeSpentSummoningPortal = 0f;
        }
        
        bool ignoreCollisionDamage = !player.enmeyCollisionDamageLimiter.TimeHasPassed(gameplayConfig.repeatCollisionDamageDelay);
        if (damageType == PlayerDamageType.Collision && ignoreCollisionDamage) return;
        
        player.health -= damage;
        AddFlashHitEffect(player);
        SpawnDamageNumber(player.position, damage, DamageColor.Blood);
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
            Player.Stat.CarryCapacity           => gameplayConfig.defaultStartingEncumberingWeight,
            Player.Stat.CritChance              => gameplayConfig.defaultCritChance,
            Player.Stat.CritMulti               => gameplayConfig.defaultCritMulti,
            Player.Stat.DamageMulti             => 1f,
            Player.Stat.FireratePercentage      => 1f,
            Player.Stat.Health                  => 100f,
            Player.Stat.MovementSpeedPercentage => 1f,
            Player.Stat.ProjectileCount         => 1f,
            Player.Stat.RangePercentage         => 1f,
            _                                   => 0f, 
        };
        return startingValue + GetPlayerStatAdjustment(stat);
    }

    private float GetPlayerStatAdjustment(Player.Stat stat) {
        return stat switch {
            Player.Stat.CarryCapacity           => GetPlayerStatLevel(Player.Stat.CarryCapacity) * gameplayConfig.carryCapacityIncPerLevel,
            Player.Stat.CritChance              => GetPlayerStatLevel(Player.Stat.CritChance) * gameplayConfig.critChanceIncPerLevel,
            Player.Stat.CritMulti               => GetPlayerStatLevel(Player.Stat.CritMulti) * gameplayConfig.critMultiplierIncPerLevel,
            Player.Stat.DamageMulti             => GetPlayerStatLevel(Player.Stat.DamageMulti) * gameplayConfig.damageMultiplierIncPerLevel,
            Player.Stat.FireratePercentage      => GetPlayerStatLevel(Player.Stat.FireratePercentage) * gameplayConfig.firerateIncPerLevel,
            Player.Stat.Health                  => GetPlayerStatLevel(Player.Stat.Health) * gameplayConfig.healthIncPerLevel,
            Player.Stat.HealingAmount           => GetPlayerStatLevel(Player.Stat.HealingAmount) * gameplayConfig.healingIncPerLevel,
            Player.Stat.HealingSpeed            => GetPlayerStatLevel(Player.Stat.HealingSpeed) * gameplayConfig.healingSpeedIncPerLevel,
            Player.Stat.LootingSpeed            => GetPlayerStatLevel(Player.Stat.LootingSpeed) * gameplayConfig.lootingSpeedIncPerLevel,
            Player.Stat.MovementSpeedPercentage => GetPlayerStatLevel(Player.Stat.MovementSpeedPercentage) * gameplayConfig.movementSpeedIncPerLevel,
            Player.Stat.ProjectileCount         => GetPlayerStatLevel(Player.Stat.ProjectileCount) * gameplayConfig.projectileCountIncPerLevel,
            _                                   => 0f,
        };
    }
    
    private float GetEquipmentStatAdjustment(Player.Stat stat) {
        float statSum = 0f;
        
        for (int i = 0; i < playerEquipmentSize; i++) {
            Item item = playerInventory.slots[i].itemInstance?.ItemRef;
            if (!item || !item.modifiesStats) continue;
            
            switch (stat) {
                case Player.Stat.MovementSpeedPercentage:
                    statSum += item.GetMovementSpeedPercentage(1);
                    break;
            }
        }

        foreach (EquipedModInstance mod in equipedEye.modInstances) {
            ModifierItem modifierItem = mod.ModifierItem;
            int stackCount = mod.stackCount;
            if (!modifierItem.modifiesStats) continue;

            switch (stat) {
                case Player.Stat.CritChance:
                    statSum += modifierItem.GetCritChance(stackCount); 
                    break;
                case Player.Stat.CritMulti:
                    statSum += modifierItem.GetCritMultiplier(stackCount); 
                    break;
                case Player.Stat.DamageMulti:
                    statSum += modifierItem.GetDamageMultiplier(stackCount); 
                    break;
                case Player.Stat.FireratePercentage:
                    statSum += modifierItem.GetFireratePercentage(stackCount); 
                    break;
                case Player.Stat.ProjectileCount:
                    statSum += modifierItem.GetProjectileCount(stackCount); 
                    break;
                case Player.Stat.RangePercentage:
                    statSum += modifierItem.GetRangePercentage(stackCount);
                    break;
            }
        }
        
        return statSum;
    }

    private int FullPlayerHealth => 100 + (int)GetPlayerStatAdjustment(Player.Stat.Health);

    private float GetPlayerSpeed() {
        float playerSpeed = gameplayConfig.baseSpeed * GetAbsoluteStat(Player.Stat.MovementSpeedPercentage);
        
        float speedReductionFromWeight = Mathf.Lerp(0f, gameplayConfig.maxEncumberedSpeedReduction, GetOverweightCompletion());
        speedReductionFromWeight = Mathf.Clamp(speedReductionFromWeight, 0f, gameplayConfig.maxEncumberedSpeedReduction);

        playerSpeed -= speedReductionFromWeight;
        return playerSpeed;
    }

    private float GetFirerateDelayBasedOnStats() {
        if (equipedEye == emptyDemonEye) {
            return gameplayConfig.attackDelay;
        }

        float attackDelay = gameplayConfig.attackDelay / GetAbsoluteStat(Player.Stat.FireratePercentage);
        return Mathf.Clamp(attackDelay, gameplayConfig.cappedMinAttackDelay, gameplayConfig.attackDelay);
    }
    
    private float GetProjectileRangeInSeconds() {
        return gameplayConfig.rangeInSeconds * GetAbsoluteStat(Player.Stat.RangePercentage);
    }
    
    private void GetEncumberingWeightRange(out int startingWeight, out int endingWeight) {
        startingWeight = (int)GetPlayerStat(Player.Stat.CarryCapacity);
        int encumberingIncreaseFromStrength = (int)GetPlayerStatAdjustment(Player.Stat.CarryCapacity);
        endingWeight = gameplayConfig.maxEncumberedWeight + encumberingIncreaseFromStrength;
    }

    private float GetOverweightCompletion() {
        GetEncumberingWeightRange(out int startingEncumberingWeight, out int endingEncumberingWeight);
        int inventoryWeight = GetInventoryWeight(playerInventory);
        int curOverweightAmount = Mathf.Clamp(inventoryWeight - startingEncumberingWeight, 0, int.MaxValue);
        float maxOverweightAmount = (float)endingEncumberingWeight - startingEncumberingWeight;
        float overweightComp = curOverweightAmount / maxOverweightAmount;
        return Mathf.Clamp01(overweightComp);
    }
    
    private void UpdatePlayerPanelUI() {
        if (!playerInventoryParent.gameObject.activeInHierarchy) return;
            
        playerPanelHealthText.text = $"<color=#5CF25B>{player.health}</color><size=22>/{FullPlayerHealth}";

        int inventoryWeight = GetInventoryWeight(playerInventory);
        GetEncumberingWeightRange(out int startEncumberingWeight, out _);
        playerPanelWeightText.text = $"<color=#98C5CC>{inventoryWeight}</color><size=22>/{startEncumberingWeight}";
        
        Color boostedColor = styles.increaseDescColor;
        
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
    
}
