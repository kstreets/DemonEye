using System;
using UnityEngine;
using VInspector;
using static Game;

public class Augment : UuidScriptableObject {

    public EyeUpgradeItem eyeUpgradeDerivedFrom;
    [Range(0f, 0.99f)] public float chanceToSpawnReduction;
    
    [NonSerialized] public EyeUpgradeItem augmentedEyeUpgradeItem;
    
    public void CreateAugmentItemFromDerived() {
        augmentedEyeUpgradeItem = Instantiate(eyeUpgradeDerivedFrom);
        augmentedEyeUpgradeItem.uuid = uuid;
        
        // Modify item rarity
        augmentedEyeUpgradeItem.chanceToSpawnFromRock -= chanceToSpawnReduction;
        augmentedEyeUpgradeItem.chanceToSpawnOnBody -= chanceToSpawnReduction;
        augmentedEyeUpgradeItem.chanceToSpawnOnTrader -= chanceToSpawnReduction;
        augmentedEyeUpgradeItem.chanceToSpawnFromBush -= chanceToSpawnReduction;
        augmentedEyeUpgradeItem.chanceToSpawnFromEnemy -= chanceToSpawnReduction;
        augmentedEyeUpgradeItem.chanceToExistInLevel -= chanceToSpawnReduction;
    }
    
    public virtual void AddInstanceToEnemy(Enemy enemy) { }
    
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance) { }
    
    public virtual string GetDescription() {
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
