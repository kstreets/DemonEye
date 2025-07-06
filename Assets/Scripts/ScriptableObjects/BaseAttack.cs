using UnityEngine;

[CreateAssetMenu(fileName = "BaseAttack", menuName = "Scriptable Objects/BaseAttack")]
public class BaseAttack : UuidScriptableObject {
    
    public enum AttackType { Projectile, Laser }

    public AttackType attackType;
    public float attackDelay;
    public float cappedMinAttackDelay;
    public float damage;
    public float range;
    
    [VInspector.ShowIf("attackType", AttackType.Projectile)]
    public float projectileSpeed;

    [VInspector.ShowIf("attackType", AttackType.Laser)]
    public float laserDuration;
    public float laserDamageTickDelay;

}
