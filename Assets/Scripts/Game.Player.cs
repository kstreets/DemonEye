using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using static GameData;
using Random = UnityEngine.Random;

public partial class Game {
    
    public class Player : Entity {
        public CapsuleCollider2D hurtCollider;
        public Vector3 velocity;
        public Vector3 predictedVelocity;
        
        public int nextIdleAnimHash;
        public int nextIdleDir;
        
        public bool bleeding;
        public Limiter bleedLimiter;
        
        public Limiter attackLimiter;
        public Limiter enemyCollisionDamageLimiter;
        
        public float lastShotTime;
        public int consecutiveShotCount;
        public float curStepDistance;

        public EscalatingCooldown teleportCooldown;
        public bool isTeleporting;
        public float teleportEndTime;
        public Vector2 teleportDestination;
        public Vector2 teleportDir;
        public float invulnerableUntil;

        public bool isSliding;
        public float slideStartTime;

        public ItemConsumption consumption;
        public bool isConsumingItem => consumption.tween.isAlive;
        
        public HealingOverTime healing;
        public bool isHealingOverTime => health > 0 && healing.tween.isAlive;

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
        HealingOnRaidExit, HealingSpeed, LootingSpeed, MovementSpeedPercentage, ProjectileCount, RangePercentage,
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
        public static int slideSide = Animator.StringToHash("PlayerSlideSide");
        public static int slideDown = Animator.StringToHash("PlayerSlideDown");
        public static int slideUp = Animator.StringToHash("PlayerSlideUp");
    }
    
    private const int startingSoulCurrency = 100;

    public static Player player => gameInstance.entities.player;
    
    private Player MakePlayer() {
        Player newPlayer = SpawnEntity<Player>(prefabs.player, Vector3.zero, Quaternion.identity, null, EntityLifetime.Global);
        newPlayer.hurtCollider = newPlayer.gameObject.GetComponentInChildren<CapsuleCollider2D>();
        newPlayer.defaultPlayerPreviewSprite = playerPanel.previewImage.sprite;
        newPlayer.teleportCooldown = new(
            config.gameplay.teleportBaseCooldown,
            config.gameplay.teleportMaxCooldown,
            config.gameplay.teleportCooldownIncreasePerRapidUse,
            config.gameplay.teleportRapidUseWindow
        );
        return newPlayer;
    }
    
    private void InitPlayerState(Player instancedPlayer, GameState gameState) {
        // No save means this is a fresh game, so give the player some starting souls
        instancedPlayer.state = gameState?.playerState ?? new() { soulCurrency = startingSoulCurrency };
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
        player.velocity = Vector3.zero;
        // The raid can end mid teleport, so the player needs to be made visible and hittable again
        SetPlayerTeleportHidden(false);
        player.isTeleporting = false;
        player.isSliding = false;
        player.invulnerableUntil = 0f;
        player.teleportCooldown.Reset();
        playerPanel.previewImage.sprite = player.defaultPlayerPreviewSprite;
        if (player.health > 0) { // We only want to restore health if the player isn't dead
            HealPlayer((int)GetAbsoluteStat(PlayerStat.HealingOnRaidExit));
        }
    }
    
    public void PlayerOnExitPortalSummoned(Portal portal) {
        if (trinkets.equiped is not PortalBlastTrinket portalBlast) return;

        Vector2 portalPos = portal.transform.position;
        foreach (Enemy enemy in entities.enemies) {
            if (enemy.health <= 0) continue; // Already dying, death gets handled in UpdateEnemies
            if (Vector2.Distance(portalPos, enemy.Center) > portalBlast.killRadius) continue;
            DamageEnemy(enemy, enemy.health, isCriticalStrike: false, useEnemyDamageMultiplier: false);
        }

        if (!string.IsNullOrEmpty(portalBlast.activationPopUpText)) {
            SpawnTrinketActivationText(portalBlast.activationPopUpText);
        }
    }

    private void PlayerOnEnemyDeath(Enemy enemy) {
        if (trinkets.equiped is SpeedBoostTrinket speedBoost) {
            if (++trinkets.data.trackingCount >= speedBoost.killsPerBoost) {
                trinkets.data.trackingCount = 0;
                trinkets.data.activeDuration.Add(speedBoost.duration);
            }
        }
        
        int souls = enemy.data.soulWorthPerKill;
        if (trinkets.equiped is SoulSuckerTrinket soulSucker) {
            souls = Mathf.RoundToInt(souls * soulSucker.soulsPerKillMultiplier);
        }
        player.state.soulCurrency += souls;
        curRaid.data.soulsGained += souls;
        
        if (demonEye.equiped.soulVolley.HasValue) {
            ref var soulVolley = ref demonEye.equiped.soulVolley.GetValue();
            soulVolley.curSoulsTowardsVolley += souls;
            
            if (soulVolley.curSoulsTowardsVolley >= soulVolley.soulsNeededPerVolley) {
                soulVolley.curSoulsTowardsVolley %= soulVolley.soulsNeededPerVolley; // Keep any overflow
                
                List<Collider2D> targetEnemies = Physics.OverlapCircle(player.position, GetProjectileRangeDistance(), Masks.EnemyMask);

                targetEnemies.Sort((a, b) => {
                    float distA = (a.transform.position - player.position).sqrMagnitude;
                    float distB = (b.transform.position - player.position).sqrMagnitude;
                    return distA.CompareTo(distB);
                });

                if (targetEnemies.Count > 0) {
                    for (int i = 0; i < soulVolley.volleyCount; i++) {
                        Enemy targetedEnemy = entities.lookup[targetEnemies[i % targetEnemies.Count].gameObject] as Enemy;
                        int damage = Mathf.RoundToInt(GetBaseDamage() * soulVolley.damageMultiplier);
                        SpawnSoulTrackingProjectile(targetedEnemy, damage, i * 0.06f);
                    }
                }
            }
        }
    }
    
    private void UpdatePlayer() {
        if (raidEnterSequence.isAlive) return;
        
        if (player.bleeding && player.bleedLimiter.TimeHasPassed(3.5f)) {
            // Bleeding never takes the player below the auto stop threshold, so it can't kill them
            const int maxBleedDamage = 5;
            int bleedDamage = Mathf.Min(maxBleedDamage, player.health - AutoBleedStopHealth());
            if (bleedDamage > 0) {
                player.health -= bleedDamage;
                SpawnPlayerDamageNumber(bleedDamage);

                Entity bloodDrop = SpawnEntity(entityPools.bloodDrop, OffsetY(player.position, 0.11f), Quaternion.identity);
                AddParentEffect(bloodDrop, player, 0.4f);
                DestroyEntity(bloodDrop, 0.8f);

                AddFlashHitEffect(player);
                Tween.PunchScale(playerInfo.bleedDebuffIcon.transform, Vector3.one * 0.8f, 0.25f, 5f);
            }

            if (PlayerHealthIsAtAutoBleedStop()) {
                player.bleeding = false;
            }
        }

        // The player can't move or shoot while hidden mid teleport
        if (player.isTeleporting) {
            if (Time.time < player.teleportEndTime) return;
            FinishTeleport();
        }

        if (player.isConsumingItem) {
            player.nextIdleAnimHash = PlayerAnimations.idleDown;
            return;
        }

        if (PlayerInventoryIsOpen) {
            player.velocity = Vector3.zero;
            return;
        }
        
        Vector2 moveInput = input.move.ReadValue<Vector2>();
        Vector2 prevPos = player.position;

        if (input.teleport.WasPressedThisFrame() && player.teleportCooldown.TryUse()) {
            StartTeleport(moveInput);
            return;
        }

        float speed = GetPlayerSpeed();
        Vector3 frameVelocity = new Vector3(moveInput.x, moveInput.y, 0f) * speed;
        
        const float minMagnitdeNeededToMove = 0.25f;
        if (moveInput.magnitude <= minMagnitdeNeededToMove) {
            frameVelocity = Vector3.zero;
        }

        bool sliding = UpdateSlide(moveInput);
        if (sliding) {
            float slideComp = (Time.time - player.slideStartTime) / config.gameplay.teleportSlideDuration;
            float slideSpeed = speed * Mathf.Lerp(config.gameplay.teleportSlideStartSpeedMulti, 1f, slideComp);
            player.velocity = player.teleportDir * slideSpeed;
        }
        else {
            const float acceleration = 18f;
            player.velocity = Vector3.Lerp(player.velocity, frameVelocity, acceleration * Time.deltaTime);
        }
        
        player.position += player.velocity * Time.deltaTime;
        player.curStepDistance += Vector2.Distance(prevPos, player.position);

        // Update player's smooth velocity for velocity prediction
        {
            const float predictionSmoothingTime = 0.25f; // Controls how fast we react to velocity changes (0.05 is fast, 0.3 is slow)
            float smoothingComp = 1f - Mathf.Exp(-Time.deltaTime / predictionSmoothingTime); // Makes lerp completion stable at low framerates
            Vector2 actualFrameVelocity = ((Vector2)player.position - prevPos) / Time.deltaTime;
            player.predictedVelocity = Vector2.Lerp(player.predictedVelocity, actualFrameVelocity, smoothingComp);
        }
        
        bool noFrameVelocity = frameVelocity.magnitude <= Mathf.Epsilon;

        if (!noFrameVelocity) {
            Vector2 moveDir = moveInput.normalized;
            bool movingPredominatelyVertical = Mathf.Abs(moveDir.y) > 0.93f;
            
            if (!movingPredominatelyVertical) {
                player.nextIdleDir = moveDir.x < 0 ? -1 : 1;
                player.nextIdleAnimHash = PlayerAnimations.idleSide;
            }
            else if (moveDir.y > 0) {
                player.nextIdleAnimHash = PlayerAnimations.idleUp;
            }
            else {
                player.nextIdleAnimHash = PlayerAnimations.idleDown;
            }
        }
        player.spriteRenderer.flipX = player.nextIdleDir < 0;

        if (sliding) {
            if (Mathf.Abs(player.teleportDir.x) > 0.1f) {
                player.spriteRenderer.flipX = player.teleportDir.x < 0;
                player.animator.Play(PlayerAnimations.slideSide);
            }
            else if (player.teleportDir.y > 0) {
                player.animator.Play(PlayerAnimations.slideUp);
            }
            else {
                player.animator.Play(PlayerAnimations.slideDown);
            }
        }
        else if (noFrameVelocity) {
            player.animator.Play(player.nextIdleAnimHash);
        }
        else if (player.nextIdleAnimHash == PlayerAnimations.idleUp) {
            player.animator.Play(PlayerAnimations.runUp);
        }
        else if (player.nextIdleAnimHash == PlayerAnimations.idleDown) {
            player.animator.Play(PlayerAnimations.runDown);
        }
        else {
            player.animator.Play(PlayerAnimations.runSide);
        }
        
        bool playerStepped = !sliding && moveInput != Vector2.zero && player.curStepDistance > 0.18f;
        if (playerStepped) {
            Entity runSmokeEntity = SpawnEntity(entityPools.runSmoke, OffsetY(player.position, 0.043f), Quaternion.identity);
            DestroyEntity(runSmokeEntity, CurrentClipLength(runSmokeEntity.animator));
            PlayAudioClip(audio.footStepClip, player.position);
            player.curStepDistance = 0f;
            
            if (trinkets.equiped is HealingStepsTrinket healingSteps) {
                if (++trinkets.data.trackingCount >= healingSteps.stepsPerHeal) {
                    HealPlayer(healingSteps.healing);
                    SpawnTrinketActivationText($"+{healingSteps.healing:N0}", config.styles.increaseDescColor);
                    trinkets.data.trackingCount = 0;
                }
            }
        }

        bool wantsToShoot = false;
        Vector2 shootDir = Vector2.zero;
        if (!usingController) {
            wantsToShoot = Mouse.current?.leftButton.isPressed ?? false;
            TryGetAimVector(out shootDir);
        }
        else {
            wantsToShoot = TryGetAimVector(out shootDir);
        }
        if (!wantsToShoot) return;
        
        bool canShoot = player.attackLimiter.TimeHasPassed(GetFirerateDelay());
        if (!canShoot) return;

        Vector3 attackTarget = (Vector2)PlayerEyePos + shootDir;

        PlayAudioClip(audio.shootClip, player.position);
        ShootProjectile(attackTarget);
        if (trinkets.equiped is CauterizingWoundsTrinket cauterizingWounds) {
            if (player.bleeding && RollProbability(cauterizingWounds.chancePerShotToStopBleeding)) {
                player.bleeding = false;
                SpawnTrinketActivationText(cauterizingWounds.activationText);
            }
        }

        if (demonEye.equiped.doubleTapAugment.TryGetValue(out var doubleTap) && RollProbability(doubleTap.probability)) {
            ShootProjectile(attackTarget, spawnDelay: doubleTap.delayBetweenShots);
        }

        float consecutiveShotDelay = config.gameplay.attackDelay * 1.5f;
        if (Time.time - player.lastShotTime <= consecutiveShotDelay) {
            player.consecutiveShotCount++;
        }
        else {
            player.consecutiveShotCount = 0;
        }
        
        if (demonEye.equiped.blast.TryGetValue(out var blast) && player.consecutiveShotCount > 0 && player.consecutiveShotCount % blast.numshotsUntilOverheat == 0) {
            Vector2 spawnPos = OffsetY(player.position, 0.35f);
            PlayAudioClip(audio.overheatBlastClip, spawnPos);
            
            SpawnEntityOneShot(entityPools.blast, spawnPos, Quaternion.identity); 
            List<Collider2D> cols = Physics.OverlapCircle(player.position, blast.radius, Masks.EnemyMask);
            foreach (Collider2D col in cols) {
                Enemy enemy = entities.lookup[col.gameObject] as Enemy;
                int damage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * blast.damageMulti);
                DamageEnemyAfterDelay(enemy, damage, false, 0.15f);
            }
        }
            
        player.lastShotTime = Time.time;
    }
    
    private void StartTeleport(Vector2 moveInput) {
        Vector2 dir;
        if (moveInput.magnitude > 0.25f) {
            dir = moveInput.normalized;
        }
        else if (player.nextIdleAnimHash == PlayerAnimations.idleUp) {
            dir = Vector2.up;
        }
        else if (player.nextIdleAnimHash == PlayerAnimations.idleDown) {
            dir = Vector2.down;
        }
        else {
            dir = new(player.nextIdleDir < 0 ? -1f : 1f, 0f);
        }

        player.teleportDir = dir;
        player.teleportDestination = GetTeleportDestination(dir);
        player.isTeleporting = true;
        player.isSliding = false;
        player.teleportEndTime = Time.time + config.gameplay.teleportHiddenDuration;
        player.invulnerableUntil = player.teleportEndTime + config.gameplay.teleportInvulnerabilityDuration;
        player.velocity = Vector3.zero;
        SetPlayerTeleportHidden(true);
        
        Entity outTeleportFxEntity = SpawnEntity(entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);
        
        camera.cameraShake.Shake(4.5f, 0.21f, 0.35f);
    }

    private void FinishTeleport() {
        player.position = new(player.teleportDestination.x, player.teleportDestination.y, player.position.z);
        player.isTeleporting = false;
        SetPlayerTeleportHidden(false);

        Entity outTeleportFxEntity = SpawnEntity(entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);

        // Still holding towards where they teleported carries the teleport's momentum into a slide, letting go means no slide
        if (MoveInputInTeleportDirection(input.move.ReadValue<Vector2>())) {
            player.isSliding = true;
            player.slideStartTime = Time.time;
        }
    }

    private bool UpdateSlide(Vector2 moveInput) {
        if (!player.isSliding) return false;

        bool slideFinished = Time.time - player.slideStartTime >= config.gameplay.teleportSlideDuration;
        if (slideFinished || !MoveInputInTeleportDirection(moveInput)) {
            player.isSliding = false;
        }
        return player.isSliding;
    }

    // Analog input is never exactly the teleport direction, so anything within the max angle counts as the same general direction
    private bool MoveInputInTeleportDirection(Vector2 moveInput) {
        if (moveInput.magnitude <= 0.25f) return false;
        return Vector2.Angle(moveInput, player.teleportDir) <= config.gameplay.teleportSlideMaxAngle;
    }

    private void SetPlayerTeleportHidden(bool hidden) {
        player.spriteRenderer.enabled = !hidden;
        player.hurtCollider.enabled = !hidden;
    }

    // Casts the player's body collider along the teleport so they stop short of walls and obstacles instead of ending up inside them.
    // Enemies are ignored so the player can teleport through them.
    private Vector2 GetTeleportDestination(Vector2 dir) {
        float distance = config.gameplay.teleportDistance;

        int blockingLayers = Physics2D.GetLayerCollisionMask(player.gameObject.layer) & ~(int)Masks.EnemyMask;
        if (player.rigidbody) {
            blockingLayers &= ~(int)player.rigidbody.excludeLayers;
        }
        ContactFilter2D filter = new() { useTriggers = false };
        filter.SetLayerMask(blockingLayers);

        using var _ = ListPool<RaycastHit2D>.Get(out var hits);
        player.collider.Cast(dir, filter, hits, distance);
        foreach (RaycastHit2D hit in hits) {
            distance = Mathf.Min(distance, hit.distance);
        }

        return (Vector2)player.position + dir * distance;
    }

    private bool TryGetAimVector(out Vector2 aim) {
        aim = Vector2.zero;

        if (usingController) {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) {
                return false;
            }

            Vector2 stick = gamepad.rightStick.ReadValue();
            if (stick.magnitude <= ControllerNavigation.stickDeadzone) {
                return false;
            }

            aim = stick;
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null) {
            return false;
        }

        Vector2 mouseWorldPos = camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 toMouse = mouseWorldPos - (Vector2)PlayerEyePos;
        if (toMouse.sqrMagnitude <= Mathf.Epsilon) {
            return false;
        }

        aim = toMouse;
        return true;
    }

    private Vector3 PlayerEyePos => player.position + new Vector3(0f, 0.182f, 0f);
    
    private void ShootProjectile(Vector2 targetPos, float? spawnDelay = default, float? flatCritChance = default) {
        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - config.gameplay.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);

        float projectileSpeed = config.gameplay.projectileSpeed;
        Vector2 dir = (targetPos - (Vector2)PlayerEyePos).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * projectileSpeed; 
        
        if (demonEye.equiped.reflectingShot.TryGetValue(out var reflecting) && RollProbability(reflecting.probability)) {
            _SpawnProjectile(PlayerEyePos, velocity, entityPools.projectile, ProjectileTypeFlags.ReflectedShot);
        }
        else {
            _SpawnProjectile(PlayerEyePos, velocity, entityPools.projectile);
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
        
        thisFrame.flags |= FrameFlags.TookConsumable;
        
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
                float healingDuration = gameInstance.InRaid ? item.healingDuration : 0f; // Instant healing while not in raid
                gameInstance.HealPlayer(item.healingAmount, healingDuration);
            }
            // Healing bandages can be used while not bleeding, and a bleed can also end on its own during the animation
            if (item.bandageAmount > 0 && player.bleeding) {
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
        
        float durationSkillReduction = (duration * GetAbsoluteStat(PlayerStat.HealingSpeed)) - duration;  
        duration -= durationSkillReduction;
        
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
        // Teleporting gives a short window of invulnerability. Enemy collisions use their own overlap check rather than the hurt collider, so this also covers them
        if (Time.time < player.invulnerableUntil) return;

        bool ignoreCollisionDamage = !player.enemyCollisionDamageLimiter.TimeHasPassed(config.gameplay.repeatCollisionDamageDelay);
        if (damageType == PlayerDamageType.Collision && ignoreCollisionDamage) return;
        
        // Thorns makes it so collision damage is ignored and imposed on the enemy
        if (trinkets.equiped is Thorns thorns && trinkets.data.cooldownDuration.HasPassed() && damageType == PlayerDamageType.Collision) {
            Entity damageEntity = sourceEntity switch {
                Enemy enemy     => enemy,
                Projectile proj => proj.sourceEntity,
                _               => null,
            };
            if (damageEntity != null) {
                DamageEnemy(damageEntity, damage, isCriticalStrike: false);
                SpawnTrinketActivationText(thorns.activationPopUpText);
                trinkets.data.cooldownDuration.Reset(thorns.cooldownTime);
            }
            return;
        }
        
        player.health = Mathf.Clamp(player.health - damage, 0, int.MaxValue);
        
        // Rolled after the damage is applied so a hit that takes the player under the auto stop threshold can't start a bleed
        chanceToBleed -= GetAbsoluteStat(PlayerStat.BleedResist); 
        if (!player.bleeding && !curRaid.map.playerCantBleed && !PlayerHealthIsAtAutoBleedStop() && RollProbability(chanceToBleed)) {
            player.bleeding = true;
        }
        AddFlashHitEffect(player);
        SpawnPlayerDamageNumber(damage);
        
        float damageImpactScale = Mathf.Clamp01(damage / 35f);
        float damageShakeFreq = Mathf.Lerp(6f, 10f, damageImpactScale);
        float damageShakeMag = Mathf.Lerp(0.02f, 0.1f, damageImpactScale);
        camera.cameraShake.Shake(damageShakeFreq, damageShakeMag, 0.6f);
        
    }
    
    private bool PlayerHealthIsAtAutoBleedStop() {
        return player.health <= AutoBleedStopHealth();
    }
    
    private int AutoBleedStopHealth() {
        const float percentageOfHealthBleedingStops = 0.10f;
        return Mathf.CeilToInt(FullPlayerHealth() * percentageOfHealthBleedingStops);
    }
    
    private int GetPlayerStatLevel(PlayerStat stat) {
        return stat switch {
            PlayerStat.FireratePercentage      => player.state.hasteSkillLevel,
            PlayerStat.MovementSpeedPercentage => player.state.hasteSkillLevel,
            PlayerStat.LootingSpeed            => player.state.hasteSkillLevel,
        
            PlayerStat.CritChance      => player.state.intellectSkillLevel,
            PlayerStat.CritMulti       => player.state.intellectSkillLevel,
            PlayerStat.ProjectileCount => player.state.intellectSkillLevel,
            
            PlayerStat.Health            => player.state.lifeBloodSkillLevel,
            PlayerStat.HealingOnRaidExit => player.state.lifeBloodSkillLevel,
            PlayerStat.HealingSpeed      => player.state.lifeBloodSkillLevel,
            
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
            PlayerStat.HealingSpeed            => 1f,
            _                                  => 0f, 
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
            PlayerStat.HealingOnRaidExit       => GetPlayerStatLevel(PlayerStat.HealingOnRaidExit) * config.gameplay.healingIncOnRaidExitPerLevel,
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
    private float CurPlayerHealthPercentage() => player.health / (float)FullPlayerHealth();
    private float HealingToHealthPercentage() => (player.health + (player.healing.targetHealing - player.healing.healingGiven)) / (float)FullPlayerHealth();

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
    
    private float GetProjectileRangeDistance() {
        return config.gameplay.projectileSpeed * GetProjectileRangeInSeconds();
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
    
    public Vector2 PredictedPlayerPosition(float leadTimeInSeconds) {
        Vector2 targetPos = player.hurtCollider.bounds.center;
        Vector2 velocity = player.predictedVelocity * leadTimeInSeconds;
        return targetPos + velocity;
    }
    
    public bool TryGetPlayerInterceptPoint(Vector2 shooterPos, float projectileSpeed, out Vector2 aimPoint, float leadFactor = 1f) {
        Vector2 targetPos = player.hurtCollider.bounds.center;
        Vector2 velocity = player.predictedVelocity * leadFactor;
        Vector2 delta = targetPos - shooterPos;

        float a = Vector2.Dot(velocity, velocity) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector2.Dot(delta, velocity);
        float c = Vector2.Dot(delta, delta);

        float time;
        if (Mathf.Abs(a) < 0.0001f) {
            time = -c / b; // Player and projectile have the same speed, so the equation is linear
        }
        else {
            float disc = b * b - 4f * a * c;
            if (disc < 0f) {
                aimPoint = targetPos; 
                return false;
            }
            float sqrt = Mathf.Sqrt(disc);
            float t1 = (-b - sqrt) / (2f * a);
            float t2 = (-b + sqrt) / (2f * a);
            time = Mathf.Min(t1, t2) > 0f ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
        }

        if (time <= 0f) {
            aimPoint = targetPos; 
            return false;
        }
        
        aimPoint = targetPos + velocity * time; // This is a simple fallback
        return true;
    }
    
}
