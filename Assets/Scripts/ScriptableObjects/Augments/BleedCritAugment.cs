using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/BleedCrit")]
public class BleedCritAugment : Augment {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.bleedCritAugment = new() {
            probability = GetProbability(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayProbIncrease(GetProbability(stackCount))} critical strike chance on a bleeding enemy";
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.3f);
    }
    
}