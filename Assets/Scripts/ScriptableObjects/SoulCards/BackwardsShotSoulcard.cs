using UnityEngine;
using static Game;

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
        return $"{DisplayProb(GetProbability(stackCount))} chance to shoot an additional projectile backwards";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
