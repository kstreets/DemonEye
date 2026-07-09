using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MapReference", menuName = "Scriptable Objects/MapReference")]
public class MapData : ScriptableObject {

    public string displayName;
    public string sceneReference;
    public RaidSpawnPattern spawning;
    public MapWaterSettings waterSettings;

    [Header("Variables")]
    public int altarSoulPrice;
    public int exitPortalsCount;
    public bool playerCantBleed;
    
    [Header("Eye Upgrade Drop Chances")]
    public float eyeUpgradeOnBodyChance;
    public float consecutiveEyeUpgradeChanceReductionOnBody;

    [Header("Loot Increase")]
    public float commonLootRarityIncrease;
    public float uncommonLootRarityIncrease;
    public float rareLootRarityIncrease;
    public float epicLootRarityIncrease;
    public float legendaryLootRarityIncrease;

    public class State {
        public bool isUnlocked;
        public List<Vector2> bloodMushroomSpawns;
    }
    
    [NonSerialized] public State state;
    
    // -------------------------------------------------------------
    // Interface to inject different raid spawn patterns for testing
    // -------------------------------------------------------------

#if UNITY_EDITOR
    
    private Func<RaidSpawnPattern> GetInjectedRaidSpawnPattern;
    
    public void SetRaidSpawnPatternInjection(Func<RaidSpawnPattern> injectedFunc) {
        GetInjectedRaidSpawnPattern = injectedFunc;
    }
    
    private void OnEnable() {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    private RaidSpawnPattern originalRaidSpawnPattern;
    
    private void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
        switch (stateChange) {
            case PlayModeStateChange.EnteredPlayMode:
                originalRaidSpawnPattern = spawning;
                spawning = GetInjectedRaidSpawnPattern != null ? GetInjectedRaidSpawnPattern() : spawning;
                break;
            case PlayModeStateChange.ExitingPlayMode:
                spawning = originalRaidSpawnPattern;
                break;
        }     
    }
    
#endif

}
