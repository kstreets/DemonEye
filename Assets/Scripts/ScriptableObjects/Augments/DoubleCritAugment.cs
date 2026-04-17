using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/DoubleCrit")]
public class DoubleCritAugment : Augment {
    
    public struct InstanceData {
        public float damageMulti;
        public float multiplierDuration;
    }

    public float damageMulti;
    public float multiplierDuration;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.doubleCritAugment = new() {
            damageMulti = GetDamageMultiplier(stackCount),
            multiplierDuration = GetMultiplierDuration(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayMultiplier(GetDamageMultiplier(stackCount))} damage for {DisplaySeconds(GetMultiplierDuration(stackCount))} after {DisplayNumber(2)} critical strikes in a row";
    }
    
    private float GetDamageMultiplier(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.25f);
    }
    
    private float GetMultiplierDuration(int stackCount) {
        return TaperFloat(multiplierDuration, stackCount, 0.25f);
    }
    
}
