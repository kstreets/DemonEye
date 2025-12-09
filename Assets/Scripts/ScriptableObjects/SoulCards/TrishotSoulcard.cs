using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "TrishotSoulcard", menuName = "Scriptable Objects/Soulcards/TrishotSoulcard")]
public class TrishotSoulcard : Soulcard {

    public struct InstanceData {
        public float probability;
        public float reducedDamageMultiplier;
    }
    
    public float probability;
    public float reducedDamageMultiplier;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
            probability = GetProbability(stackCount),
            reducedDamageMultiplier = reducedDamageMultiplier,
        };
        eyeInstance.trishot = instance;
    }

    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to split a projectile into {DisplayNumber(3)}, " +
               $"each dealing {DisplayMultiplier(reducedDamageMultiplier)} damage";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.4f);
    }
    
}
