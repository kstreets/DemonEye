using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ExplosionSoulcard", menuName = "Scriptable Objects/Soulcards/ExplosionSoulcard")]
public class ExplosionSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
        public float damageMulti;
        public float radius;
    }
    
    public float probability;
    public float damageMulti;
    public float radius;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.explosion = new() {
            probability = GetProbability(stackCount),
            damageMulti = GetDamage(stackCount),
            radius = radius,
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to explode on impact causing {DisplayMultiplier(GetDamage(stackCount))} damage";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.65f);
    }

    private float GetDamage(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.5f);
    }
    
}
