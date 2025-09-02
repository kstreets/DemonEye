using UnityEngine;

[CreateAssetMenu(fileName = "StoppingPowerSoulcard", menuName = "Scriptable Objects/Soulcards/StoppingPowerSoulcard")]
public class StoppingPowerSoulcard : Soulcard {

    public struct InstanceData {
        public int extraDamage;
        public float percentSpeedReduction;
    }

    public int extraDamage;
    public float percentSpeedReduction;

    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.stoppingPower = new() {
            extraDamage = GetExtraDamage(stackCount),
            percentSpeedReduction = GetProjectileSpeedReduction(stackCount),
        };
    }

    public override string GetStackDescription(int stackCount) {
        return $"Increases projectile damage by {GetExtraDamage(stackCount)} and reduces the projectile velocity by {DisplayProbability(percentSpeedReduction)}";
    }

    private int GetExtraDamage(int stackCount) {
        return extraDamage * stackCount;
    }
    
    private float GetProjectileSpeedReduction(int stackCount) {
        return percentSpeedReduction * stackCount;
    }
    
}
