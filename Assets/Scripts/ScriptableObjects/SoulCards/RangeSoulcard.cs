using UnityEngine;

public struct RangeInstance {
    public float distanceIncrease;
}

[CreateAssetMenu(fileName = "RangeSoulcard", menuName = "Scriptable Objects/RangeSoulcard")]
public class RangeSoulcard : Soulcard {

    public float rangeIncrease;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.rangeInstance = new() {
            distanceIncrease = GetRangeIncrease(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"Increases range by {GetRangeIncrease(stackCount)}";
    }

    private float GetRangeIncrease(int stackCount) {
        return rangeIncrease * stackCount;
    }
    
}
