using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/MultiProjectileCritAugment")]
public class MultiProjectileCritAugment : Augment {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;

    public override void AddInstanceToEye(DemonEyeInstance demonEyeInstance) {
        demonEyeInstance.multiProjectileCritAugment = new() {
            probability = probability,
        };
    }

    public override string GetDescription() {
        return $"Each additional projectile has a {DisplayProb(probability)} critical strike chance";
    }

}
