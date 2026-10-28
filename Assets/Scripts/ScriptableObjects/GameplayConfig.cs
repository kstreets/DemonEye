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
    // 0 shoots straight in the cardinal direction, 1 shoots exactly where the mouse/stick is pointing
    [Range(0f, 1f)] public float aimAssistBias = 0.3f;

    [Header("Crit")]
    public float defaultCritChance;
    public float defaultCritMulti;

    [Header("Player")]
    public float baseSpeed;
    public float repeatCollisionDamageDelay;

    [Header("Teleport")]
    public float teleportDistance = 0.4f;
    public float teleportHiddenDuration = 0.08f;
    public float teleportInvulnerabilityDuration = 0.3f;
    public float teleportBaseCooldown = 0.8f;
    public float teleportMaxCooldown = 1.5f;
    public float teleportCooldownIncreasePerRapidUse = 0.25f;
    public float teleportRapidUseWindow = 0.3f;
    // After teleporting, holding roughly the same direction slides the player, tapering from the start speed down to normal run speed
    public float teleportSlideDuration = 0.42f;
    public float teleportSlideStartSpeedMulti = 1.5f;
    // How far the move input can be from the teleport direction (in degrees) and still count as the same general direction
    public float teleportSlideMaxAngle = 45f;

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
