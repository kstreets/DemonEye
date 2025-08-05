using UnityEngine;

public class BleedModInstance {
    public int bleedDamage;
    public float bleedInterval;
    public float lastBleedTime;
}

[CreateAssetMenu(fileName = "BleedSoulcard", menuName = "Scriptable Objects/BleedSoulcard")]
public class BleedSoulcard : Soulcard {

    public int bleedDamage;
    public float bleedInterval;
    
    public override void AddInstanceToEnemy(GameManager.Enemy enemy, int stackCount) {
        BleedModInstance instance = new() {
            bleedDamage = GetBleedDamage(stackCount),
            bleedInterval = bleedInterval,
            lastBleedTime = 0f,
        };
        enemy.bleed = instance;
    }

    public override string GetModifierDescription(int stackCount) {
        return $"Applies {GetBleedDamage(stackCount)} damage every {bleedInterval}s";
    }

    private int GetBleedDamage(int stackCount) {
        return bleedDamage * stackCount;
    }
    
}
