using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/BleedCrit")]
public class BleedCritAugment : Augment {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.bleedCritAugment = new() {
            probability = probability,
        };
    }

    public override string GetDescription() {
        return $"{DisplayProbIncrease(probability)} critical strike chance on a bleeding enemy";
    }
    
}