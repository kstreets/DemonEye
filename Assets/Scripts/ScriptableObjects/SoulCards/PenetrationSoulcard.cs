using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "PenetrationSoulcard", menuName = "Scriptable Objects/Soulcards/PenetrationSoulcard")]
public class PenetrationSoulcard : Soulcard {

    public struct InstanceData {
        public int goThroughCount;
    }
    
    public int startingGoThroughCount;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.penetration = new() {
            goThroughCount = GetGoThroughCount(stackCount),
        };
    }

    public override string GetStackDescription(int stackCount) {
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
