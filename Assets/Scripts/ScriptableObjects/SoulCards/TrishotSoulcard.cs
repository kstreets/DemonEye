using UnityEngine;

[CreateAssetMenu(fileName = "TrishotSoulcard", menuName = "Scriptable Objects/Soulcards/TrishotSoulcard")]
public class TrishotSoulcard : Soulcard {

    public struct InstanceData {
        public float probability;
    }
    
    public float probability;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
            probability = GetProbability(stackCount),
        };
        eyeInstance.trishot = instance;
    }

    public override string GetStackDescription(int stackCount) {
        string displayProb = DisplayProbability(GetProbability(stackCount));
        return $"{displayProb} chance that an attack splits into 3";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
