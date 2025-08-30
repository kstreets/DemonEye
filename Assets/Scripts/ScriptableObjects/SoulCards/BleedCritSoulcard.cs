using UnityEngine;

public struct BleedCritInstance {
    public float probability;
}

[CreateAssetMenu(fileName = "BleedCritSoulcard", menuName = "Scriptable Objects/Soulcards/BleedCritSoulcard")]
public class BleedCritSoulcard : Soulcard {

    public float probability;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.bleedCrit = new() {
            probability = GetProbability(stackCount) 
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        string displayProb = DisplayProbability(GetProbability(stackCount)); 
        return $"Increases chance for critical strike by {displayProb} on a bleeding enemy";
    }
    
    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
