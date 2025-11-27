using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject {

    public enum EnemyType { Normal, Boomon, Meatbalon }

    public EnemyType type;
    public GameObject enemyPrefab;
    public AnimatorOverrideController animatorOverride;
    
    [Header("Movement")]
    public bool usesFlowField;
    public LayerMask excludeCollisionLayers;
    
    [Header("Attack")]
    public float attackDistance;
    public float attackDamageDelay;
    public float attackReach;
    public float attackRadius;
    
    [Header("Stats")]
    public float speed;
    public int health;
    public int damage;
    public float changeToCauseBleed;
    public float chanceToDropItem;
    public int soulWorthPerKill;

}
