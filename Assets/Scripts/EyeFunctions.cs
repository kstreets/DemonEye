using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class DemonEyeInstance {
    
    public struct EquipedModInstance {
        public string modId;
        public int stackCount;
    }
    
    public List<EquipedModInstance> modInstances = new();
    public CoreAttack coreAttack;
    
    public FirerateModInstance? firerateModInstance;
    public TrishotModInstance? trishotModModInstance;
}

public partial class GameManager {

    public Dictionary<string, DemonEyeInstance> eyeInstanceFromItemId = new();
    public DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    public DemonEyeInstance BuildAndRegisterEye(InventoryItem item) {
        item.itemDataUuid = Guid.NewGuid().ToString();
        item._itemRef = demonEyeItem;
        
        Dictionary<string, int> eyeModCountFromId = new();
        foreach (string modUuid in item.modifierUuids) {
            if (!eyeModCountFromId.TryAdd(modUuid, 1)) {
                eyeModCountFromId[modUuid]++;
            }
        }
        
        List<DemonEyeInstance.EquipedModInstance> eyeModifiers = new();
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
        
        foreach (DemonEyeInstance.EquipedModInstance modInstance in eyeModifiers) { 
            eyeModifierLookup[modInstance.modId].AddInstanceToEye(newDemonEye, modInstance.stackCount); 
        }
        
        eyeInstanceFromItemId.Add(item.itemDataUuid, newDemonEye);
        return newDemonEye;
    }

    public bool CanShootPrimary() {
        float attackDelay = equipedEye.coreAttack.attackDelay;
        if (equipedEye.firerateModInstance.TryGetValue(out FirerateModInstance firerate)) {
            attackDelay -= firerate.reduction;
            attackDelay = Mathf.Clamp(attackDelay, equipedEye.coreAttack.cappedMinAttackDelay, equipedEye.coreAttack.attackDelay);
        }
        return attackLimiter.TimeHasPassed(attackDelay);
    }

    public void ShootPrimary() {
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

    private bool RollProbability(float probability) {
        return Random.value < probability;
    }

}
