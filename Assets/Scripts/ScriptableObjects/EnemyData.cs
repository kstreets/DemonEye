using UnityEditor;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject {

    public enum EnemyType { Normal, Doughmon, Boomon, Meatbalon, }

    public string displayName;
    public EnemyType type;
    public GameObject enemyPrefab;
    public AnimatorOverrideController animatorOverride;
    
    [Header("Drops")]
    public DropPool dropPool;
    public float chanceToDropItem;
    
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
    public int soulWorthPerKill;
    
    [Header("Info")]
    [ReadOnly] public float defualtMass;
    
#if UNITY_EDITOR
    
    private void OnValidate() {
        string[] assetGuids = AssetDatabase.FindAssets($"t:{nameof(EnemyData)}");
        
        float slowestEnemySpeed = speed;
        float fastestEnemySpeed = speed;
        foreach (string guidString in assetGuids) {
            if (GUID.TryParse(guidString, out GUID guid)) {
                EnemyData enemy = AssetDatabase.LoadAssetByGUID<EnemyData>(guid);
                slowestEnemySpeed = Mathf.Min(slowestEnemySpeed, enemy.speed);
                fastestEnemySpeed = Mathf.Max(fastestEnemySpeed, enemy.speed);
            }
        }
        
        const float minMass = 1;
        const float maxMass = 100f;
        float t = Mathf.Clamp01((speed - slowestEnemySpeed) / (fastestEnemySpeed - slowestEnemySpeed));
        defualtMass = Mathf.Lerp(maxMass, minMass, 1f - (1f - t) * (1f - t));
    }
    
#endif
    
}
