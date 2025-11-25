using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BoneShatter", menuName = "Scriptable Objects/Soulcards/BoneShatter")]
public class BoneShatterSoulcard : Soulcard {
    
    public struct InstanceData {
        public int shardsCount;
        public int perShardDamage;
        public float probability;
    }

    public float probability;
    public int shardsCount;
    public int perShardDamage;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.boneShatter = new() {
            shardsCount = shardsCount,
            probability = probability,
            perShardDamage = perShardDamage,
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to shatter an enemy's bone, sending out {DisplayNumber(shardsCount)} bone fragments, each dealing {DisplayNumber(GetDamage(stackCount))}";
    }

    private int GetDamage(int stackCount) {
        return TaperInteger(perShardDamage, stackCount, 0.5f);
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.4f);
    }
    
}
