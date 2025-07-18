using UnityEngine;

public class TrishotModInstance {
    public float probability;
}

[CreateAssetMenu(fileName = "TrishotModifier", menuName = "Scriptable Objects/TrishotModifier")]
public class TrishotModifier : EyeModifier {

    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        TrishotModInstance instance = new() {
            probability = GetProbability(stackCount),
        };
        eyeInstance.trishotModModInstance = instance;
    }
    
    protected override string GetModifierDescription(int stackCount) {
        return $"{GetProbability(stackCount)}% chance that an attack splits into 3";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
