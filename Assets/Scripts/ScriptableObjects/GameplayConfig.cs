using UnityEngine;

[CreateAssetMenu(fileName = "GameplayConfig", menuName = "Scriptable Objects/GameplayConfig")]
public class GameplayConfig : ScriptableObject {

    [Header("Attack")]
    public float attackDelay;
    public float cappedMinAttackDelay;
    public int damage;
    public float rangeInSeconds;
    public float accuracy;
    public float projectileSpeed;

    [Header("Crit")]
    public float defaultCritChance;
    public float defaultCritMulti;

    [Header("Player")]
    public float baseSpeed;
    public float repeatCollisionDamageDelay;
    
    [Header("Portal")]
    public float portalSummonTime;
    public float portalPostSummonDelay;
    public float portalActiveDuration;

    [Header("Misc")]
    public float distancePerUnit;
    public int raidsPerTraderRestock;

    [Header("Encumberment")]
    public int defaultStartingEncumberingWeight;
    public int maxEncumberedWeight;
    public float maxEncumberedSpeedReduction;

    [Header("Stat Upgrades")]
    public float bleedResistIncPerLevel;
    public int carryCapacityIncPerLevel;
    public float critChanceIncPerLevel;
    public float critMultiplierIncPerLevel;
    public float damageMultiplierIncPerLevel;
    public float firerateIncPerLevel;
    public int healthIncPerLevel;
    public int healingIncOnRaidExitPerLevel;
    public float healingSpeedIncPerLevel;
    public float lootingSpeedIncPerLevel;
    public float movementSpeedIncPerLevel;
    public float projectileCountIncPerLevel;

    [Header("Searching")]
    public float discoverSlotTime;
    public float discoverItemTime;

    // Added to every wave's max time and early delay on the first two raids of the first map, so new players aren't rushed
    [Header("New Player Waves")]
    public float firstRaidExtraMaxTime = 10f;
    public float firstRaidExtraEarlyDelay = 5f;
    public float secondRaidExtraMaxTime = 5f;
    public float secondRaidExtraEarlyDelay = 2.5f;

}
