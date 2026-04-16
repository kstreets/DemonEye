using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "StoppingPowerModifier", menuName = "Scriptable Objects/Modifiers/StoppingPowerModifier")]
public class StoppingPowerEyeUpgradeItem : EyeUpgradeItem {

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

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayIncrease(GetExtraDamage(stackCount))} damage\n{DisplayProbDecrease(percentSpeedReduction)} projectile velocity";
    }

    private int GetExtraDamage(int stackCount) {
        return TaperInteger(extraDamage, stackCount, 0.7f);
    }
    
    private float GetProjectileSpeedReduction(int stackCount) {
        return TaperFloat(percentSpeedReduction, stackCount, 0.15f);
    }
    
}
