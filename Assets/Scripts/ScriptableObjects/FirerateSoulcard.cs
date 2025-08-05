using UnityEngine;

public class FirerateModInstance {
    public float reduction;
}

[CreateAssetMenu(fileName = "FirerateSoulcard", menuName = "Scriptable Objects/FirerateSoulcard")]
public class FirerateSoulcard : Soulcard {

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
