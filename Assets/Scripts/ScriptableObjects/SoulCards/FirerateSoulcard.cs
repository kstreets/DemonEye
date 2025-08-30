using UnityEngine;

public struct FirerateModInstance {
    public float reduction;
}

[CreateAssetMenu(fileName = "FirerateSoulcard", menuName = "Scriptable Objects/Soulcards/FirerateSoulcard")]
public class FirerateSoulcard : Soulcard {

    public float reduction;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        FirerateModInstance instance = new() {
            reduction = GetReduction(stackCount) 
        };
        eyeInstance.firerate = instance;
    }

    public override string GetStackDescription(int stackCount) {
        return $"Reduces attack cooldown by {GetReduction(stackCount)}s";
    }

    private float GetReduction(int stackCount) {
        return reduction * stackCount;
    }
    
}
