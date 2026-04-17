using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/PenetrationDamage")]
public class PenetrationDamageAugment : Augment {
    
    public struct InstanceData {
        public float damageMultiplierPerPenetration;
    }

    public float damageMultiplierPerPenetration;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.penetrationDamageAugment = new() {
            damageMultiplierPerPenetration = GetDamageMultiplierPerPenetration(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayMultiplierIncrease(GetDamageMultiplierPerPenetration(stackCount))} damage per enemy penetrated.";
    }
    
    private float GetDamageMultiplierPerPenetration(int stackCount) {
        return TaperFloat(damageMultiplierPerPenetration, stackCount, 0.35f);
    }
    
}
