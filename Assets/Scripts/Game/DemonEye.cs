using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public partial class GameManager {
    
    public struct EquipedModInstance {
        public string modId;
        public int stackCount;
        
        public Soulcard Soulcard => eyeModifierLookup[modId];
        public void ApplyToEnemy(Enemy enemy) => Soulcard.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Soulcard.AddInstanceToEye(eyeInstance, stackCount);
        public string GetDescriptionForEye() => Soulcard.GetStackDescription(stackCount);
    }

    public class DemonEyeInstance {
        public List<EquipedModInstance> modInstances = new();
        public CoreAttack coreAttack;
        
        public FirerateModInstance? firerateModInstance;
        public TrishotModInstance? trishotModModInstance;
        public BleedCritInstance? bleedCritInstance;
    }

    private Dictionary<string, DemonEyeInstance> eyeInstanceFromItemId = new();
    private DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    private DemonEyeInstance BuildAndRegisterEye(InventoryItem item) {
        item.itemDataUuid = Guid.NewGuid().ToString();
        item._itemRef = demonEyeItem;
        
        Dictionary<string, int> eyeModCountFromId = new();
        foreach (string modUuid in item.modifierUuids) {
            if (!eyeModCountFromId.TryAdd(modUuid, 1)) {
                eyeModCountFromId[modUuid]++;
            }
        }
        
        List<EquipedModInstance> eyeModifiers = new();
        foreach (KeyValuePair<string, int> pair in eyeModCountFromId) {
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
        if (equipedEye.firerateModInstance.TryGetValue(out FirerateModInstance firerate)) {
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

        if (equipedEye.trishotModModInstance.TryGetValue(out TrishotModInstance trishot) && RollProbability(trishot.probability)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(secondShotVelocity);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(thirdShotVelocity);
        }
    }
    
    private void SpawnProjectile(Vector2 velocity) {
        float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
        Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        GameObject projectile = Instantiate(equipedEye.coreAttack.projectilePrefab, player.position + new Vector3(0f, 0.13f, 0f), projectileRotation);
        
        projectiles.Add(new() {
            timeAlive = 0f,
            range = equipedEye.coreAttack.range,
            trans = projectile.transform,
            velocity = velocity,
            EyeInstanceSpawnedFrom = equipedEye,
        });
    }

}
