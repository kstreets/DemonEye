using UnityEngine;

[CreateAssetMenu(fileName = "CoreAttack", menuName = "Scriptable Objects/CoreAttack")]
public class CoreAttack : Item {
    
    public enum AttackType { Projectile, Laser }

    public AttackType attackType;
    public float attackDelay;
    public float cappedMinAttackDelay;
    public float damage;
    public float range;
    public float accuracy;
    public float enemySpeedReductionPercent;
    
    [VInspector.ShowIf("attackType", AttackType.Projectile)]
    public float projectileSpeed;
    public GameObject projectilePrefab;

    [VInspector.ShowIf("attackType", AttackType.Laser)]
    public float laserDuration;
    public float laserDamageTickDelay;

    public override string GetDescription() {
        string desc = base.GetDescription() + "\n";
        desc += $"Damage: {damage}\nFirerate: {1.0f / attackDelay}\nSpeed: {projectileSpeed}";
        return desc;
    }

}
