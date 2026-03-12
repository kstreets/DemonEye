using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BleedModifier", menuName = "Scriptable Objects/Modifiers/BleedModifier")]
public class BleedModifierItem : ModifierItem {
    
    public struct InstanceData {
        public int bleedDamage;
        public float bleedInterval;
        public float lastBleedTime;
    }

    public int bleedDamage;
    public float bleedInterval;
    
    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        if (enemy.bleed.TryGetValue(out InstanceData bleed)) {
            bleed.bleedDamage = GetBleedDamage(stackCount);
            bleed.bleedInterval = bleedInterval;
            enemy.bleed = bleed;
            return;
        }
        
        InstanceData instance = new() {
            bleedDamage = GetBleedDamage(stackCount),
            bleedInterval = bleedInterval,
            lastBleedTime = 0f,
        };
        enemy.bleed = instance;
    }

    protected override string GetModifierDescription(int stackCount) {
        return $"{DisplayNumber(GetBleedDamage(stackCount))} damage per bleed every {DisplaySeconds(bleedInterval)}";
    }

    private int GetBleedDamage(int stackCount) {
        return TaperInteger(bleedDamage, stackCount, 0.75f);
    }
    
}
