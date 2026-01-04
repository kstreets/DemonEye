using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BackwardsShotSoulcard", menuName = "Scriptable Objects/Soulcards/BackwardsShotSoulcard")]
public class BackwardsShotSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
    }
    
    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.backwardShot = new() {
            probability = GetProbability(stackCount),
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance per projectile to shoot a mirrored one with {DisplayProb(1)} critical strike chance";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }

}
