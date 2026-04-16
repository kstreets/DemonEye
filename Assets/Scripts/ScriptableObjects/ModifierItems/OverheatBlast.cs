using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "OverheatBlast", menuName = "Scriptable Objects/Modifiers/OverheatBlast")]
public class OverheatBlast : EyeUpgradeItem {
    
    public struct InstanceData {
        public int numshotsUntilOverheat;
        public float damageMulti;
        public float radius;
    }

    public int numShotsUntilOverheat;
    public float damageMulti;
    public float radius;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.blast = new() {
            numshotsUntilOverheat = GetOverheatShotCount(stackCount),
            damageMulti = GetDamageMultiplier(stackCount),
            radius = radius,
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"After {DisplayNumber(GetOverheatShotCount(stackCount))} consecutive shots release a blast causing {DisplayMultiplier(GetDamageMultiplier(stackCount))} damage";
    }

    private int GetOverheatShotCount(int stackCount) {
        return TaperInteger(numShotsUntilOverheat, stackCount, 0.3f);
    }

    private float GetDamageMultiplier(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.5f);
    }
    
}
