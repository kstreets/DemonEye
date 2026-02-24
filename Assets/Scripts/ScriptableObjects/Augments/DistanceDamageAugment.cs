using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/DistanceDamage")]
public class DistanceDamageAugment : Augment {
    
    public struct InstanceData {
        public int damageIncreasePerUnitTraveled;
    }
    
    public int damageIncreasePerUnitTraveled;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.distanceDamage = new() {
            damageIncreasePerUnitTraveled = damageIncreasePerUnitTraveled,
        };
    }

    public override string GetDescription() {
        return $"{DisplayIncrease(damageIncreasePerUnitTraveled)} damage per unit a projectile travels";
    }

}
