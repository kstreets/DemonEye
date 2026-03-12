using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "PenetrationModifier", menuName = "Scriptable Objects/Modifiers/PenetrationModifier")]
public class PenetrationModifierItem : ModifierItem {

    public struct InstanceData {
        public int goThroughCount;
    }
    
    public int startingGoThroughCount;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.penetration = new() {
            goThroughCount = GetGoThroughCount(stackCount),
        };
    }

    protected override string GetModifierDescription(int stackCount) {
        int throughCount = GetGoThroughCount(stackCount);
        if (throughCount == 1) {
            return $"Projectiles pass through {DisplayNumber(throughCount)} enemy before stopping";
        }
        return $"Projectiles pass through {DisplayNumber(throughCount)} enemies before stopping";
    }

    private int GetGoThroughCount(int stackCount) {
        return startingGoThroughCount + (stackCount - 1);
    }

}
