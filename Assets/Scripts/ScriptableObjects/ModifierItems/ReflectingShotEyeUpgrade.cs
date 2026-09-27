using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BackwardsShotModifier", menuName = "Scriptable Objects/Modifiers/BackwardsShotModifier")]
public class ReflectingShotEyeUpgrade : EyeUpgrade {
    
    public struct InstanceData {
        public float probability;
    }
    
    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.reflectingShot = new() {
            probability = GetProbability(stackCount),
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to reflect in a random direction after damaging an enemy";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }

}
