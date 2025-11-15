using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BackwardsShotSoulcard", menuName = "Scriptable Objects/Soulcards/BackwardsShotSoulcard")]
public class BackwardsShotSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
        public float damageMultiplier;
    }
    
    public float probability;
    public float damageMultiplier;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.backwardShot = new() {
            probability = GetProbability(stackCount),
            damageMultiplier = GetDamageMultiplier(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance per projectile to shoot a mirrored one with {DisplayMultiplier(GetDamageMultiplier(stackCount))} damge";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }

    private float GetDamageMultiplier(int stackCount) {
        return TaperFloat(damageMultiplier, stackCount, 0.75f);        
    }

}
