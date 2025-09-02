using UnityEngine;

[CreateAssetMenu(fileName = "FirerateSoulcard", menuName = "Scriptable Objects/Soulcards/FirerateSoulcard")]
public class FirerateSoulcard : Soulcard {

    public struct InstanceData {
        public float reduction;
    }
    
    public float reduction;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
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
