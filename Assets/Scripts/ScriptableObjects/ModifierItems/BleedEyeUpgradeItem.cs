using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BleedModifier", menuName = "Scriptable Objects/Modifiers/BleedModifier")]
public class BleedEyeUpgradeItem : EyeUpgradeItem {
    
    public struct InstanceData {
        public float damageMultiplier;
        public float bleedInterval;
        public float lastBleedTime;
    }

    public float damageMulti;
    public float bleedInterval;
    
    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        if (enemy.bleed.TryGetValue(out InstanceData bleed)) {
            bleed.damageMultiplier = GetBleedDamageMultiplier(stackCount);
            bleed.bleedInterval = bleedInterval;
            enemy.bleed = bleed;
            return;
        }
        
        InstanceData instance = new() {
            damageMultiplier = GetBleedDamageMultiplier(stackCount),
            bleedInterval = bleedInterval,
            lastBleedTime = 0f,
        };
        enemy.bleed = instance;
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"{DisplayProb(1f)} chance to inflict bleed, causing {DisplayMultiplier(GetBleedDamageMultiplier(stackCount))} damage initially and per bleed tick ({DisplaySeconds(bleedInterval)})";
    }

    private float GetBleedDamageMultiplier(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.6f);
    }
    
}
