using UnityEngine;

public class FirerateModInstance {
    public float reduction;
}

[CreateAssetMenu(fileName = "FirerateModifier", menuName = "Scriptable Objects/FirerateModifier")]
public class FirerateModifier : EyeModifier {

    public float reduction;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        FirerateModInstance instance = new() {
            reduction = GetReduction(stackCount) 
        };
        eyeInstance.firerateModInstance = instance;
    }

    public override string GetModifierDescription(int stackCount) {
        return $"Reduces attack cooldown by {GetReduction(stackCount)}s";
    }

    private float GetReduction(int stackCount) {
        return reduction * stackCount;
    }
    
}
