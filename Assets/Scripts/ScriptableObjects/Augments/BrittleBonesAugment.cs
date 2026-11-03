using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/BrittleBones")]
public class BrittleBonesAugment : Augment {

    public struct InstanceData {
        public float probability;
    }

    [Range(0f, 1f)]
    public float probability;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.brittleBonesAugment = new() {
            probability = GetProbability(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayProbIncrease(GetProbability(stackCount))} chance to shatter a poisoned enemy's bone";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.3f);
    }

}
