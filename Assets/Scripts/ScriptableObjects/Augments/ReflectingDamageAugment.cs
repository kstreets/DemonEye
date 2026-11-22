using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/ReflectingDamage")]
public class ReflectingDamageAugment : Augment {

    public struct InstanceData {
        public float damageMultiplierPerReflection;
    }

    public float damageMultiplierPerReflection;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.reflectingDamageAugment = new() {
            damageMultiplierPerReflection = GetDamageMultiplierPerReflection(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"Reflected projectiles gain {DisplayMultiplierIncrease(GetDamageMultiplierPerReflection(stackCount))} damage each time they reflect";
    }

    float GetDamageMultiplierPerReflection(int stackCount) {
        return TaperFloat(damageMultiplierPerReflection, stackCount, 0.35f);
    }

}
