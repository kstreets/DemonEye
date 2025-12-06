using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ProjectileCountSoulcard", menuName = "Scriptable Objects/Soulcards/ProjectileCountSoulcard")]
public class ProjectileCountSoulcard : Soulcard {

    public struct InstanceData {
        public float probability;
        public int extraProjectileCount;
    }
    
    public float probability;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.projectileCount = new() {
            probability = GetProbability(stackCount),
            extraProjectileCount = stackCount,
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        if (stackCount == 1) {
            return $"{DisplayProb(GetProbability(stackCount))} chance to shoot an extra projectile";
        }
        return $"{DisplayProb(GetProbability(stackCount))} chance shoot an extra projectile, up to {DisplayNumber(stackCount)} times";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.25f);
    }

}
