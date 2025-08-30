using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public partial class GameManager {
    
    public struct EquipedModInstance {
        public int modId;
        public int stackCount;
        
        public Soulcard Soulcard => eyeModifierLookup[modId];
        public void ApplyToEnemy(Enemy enemy) => Soulcard.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Soulcard.AddInstanceToEye(eyeInstance, stackCount);
        public string GetDescriptionForEye() => Soulcard.GetStackDescription(stackCount);
    }

    public class DemonEyeInstance {
        public List<EquipedModInstance> modInstances = new();
        public CoreAttack coreAttack;
        
        public FirerateModInstance? firerate;
        public TrishotModInstance? trishot;
        public BleedCritInstance? bleedCrit;
        public RangeInstance? range;
        public FarDamageInstance? farDamage;
        public PenetrationInstance? penetration;
        public DoubleCritInstance? doubleCrit;
        public BackwardShotInstance? backwardShot;
        public PoisonInstance? poison;
    }

    private Dictionary<int, DemonEyeInstance> eyeInstanceFromItemId = new();
    private DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    private DemonEyeInstance BuildAndRegisterEye(InventoryItem item) {
        item.itemDataUuid = GenerateNewItemUuid();
        item._itemRef = demonEyeItem;
        
        Dictionary<int, int> eyeModCountFromId = new();
        foreach (int modUuid in item.modifierUuids) {
            if (!eyeModCountFromId.TryAdd(modUuid, 1)) {
                eyeModCountFromId[modUuid]++;
            }
        }
        
        List<EquipedModInstance> eyeModifiers = new();
        foreach (KeyValuePair<int, int> pair in eyeModCountFromId) {
            eyeModifiers.Add(new() {
                modId = pair.Key,
                stackCount = pair.Value,
            });
        }
        
        DemonEyeInstance newDemonEye = new() {
            coreAttack = defaultAttack,
            modInstances = eyeModifiers,
        };
        
        foreach (EquipedModInstance modInstance in eyeModifiers) { 
            modInstance.ApplyToEye(newDemonEye); 
        }
        
        eyeInstanceFromItemId.Add(item.itemDataUuid, newDemonEye);
        return newDemonEye;
    }

    private bool CanShootPrimary() {
        float attackDelay = equipedEye.coreAttack.attackDelay;
        if (equipedEye.firerate.TryGetValue(out FirerateModInstance firerate)) {
            attackDelay -= firerate.reduction;
            attackDelay = Mathf.Clamp(attackDelay, equipedEye.coreAttack.cappedMinAttackDelay, equipedEye.coreAttack.attackDelay);
        }
        return attackLimiter.TimeHasPassed(attackDelay);
    }

    private void ShootPrimary() {
        ProjectilePrimaryShoot();
    }

    private void ProjectilePrimaryShoot() {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);

        const float maxInaccuracyAngle = 18f;
        float maxAccuracyAngle = maxInaccuracyAngle * (1f - equipedEye.coreAttack.accuracy);
        float accuracyAngle = Random.Range(-maxAccuracyAngle, maxAccuracyAngle);
        
        Vector2 dir = (mouseWorldPos - player.trans.PositionV2()).normalized;
        dir = Quaternion.AngleAxis(accuracyAngle, Vector3.forward) * dir;
        Vector2 velocity = dir * equipedEye.coreAttack.projectileSpeed; 
        SpawnProjectile(velocity);

        if (equipedEye.trishot.TryGetValue(out TrishotModInstance trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(secondShotVelocity);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(thirdShotVelocity);
        }

        if (equipedEye.backwardShot.TryGetValue(out BackwardShotInstance backShot) && RollProbability(backShot.probability)) {
            SpawnProjectile(-velocity);
        }
    }
    
    private void SpawnProjectile(Vector2 velocity) {
        float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
        Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        float travelDist = equipedEye.coreAttack.range;
        if (equipedEye.range.TryGetValue(out RangeInstance rangeIncrease)) {
            travelDist += rangeIncrease.distanceIncrease;
        }
        float destroyTime = travelDist / velocity.magnitude;

        if (equipedEye.farDamage.TryGetValue(out FarDamageInstance farDamage)) {
             
        }

        Projectile projectile = SpawnEntity(projectilePool, player.position + new Vector3(0f, 0.13f, 0f), projectileRotation);
        projectile.destroyTime = destroyTime;
        projectile.velocity = velocity;
        projectile.eyeInstanceSpawnedFrom = equipedEye;
        projectiles.Add(projectile);
    }

}
