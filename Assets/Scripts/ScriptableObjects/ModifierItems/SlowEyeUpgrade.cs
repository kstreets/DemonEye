using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "SlowModifier", menuName = "Scriptable Objects/Modifiers/SlowModifier")]
public class SlowEyeUpgrade : EyeUpgrade {
    
    public struct InstanceData {
        public float speedReductionPercent;
        public float duration;
        public float timeSpentSlowed;
    }
    
    public float speedReductionPercent;
    public float slowDuration;

    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        InstanceData slow = new() {
            speedReductionPercent = GetSpeedReduction(stackCount),
            duration = slowDuration,
        };
        enemy.slow = slow;
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProb(GetSpeedReduction(stackCount))} enemy speed reduction for {DisplaySeconds(slowDuration)}";
    }

    private float GetSpeedReduction(int stackCount) {
        return TaperFloat(speedReductionPercent, stackCount, 0.4f);
    }
    
}
