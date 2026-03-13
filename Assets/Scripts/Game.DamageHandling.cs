using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public partial class Game {
    
    private void DamageEnemyAfterDelay(Entity enemy, int damage, bool isCriticalStrike, float delay) {
        enemy.delayedDamage = damage;
        enemy.delayedDamageIsCrit = isCriticalStrike;
        Delay(enemy, delay, static enemy => {
            inst.DamageEnemy(enemy, enemy.delayedDamage, enemy.delayedDamageIsCrit);
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
        
        DemonEyeInstance eyeInstance = projectile.eyeInstanceSpawnedFrom;
        
        if (entity.gameObject.CompareTag(Tags.Enemy)) {
            if (entityLookup[entity.gameObject] is not Enemy enemy) return;

            if (projectile.flatDamage.HasValue) {
                DamageEnemy(enemy, projectile.flatDamage.Value, false);
                return;
            }
            
            bool isCriticalStrike = RollProbability(GetCriticalStrikeProbability(projectile, enemy));
            if (isCriticalStrike) {
                demonEyeRaidStats.consecutiveCriticalHits++;
            }
            else {
                demonEyeRaidStats.consecutiveCriticalHits = 0;
            }

            int damage = GetProjectileDamage(projectile, enemy, isCriticalStrike);
            DamageEnemy(enemy, damage, isCriticalStrike);
            
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                modInstance.ApplyToEnemy(enemy);
            }
            
            if (eyeInstance.explosion.TryGetValue(out var explosion) && RollProbability(explosion.probability)) {
                Vector2 expSpawnPos = projectile.position + (enemy.position - projectile.position) / 2f;
                
                Entity expEntity = SpawnEntity(explosionPool, expSpawnPos, Quaternion.identity); 
                DestroyEntity(expEntity, CurrentClipLength(expEntity.animator));
                
                List<Collider2D> cols = OverlapCircle(expSpawnPos, explosion.radius, Masks.EnemyMask);
                foreach (Collider2D col in cols) {
                    Enemy explosionEnemy = entityLookup[col.gameObject] as Enemy;
                    int explosionDamage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(explosionEnemy) * explosion.damageMulti);
                    DamageEnemyAfterDelay(entityLookup[col.gameObject], explosionDamage, false, 0.1f);
                }
            }
            
            if (equipedEye.boneShatter.TryGetValue(out var boneShatter) && RollProbability(boneShatter.probability)) {
                for (int i = 0; i < boneShatter.shardsCount; i++) {
                    float randomDelay = Random.Range(0f, 0.06f);
                    float randomSpeedScaler = Random.Range(0.4f, 0.6f);
                    Vector2 boneShatterVelocity = RandomizeVectorAngle(projectile.velocity * randomSpeedScaler, 40f);
                    int boneDamage = Mathf.RoundToInt(GetBaseDamage() * GetDamageMultiplierOnEnemy(enemy) * boneShatter.perShardDamageMulti);
                    Projectile boneShatterProj = SpawnProjectile(enemy.position, boneShatterVelocity, boneShatterProjectilePool, 
                        rotation: RandomRotation(), spawnDelay: randomDelay, flatDamage: boneDamage, lifetime: boneShatter.lifeTime);
                    ProjectileMarkEntityToIgnore(boneShatterProj, enemy);
                }
            }
        }
        else {
            entity.health -= gameplayConfig.damage;

            PlayAudioClip(stoneHitClip, entity.position);
                
            if (entity.health <= 0) {
                if (entity.obstacleCellRadius > 0) {
                    loadedMapInst.grid.ClearObstacle(entity.obstaclePosition, entity.obstacleCellRadius);
                }
                
                Entity smokeEntity = SpawnEntity<Entity>(rockSmokePrefab, entity.position, Quaternion.identity);
                DestroyEntity(smokeEntity, 0.417f);
                DestroyEntity(entity);
                
                PlayAudioClip(stoneBreakClip, entity.position);

                Entity rockDropEntity = null;
                if (RollProbability(loadedMapData.eyeUpgradeFromRockChance)) {
                    ModifierItem modifierItem = GetItemFromDropPool(eyeUpgradesDropPool) as ModifierItem;
                    ItemInstance modifierItemInstance = new(modifierItem);
                    
                    if (modifierItem.augments.Count > 0) {
                        Augment randomAugment = modifierItem.augments[Random.Range(0, modifierItem.augments.Count)];
                        modifierItemInstance.nestedUuids = new() { randomAugment.uuid };
                    }
                    
                    rockDropEntity = SpawnItemInstanceAsEntity(modifierItemInstance, entity.position, Quaternion.identity);
                }
                else {
                    Item dropItem = GetItemFromDropPool(rockStonesDropPool);
                    rockDropEntity = SpawnItemAsEntity(dropItem, 1, entity.position, Quaternion.identity);
                }

                Vector3 endPos = entity.position + RotationVector(Random.Range(0f, 360f), 0.18f, 0.25f);
                AddBounceEffect(rockDropEntity, endPos, 0.8f);
            }
            else {
                AddFlashHitEffect(entity);
                AddShakeEffect(entity, 8f, 0.038f, 0.35f, shakeCurve);
                Tween.PunchScale(entity.trans, Vector3.one * 0.12f, 0.1f, 15f);
            }
        }
    }

    private float GetCriticalStrikeProbability(Projectile proj, Enemy enemy) {
        if (proj.flatCritChance.TryGetValue(out float flatCrit)) {
            return flatCrit;
        }
        
        if (ProjectileIsType(proj, ProjectileTypeFlags.BackwardsShot)) {
            return 1f;
        }
        
        float criticalStrikeProb = gameplayConfig.defaultCritChance + GetStatAdjustmentValue(StatAdjustmentType.CritChance);

        if (equipedEye.bleedCritAugment.HasValue && enemy.bleed.HasValue) {
            criticalStrikeProb += equipedEye.bleedCritAugment.Value.probability;
        }

        return criticalStrikeProb;
    }

    private int GetProjectileDamage(Projectile proj, Enemy enemy, bool isCriticalHit) {
        DemonEyeInstance eyeInstance = proj.eyeInstanceSpawnedFrom;
        
        int damage = GetBaseDamage();
        
        // Phase 1 : Additions
        {
            if (equipedEye.distanceDamage.TryGetValue(out var distDamage)) {
                float convertedUnits = proj.distTraveled / gameplayConfig.distancePerUnit;
                int increasedDamageFromDist = Mathf.FloorToInt(convertedUnits) * distDamage.damageIncreasePerUnitTraveled;
                damage += increasedDamageFromDist;
            }
        }
        
        // Phase 2 : Multipliers
        {
            float damageMultiplier = GetDamageMultiplierOnEnemy(enemy);
            
            if (isCriticalHit) {
                float critMultiplier = gameplayConfig.defaultCritMulti + GetStatAdjustmentValue(StatAdjustmentType.CritMulti);
                damageMultiplier += critMultiplier;
            }
            
            if (ProjectileIsType(proj, ProjectileTypeFlags.Trishot) && eyeInstance.trishot.TryGetValue(out var triShot)) {
                damageMultiplier += triShot.damageMultiplier;
            }
            
            if (equipedEye.doubleCritAugment.TryGetValue(out var doubleCrit)) {
                int consecutiveCriticalHits = demonEyeRaidStats.consecutiveCriticalHits;
                if (consecutiveCriticalHits > 0 && consecutiveCriticalHits % 2 == 0) {
                    demonEyeRaidStats.lastDoubleCritActivationTime = Time.time;
                }

                if (Time.time - demonEyeRaidStats.lastDoubleCritActivationTime <= doubleCrit.multiplierDuration) {
                    damageMultiplier += doubleCrit.damageMulti;
                }
            }

            if (equipedEye.penetrationDamageAugment.TryGetValue(out var penetrationDamage)) {
                int penetrationCountBeforeDamagingThisEnemy = proj.ignoreEntities == null ? 0 : proj.ignoreEntities.Count;
                if (penetrationCountBeforeDamagingThisEnemy > 0) {
                    damageMultiplier += penetrationCountBeforeDamagingThisEnemy * penetrationDamage.damageMultiplierPerPenetration;
                }
            }
            
            damage = Mathf.RoundToInt(damage * damageMultiplier);
        }

        return damage;
    }

    private int GetBaseDamage() {
        int damage = gameplayConfig.damage;
        int damageRange = Mathf.RoundToInt(damage * 0.1f);
        damage += Random.Range(-damageRange, damageRange);
        damage += Mathf.RoundToInt(GetStatAdjustmentValue(StatAdjustmentType.Damage));
        return damage;
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

    private enum DamageColor { Normal, Crit, Blood, Poison }

    private void SpawnDamageNumber(Vector3 spawnPos, int damage, DamageColor damageColor) {
        Vector3 startSize = Vector3.one * 0.8f;
        Vector3 endSize = Vector3.one * damageColor switch {
            DamageColor.Normal => 1.0f,
            DamageColor.Crit   => 1.25f,
            DamageColor.Blood  => 0.8f,
            DamageColor.Poison => 0.8f,
            _                  => 1f,
        };
        
        float xOffset = Random.Range(-0.08f, 0.08f);
        float yOffset = Random.Range(0.05f, 0.1f);
        Vector2 endDamageNumPos;
        
        if (damageColor == DamageColor.Blood) {
            spawnPos = OffsetY(spawnPos, 0.05f);
            endDamageNumPos = OffsetY(spawnPos, yOffset * 2.3f);
        }
        else {
            endDamageNumPos = OffsetY(OffsetX(spawnPos, xOffset), yOffset);
        }
        
        Entity damageNumber = SpawnEntity(damageNumberPool, spawnPos, Quaternion.identity, damageNumbersParent);
        damageNumber.textMesh.text = damage.ToString();
        
        const float alpha = 0.68f;
        switch (damageColor) {
            case DamageColor.Normal:
                damageNumber.textMesh.color = styles.normalDamageColor.Alpha(alpha);
                break;
            case DamageColor.Crit:
                damageNumber.textMesh.color = styles.critDamageColor.Alpha(alpha);
                break;
            case DamageColor.Blood:
                damageNumber.textMesh.color = styles.bleedDamageColor.Alpha(alpha);
                break;
            case DamageColor.Poison:
                damageNumber.textMesh.color = styles.poisonDamageColor.Alpha(alpha);
                break;
        }

        if (damageColor == DamageColor.Blood) {
            const float bloodMoveDuration = 0.65f;
            const float bloodScaleUpDuration = 0.25f;
            const float bloodPopOutDuration = 0.3f;
            Tween.Position(damageNumber.trans, endDamageNumPos, bloodMoveDuration, Ease.OutCubic)
            .Group(Tween.Scale(damageNumber.trans, startSize, endSize, bloodScaleUpDuration, Ease.InOutBack))
            .Chain(Tween.Scale(damageNumber.trans, 0f, bloodPopOutDuration, Ease.InBack));
            DestroyEntity(damageNumber, bloodMoveDuration + bloodPopOutDuration);
            return;
        }

        float moveDuration = damageColor == DamageColor.Crit ? Random.Range(0.37f, 0.4f) : Random.Range(0.3f, 0.35f);
        const float scaleUpDuration = 0.25f;
        const float popOutDuration = 0.3f;

        Tween.Position(damageNumber.trans, endDamageNumPos, moveDuration, Ease.OutBack)
        .Group(Tween.Scale(damageNumber.trans, startSize, endSize, scaleUpDuration, Ease.InOutBack))
        .Chain(Tween.Scale(damageNumber.trans, 0f, popOutDuration, Ease.InBack));
        DestroyEntity(damageNumber, moveDuration + popOutDuration);
    }

    private Vector3 EnemyDamageNumberSpawnPos(Entity entity) {
        return OffsetY(entity.position, 0.28f);
    }
    
}
