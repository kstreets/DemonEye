using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "FirerateModifier", menuName = "Scriptable Objects/Modifiers/FirerateModifier")]
public class FirerateEyeUpgradeItem : EyeUpgradeItem {

    public struct InstanceData {
        public float rateIncreasePercentage;
    }
    
    public float rateIncreasePercentage;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        InstanceData instance = new() {
            rateIncreasePercentage = GetReductionPercentage(stackCount), 
        };
        eyeInstance.firerate = instance;
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProbIncrease(GetReductionPercentage(stackCount))} rate of fire";
    }

    private float GetReductionPercentage(int stackCount) {
        return TaperFloat(rateIncreasePercentage, stackCount, 0.45f);
    }
    
}
