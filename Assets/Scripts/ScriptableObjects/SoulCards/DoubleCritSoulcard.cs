using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "DoubleCritSoulcard", menuName = "Scriptable Objects/Soulcards/DoubleCritSoulcard")]
public class DoubleCritSoulcard : Soulcard {
    
    public struct InstanceData {
        public float damageMultiplier;
    }
    
    public float damageMultiplier;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.doubleCrit = new() {
            damageMultiplier = GetDamageMultiplier(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayMultiplier(GetDamageMultiplier(stackCount))} damage when landing {DisplayNumber(2)} critical strikes in a row";
    }

    private float GetDamageMultiplier(int stackCount) {
        return damageMultiplier * stackCount;
    }
}
