using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/Hemorrhage")]
public class HemorrhageAugment : Augment {
    
    public float bleedDamageMulti;
    public float maxGraceTime;
    
    public struct InstanceData {
        public float bleedDamageMulti;
    }
    
    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        if (!enemy.bleed.HasValue || enemy.hemorrhage.HasValue) return;
        
        var bleed = enemy.bleed.GetValue();
        // Ignore the first shot where bleed is applied
        if (bleed.timeAddedToEnemy == Time.time) return;
        
        float nextBleedTime = bleed.lastBleedTime + bleed.bleedInterval;
        float timeDistToNextBleed = nextBleedTime - Time.time;
        if (timeDistToNextBleed > maxGraceTime) return;
        
        enemy.hemorrhage = new InstanceData {
            bleedDamageMulti = GetBleedDamageMulti(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        return $"Shooting an enemy exactly when it bleeds will cause {DisplayMultiplier(GetBleedDamageMulti(stackCount))} bleed damage";
    }
    
    private float GetBleedDamageMulti(int stackCount) {
        return TaperFloat(bleedDamageMulti, stackCount, 0.25f);
    }
    
}