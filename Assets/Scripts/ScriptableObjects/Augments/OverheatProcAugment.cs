using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/OverheatProc")]
public class OverheatProcAugment : Augment {

    public struct InstanceData {
        public float probability;
    }

    [Range(0f, 1f)]
    public float probability;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.overheatProcAugment = new() {
            probability = GetProbability(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"Each additional shot fired has a {DisplayProb(GetProbability(stackCount))} chance to release an overheat blast";
    }

    private float GetProbability(int stackCount) {
        return Mathf.Clamp01(TaperFloat(probability, stackCount, 0.3f));
    }

}
