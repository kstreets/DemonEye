using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "PoisonSoulcard", menuName = "Scriptable Objects/Soulcards/PoisonSoulcard")]
public class PoisonSoulcard : Soulcard {

    public struct InstanceData {
        public float duration;
        public float damageMulti;
        public float minHealthPercentForMulti;
    }
    
    public float probability;
    public float duration;
    public float damageMulti;
    public float minHealthPercentForMulti; 

    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        float prob = GetProbability(stackCount);
        if (RollProbability(prob)) {
            enemy.poison = new() {
                duration = GetDuration(stackCount),
                damageMulti = damageMulti,
                minHealthPercentForMulti = minHealthPercentForMulti,
            };
            inst.AddPoisonedEffect(enemy, enemy.poison.Value.duration);
        }
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProb(GetProbability(stackCount))} chance for a projectile to poison an enemy for {DisplaySeconds(GetDuration(stackCount))}, " +
               $"dealing {DisplayMultiplier(damageMulti)} damage to enemies over {DisplayProb(minHealthPercentForMulti)} health.";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }

    private float GetDuration(int stackCount) {
        return duration * stackCount;
    }
    
}
