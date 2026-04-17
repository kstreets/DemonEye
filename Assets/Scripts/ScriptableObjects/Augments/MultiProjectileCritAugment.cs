using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/MultiProjectileCritAugment")]
public class MultiProjectileCritAugment : Augment {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;

    public override void AddInstanceToEye(DemonEyeInstance demonEyeInstance, int stackCount) {
        demonEyeInstance.multiProjectileCritAugment = new() {
            probability = GetProbability(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"Each additional projectile has a {DisplayProb(GetProbability(stackCount))} critical strike chance";
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.3f);
    }

}
