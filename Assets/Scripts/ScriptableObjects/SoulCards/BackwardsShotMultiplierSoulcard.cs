using UnityEngine;

[CreateAssetMenu(fileName = "BackwardsShotMultiplierSoulcard", menuName = "Scriptable Objects/Soulcards/BackwardsShotMultiplierSoulcard")]
public class BackwardsShotMultiplierSoulcard : Soulcard {

    public struct InstanceData {
        public float damageMultiplier;
    }

    
    public float damageMultiplier;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.backwardsShotCrit = new() {
            damageMultiplier = GetDamageMultiplier(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayMultiplier(GetDamageMultiplier(stackCount))} damage for backwards shots";
    }

    private float GetDamageMultiplier(int stackCount) {
        return damageMultiplier * stackCount;
    }
    
}
