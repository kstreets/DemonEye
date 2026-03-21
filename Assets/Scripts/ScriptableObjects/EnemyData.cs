using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject {

    public enum EnemyType { Normal, Doughmon, Boomon, Meatbalon }

    public string displayName;
    public EnemyType type;
    public GameObject enemyPrefab;
    public AnimatorOverrideController animatorOverride;
    
    [Header("Movement")]
    public bool usesFlowField;
    public LayerMask excludeCollisionLayers;
    
    [Header("Attack")]
    public float attackDistance;
    public float attackDamageDelay;
    public float attackRadius;
    public Vector2 sideAttackOffset;
    public Vector2 upAttackOffset;
    public Vector2 donwAttackOffset;
    
    [Header("Stats")]
    public float speed;
    public int health;
    public int damage;
    public int collisionDamage;
    public float changeToCauseBleed;
    public float chanceToDropItem;
    public int soulWorthPerKill;
    
}
