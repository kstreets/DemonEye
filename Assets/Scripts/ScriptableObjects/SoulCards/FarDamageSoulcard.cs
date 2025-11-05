using UnityEngine;

[CreateAssetMenu(fileName = "FarDamageSoulcard", menuName = "Scriptable Objects/Soulcards/FarDamageSoulcard")]
public class FarDamageSoulcard : Soulcard {
    
    public struct InstanceData {
        public int damageIncreasePerUnitTraveled;
    }
    
    public int damageIncreasePerUnitTraveled;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.farDamage = new() {
            damageIncreasePerUnitTraveled = GetDamageIncrease(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayIncrease(GetDamageIncrease(stackCount))} damage per unit a projectile travels";
    }

    private int GetDamageIncrease(int stackCount) {
        return damageIncreasePerUnitTraveled * stackCount;
    }
    
}
