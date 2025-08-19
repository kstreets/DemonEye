using UnityEngine;

[CreateAssetMenu(fileName = "CoreAttack", menuName = "Scriptable Objects/CoreAttack")]
public class CoreAttack : Item {
    
    public float attackDelay;
    public float cappedMinAttackDelay;
    public int damage;
    public float range;
    public float accuracy;
    public float enemySpeedReductionPercent;
    public float projectileSpeed;
    public GameObject projectilePrefab;

    public override string GetDescription() {
        string desc = base.GetDescription() + "\n";
        desc += $"Damage: {damage}\nFirerate: {1.0f / attackDelay}\nSpeed: {projectileSpeed}";
        return desc;
    }

}
