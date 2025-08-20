using UnityEngine;

public struct PenetrationInstance {
    public int goThroughCount;
}

[CreateAssetMenu(fileName = "PenetrationSoulcard", menuName = "Scriptable Objects/PenetrationSoulcard")]
public class PenetrationSoulcard : Soulcard {

    public int startingGoThroughCount;
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.penetrationInstance = new() {
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
