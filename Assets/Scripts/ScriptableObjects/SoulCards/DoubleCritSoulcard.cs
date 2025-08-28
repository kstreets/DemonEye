using UnityEngine;

public struct DoubleCritInstance {
    public float damageMultiplier;
}

[CreateAssetMenu(fileName = "DoubleCritSoulcard", menuName = "Scriptable Objects/DoubleCritSoulcard")]
public class DoubleCritSoulcard : Soulcard {
    
    public float damageMultiplier;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.doubleCritInstance = new() {
            damageMultiplier = GetDamageMultiplier(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"Landing 2 critical strikes in a row adds a {GetDamageMultiplier(stackCount)} multiplier.";
    }

    private float GetDamageMultiplier(int stackCount) {
        return damageMultiplier * stackCount;
    }
}
