using UnityEngine;

[CreateAssetMenu(fileName = "CoreAttack", menuName = "Scriptable Objects/CoreAttack")]
public class CoreAttack : UuidScriptableObject {
    
    public enum AttackType { Projectile, Laser }

    public AttackType attackType;
    public float attackDelay;
    public float cappedMinAttackDelay;
    public float damage;
    public float range;
    
    [VInspector.ShowIf("attackType", AttackType.Projectile)]
    public float projectileSpeed;
    public GameObject projectilePrefab;

    [VInspector.ShowIf("attackType", AttackType.Laser)]
    public float laserDuration;
    public float laserDamageTickDelay;

}
