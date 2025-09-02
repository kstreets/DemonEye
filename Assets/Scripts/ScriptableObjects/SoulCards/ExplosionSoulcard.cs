using UnityEngine;

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
            damage = damage,
            radius = radius,
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProbability(GetProbability(stackCount))} chance for a projectile to explode on impact causing {damage} damage.";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }
}
