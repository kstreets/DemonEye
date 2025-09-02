using UnityEngine;

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
        return $"Projectiles can penetrate {GetGoThroughCount(stackCount)} enemies before stopping";
    }

    private int GetGoThroughCount(int stackCount) {
        return startingGoThroughCount * stackCount;
    }

}
