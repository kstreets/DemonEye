using UnityEngine;
using static Game;

public struct SlowInstance {
    public float speedReductionPercent;
    public float activationTime;
    public float duration;
}

[CreateAssetMenu(fileName = "SlowSoulcard", menuName = "Scriptable Objects/Soulcards/SlowSoulcard")]
public class SlowSoulcard : Soulcard {
    
    public float speedReductionPercent;
    public float slowDuration;

    public override void AddInstanceToEnemy(Game.Enemy enemy, int stackCount) {
        SlowInstance slow = new() {
            speedReductionPercent = GetSpeedReduction(stackCount),
            duration = slowDuration,
            activationTime = Time.time,
        };
        enemy.slow = slow;
    }

    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetSpeedReduction(stackCount))} enemy speed reduction for {DisplaySeconds(slowDuration)}";
    }

    private float GetSpeedReduction(int stackCount) {
        return speedReductionPercent * stackCount;
    }
    
}
