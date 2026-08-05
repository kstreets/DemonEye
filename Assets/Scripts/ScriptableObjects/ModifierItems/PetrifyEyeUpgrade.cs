using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "Petrify", menuName = "Scriptable Objects/Modifiers/Petrify")]
public class PetrifyEyeUpgrade : EyeUpgrade {
    
    public float probability;
    public float duration;
    public int volleyCount;
    
    public struct InstanceData {
        public float duration;
        public int volleyCount;
    }
    
    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        if (enemy.petrify.HasValue) return;
        if (!RollProbability(GetProbability(stackCount))) return;
        
        ClearEnemyDebuffs(enemy);
        
        float duration = GetDuration(stackCount);
        gameInstance.AddPetrifyEffect(enemy, duration);
        
        enemy.petrify = new InstanceData {
            duration = duration,
            volleyCount = GetVolleyCount(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayProb(GetProbability(stackCount))} chance to petrify an enemy for {DisplaySeconds(GetDuration(stackCount))} (Petrified enemies crumble when ran through)";
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.4f);
    }
    
    private float GetDuration(int stackCount) {
        return TaperFloat(duration, stackCount, 0.8f);
    }
    
    private int GetVolleyCount(int stackCount) {
        return TaperInteger(volleyCount, stackCount, 0.75f);
    }
    
}
