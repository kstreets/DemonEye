using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;
using static Game;

public class Augment : UuidScriptableObject {

    [Header("Augment")]
    public EyeUpgradeItem eyeUpgradeDerivedFrom;
    [Range(0f, 0.99f)] public float percentChanceOfDerivedToSpawn;
    
    [Header("Map Spawning")]
    public bool spawnsOnAllMaps;
    [ShowIf(nameof(spawnsOnAllMaps), false)]
    public MapData firstSpawnMap;
    public List<MapData> spawnsOnMaps;
    [EndIf] 
    
    [Header("Trader Spawning")]
    [Range(1, 10)] public int traderLevelRequired;
    [MinMaxSlider(1, 15)] public Vector2Int traderStockRange;
    public List<ItemWithCount> barterRequirements;
    
    [NonSerialized] public EyeUpgradeItem augmentedEyeUpgradeItem;
    
    public void CreateAugmentItemFromDerived() {
        augmentedEyeUpgradeItem = Instantiate(eyeUpgradeDerivedFrom);
        augmentedEyeUpgradeItem.uuid = uuid;
        
        // Modify item rarity
        augmentedEyeUpgradeItem.chanceToSpawnFromRock *= percentChanceOfDerivedToSpawn;
        augmentedEyeUpgradeItem.chanceToSpawnOnBody *= percentChanceOfDerivedToSpawn;
        augmentedEyeUpgradeItem.chanceToSpawnOnTrader *= percentChanceOfDerivedToSpawn;
        augmentedEyeUpgradeItem.chanceToSpawnFromBush *= percentChanceOfDerivedToSpawn;
        augmentedEyeUpgradeItem.chanceToSpawnFromEnemy *= percentChanceOfDerivedToSpawn;
        augmentedEyeUpgradeItem.chanceToExistInLevel *= percentChanceOfDerivedToSpawn;
    }
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }
    
    public virtual string GetDescription(int stackCount = 1) {
        return string.Empty;
    }
    
#if UNITY_EDITOR
    
    [Button]
    private void AddToStash() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to stash inventory when game is playing");
            return;
        }
        gameInstance.TryAddItemToInventory(gameInstance.stashInventory, new(this));
    }
    
#endif 

}
