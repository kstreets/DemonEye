using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/PenetrationDamage")]
public class PenetrationDamageAugment : Augment {
    
    public struct InstanceData {
        public float damageMultiplierPerPenetration;
    }

    public float damageMultiplierPerPenetration;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.penetrationDamageAugment = new() {
            damageMultiplierPerPenetration = damageMultiplierPerPenetration,
        };
    }

    public override string GetDescription() {
        return $"{DisplayMultiplierIncrease(damageMultiplierPerPenetration)} damage per enemy penetrated.";
    }
    
}
