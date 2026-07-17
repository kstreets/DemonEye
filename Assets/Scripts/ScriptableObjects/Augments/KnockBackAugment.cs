using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/KnockBack")]
public class KnockBackAugment : Augment {
    
    public float knockBackDist;
    public float knockBackSpeed;
    public float probability;
    
    public struct InstanceData {
        public float knockBackDist;
        public float knockBackSpeed;
        public Vector2 dir;
        public float timeSpentInKnockBack;
    }

    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        if (!RollProbability(Probability(stackCount))) return;
        
        Player player = gameInstance.entities.player;
        enemy.knockBack = new() {
            knockBackDist = knockBackDist,
            knockBackSpeed = knockBackSpeed,
            dir = (enemy.position - player.position).normalized,
        }; 
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayProb(Probability(stackCount))} chance to knock an enemy back {DisplayDistance(knockBackDist)}";
    }
    
    private float Probability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.1f);
    }
    
}
