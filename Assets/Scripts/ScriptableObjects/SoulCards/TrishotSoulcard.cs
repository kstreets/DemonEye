using UnityEngine;

public struct TrishotModInstance {
    public float probability;
}

[CreateAssetMenu(fileName = "TrishotSoulcard", menuName = "Scriptable Objects/TrishotSoulcard")]
public class TrishotSoulcard : Soulcard {

    public float probability;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        TrishotModInstance instance = new() {
            probability = GetProbability(stackCount),
        };
        eyeInstance.trishotModModInstance = instance;
    }

    public override string GetStackDescription(int stackCount) {
        string displayProb = DisplayProbability(GetProbability(stackCount));
        return $"{displayProb} chance that an attack splits into 3";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
