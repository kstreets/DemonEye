using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MapReference", menuName = "Scriptable Objects/MapReference")]
public class MapData : ScriptableObject {

    public string displayName;
    public string sceneReference;
    public RaidSpawnPattern waves;

    [Header("Variables")]
    public int altarSoulPrice;
    public int exitPortalsCount;
    
    [Header("Spawns")]
    public int minRockCount;
    public int maxRockCount;
    public int minBodyCount;
    public int maxBodyCount;
    public int minForageCount;
    public int maxForageCount;
    public int minBushesCount;
    public int maxBushesCount;
    public int minAltarCount;
    public int maxAltarCount;
    
    [Header("Eye Upgrade Drop Chances")]
    public float eyeUpgradeOnBodyChance;
    public float eyeUpgradeFromRockChance;
    public float consecutiveEyeUpgradeChanceReductionOnBody;

    [Header("Gem Spawns")]
    public int maxGemCountPerRock;
    
    [Header("Loot Increase")]
    public float commonLootRarityIncrease;
    public float uncommonLootRarityIncrease;
    public float rareLootRarityIncrease;
    public float epicLootRarityIncrease;
    public float legendaryLootRarityIncrease;
    
    [NonSerialized] public bool isUnlocked;
    
}
