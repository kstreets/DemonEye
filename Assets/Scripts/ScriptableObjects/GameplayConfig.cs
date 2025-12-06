using UnityEngine;

[CreateAssetMenu(fileName = "GameplayConfig", menuName = "Scriptable Objects/GameplayConfig")]
public class GameplayConfig : ScriptableObject {

    [Header("Attack")]
    public float attackDelay;
    public float cappedMinAttackDelay;
    public int damage;
    public float range;
    public float accuracy;
    public float projectileSpeed;

    [Header("Crit")]
    public float defaultCritChance;
    public float defaultCritMultiplier;

    [Header("Player")]
    public int postDeathStartingHealth;
    
    [Header("Misc")]
    public float distancePerUnit;

}
