using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UI;
using EffectsIndicies = Game.Entity.EffectsIndicies;
using Random = UnityEngine.Random;

public partial class Game {
    
    public interface IEntityPooler { 
        public void ReleaseEntity(Entity entity);
    }

    public class EntityPool<T> : IEntityPooler where T : Entity {
        public Action<T> OnSpawnCallback;
        public GameObject prefab;
        public List<T> standbyList = new();

        public void ReleaseEntity(Entity entity) {
            Assert.IsFalse(standbyList.Contains((T)entity), "Already released this entity!");
            entity.gameObject.SetActive(false);
            standbyList.Add((T)entity);
        }
    }
    
    private EntityPool<T> CreateEntityPool<T>(GameObject gameObj, int initialSize, Action<T> onSpawnCallback) where T : Entity, new() {
        EntityPool<T> pool = new() {
            OnSpawnCallback = onSpawnCallback,
            prefab = gameObj,
        };
        
        for (int i = 0; i < initialSize; i++) {
            GameObject obj = Instantiate(gameObj, Vector3.zero, Quaternion.identity, transform);
            T entity = InitializeEntity<T>(obj, EntityLifetime.Level);
            entity.entityPool = pool;
            entity.gameObject.SetActive(false);
            pool.standbyList.Add(entity);
        }
        
        return pool;
    }

    public enum EntityLifetime { Level, Global }

    public class Entity {
        public Transform trans;
        public Collider2D collider;
        public Rigidbody2D rigidbody;
        public SpriteRenderer spriteRenderer;
        public Image image;
        public Animator animator;
        public TextMeshProUGUI textMesh;
        public EntityLifetime lifetime;
        
        public readonly MaterialPropertyBlock matPropertyBlock = new();
        public IEntityPooler entityPool;
        public int health;
        
        public Vector2 gridObstaclePos;
        public int gridObstacleRadius;

        public int delayedDamage;
        public bool delayedDamageIsCrit;
        
        public PoisonedEffect poisonedEffect;
        public BounceEffect bounceEffect;
        public ParentToEntity parentEffect;
        public ShakeEffect shakeEffect;

        public enum EffectsIndicies { HitFlash, Poisoned, Petrify, Bounce, Parent, Shake, Dissolve, Count }
        public readonly Tween[] tweenEffects = new Tween[(int)EffectsIndicies.Count];
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }
        
        public Quaternion rotation {
            get => trans.rotation;
            set => trans.rotation = value;
        }
        
        public RectTransform rectTransform => trans as RectTransform;

        public Vector3 Center => collider.bounds.center;
        public GameObject gameObject => trans.gameObject;
        public Tween GetEffect(EffectsIndicies effectIndex) => tweenEffects[(int)effectIndex];
        public void SetEffect(EffectsIndicies effectIndex, Tween tween) => tweenEffects[(int)effectIndex] = tween;
    }

    private bool EntityIsValid(Entity entity) {
        return entity.trans && entities.lookup.ContainsKey(entity.gameObject);
    }
    
    private Entity SpawnItemAsEntity(Item item, int count, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) {
        Entity entity = SpawnEntity(entityPools.itemDrop, position, rotation, parent, lifetime);
        ItemInstance itemInstance = new(item, count);
        entity.gameObject.GetComponent<ItemDrop>().Init(itemInstance);
        return entity;
    }
    
    private T SpawnEntityOneShot<T>(EntityPool<T> pool, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) where T : Entity, new() {
        T entity = SpawnEntity(pool, position, rotation, parent, lifetime);
        Assert.IsNotNull(entity.animator, "Cannot spawn entity with one shot because there is no animator");
        DestroyEntity(entity, CurrentClipLength(entity.animator));
        return entity;
    }
    
    private T SpawnEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) where T : Entity, new() {
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        T entity = InitializeEntity<T>(obj, lifetime);
        ResetEntity(entity);
        RegisterEntity(entity);
        return entity;
    }
    
    private T SpawnEntity<T>(EntityPool<T> pool, Vector3 position, Quaternion rotation, Transform parent = null, EntityLifetime lifetime = EntityLifetime.Level) where T : Entity, new() {
        T entity;
        if (pool.standbyList.Count > 0) {
            entity = pool.standbyList.PopLast();
            entity.lifetime = lifetime;
            entity.trans.SetPositionAndRotation(position, rotation);
            entity.trans.SetParent(parent, true);
            ResetEntity(entity);
            RegisterEntity(entity);
        }
        else { 
            entity = SpawnEntity<T>(pool.prefab, position, rotation, parent, lifetime);
            entity.entityPool = pool;
        }
        
        entity.gameObject.SetActive(true);
        pool.OnSpawnCallback?.Invoke(entity);
        return entity;
    }
    
    private T InitializeEntity<T>(GameObject objInstance, EntityLifetime lifetime) where T : Entity, new() {
        T newEntity = new() {
            trans = objInstance.transform,
            collider = objInstance.TryGetComponent(out Collider2D col) ? col : null,
            rigidbody = objInstance.TryGetComponent(out Rigidbody2D rbody) ? rbody : null,
            spriteRenderer = objInstance.TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer : null,
            image = objInstance.TryGetComponent(out Image image) ? image : null,
            animator = objInstance.TryGetComponent(out Animator anim) ? anim : null,
            textMesh = objInstance.TryGetComponent(out TextMeshProUGUI text) ? text : null,
            lifetime = lifetime,
        };
        return newEntity;
    }

    private void ResetEntity<T>(T entity) where T : Entity {
        entity.health = 100;
        entity.trans.localScale = Vector3.one;
        if (entity.animator) {
            entity.animator.enabled = true;
            entity.animator.Rebind();
        }
        if (entity.gameObject.activeInHierarchy) {
            entity.animator?.Update(0);
        }
    }

    private void RegisterEntity<T>(T entity) where T : Entity {
        entities.all.Add(entity);
        entities.lookup.Add(entity.gameObject, entity);
    }
    
    private void DestroyEntities(EntityLifetime lifetime) {
        for (int i = entities.all.Count - 1; i >= 0; i--) {
            if (entities.all[i].lifetime == lifetime) {
                DestroyEntity(entities.all[i]);
            }
        }
    }
    
    private void DestroyEntity(Entity rootEntity) {
        using var autoRelease = ListPool<Entity>.Get(out List<Entity> entityHierarchy); 
        GetEntityHierarchy(rootEntity.trans, entityHierarchy);

        foreach (Entity entity in entityHierarchy) {
            for (int i = 0; i < entity.tweenEffects.Length; i++) {
                entity.tweenEffects[i].Complete();
            }
            
            bool enemyWasInLookup = entities.lookup.Remove(entity.gameObject, out _);
            if (enemyWasInLookup) {
                entities.all.Remove(entity);
                DestroyOrReleaseEntitysGameObject(entity);
            }
        }
    }

    private void GetEntityHierarchy(Transform root, List<Entity> entityHierarchy) {
        if (entities.lookup.TryGetValue(root.gameObject, out Entity associatedEntity)) {
            entityHierarchy.Add(associatedEntity);
        }
        foreach (Transform trans in root) {
            GetEntityHierarchy(trans, entityHierarchy);
        }
    }

    private void DestroyOrReleaseEntitysGameObject(Entity entity) {
        if (entity.entityPool == null) {
            Destroy(entity.gameObject);
            return;
        }
        entity.gameObject.transform.SetParent(transform, true);
        entity.entityPool.ReleaseEntity(entity);
    }

    private void DestroyEntity(Entity entity, float delay) {
        if (delay == 0f) {
            DestroyEntity(entity);
            return;
        }
        Delay(entity, delay, static entity => gameInstance.DestroyEntity(entity));
    }
    
    private static int damageFlashTintPropertyId = Shader.PropertyToID("_DamageFlashTint");
    
    private void AddFlashHitEffect(Entity entity) {
        float duration = curves.hitFlash.keys[^1].time;
        
        entity.GetEffect(EffectsIndicies.HitFlash).Stop();
        
        Tween tween = Tween.Custom(entity, 0f, 1f, duration, ease: Ease.Linear, onValueChange: static (entity, val) => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, gameInstance.curves.hitFlash.Evaluate(val));
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        })
        .OnComplete(entity, static entity => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 0);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        });

        entity.SetEffect(EffectsIndicies.HitFlash, tween);
    }

    private static int poisonedPropertyId = Shader.PropertyToID("_Poisoned");
    
    public struct PoisonedEffect {
        public Entity poisonDebuffEntity;
    }
    
    public void AddPoisonedEffect(Entity entity, float duration) {
        if (!entity.GetEffect(EffectsIndicies.Poisoned).isAlive) {
            Entity poisonDebuff = SpawnEntity(entityPools.poisonDebuff, OffsetY(entity.position, -0.01f), Quaternion.identity, entity.trans);
            entity.poisonedEffect = new() {
                poisonDebuffEntity = poisonDebuff,
            };
        }
        
        entity.GetEffect(EffectsIndicies.Poisoned).Stop();
        
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(poisonedPropertyId, 1);
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);

        Tween tween = Delay(entity, duration, static entity => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(poisonedPropertyId, 0);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
            gameInstance.DestroyEntity(entity.poisonedEffect.poisonDebuffEntity);
        });
        
        entity.SetEffect(EffectsIndicies.Poisoned, tween);
    }
    
    public void AddPetrifyEffect(Entity entity, float duration) {
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(poisonedPropertyId, 1);
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);

        Tween tween = Delay(entity, duration, static entity => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(poisonedPropertyId, 0);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        });
        
        entity.SetEffect(EffectsIndicies.Petrify, tween);
    }
    
    public struct BounceEffect {
        public Vector2 targetPos;
        public Vector2 initialPos;
    }

    private void AddBounceEffect(Entity entity, Vector3 pos, float duration) {
        entity.bounceEffect = new() {
            targetPos = pos,
            initialPos = entity.position,
        };
        
        Tween.Custom(entity, 0f, 1f, duration, ease: Ease.Linear, onValueChange: static (entity, val) => {
            float yPos = gameInstance.curves.bounce.Evaluate(val);
            Vector2 newPos = Vector2.Lerp(entity.bounceEffect.initialPos, entity.bounceEffect.targetPos, val);
            entity.position = new(newPos.x, newPos.y + yPos, entity.position.z);
        });
    }

    public struct ParentToEntity {
        public Entity parentEntity;
        public Vector2 localOffset;
    }

    private void AddParentEffect(Entity entity, Entity parent, float duration) {
        entity.parentEffect = new() {
            parentEntity = parent,
            localOffset = parent.position - entity.position,
        };

        Tween tween = Tween.Custom(entity, 0f, 0f, duration, static (entity, _) => {
            if (!gameInstance.EntityIsValid(entity.parentEffect.parentEntity)) { 
                entity.GetEffect(EffectsIndicies.Parent).Stop();
                return;
            }
            entity.position = entity.parentEffect.parentEntity.position + (Vector3)entity.parentEffect.localOffset;
        });
        
        entity.SetEffect(EffectsIndicies.Parent, tween);
    }

    public struct ShakeEffect {
        public float jitter;
        public float magnitude;
        public AnimationCurve animCurve;
        public Vector2 randomSeed;
        public Vector3 entityStartPos;
        public float noisePos;
    }

    private void AddShakeEffect(Entity entity, float jitter, float magnitude, float time, AnimationCurve animCurve) {
        entity.shakeEffect = new() {
            jitter = jitter,
            magnitude = magnitude,
            animCurve = animCurve,
            randomSeed = new(Random.Range(int.MinValue, int.MaxValue), Random.Range(int.MinValue, int.MaxValue)),
            noisePos = 0f,
            entityStartPos = entity.position,
        }; 
        
        entity.GetEffect(EffectsIndicies.Shake).Stop();
        
        Tween tween = Tween.Custom(entity, 0f, 1f, time, ease: Ease.Linear, onValueChange: static (entity, val) => {
            ShakeEffect shakeEffect = entity.shakeEffect;
            float magnitude = shakeEffect.animCurve.Evaluate(val) * shakeEffect.magnitude;
            shakeEffect.noisePos = (shakeEffect.noisePos + shakeEffect.jitter * Time.deltaTime) % 1f;
            float x = (Mathf.PerlinNoise(shakeEffect.randomSeed.x, shakeEffect.noisePos) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(shakeEffect.randomSeed.y, shakeEffect.noisePos + 100f) - 0.5f) * 2f;
            Vector3 targetVector = new Vector3(x, y, entity.position.z) * magnitude;
            entity.position = shakeEffect.entityStartPos + targetVector;
            entity.shakeEffect = shakeEffect;
        });
        
        entity.SetEffect(EffectsIndicies.Shake, tween);
    }
    
    
    private static int dissolvePropertyId = Shader.PropertyToID("_Dissolve");
    private static int dissolveAspectRatioPropertyId = Shader.PropertyToID("_AspectRatio");
    
    private void DissolveAndDestroy(Entity entity, float duration) {
        if (entity.GetEffect(EffectsIndicies.Dissolve).isAlive) return;
        
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.SetFloat(dissolveAspectRatioPropertyId, entity.spriteRenderer.sprite.AspectRatio());
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        
        Tween tween = Tween.Custom(entity, 0f, 1f, duration, static (entity, val) => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(dissolvePropertyId, val);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        })
        .OnComplete(entity, static entity => {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.SetFloat(dissolvePropertyId, 0);
            entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
            gameInstance.DestroyEntity(entity);
        });
        entity.SetEffect(EffectsIndicies.Dissolve, tween);
    }
    
    private Tween Delay<T>(T entity, float delay, Action<T> callback) where T: Entity {
        return Tween.Delay(entity, delay, onComplete: callback, onValidate: EntityIsValid);
    }
    
}
