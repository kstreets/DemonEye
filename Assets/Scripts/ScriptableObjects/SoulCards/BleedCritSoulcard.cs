using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BleedCritSoulcard", menuName = "Scriptable Objects/Soulcards/BleedCritSoulcard")]
public class BleedCritSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.bleedCrit = new() {
            probability = GetProbability(stackCount),
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayProbIncrease(GetProbability(stackCount))} critical strike chance on a bleeding enemy";
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }
    
}
