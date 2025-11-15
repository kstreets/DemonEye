using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ExplosionSoulcard", menuName = "Scriptable Objects/Soulcards/ExplosionSoulcard")]
public class ExplosionSoulcard : Soulcard {
    
    public struct InstanceData {
        public float probability;
        public int damage;
        public float radius;
    }
    
    public float probability;
    public int damage;
    public float radius;
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.explosion = new() {
            probability = GetProbability(stackCount),
            damage = GetDamage(stackCount),
            radius = radius,
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to explode on impact causing {DisplayNumber(GetDamage(stackCount))} damage";
    }

    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.65f);
    }

    private int GetDamage(int stackCount) {
        return TaperInteger(damage, stackCount, 0.5f);
    }
    
}
