using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "RangeSoulcard", menuName = "Scriptable Objects/Soulcards/RangeSoulcard")]
public class RangeSoulcard : Soulcard {
    
    public struct InstanceData {
        public float timeAliveIncrease;
    }

    public float timeAliveIncrease;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.range = new() {
            timeAliveIncrease = GetTimeIncrease(stackCount),
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayIncrease(GetTimeIncrease(stackCount))} projectile lifetime";
    }

    private float GetTimeIncrease(int stackCount) {
        return timeAliveIncrease * stackCount;
    }
    
}
