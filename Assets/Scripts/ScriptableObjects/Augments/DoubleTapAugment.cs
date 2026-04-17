using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/DoubleTap")]
public class DoubleTapAugment : Augment {
    
    public struct InstanceData {
        public float delayBetweenShots;
        public float probability;
    }

    public float delayBetweenShots;
    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.doubleTapAugment = new() {
            delayBetweenShots = delayBetweenShots,
            probability = GetProbability(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to shoot {DisplayNumber(2)} times in quick succession";
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.3f);
    }
    
}
