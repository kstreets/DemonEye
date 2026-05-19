using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

public partial class Game {
    
    [Flags] public enum ProjectileTypeFlags { None, Trishot, BackwardsShot, }
    
    public class Projectile : Entity {
        public ProjectileTypeFlags typeFlags;
        public int? flatDamage;
        public float? flatCritChance;
        public float lifeTimeDuration;
        public float curTimeAlive;
        public float distTraveled;
        public Vector2 velocity;
        public LayerMask layerMask;
        public DemonEyeInstance eyeInstanceSpawnedFrom;
        public Entity sourceEntity;
        public List<Entity> ignoreEntities;
    }
    
    private static void OnSpawnProjectile(Projectile projectile) {
        projectile.typeFlags = ProjectileTypeFlags.None;
        projectile.flatDamage = default;
        projectile.flatCritChance = default;
        projectile.lifeTimeDuration = default;
        projectile.curTimeAlive = default;
        projectile.distTraveled = default;
        projectile.velocity = default;
        projectile.layerMask = default;
        projectile.eyeInstanceSpawnedFrom = default;
        projectile.sourceEntity = default;
        if (projectile.ignoreEntities != null) {
            ListPool<Entity>.Release(projectile.ignoreEntities);
        }
        projectile.ignoreEntities = default;
    }
    
    private Projectile SpawnProjectile(
        EntityPool<Projectile> pool, Vector2 spawnPos, Vector2 velocity, float lifetime, Entity sourceEntity,
        Quaternion? rotation = default, int? flatDamage = default, float? spawnDelay = default, float? flatCritChance = default, 
        LayerMask? layermask = default, ProjectileTypeFlags typeFlags = ProjectileTypeFlags.None) 
    {
        Quaternion projectileRotation = rotation ?? Quaternion.AngleAxis(Vector2.SignedAngle(Vector2.right, velocity.normalized), Vector3.forward);
        Projectile projectile = SpawnEntity(pool, spawnPos, projectileRotation);
        
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = demonEye.equiped;
        projectile.sourceEntity = sourceEntity;
        projectile.lifeTimeDuration = lifetime;
        projectile.flatDamage = flatDamage;
        projectile.flatCritChance = flatCritChance;
        projectile.layerMask = layermask ?? Masks.DamagableMask;
        projectile.typeFlags = typeFlags;

        if (!spawnDelay.HasValue) {
            entities.projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
            return projectile;
        }

        projectile.gameObject.SetActive(false);
        Delay(projectile, spawnDelay.Value, static (projectile) => {
            projectile.gameObject.SetActive(true);
            gameInstance.entities.projectiles.Add(projectile);
            projectile.trans.localScale = Vector3.zero;
            Tween.Scale(projectile.trans, Vector3.one, 0.025f, Ease.InBounce);
        });

        return projectile;
    }

    private void UpdateProjectiles() {
        const float projectileRadius = 0.035f;
        List<Projectile> projectiles = entities.projectiles;
        
        for (int i = projectiles.Count - 1; i >= 0; i--) {
            Projectile proj = projectiles[i];
            proj.curTimeAlive += Time.deltaTime;
            proj.trans.position += (Vector3)proj.velocity * Time.deltaTime;
            proj.distTraveled += proj.velocity.magnitude * Time.deltaTime;
            
            Collider2D col = Physics2D.OverlapCircle(proj.trans.position, projectileRadius, proj.layerMask);
            if (!col) continue;
            
            // Hack for identifying if the projectile hit the player 
            if (proj.layerMask == Masks.PlayerHurtMask) {
                Assert.IsTrue(proj.flatDamage.HasValue, "Projectiles that damage the player need to have a flat damage value");
                DamagePlayer(proj.flatDamage.Value, PlayerDamageType.Normal, proj);
                DestroyEntity(projectiles[i]);
                projectiles.RemoveAt(i);
                continue;
            }
            
            Entity entity = entities.lookup[col.gameObject];
                    
            if (!ProjectileIsIgnoringEntity(proj, entity)) {
                HandleDamage(proj, entity);
                PlayAudioClip(audio.projectileImpact, proj.position);
            }

            if (entity is Enemy && ProjectileShouldPassThrough(proj, entity)) continue;
            
            Entity impact = SpawnEntity(entityPools.projectileImpact, proj.position, RandomRotation());
            DestroyEntity(impact, CurrentClipLength(impact.animator));
            
            DestroyEntity(projectiles[i]);
            projectiles.RemoveAt(i);
        }

        for (int i = projectiles.Count - 1; i >= 0; i--) {
            if (projectiles[i].curTimeAlive > projectiles[i].lifeTimeDuration) {
                const float despawnTime = 0.2f;
                Tween.Scale(projectiles[i].trans, 0f, despawnTime, Ease.InOutBounce);
                DestroyEntity(projectiles[i], despawnTime);
                projectiles.RemoveAt(i);
            }
        }
    }

    private bool ProjectileShouldPassThrough(Projectile proj, Entity entity) {
        if (ProjectileIsIgnoringEntity(proj, entity)) {
            return true;
        }

        if (ProjectileIsType(proj, ProjectileTypeFlags.BackwardsShot) && proj.eyeInstanceSpawnedFrom.backwardsPiercingAugment.HasValue) {
            ProjectileMarkEntityToIgnore(proj, entity);
            return true;
        }

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
    
    private bool ProjectileIsType(Projectile proj, ProjectileTypeFlags flags) {
        return (proj.typeFlags & flags) != 0;
    }

}
