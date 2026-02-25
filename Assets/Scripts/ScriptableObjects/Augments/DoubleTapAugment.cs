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
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.doubleTapAugment = new() {
            delayBetweenShots = delayBetweenShots,
            probability = probability,
        };
    }

    public override string GetDescription() {
        return $"{DisplayProb(probability)} chance to shoot {DisplayNumber(2)} times in quick succession";
    }
    
}
