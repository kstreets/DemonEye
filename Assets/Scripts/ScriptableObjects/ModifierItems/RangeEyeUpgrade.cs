using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "RangeModifier", menuName = "Scriptable Objects/Modifiers/RangeModifier")]
public class RangeEyeUpgrade : EyeUpgrade {
    
    public struct InstanceData {
        public float timeAliveIncrease;
    }

    public float timeAliveIncrease;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.range = new() {
            timeAliveIncrease = GetTimeIncrease(stackCount),
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayIncrease(GetTimeIncrease(stackCount))} projectile lifetime";
    }

    private float GetTimeIncrease(int stackCount) {
        return timeAliveIncrease * stackCount;
    }
    
}
