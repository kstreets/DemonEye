using System;
using UnityEngine;
using VInspector;
using static Game;

public class Augment : UuidScriptableObject {

    public ModifierItem modifierDerivedFrom;
    [Range(0f, 0.99f)] public float chanceToSpawnReduction;
    
    [NonSerialized] public ModifierItem augmentedModifierItem;
    
    public void CreateAugmentItemFromDerived() {
        augmentedModifierItem = Instantiate(modifierDerivedFrom);
        augmentedModifierItem.uuid = uuid;
        
        // Modify item rarity
        augmentedModifierItem.chanceToSpawnFromRock -= chanceToSpawnReduction;
        augmentedModifierItem.chanceToSpawnOnBody -= chanceToSpawnReduction;
        augmentedModifierItem.chanceToSpawnOnTrader -= chanceToSpawnReduction;
        augmentedModifierItem.chanceToSpawnFromBush -= chanceToSpawnReduction;
        augmentedModifierItem.chanceToSpawnFromEnemy -= chanceToSpawnReduction;
        augmentedModifierItem.chanceToExistInLevel -= chanceToSpawnReduction;
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
