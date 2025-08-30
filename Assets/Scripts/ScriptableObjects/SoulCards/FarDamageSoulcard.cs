using UnityEngine;

public struct FarDamageInstance {
    public int damageIncreasePerUnitTraveled;
}
 
[CreateAssetMenu(fileName = "FarDamageSoulcard", menuName = "Scriptable Objects/Soulcards/FarDamageSoulcard")]
public class FarDamageSoulcard : Soulcard {
    
    public int damageIncreasePerUnitTraveled;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.farDamage = new() {
            damageIncreasePerUnitTraveled = GetDamageIncrease(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"Adds an additional {GetDamageIncrease(stackCount)} damage per unit a projectile travels.";
    }

    private int GetDamageIncrease(int stackCount) {
        return damageIncreasePerUnitTraveled * stackCount;
    }
    
}
