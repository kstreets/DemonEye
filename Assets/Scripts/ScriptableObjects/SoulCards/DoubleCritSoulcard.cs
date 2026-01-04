using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "DoubleCritSoulcard", menuName = "Scriptable Objects/Soulcards/DoubleCritSoulcard")]
public class DoubleCritSoulcard : Soulcard {
    
    public struct InstanceData {
        public float damageMulti;
        public float multiplierDuration;
    }
    
    public float damageMulti;
    public float multiplierDuration;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.doubleCrit = new() {
            damageMulti = GetDamageMultiplierIncrease(stackCount),
            multiplierDuration = GetMultiplierDuration(stackCount),
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayMultiplier(GetDamageMultiplierIncrease(stackCount))} damage for {DisplaySeconds(GetMultiplierDuration(stackCount))} after {DisplayNumber(2)} critical strikes in a row";
    }

    private float GetDamageMultiplierIncrease(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.3f);
    }

    private float GetMultiplierDuration(int stackCount) {
        return TaperFloat(multiplierDuration, stackCount, 0.4f);
    }
    
}
