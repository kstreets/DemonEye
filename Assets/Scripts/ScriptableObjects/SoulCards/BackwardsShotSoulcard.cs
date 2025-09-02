using UnityEngine;

[CreateAssetMenu(fileName = "BackwardsShotSoulcard", menuName = "Scriptable Objects/Soulcards/BackwardsShotSoulcard")]
public class BackwardsShotSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
    }
    
    public float probability;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.backwardShot = new() {
            probability = GetProbability(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProbability(GetProbability(stackCount))} chance to shoot a projectile out the back of your head.";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
