using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class GameManager {
    
    public enum EntityLifeTime { Global, Level }

    public class Entity {
        public Transform trans;
        public Collider2D collider;
        public Rigidbody2D rigidbody;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public TextMeshProUGUI textMesh;
        
        public MaterialPropertyBlock matPropertyBlock = new();
        public ObjectPool objectPool;
        public int health;
        public int damageAccumilation;
        public EntityLifeTime lifeTime;
        
        public SpringShake? springShake;
        public ScaleEffect? scaleEffect;
        public HitFlashEffect? hitFlashEffect;
        public BounceEffect? bounceEffect;
        public ParentToEntity? parentEffect;
        public TweenPosition? tweenPosition;
        
        public Vector3 position {
            get => trans.position;
            set => trans.position = value;
        }

        public bool IsValid => trans;
        public GameObject gameObject => trans.gameObject;
    }
    
    private T SpawnGlobalEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        return SpawnAndBindEntity<T>(obj, position, rotation, parent, EntityLifeTime.Global);
    }
    
    private T SpawnGlobalEntity<T>(ObjectPool pool, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        GameObject obj = pool.availableQueue.Dequeue();
        obj.SetActive(true);
        T entity = SpawnAndBindEntity<T>(obj, position, rotation, parent, EntityLifeTime.Global);
        entity.objectPool = pool;
        return entity;
    }
    
    private T SpawnLevelEntity<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        return SpawnAndBindEntity<T>(obj, position, rotation, parent, EntityLifeTime.Level);
    }

    private T SpawnLevelEntity<T>(ObjectPool pool, Vector3 position, Quaternion rotation, Transform parent = null) where T : Entity, new() {
        GameObject obj = pool.availableQueue.Dequeue();
        obj.SetActive(true);
        T entity = SpawnAndBindEntity<T>(obj, position, rotation, parent, EntityLifeTime.Level);
        entity.objectPool = pool;
        return entity;
    }
    
    private T SpawnAndBindEntity<T>(GameObject objInstance, Vector3 position, Quaternion rotation, Transform parent, EntityLifeTime lifeTime) where T : Entity, new() {
        objInstance.transform.SetPositionAndRotation(position, rotation);
        objInstance.transform.SetParent(parent);
        T newEntity = new() {
            trans = objInstance.transform,
            health = 100,
            lifeTime = lifeTime,
            collider = objInstance.TryGetComponent(out Collider2D col) ? col : null,
            rigidbody = objInstance.TryGetComponent(out Rigidbody2D rbody) ? rbody : null,
            spriteRenderer = objInstance.TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer : null,
            animator = objInstance.TryGetComponent(out Animator anim) ? anim : null,
            textMesh = objInstance.TryGetComponent(out TextMeshProUGUI text) ? text : null,
        };
        entities.Add(newEntity);
        entityLookup.Add(objInstance, newEntity);
        return newEntity;
    }
    
    private void DestroyEntity(GameObject gameObj) {
        DestroyEntity(entityLookup[gameObj]);
    }
    
    private void DestroyEntity(Entity entity) {
        entityLookup.Remove(entity.gameObject);
        entities.Remove(entity);
        DestroyOrReleaseEntitysGameObject(entity);
    }
    
    private void DestroyEntityAtIndex(int entityIndex) {
        Entity entity = entities[entityIndex];
        entityLookup.Remove(entity.gameObject);
        entities.RemoveAt(entityIndex);
        DestroyOrReleaseEntitysGameObject(entity);
    }

    private void DestroyOrReleaseEntitysGameObject(Entity entity) {
        if (!entity.objectPool) {
            Destroy(entity.gameObject);
            return;
        }
        
        // Return game object back to pool
        entity.gameObject.transform.SetParent(transform);
        entity.gameObject.SetActive(false);
        entity.objectPool.availableQueue.Enqueue(entity.gameObject);
    }

    private List<(Entity entity, float delay)> delayedEntitiesToDestroy = new();
    
    private void DestroyEntity(Entity entity, float delay) {
        delayedEntitiesToDestroy.Add((entity, delay));
    }

    private void UpdateDelayedEntitiesToDestroy() {
        for (int i = delayedEntitiesToDestroy.Count - 1; i >= 0; i--) {
            (Entity entity, float delay) tuple = delayedEntitiesToDestroy[i];
            tuple.delay -= Time.deltaTime;
            if (tuple.delay < 0) {
                if (tuple.entity.IsValid) { // Could already be cleaned up on gameover
                    DestroyEntity(tuple.entity);
                }
                delayedEntitiesToDestroy.RemoveAt(i);
            }
            else {
                delayedEntitiesToDestroy[i] = tuple;
            }
        }
    }
    
    private void UpdateEntityEffects() {
        foreach (Entity entity in entities) {
            UpdateShakeEffect(entity);
            UpdateScaleEffect(entity);
            UpdateHitFlashEffect(entity);
            UpdateBounceEffect(entity);
            UpdateParentEffect(entity);
            UpdateTweenPosition(entity);
        }
    }
    
    
    public struct SpringShake {
        public float stiffness;
        public float damping; 
        public Vector2 velocity;
        public Vector2 offset;
        public Vector2 targetPos;
    }

    private void AddSpringShakeEffect(Entity entity, Vector2 velocity) {
        const float stiffness = 1000f;
        const float damping = 15f;
        
        SpringShake shake = new() {
            stiffness = stiffness,
            damping = damping,
            targetPos = entity.trans.localPosition,
        };

        const float randomVelocityAngle = 15f;
        const float shakeMagnitude = 0.017f;
        shake.offset = (Quaternion.AngleAxis(Random.Range(-randomVelocityAngle, randomVelocityAngle), Vector3.forward) * velocity.normalized * shakeMagnitude).ToVector2();
        
        entity.springShake = shake;
    }

    private void UpdateShakeEffect(Entity entity) {
        if (!entity.springShake.TryGetValue(out SpringShake shake)) return;
        
        Vector2 displacement = shake.offset;
        Vector2 acceleration = -shake.stiffness * displacement - shake.damping * shake.velocity;
        shake.velocity += acceleration * Time.deltaTime;
        shake.offset += shake.velocity * Time.deltaTime;
        
        entity.trans.localPosition = shake.targetPos + shake.offset;
        entity.springShake = shake;
    }

    
    public struct ScaleEffect {
        public Vector3 targetScale;
        public Timer timer;
    }

    private void AddScaleEffect(Entity entity, float scalePercent, float duration) {
        if (entity.scaleEffect.TryGetValue(out ScaleEffect oldScale)) {
            entity.trans.localScale = oldScale.targetScale;
        }
        ScaleEffect scale = new() {
            targetScale = entity.trans.localScale,
        };
        entity.trans.localScale *= scalePercent;
        scale.timer.SetTime(duration);
        entity.scaleEffect = scale;
    }

    private void UpdateScaleEffect(Entity entity) {
        if (!entity.scaleEffect.TryGetValue(out ScaleEffect scale)) return;
        scale.timer.Tick();
        float comp = scale.timer.Comp();
        entity.trans.localScale = Vector3.Lerp(entity.trans.localScale, scale.targetScale, comp);  
        entity.scaleEffect = scale;
    }

    
    private int damageFlashTintPropertyId = Shader.PropertyToID("_DamageFlashTint");
    
    public struct HitFlashEffect {
        public Timer timer;
    }

    private void AddFlashHitEffect(Entity entity) {
        HitFlashEffect hitFlash = new();
        float duration = hitFlashCurve.keys[^1].time;
        hitFlash.timer.SetTime(duration);
        entity.hitFlashEffect = hitFlash;
    }

    private void UpdateHitFlashEffect(Entity entity) {
        if (!entity.hitFlashEffect.TryGetValue(out HitFlashEffect hitFlash)) return;

        if (hitFlash.timer.IsFinished) {
            entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
            entity.matPropertyBlock.Clear();
            entity.hitFlashEffect = null;
            return;
        }
        
        hitFlash.timer.Tick();
        float comp = hitFlash.timer.Comp();
        entity.spriteRenderer.GetPropertyBlock(entity.matPropertyBlock);
        entity.matPropertyBlock.Clear();
        entity.matPropertyBlock.SetFloat(damageFlashTintPropertyId, hitFlashCurve.Evaluate(comp));
        entity.spriteRenderer.SetPropertyBlock(entity.matPropertyBlock);
        entity.hitFlashEffect = hitFlash;
    }

    
    public struct BounceEffect {
        public Vector2 targetPos;
        public Vector2 initialPos;
        public Timer timer;
    }

    private void AddBounceEffect(Entity entity, Vector3 pos, float duration) {
        BounceEffect bounce = new() {
            targetPos = pos,
            initialPos = entity.position
        };
        bounce.timer.SetTime(duration);
        entity.bounceEffect = bounce;
    }

    private void UpdateBounceEffect(Entity entity) {
        if (!entity.bounceEffect.TryGetValue(out BounceEffect bounce)) return;
        
        bounce.timer.Tick();
        float comp = bounce.timer.Comp();
        float yPos = bounceCurve.Evaluate(comp);
        entity.position = Vector2.Lerp(bounce.initialPos, bounce.targetPos, comp);
        entity.position = new(entity.position.x, entity.position.y + yPos, entity.position.y);
        entity.bounceEffect = bounce;
    }
    
    
    public struct ParentToEntity {
        public Entity parentEntity;
        public Vector2 localOffset;
        public float endTime;
    }

    private void AddParentEffect(Entity entity, Entity parent, float duration) {
        ParentToEntity parentToEntity = new() {
            parentEntity = parent,
            localOffset = parent.position - entity.position,
            endTime = Time.time + duration,
        };
        entity.parentEffect = parentToEntity;
    }

    private void UpdateParentEffect(Entity entity) {
        if (!entity.parentEffect.TryGetValue(out ParentToEntity parentToEntity)) return;
        if (Time.time > parentToEntity.endTime || !parentToEntity.parentEntity.IsValid) {
            entity.parentEffect = null;
            return;
        }
        entity.position = parentToEntity.parentEntity.position + parentToEntity.localOffset.ToVector3();
    }


    public enum TweenCurve { Linear, EaseOut, EaseIn, EaseInOut }

    public struct TweenPosition {
        public Vector2 startPos;
        public Vector2 endPos;
        public Timer timer;
        public TweenCurve curve;
    }

    private void AddTweenPosition(Entity entity, Vector2 endPos, float duration, TweenCurve curve = TweenCurve.Linear) {
        TweenPosition tween = new() {
            startPos = entity.position,
            endPos = endPos,
            curve = curve
        };
        tween.timer.SetTime(duration);
        entity.tweenPosition = tween;
    }

    private void UpdateTweenPosition(Entity entity) {
        if (!entity.tweenPosition.TryGetValue(out TweenPosition tween)) return;

        tween.timer.Tick();
        float comp = tween.timer.Comp();
        
        switch (tween.curve) {
            case TweenCurve.EaseOut:
                comp = 1 - Mathf.Pow(1 - comp, 3);
                break;
            case TweenCurve.EaseIn:
                comp = Mathf.Pow(comp, 3);
                break;
            case TweenCurve.EaseInOut:
                comp = Mathf.SmoothStep(0f, 1f, comp);
                break;
        }
        
        entity.position = Vector2.Lerp(tween.startPos, tween.endPos, comp);
        entity.tweenPosition = tween;
    }
    
}
