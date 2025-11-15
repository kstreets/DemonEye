using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ProjectileCountSoulcard", menuName = "Scriptable Objects/Soulcards/ProjectileCountSoulcard")]
public class ProjectileCountSoulcard : Soulcard {

    public struct InstanceData {
        public int extraProjectileCount;
    }
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.projectileCount = new() {
            extraProjectileCount = stackCount,
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        if (stackCount == 1) {
            return $"{DisplayIncrease(stackCount)} projectile";
        }
        return $"{DisplayIncrease(stackCount)} projectiles";
    }
    
}
