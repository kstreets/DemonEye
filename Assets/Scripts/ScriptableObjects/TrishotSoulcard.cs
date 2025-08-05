using UnityEngine;

public class TrishotModInstance {
    public float probability;
}

[CreateAssetMenu(fileName = "TrishotSoulcard", menuName = "Scriptable Objects/TrishotSoulcard")]
public class TrishotSoulcard : Soulcard {

    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        TrishotModInstance instance = new() {
            probability = GetProbability(stackCount),
        };
        eyeInstance.trishotModModInstance = instance;
    }

    public override string GetModifierDescription(int stackCount) {
        return $"{GetProbability(stackCount)}% chance that an attack splits into 3";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
