using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Modifiers/BackwardsPiercingAugment")]
public class BackwardsPiercingAugment : Augment {
    
    public struct InstanceData { }

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.backwardsPiercingAugment = new();
    }

    public override string GetDescription(int stackCount) {
        return "Backwards shots travel through all enemies";
    }
    
}