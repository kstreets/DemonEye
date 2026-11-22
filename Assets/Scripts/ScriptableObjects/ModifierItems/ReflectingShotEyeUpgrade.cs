using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BackwardsShotModifier", menuName = "Scriptable Objects/Modifiers/BackwardsShotModifier")]
public class ReflectingShotEyeUpgrade : EyeUpgrade {

    public struct InstanceData {
        public float probability;
        public int maxReflections;
    }

    public float probability;
    public int maxReflections = 1;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.reflectingShot = new() {
            probability = GetProbability(stackCount),
            maxReflections = maxReflections,
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        string reflectText = maxReflections == 1 ? "reflect in a random direction" : $"reflect in a random direction up to {DisplayNumber(maxReflections)} times";
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to {reflectText} after damaging an enemy";
    }

    float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }

}
