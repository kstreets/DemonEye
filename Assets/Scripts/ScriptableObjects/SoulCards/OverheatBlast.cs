using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "OverheatBlast", menuName = "Scriptable Objects/Soulcards/OverheatBlast")]
public class OverheatBlast : Soulcard {
    
    public struct InstanceData {
        public int numshotsUntilOverheat;
        public int damage;
        public float radius;
    }

    public int numShotsUntilOverheat;
    public int damage;
    public float radius;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.blast = new() {
            numshotsUntilOverheat = GetOverheatShotCount(stackCount),
            damage = GetDamage(stackCount),
            radius = radius,
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"After {DisplayNumber(GetOverheatShotCount(stackCount))} consecutive shots release a blast causing {DisplayNumber(GetDamage(stackCount))} damage";
    }

    private int GetOverheatShotCount(int stackCount) {
        return TaperInteger(numShotsUntilOverheat, stackCount, 0.3f);
    }

    private int GetDamage(int stackCount) {
        return TaperInteger(damage, stackCount, 0.5f);
    }
    
}
