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
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.doubleCritAugment = new() {
            damageMulti = damageMulti,
            multiplierDuration = multiplierDuration,
        };
    }

    public override string GetDescription() {
        return $"{DisplayMultiplier(damageMulti)} damage for {DisplaySeconds(multiplierDuration)} after {DisplayNumber(2)} critical strikes in a row";
    }
    
}
