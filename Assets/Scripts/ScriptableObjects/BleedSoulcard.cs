using UnityEngine;

public struct BleedModInstance {
    public int bleedDamage;
    public float bleedInterval;
    public float lastBleedTime;
}

[CreateAssetMenu(fileName = "BleedSoulcard", menuName = "Scriptable Objects/BleedSoulcard")]
public class BleedSoulcard : Soulcard {

    public int bleedDamage;
    public float bleedInterval;
    
    public override void AddInstanceToEnemy(GameManager.Enemy enemy, int stackCount) {
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
        return $"Applies {GetBleedDamage(stackCount)} damage every {bleedInterval}s";
    }

    private int GetBleedDamage(int stackCount) {
        return bleedDamage * stackCount;
    }
    
}
