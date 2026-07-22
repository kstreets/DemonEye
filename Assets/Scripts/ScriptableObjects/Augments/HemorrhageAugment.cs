using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/Hemorrhage")]
public class HemorrhageAugment : Augment {
    
    public struct InstanceData { }
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.hemorrhageAugment = new();
    }

    public override string GetDescription(int stackCount = 1) {
        return "Shooting an enemy exactly when it bleeds will cause it to hemorrhage";
    }
    
}