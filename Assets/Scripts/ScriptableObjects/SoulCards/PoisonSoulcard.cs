using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "PoisonSoulcard", menuName = "Scriptable Objects/Soulcards/PoisonSoulcard")]
public class PoisonSoulcard : Soulcard {

    public struct InstanceData {
        public float probability;
        public float duration;
    }
    
    public float probability;
    public float duration;

    public override void AddInstanceToEnemy(Game.Enemy enemy, int stackCount) {
        float prob = GetProbability(stackCount);
        if (RollProbability(prob)) {
            enemy.poisoned = new() {
                probability = GetProbability(stackCount),
                duration = GetDuration(stackCount),
            };
            inst.AddPoisonedEffect(enemy, enemy.poisoned.Value.duration);
        }
    }
    
    public override void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.poison = new() {
            probability = GetProbability(stackCount),
            duration = GetDuration(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to poison an enemy for {GetDuration(stackCount)} seconds";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }

    private float GetDuration(int stackCount) {
        return duration * stackCount;
    }

}
