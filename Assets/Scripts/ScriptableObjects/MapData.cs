using UnityEngine;

[CreateAssetMenu(fileName = "MapReference", menuName = "Scriptable Objects/MapReference")]
public class MapData : ScriptableObject {

    public string displayName;
    public string sceneReference;
    public RaidSpawnPattern waves;

    [Header("Variables")]
    public int altarSoulPrice;
    
    [Header("Spawns")]
    public int minRockCount;
    public int maxRockCount;
    public int minBodyCount;
    public int maxBodyCount;
    public int minForageCount;
    public int maxForageCount;
    public int minAltarCount;
    public int maxAltarCount;
    
    [Header("Eye Upgrade Drop Chances")]
    public float eyeUpgradeOnBodyChance;
    public float eyeUpgradeFromRockChance;

    [Header("Gem Spawns")]
    public int maxGemCountPerRock;
    
    [Header("Loot Increase")]
    public float increasedLootRarityChance;

}
