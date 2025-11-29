using UnityEngine;
using static Game;

public struct BleedModInstance {
    public int bleedDamage;
    public float bleedInterval;
    public float lastBleedTime;
}

[CreateAssetMenu(fileName = "BleedSoulcard", menuName = "Scriptable Objects/Soulcards/BleedSoulcard")]
public class BleedSoulcard : Soulcard {

    public int bleedDamage;
    public float bleedInterval;
    
    public override void AddInstanceToEnemy(Enemy enemy, int stackCount) {
        if (enemy.bleed.TryGetValue(out BleedModInstance bleed)) {
            bleed.bleedDamage = GetBleedDamage(stackCount);
            bleed.bleedInterval = bleedInterval;
            enemy.bleed = bleed;
            return;
        }
        
        BleedModInstance instance = new() {
            bleedDamage = GetBleedDamage(stackCount),
            bleedInterval = bleedInterval,
            lastBleedTime = 0f,
        };
        enemy.bleed = instance;
    }

    public override string GetStackDescription(int stackCount) {
        return $"{DisplayNumber(GetBleedDamage(stackCount))} damage per bleed every {DisplaySeconds(bleedInterval)}";
    }

    private int GetBleedDamage(int stackCount) {
        return TaperInteger(bleedDamage, stackCount, 0.75f);
    }
    
}
