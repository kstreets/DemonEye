using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BoneShatter", menuName = "Scriptable Objects/Modifiers/BoneShatter")]
public class BoneShatterEyeUpgrade : EyeUpgrade {
    
    public struct InstanceData {
        public int shardsCount;
        public float perShardDamageMulti;
        public float probability;
        public float lifeTime;
    }

    public float probability;
    public int shardsCount;
    public float perShardDamageMulti;
    public float projectileLifetime;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.boneShatter = new() {
            shardsCount = shardsCount,
            probability = probability,
            perShardDamageMulti = perShardDamageMulti,
            lifeTime = projectileLifetime,
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to shatter an enemy's bone, " +
               $"sending out {DisplayNumber(shardsCount)} bone fragments, each dealing {DisplayMultiplier(GetDamage(stackCount))} damage";
    }

    private float GetDamage(int stackCount) {
        return TaperFloat(perShardDamageMulti, stackCount, 0.5f);
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.4f);
    }
    
}
