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
    
    [Header("Portal")]
    public float portalSummonTime;
    public float portalPostSummonDelay;
    public float portalActiveDuration;

    [Header("Misc")]
    public float distancePerUnit;

    [Header("Stat Upgrades")]
    public int carryCapacityIncPerLevel;
    public int critChanceIncPerLevel;
    public int critMultiplierIncPerLevel;
    public int damageIncPerLevel;
    public int firerateIncPerLevel;
    public int healthIncPerLevel;
    public int healingIncPerLevel;
    public int healingSpeedIncPerLevel;
    public int lootingSpeedIncPerLevel;
    public int movementSpeedIncPerLevel;
    public int projectileCountIncPerLevel;

}
