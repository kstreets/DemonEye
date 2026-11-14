using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    [Flags] 
    public enum ProjectileTypeFlags {
        None                        = 1 << 0, 
        ReflectedShot               = 1 << 1, 
    }
    
    public class Projectile : Entity {
        public ProjectileTypeFlags typeFlags;
        public int? flatDamage;
        public float? flatCritChance;
        public float? damageMultiplier;
        public float lifeTimeDuration;
        public float curTimeAlive;
        public float distTraveled;
        public Vector2 velocity;
        public Vector2 initDir;
        public float? gravity;
        public Vector2 lobTarget; // Where a gravity projectile lands
        public float lobTime; // How long a gravity projectile is in the air
        public LayerMask layerMask;
        public DemonEyeInstance eyeInstanceSpawnedFrom;
        public Entity sourceEntity;
        public Entity targetEntity;
        public List<Entity> ignoreEntities;
        public float detonateNearPlayerRadius;
        public Action<Vector2> onDetonate;
        public Entity shadowEntity;
        public float height; // How far above its shadow the projectile flies
        public Vector2 shadowStart; // Only used by gravity projectiles, their shadow lerps along the ground to the lob target
    }

    private const float defaultProjectileRadius = 0.035f;

    private static void OnSpawnProjectile(Projectile projectile) {
        projectile.typeFlags = ProjectileTypeFlags.None;
        projectile.flatDamage = default;
        projectile.flatCritChance = default;
        projectile.damageMultiplier = default;
        projectile.lifeTimeDuration = default;
        projectile.curTimeAlive = default;
        projectile.distTraveled = default;
        projectile.velocity = default;
        projectile.gravity = default;
        projectile.lobTarget = default;
        projectile.lobTime = default;
        projectile.layerMask = default;
        projectile.eyeInstanceSpawnedFrom = default;
        projectile.sourceEntity = default;
        projectile.targetEntity = default;
        if (projectile.ignoreEntities != null) {
            ListPool<Entity>.Release(projectile.ignoreEntities);
        }
        projectile.ignoreEntities = default;
        projectile.detonateNearPlayerRadius = default;
        projectile.onDetonate = default;
        projectile.shadowEntity = default;
        projectile.height = default;
        projectile.shadowStart = default;
    }
    
    private Projectile SpawnProjectile(EntityPool<Projectile> pool, Vector2 spawnPos, Vector2 velocity, float lifetime, Entity sourceEntity,
        Quaternion? rotation = default, float? damageMulti = default, int? flatDamage = default, float? spawnDelay = default, 
        float? flatCritChance = default, LayerMask? layermask = default, ProjectileTypeFlags typeFlags = ProjectileTypeFlags.None,
        float? gravity = default, Vector2? lobTarget = default, float? lobTime = default)
    {
        Assert.IsTrue(!gravity.HasValue || (lobTarget.HasValue && lobTime.HasValue), "Projectiles with gravity need a target and flight time to land with");
        
        Quaternion projectileRotation = rotation ?? ProjectileRotationFromVelocity(velocity);
        Projectile projectile = SpawnEntity(pool, spawnPos, projectileRotation);
        
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = demonEye.equiped;
        projectile.sourceEntity = sourceEntity;
        projectile.lifeTimeDuration = lifetime;
        projectile.flatDamage = flatDamage;
        projectile.damageMultiplier = damageMulti;
        projectile.flatCritChance = flatCritChance;
        projectile.layerMask = layermask ?? Masks.DamagableMask;
        projectile.typeFlags = typeFlags;
        projectile.gravity = gravity;
        projectile.lobTarget = lobTarget ?? default;
        projectile.lobTime = lobTime ?? default;

        SpawnProjectileShadow(projectile, GetPlayerEyeHeight());
        projectile.shadowStart = projectile.shadowEntity.position;

        if (!spawnDelay.HasValue) {
            entities.projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
            return projectile;
        }

        projectile.gameObject.SetActive(false);
        projectile.shadowEntity.gameObject.SetActive(false);
        Delay(projectile, spawnDelay.Value, static (projectile) => {
            projectile.gameObject.SetActive(true);
            projectile.shadowEntity.gameObject.SetActive(true);
            projectile.shadowEntity.shadow.SetRadius(GetProjectileRadius(projectile)); // Disabling the shadow resets its radius
            gameInstance.entities.projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
        });

        return projectile;
    }

    private void UpdateProjectiles() {
        UpdateSoulTrackingProjectiles();
        UpdateDefaultProjectiles();
    }
    
    private void SpawnProjectileShadow(Projectile projectile, float height) {
        projectile.height = Mathf.Max(0f, height);
        Vector2 shadowPos = projectile.position.Offset(y: -projectile.height);
        projectile.shadowEntity = SpawnEntity(entityPools.shadow, shadowPos, Quaternion.identity);
        projectile.shadowEntity.shadow.SetRadius(GetProjectileRadius(projectile));
    }

    private void UpdateProjectileShadow(Projectile proj) {
        if (proj.shadowEntity == null) return;
        if (proj.gravity.HasValue) {
            float comp = proj.lobTime > 0f ? proj.curTimeAlive / proj.lobTime : 1f;
            proj.shadowEntity.position = Vector2.Lerp(proj.shadowStart, proj.lobTarget, comp);
            
            // Remaps (0 -> 1) to (0 -> 1 -> 0) so the shadow shrinks until halfway through the flight and grows after it
            float radiusScaleComp = Mathf.Sin(comp * Mathf.PI);
            float radiusScaler = Mathf.Lerp(1f, 0.5f, radiusScaleComp);
            
            float defaultRadius = GetProjectileRadius(proj);
            proj.shadowEntity.shadow.SetRadius(defaultRadius * radiusScaler);
        }
        else {
            proj.shadowEntity.position = proj.position.Offset(y: -proj.height);
        }
    }

    private void DestroyProjectileEntity(Projectile proj, float delay = 0f) {
        if (proj.shadowEntity != null) {
            if (delay > 0f) {
                Tween.Scale(proj.shadowEntity.trans, 0f, delay, Ease.InOutBounce);
            }
            DestroyEntity(proj.shadowEntity, delay);
            proj.shadowEntity = null;
        }
        DestroyEntity(proj, delay);
    }

    private float GetPlayerEyeHeight() {
        return GetPlayerEyePos().y - player.position.y;
    }

    private static float GetProjectileRadius(Projectile proj) {
        DamageShape shape = proj.damageShape;
        if (!shape) {
            return defaultProjectileRadius;
        }
        return shape.shape switch {
            DamageShape.ShapeType.Circle => shape.radius,
            _ => Mathf.Min(shape.size.x, shape.size.y) * 0.5f,
        };
    }

    private void UpdateDefaultProjectiles() {
        List<Projectile> projectiles = entities.projectiles;
        
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.curTimeAlive += Time.deltaTime;
            
            if (proj.gravity.HasValue) {
                proj.velocity.y -= proj.gravity.Value * Time.deltaTime;
                proj.rotation = ProjectileRotationFromVelocity(proj.velocity);
            }
            
            proj.trans.position += (Vector3)proj.velocity * Time.deltaTime;
            proj.distTraveled += proj.velocity.magnitude * Time.deltaTime;
            UpdateProjectileShadow(proj);

            if (proj.onDetonate != null) {
                bool nearPlayer = Vector2.Distance(proj.position, player.position) <= proj.detonateNearPlayerRadius;
                bool lifetimeOver = proj.curTimeAlive > proj.lifeTimeDuration;
                if (nearPlayer || lifetimeOver) {
                    proj.onDetonate(proj.position);
                    DestroyProjectileEntity(projectiles[i]);
                    projectiles.RemoveAt(i);
                    continue;
                }
            }

            bool landed = proj.gravity.HasValue && proj.curTimeAlive >= proj.lobTime;
            if (landed) {
                proj.position = proj.lobTarget;
                Entity floorImpact = SpawnEntity(entityPools.projectileImpact, proj.position, RandomRotation());
                DestroyEntity(floorImpact, CurrentClipLength(floorImpact.animator));
                DestroyProjectileEntity(projectiles[i]);
                projectiles.RemoveAt(i);
                continue;
            }

            Collider2D col = proj.damageShape ? proj.damageShape.OverlapFirst(proj.layerMask) : Physics2D.OverlapCircle(proj.trans.position, defaultProjectileRadius, proj.layerMask);
            if (!col) continue;
            
            // Hack for identifying if the projectile hit the player 
            if (proj.layerMask == Masks.PlayerProjectileHurtMask) {
                Assert.IsTrue(proj.flatDamage.HasValue, "Projectiles that damage the player need to have a flat damage value");
                DamagePlayer(proj.flatDamage.Value, PlayerDamageType.Normal, proj);
                DestroyProjectileEntity(projectiles[i]);
                projectiles.RemoveAt(i);
                continue;
            }

            Entity entity = entities.lookup[col.gameObject];
                    
            if (!ProjectileIsIgnoringEntity(proj, entity)) {
                HandleDamage(proj, entity);
                PlayAudioClip(audio.projectileImpact, proj.position);
                
                if (proj.typeFlags.HasFlag(ProjectileTypeFlags.ReflectedShot)) {
                    int reflectedCount = proj.ignoreEntities?.Count ?? 0;
                    if (reflectedCount == 0) {
                        Vector2 reflectedVelocity = Quaternion.AngleAxis(Random.Range(-45, 45), Vector3.forward) * -proj.velocity;
                        proj.velocity = reflectedVelocity;
                        proj.rotation = ProjectileRotationFromVelocity(reflectedVelocity);
                        proj.curTimeAlive = 0f;
                        PlayAudioClip(audio.reflectedShotClip, proj.position);
                        continue;
                    } 
                }
            }
            
            if (entity is Enemy && ProjectileShouldPassThrough(proj, entity)) continue;

            Entity impact = SpawnEntity(entityPools.projectileImpact, proj.position, RandomRotation());
            DestroyEntity(impact, CurrentClipLength(impact.animator));

            DestroyProjectileEntity(projectiles[i]);
            projectiles.RemoveAt(i);
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].curTimeAlive > projectiles[i].lifeTimeDuration) {
                const float despawnTime = 0.2f;
                Tween.Scale(projectiles[i].trans, 0f, despawnTime, Ease.InOutBounce);
                DestroyProjectileEntity(projectiles[i], despawnTime);
                projectiles.RemoveAt(i);
            }
        }
    }
    
    private void DestroyAllProjectiles() {
        List<Projectile> projectiles = entities.projectiles;
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            DestroyProjectile(projectiles[i]);
            projectiles.RemoveAt(i);
        }
        
        List<Projectile> soulTrackingProjectiles = entities.soulTrackingProjectiles;
        for (int i = soulTrackingProjectiles.Count - 1; i >= 0; i--) {
            DestroyProjectile(soulTrackingProjectiles[i]);
            soulTrackingProjectiles.RemoveAt(i);
        }
    }
    
    private void DestroyProjectile(Projectile projectile) {
        const float despawnTime = 0.2f;
        Tween.Scale(projectile.trans, 0f, despawnTime, Ease.InOutBounce);
        DestroyProjectileEntity(projectile, despawnTime);
    }
    
    private bool ProjectileShouldPassThrough(Projectile proj, Entity entity) {
        if (ProjectileIsIgnoringEntity(proj, entity)) {
            return true;
        }
        
        if (proj.typeFlags.HasFlag(ProjectileTypeFlags.ReflectedShot)) {
            int previouslyReflectedCount = proj.ignoreEntities?.Count ?? 0;
            ProjectileMarkEntityToIgnore(proj, entity);
            return previouslyReflectedCount <= 0;
        }

        // if (proj.typeFlags.HasFlag(ProjectileTypeFlags.ReflectedShot) && proj.eyeInstanceSpawnedFrom.backwardsPiercingAugment.HasValue) {
        //     ProjectileMarkEntityToIgnore(proj, entity);
        //     return true;
        // }

        if (proj.eyeInstanceSpawnedFrom.penetration.TryGetValue(out var penetration)) {
            ProjectileMarkEntityToIgnore(proj, entity);
            int alreadyPenetratedCount = proj.ignoreEntities?.Count ?? 0;
            return alreadyPenetratedCount <= penetration.goThroughCount;
        }

        return false;
    }

    private void ProjectileMarkEntityToIgnore(Projectile proj, Entity entity) {
        bool alreadyContainsEntity = proj.ignoreEntities?.Contains(entity) ?? false;
        if (EntityIsValid(entity) && !alreadyContainsEntity) {
            proj.ignoreEntities ??= ListPool<Entity>.Get();
            proj.ignoreEntities.Add(entity);
        }
    }

    private bool ProjectileIsIgnoringEntity(Projectile proj, Entity entity) {
        return proj.ignoreEntities?.Contains(entity) ?? false;
    }
    
    private void SpawnSoulTrackingProjectile(Entity targetEntity, int damage, float spawnDelay) {
        Projectile projectile = SpawnEntity(entityPools.soulProjectile, Vector3.zero, Quaternion.identity);
        projectile.targetEntity = targetEntity;
        projectile.flatDamage = damage;
        projectile.gameObject.SetActive(false);
        
        Delay(projectile, spawnDelay, static (projectile) => {
            if (!gameInstance.EntityIsValid(projectile.targetEntity)) { // The target can die during the delay.
                gameInstance.DestroyEntity(projectile);
                return;
            }
            
            Vector2 spawnPos = gameInstance.GetPlayerEyePos();
            Vector2 dirToTarget = ((Vector2)projectile.targetEntity.Center - spawnPos).normalized; 
            Vector2 initDir = Vector2.Lerp(Vector2.up, dirToTarget, Random.Range(0.3f, 1f));
            Quaternion projectileRotation = Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, initDir), Vector3.forward);
            
            projectile.position = spawnPos;  
            projectile.rotation = projectileRotation;  
            projectile.initDir = initDir;
            // We set this in case it gets switched to a default projectile later when the target enemy dies
            projectile.lifeTimeDuration = gameInstance.GetProjectileRangeInSeconds();
            
            projectile.gameObject.SetActive(true);
            gameInstance.SpawnProjectileShadow(projectile, gameInstance.GetPlayerEyeHeight());
            gameInstance.entities.soulTrackingProjectiles.Add(projectile);
            gameInstance.PlayAudioClip(gameInstance.audio.soulVolleyClip, spawnPos);
        });
    }
    
    private void UpdateSoulTrackingProjectiles() {
        for (int i = entities.soulTrackingProjectiles.Count - 1; i >= 0; i--) {
            Projectile proj = entities.soulTrackingProjectiles[i]; 
            
            // When the tracking enemy is no longer alive, we keep the projectile going by switching it to a normal projectile
            if (!EntityIsValid(proj.targetEntity)) {
                entities.soulTrackingProjectiles.RemoveAt(i);
                entities.projectiles.Add(proj);
                continue;
            }
            
            if (Vector2.Distance(proj.position, proj.targetEntity.position) < 0.035f) {
                DestroyProjectile(proj);
                entities.soulTrackingProjectiles.RemoveAt(i);
                DamageEnemy((Enemy)proj.targetEntity, proj.flatDamage.Value, isCriticalStrike: false);
                continue;
            }
            
            proj.curTimeAlive += Time.deltaTime;
            const float arcRampDuration = 0.4f;
            float arcT = Mathf.Clamp01(proj.curTimeAlive / arcRampDuration);
            
            Vector2 dirToTarget = (proj.targetEntity.position - proj.position).normalized;
            Vector2 aimDir = Vector2.Lerp(proj.initDir, dirToTarget, arcT).normalized;
            Vector2 velocity = aimDir * (config.gameplay.projectileSpeed * Time.deltaTime);
            proj.position = proj.position.Offset(x: velocity.x, y: velocity.y);
            proj.velocity = velocity; // We don't use the velocity here but if it swiches to a normal projectile "it just works"
            UpdateProjectileShadow(proj);
        }
    }
    
    private Quaternion ProjectileRotationFromVelocity(Vector2 velocity) {
        return Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, velocity.normalized), Vector3.forward);
    }

    // The velocity that lobs a gravity projectile from start to target
    private static Vector2 ProjectileLobVelocity(Vector2 start, Vector2 target, float gravity, float speed) {
        Assert.IsTrue(speed > 0f, "Lob speed must be greater than zero");
        Vector2 toTarget = target - start;
        float flightTime = toTarget.magnitude / speed;
        if (flightTime <= Mathf.Epsilon) {
            return Vector2.zero;
        }
        return new(toTarget.x / flightTime, toTarget.y / flightTime + 0.5f * gravity * flightTime);
    }

}
