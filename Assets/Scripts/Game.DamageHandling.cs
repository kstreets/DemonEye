using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Game {
    
    public struct DamagingData {
        public int consecutiveCriticalHits;
        public float lastDoubleCritActivationTime;
    }
    
    private void DamageEnemyAfterDelay(Entity enemy, int damage, bool isCriticalStrike, float delay) {
        enemy.delayedDamage = damage;
        enemy.delayedDamageIsCrit = isCriticalStrike;
        Delay(enemy, delay, static enemy => {
            gameInstance.DamageEnemy(enemy, enemy.delayedDamage, enemy.delayedDamageIsCrit);
        });
    }
    
    private void DamageEnemy(Entity enemy, int damage, bool isCriticalStrike) {
        enemy.health -= damage;
        AddFlashHitEffect(enemy);
        SpawnDamageNumber(EnemyDamageNumberSpawnPos(enemy), damage, isCriticalStrike ? DamageColor.Crit : DamageColor.Normal);
        Tween.PunchScale(enemy.trans, Vector3.one * 0.15f, 0.1f, 15f);
    }
    
    private void HandleDamage(Projectile projectile, Entity entity) {
        if (entity == null) return;
        if (entity.gameObject.CompareTag(Tags.Enemy)) {
            HandleDamageEnemy(projectile, entity);
        }
        else {
            HandleDamageRock(projectile, entity);
        }
    }
    
    private void HandleDamageEnemy(Projectile projectile, Entity entity) {
        var entityLookup = entities.lookup;
        if (entityLookup[entity.gameObject] is not Enemy enemy) return;
        
        /*
        ============== Simple projectile land ===================
        Any projectile with flat damage is deemed to be a simple projectile
        and will return out of the method before anything a default projectile can trigger
        */
        
        if (projectile.flatDamage.HasValue) {
            DamageEnemy(enemy, projectile.flatDamage.Value, false);
            return;
        }
        
        /*
        ============== Default projectile land ===================
        */
        
        bool isCriticalStrike = RollProbability(GetCriticalStrikeProbability(projectile, enemy));
        ref int consecutiveCriticalHits = ref curRaid.data.damaging.consecutiveCriticalHits;
        if (isCriticalStrike) {
            consecutiveCriticalHits++;
        }
        else {
            consecutiveCriticalHits = 0;
        }

        int damage = GetProjectileDamage(projectile, enemy, isCriticalStrike);
        DamageEnemy(enemy, damage, isCriticalStrike);
        
        DemonEyeInstance eyeInstance = projectile.eyeInstanceSpawnedFrom;
        foreach (EquipedUpgradeInstance modInstance in eyeInstance.upgradeInstances) {
            modInstance.ApplyToEnemy(enemy);
        }
        foreach (EquipedAugmentInstance augmentInstance in eyeInstance.augmentInstances) {
            augmentInstance.ApplyToEnemy(enemy);
        }
        
        if (eyeInstance.explosion.TryGetValue(out var explosion) && RollProbability(explosion.probability)) {
            Vector2 expSpawnPos = GetExplosionPosition(projectile, enemy);
            
            Entity expEntity = SpawnEntity(entityPools.explosion, expSpawnPos, Quaternion.identity); 
            DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
            
            List<Collider2D> cols = Physics.OverlapCircle(expSpawnPos, explosion.radius, Masks.EnemyMask);
            foreach (Collider2D col in cols) {
                if (entityLookup[col.gameObject] is Enemy explosionEnemy) {
                    int explosionDamage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(explosionEnemy) * explosion.damageMulti);
                    DamageEnemyAfterDelay(entityLookup[col.gameObject], explosionDamage, false, 0.1f);
                }
            }
        }
        
        if (demonEye.equiped.poison.TryGetValue(out var poison) && RollProbability(poison.probability)) {
            Vector2 poisonGasCloudPos = GetExplosionPosition(projectile, enemy);
            List<Collider2D> cols = Physics.OverlapCircle(poisonGasCloudPos, poison.radius, Masks.EnemyMask);
            foreach (Collider2D col in cols) {
                if (entityLookup[col.gameObject] is Enemy poisonEnemy) {
                    poisonEnemy.poison = poison;
                    AddPoisonedEffect(poisonEnemy, poison.duration);
                }
            }
        }
        
        if (demonEye.equiped.boneShatter.TryGetValue(out var boneShatter) && RollProbability(boneShatter.probability)) {
            for (int i = 0; i < boneShatter.shardsCount; i++) {
                float randomDelay = Random.Range(0f, 0.06f);
                float randomSpeedScaler = Random.Range(0.4f, 0.6f);
                Vector2 boneShatterVelocity = RandomizeVectorAngle(projectile.velocity * randomSpeedScaler, 65f);
                int boneDamage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * boneShatter.perShardDamageMulti);
                Projectile boneShatterProj = SpawnProjectile(
                    entityPools.boneShatterProjectile, enemy.position, boneShatterVelocity, boneShatter.lifeTime, player,
                    rotation: RandomRotation(), spawnDelay: randomDelay, flatDamage: boneDamage
                );
                ProjectileMarkEntityToIgnore(boneShatterProj, enemy);
            }
        }
    }
    
    private void HandleDamageRock(Projectile projectile, Entity entity) {
        entity.health -= config.gameplay.damage;

        PlayAudioClip(audio.stoneHitClip, entity.position);
        
        if (entity.health > 0) {
            AddFlashHitEffect(entity);
            AddShakeEffect(entity, 8f, 0.038f, 0.35f, curves.shake);
            Tween.PunchScale(entity.trans, Vector3.one * 0.12f, 0.1f, 15f);
            return;
        }
                
        if (entity.gridObstacleRadius > 0) {
            curRaid.mapInstance.grid.ClearObstacle(entity.gridObstaclePos, entity.gridObstacleRadius);
        }
            
        Entity smokeEntity = SpawnEntity<Entity>(prefabs.rockSmokePrefab, entity.position, Quaternion.identity);
        DestroyEntity(smokeEntity, 0.417f);
        DestroyEntity(entity);
        PlayAudioClip(audio.stoneBreakClip, entity.position);
            
        int dropCount = 1;
        if (trinkets.equiped is RockLootTrinket rockLoot) {
            if (RollProbability(rockLoot.chanceForSecondDrop)) {
                dropCount++;
            }    
        }
            
        for (int i = 0; i < dropCount; i++) {
            Item dropItem = GetItemFromDropPool(dropPools.rockStones);
            Entity rockDropEntity = SpawnItemAsEntity(dropItem, 1, entity.position, Quaternion.identity);

            Vector3 endPos = entity.position + RotationVector(Random.Range(0f, 360f), 0.18f, 0.25f);
            AddBounceEffect(rockDropEntity, endPos, 0.8f);
        }
    }
    
    private float GetCriticalStrikeProbability(Projectile proj, Enemy enemy) {
        if (proj.flatCritChance.TryGetValue(out float flatCrit)) {
            return flatCrit;
        }
        
        if (proj.typeFlags.HasFlag(ProjectileTypeFlags.BackwardsShot)) {
            return 1f;
        }
        
        float criticalStrikeProb = GetAbsoluteStat(PlayerStat.CritChance);
        if (demonEye.equiped.bleedCritAugment.HasValue && enemy.bleed.HasValue) {
            criticalStrikeProb += demonEye.equiped.bleedCritAugment.Value.probability;
        }
        return criticalStrikeProb;
    }

    private int GetProjectileDamage(Projectile proj, Enemy enemy, bool isCriticalHit) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        float damageMultiplier = GetDamageMultiplierOnEnemy(enemy);
        
        // Phase 1 - Multiplications to damage multiplier (Anything with a description of '1.5x Damage' or '0.65x Damage')
        {
            if (isCriticalHit) {
                damageMultiplier *= GetAbsoluteStat(PlayerStat.CritMulti);
            }
            
            if (proj.typeFlags.HasFlag(ProjectileTypeFlags.Trishot) && eyeInstance.trishot.TryGetValue(out var triShot)) {
                damageMultiplier *= triShot.damageMultiplier;
            }
            
            ref int consecutiveCriticalHits = ref curRaid.data.damaging.consecutiveCriticalHits;
            ref float lastDoubleCritActivationTime = ref curRaid.data.damaging.lastDoubleCritActivationTime;
            
            if (demonEye.equiped.doubleCritAugment.TryGetValue(out var doubleCrit)) {
                if (consecutiveCriticalHits > 0 && consecutiveCriticalHits % 2 == 0) {
                    lastDoubleCritActivationTime = Time.time;
                }
                if (Time.time - lastDoubleCritActivationTime <= doubleCrit.multiplierDuration) {
                    damageMultiplier *= doubleCrit.damageMulti;
                }
            }
        }
        
        // Phase 2 - Additions to damage multiplier (Anything with a description of '+1.2x Damage' or '+0.25x Damage')
        {
            if (demonEye.equiped.distanceDamage.TryGetValue(out var distDamage)) {
                float convertedUnits = proj.distTraveled / config.gameplay.distancePerUnit;
                int increasedDamageMultiFromDist = Mathf.FloorToInt(convertedUnits * distDamage.damageMultiIncreasePerUnitTraveled);
                damageMultiplier += increasedDamageMultiFromDist;
            }
            
            if (demonEye.equiped.penetrationDamageAugment.TryGetValue(out var penetrationDamage)) {
                int penetrationCountBeforeDamagingThisEnemy = proj.ignoreEntities == null ? 0 : proj.ignoreEntities.Count;
                if (penetrationCountBeforeDamagingThisEnemy > 0) {
                    damageMultiplier += penetrationCountBeforeDamagingThisEnemy * penetrationDamage.damageMultiplierPerPenetration;
                }
            }
        }

        return Mathf.RoundToInt(GetBaseDamage() * damageMultiplier);
    }

    private int GetBaseDamage() {
        int damage = Mathf.RoundToInt(config.gameplay.damage * GetAbsoluteStat(PlayerStat.DamageMulti));
        int damageRange = Mathf.RoundToInt(damage * 0.05f);
        damage += Random.Range(-damageRange, damageRange);
        return Mathf.Clamp(damage, 1, int.MaxValue);
    }

    private float GetDamageMultiplierOnEnemy(Enemy enemy) {
        float damageMultiplier = 1f;
        
        if (enemy.poison.TryGetValue(out var poison)) {
            if (enemy.health >= enemy.data.health * poison.minHealthPercentForMulti) {
                damageMultiplier *= poison.damageMulti;
            }
        }

        return damageMultiplier;
    }
    
    private Vector2 GetExplosionPosition(Projectile projectile, Enemy enemy) {
        return projectile.position + (enemy.position - projectile.position) / 2f;
    }
    
}
