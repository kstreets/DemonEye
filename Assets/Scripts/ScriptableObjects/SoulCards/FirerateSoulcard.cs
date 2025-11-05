using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "FirerateSoulcard", menuName = "Scriptable Objects/Soulcards/FirerateSoulcard")]
public class FirerateSoulcard : Soulcard {

    public struct InstanceData {
        public float rateIncrasePercentage;
    }
    
    public float rateIncreasePercentage;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
            rateIncrasePercentage = GetReductionPercentage(stackCount), 
        };
        eyeInstance.firerate = instance;
    }

    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProbIncrease(GetReductionPercentage(stackCount))} rate of fire";
    }

    private float GetReductionPercentage(int stackCount) {
        return rateIncreasePercentage * stackCount;
    }
    
}
