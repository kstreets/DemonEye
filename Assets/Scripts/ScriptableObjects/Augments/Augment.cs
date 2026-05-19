using System;
using UnityEngine;
using VInspector;
using static ItemComponents;
using static Game;

public class Augment : UuidScriptableObject, ItemInterface {

    public EyeUpgrade derivedFrom;
    [Range(0f, 0.99f)] public float percentChanceOfDerivedToSpawn;
    
    public MapSpawning mapSpawning;
    public TraderSpawning traderSpawning;
    
    [NonSerialized] public EyeUpgrade augmentedEyeUpgrade;
    
    public void CreateAugmentItemFromDerived() {
        augmentedEyeUpgrade = Instantiate(derivedFrom);
        augmentedEyeUpgrade.uuid = uuid;
        augmentedEyeUpgrade.mapSpawning = mapSpawning;
        augmentedEyeUpgrade.traderSpawning = traderSpawning;
        
        float priceMultiplier = 1f - percentChanceOfDerivedToSpawn;
        augmentedEyeUpgrade.buyPrice += Mathf.RoundToInt(augmentedEyeUpgrade.buyPrice * priceMultiplier);
        
        foreach (DropOrigin origin in augmentedEyeUpgrade.dropOrigins) {
            origin.chanceToSpawn *= percentChanceOfDerivedToSpawn;
        }
    }
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }
    
    public virtual string GetDescription(int stackCount = 1) {
        return string.Empty;
    }
    
    public bool IsAugment => true;
    public MapSpawning MapSpawning => mapSpawning;
    public TraderSpawning TraderSpawning => traderSpawning; 
    public UuidScriptableObject UuidObject => this;
    public Sprite InventorySprite => derivedFrom.inventorySprite;
    public string DisplayName => derivedFrom.displayName;
    
#if UNITY_EDITOR
    
    [Button]
    private void AddToStash() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to stash inventory when game is playing");
            return;
        }
        gameInstance.TryAddItemToInventory(gameInstance.inventories.stash, new(this));
    }
    
#endif
}
