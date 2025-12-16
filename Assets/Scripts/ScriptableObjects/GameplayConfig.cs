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
    public float baseSpeed;
    public int postDeathStartingHealth;
    
    [Header("Portal")]
    public float portalSummonTime;
    public float portalPostSummonDelay;
    public float portalActiveDuration;

    [Header("Misc")]
    public float distancePerUnit;

    [Header("Encumberment")]
    public int defaultStartingEncumberingWeight;
    public int maxEncumberedWeight;
    public float maxEncumberedSpeedReduction;

    [Header("Stat Upgrades")]
    public int carryCapacityIncPerLevel;
    public float critChanceIncPerLevel;
    public float critMultiplierIncPerLevel;
    public int damageIncPerLevel;
    public float firerateIncPerLevel;
    public int healthIncPerLevel;
    public float healingIncPerLevel;
    public float healingSpeedIncPerLevel;
    public float lootingSpeedIncPerLevel;
    public float movementSpeedIncPerLevel;
    public float projectileCountIncPerLevel;

}
