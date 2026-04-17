using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/DistanceDamage")]
public class DistanceDamageAugment : Augment {
    
    public struct InstanceData {
        public float damageMultiIncreasePerUnitTraveled;
    }
    
    public float damageMultiIncreasePerUnitTraveled;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.distanceDamage = new() {
            damageMultiIncreasePerUnitTraveled = GetDamageMultiplier(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayMultiplierIncDec(GetDamageMultiplier(stackCount))} damage per meter a projectile travels";
    }
    
    private float GetDamageMultiplier(int stackCount) {
        return TaperFloat(damageMultiIncreasePerUnitTraveled, stackCount, 0.2f);
    }

}
