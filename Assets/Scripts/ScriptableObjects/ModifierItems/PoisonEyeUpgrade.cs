using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "PoisonModifier", menuName = "Scriptable Objects/Modifiers/PoisonModifier")]
public class PoisonEyeUpgrade : EyeUpgrade {

    public struct InstanceData {
        public float probability;
        public float duration;
        public float damageMulti;
        public float minHealthPercentForMulti;
        public float radius;
    }
    
    public float probability;
    public float radius;
    public float duration;
    public float damageMulti;
    public float minHealthPercentForMulti; 

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.poison = new() {
            probability = GetProbability(stackCount),
            duration = GetDuration(stackCount),
            damageMulti = GetDamageMulti(stackCount),
            minHealthPercentForMulti = minHealthPercentForMulti,
            radius = radius,
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to poison an enemy for {DisplaySeconds(GetDuration(stackCount))}, " +
               $"dealing {DisplayMultiplier(GetDamageMulti(stackCount))} damage to enemies over {DisplayProb(minHealthPercentForMulti)} health.";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }

    private float GetDuration(int stackCount) {
        return TaperFloat(duration, stackCount, 0.3f);
    }

    private float GetDamageMulti(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.2f);
    }
    
}
