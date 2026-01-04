using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "TrishotSoulcard", menuName = "Scriptable Objects/Soulcards/TrishotSoulcard")]
public class TrishotSoulcard : Soulcard {

    public struct InstanceData {
        public float probability;
        public float damageMultiplier;
    }
    
    public float probability;
    public float damageMultiplier;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
            probability = GetProbability(stackCount),
            damageMultiplier = damageMultiplier,
        };
        eyeInstance.trishot = instance;
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to split a projectile into {DisplayNumber(3)}, " +
               $"each dealing {DisplayMultiplier(damageMultiplier)} damage";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.4f);
    }
    
}
