using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "DoubleCritModifier", menuName = "Scriptable Objects/Modifiers/DoubleCritModifier")]
public class DoubleCritModifierItem : ModifierItem {
    
    public struct InstanceData {
        public float damageMulti;
        public float multiplierDuration;
    }
    
    public float damageMulti;
    public float multiplierDuration;

    public override void AddInstanceToTrinketPower(ref TrinketPowers trinkets, int stackCount) {
        trinkets.doubleCrit = new() {
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
