using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ScatterShot", menuName = "Scriptable Objects/EyeUpgrade/ScatterShot")]
public class ScatterShotEyeUpgrade : EyeUpgrade {

    public struct InstanceData {
        public float probability;
        public int extraProjectileCount;
        public float spreadAngle;
        public float splitDamageMultiplier;
    }

    [Range(0f, 1f)]
    public float probability;
    public int extraProjectileCount;
    public float spreadAngle;
    public float splitDamageMultiplier;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.scatterShot = new() {
            probability = GetProbability(stackCount),
            extraProjectileCount = GetExtraProjectileCount(stackCount),
            spreadAngle = spreadAngle,
            splitDamageMultiplier = GetSplitDamageMultiplier(stackCount),
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        float prob = GetProbability(stackCount);
        int count = GetExtraProjectileCount(stackCount);
        float splitDamage = GetSplitDamageMultiplier(stackCount);
        string projectiles = count == 1 ? "projectile" : "projectiles";
        return $"{DisplayProb(prob)} chance to shoot {DisplayNumber(count)} extra {projectiles} with {DisplayMultiplier(splitDamage)} damage";
    }
    
    public override float GetRangePercentage(int stackCount) {
        return TaperFloat(rangePercentage, stackCount, 0.25f);
    }
    
    private float GetSplitDamageMultiplier(int stackCount) {
        return TaperFloat(splitDamageMultiplier, stackCount, 0.3f);
    }

    private float GetProbability(int stackCount) {
        return Mathf.Clamp01(TaperFloat(probability, stackCount, 0.3f));
    }

    private int GetExtraProjectileCount(int stackCount) {
        return TaperInteger(extraProjectileCount, stackCount, 0.5f);
    }

}
