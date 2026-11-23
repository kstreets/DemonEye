using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "EnemyAttack", menuName = "Scriptable Objects/Enemy Attack")]
public class EnemyAttack : ScriptableObject {

    public enum AttackKind { Melee, Projectile }
    public enum DamageFrameEffect { None, SlamSmoke }
    public enum ProjectilePattern { Aimed, Ring }

    public AttackKind kind;

    [Header("Starting")]
    public float range;
    public float cooldown;
    public bool requireFacingPlayer;
    public DynamicClip startClip;

    [Header("Facing")]
    public bool snapFaceTargetOnStart;
    public bool trackTargetUntilRelease;

    [Header("Damage")]
    public int damage;
    public bool diesOnRelease;

    [ShowIf("kind", AttackKind.Melee)]
    [Range(0f, 1f)] public float bleedChance;
    public DamageFrameEffect damageFrameEffect;
    [EndIf]

    [ShowIf("kind", AttackKind.Projectile)]
    [Header("Projectile")]
    public ProjectilePattern projectilePattern;
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float projectileLifetime;
    public bool leadTarget = true;
    public bool usesRangedAttackManager = true;
    public float rangedPriority = 1f;
    [EndIf]
    
    [ShowIf("projectilePattern", ProjectilePattern.Ring)]
    [Header("Ring")]
    public int ringProjectileCount = 8;
    [Range(0f, 1f)] public float ringAngleJitter;
    

}
