using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "TrishotModifier", menuName = "Scriptable Objects/Modifiers/TrishotModifier")]
public class TrishotEyeUpgrade : EyeUpgrade {

    public struct InstanceData {
        public float probability;
        public float damageMultiplier;
    }
    
    public float probability;
    public float damageMulti;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
            probability = GetProbability(stackCount),
            damageMultiplier = damageMulti,
        };
        eyeInstance.trishot = instance;
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to split a projectile into {DisplayNumber(3)}, " +
               $"each dealing {DisplayMultiplier(damageMulti)} damage";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.4f);
    }
    
}
