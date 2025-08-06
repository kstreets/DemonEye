using UnityEngine;

public struct SlowInstance {
    public float speedReduction;
    public float activationTime;
    public float duration;
}

[CreateAssetMenu(fileName = "SlowSoulcard", menuName = "Scriptable Objects/SlowSoulcard")]
public class SlowSoulcard : Soulcard {
    
    public float speedReduction;
    public float slowDuration;

    public override void AddInstanceToEnemy(GameManager.Enemy enemy, int stackCount) {
        SlowInstance slow = new() {
            speedReduction = GetSpeedReduction(stackCount),
            duration = slowDuration,
            activationTime = Time.time,
        };
        enemy.slow = slow;
    }

    public override string GetModifierDescription(int stackCount) {
        return $"Reduces speed by {GetSpeedReduction(stackCount)} for {slowDuration}s";
    }

    private float GetSpeedReduction(int stackCount) {
        return speedReduction * stackCount;
    }
    
}
