using System;
using UnityEngine;
using VInspector;
using static Game;

public class Augment : UuidScriptableObject {

    public EyeUpgradeItem eyeUpgradeDerivedFrom;
    [Range(0f, 0.99f)] public float percentChanceOfDerivedToSpawn;
    
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
