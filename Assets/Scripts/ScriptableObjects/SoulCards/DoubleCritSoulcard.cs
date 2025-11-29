using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "DoubleCritSoulcard", menuName = "Scriptable Objects/Soulcards/DoubleCritSoulcard")]
public class DoubleCritSoulcard : Soulcard {
    
    public struct InstanceData {
        public float damageMultiplier;
        public float multiplierDuration;
    }
    
    public float damageMultiplier;
    public float multiplierDuration;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.doubleCrit = new() {
            damageMultiplier = GetDamageMultiplier(stackCount),
            multiplierDuration = GetMultiplierDuration(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayMultiplier(GetDamageMultiplier(stackCount))} damage for {DisplaySeconds(GetMultiplierDuration(stackCount))} after {DisplayNumber(2)} critical strikes in a row";
    }

    private float GetDamageMultiplier(int stackCount) {
        return TaperFloat(damageMultiplier, stackCount, 0.3f);
    }

    private float GetMultiplierDuration(int stackCount) {
        return TaperFloat(multiplierDuration, stackCount, 0.4f);
    }
    
}
