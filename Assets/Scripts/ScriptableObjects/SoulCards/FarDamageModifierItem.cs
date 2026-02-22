using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "FarDamageModifier", menuName = "Scriptable Objects/Modifiers/FarDamageModifier")]
public class FarDamageModifierItem : ModifierItem {
    
    public struct InstanceData {
        public int damageIncreasePerUnitTraveled;
    }
    
    public int damageIncreasePerUnitTraveled;

    public override void AddInstanceToTrinketPower(ref TrinketPowers trinkets, int stackCount) {
        trinkets.farDamage = new() {
            damageIncreasePerUnitTraveled = GetDamageIncrease(stackCount),
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayIncrease(GetDamageIncrease(stackCount))} damage per unit a projectile travels";
    }

    private int GetDamageIncrease(int stackCount) {
        return damageIncreasePerUnitTraveled * stackCount;
    }
    
}
