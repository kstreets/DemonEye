using UnityEngine;

[CreateAssetMenu(fileName = "BleedCritSoulcard", menuName = "Scriptable Objects/Soulcards/BleedCritSoulcard")]
public class BleedCritSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.bleedCrit = new() {
            probability = GetProbability(stackCount) 
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProbIncrease(GetProbability(stackCount))} critical strike chance on a bleeding enemy";
    }
    
    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
    
}
