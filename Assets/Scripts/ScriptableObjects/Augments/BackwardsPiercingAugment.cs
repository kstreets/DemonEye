using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/BackwardsPiercingAugment")]
public class BackwardsPiercingAugment : Augment {
    
    public struct InstanceData { }

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.backwardsPiercingAugment = new();
    }

    public override string GetDescription() {
        return $"Backwards shots travel through all enemies";
    }

}
