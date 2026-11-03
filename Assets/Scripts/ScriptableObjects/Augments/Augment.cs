using System;
using UnityEngine;
using VInspector;
using static ItemComponents;
using static Game;

public class Augment : UuidScriptableObject, IEyeUpgrade {

    public EyeUpgrade derivedFrom;
    public Item.Rarity rarity;
    
    [Range(0f, 3f)]
    public float additionalBuyPricePercentage;

    [Header("Spawning")]
    public MapSpawning mapSpawning;
    public TraderSpawning traderSpawning;
    
    [NonSerialized] public EyeUpgrade augmentedEyeUpgrade;
    
    public MapSpawning MapSpawning => mapSpawning;
    public UuidScriptableObject UuidObject => this;
    public Sprite InventorySprite => derivedFrom.inventorySprite;
    public string DisplayName => derivedFrom.displayName;
    
    public void CreateAugmentItemFromDerived() {
        derivedFrom.augmentedVariants ??= new();
        derivedFrom.augmentedVariants.Add(this);
        
        augmentedEyeUpgrade = Instantiate(derivedFrom);
        augmentedEyeUpgrade.augmentCreatedFrom = this;
        augmentedEyeUpgrade.uuid = uuid;
        augmentedEyeUpgrade.mapSpawning = mapSpawning;
        augmentedEyeUpgrade.traderSpawning = traderSpawning;
        augmentedEyeUpgrade.rarity = rarity;

        augmentedEyeUpgrade.buyPrice += Mathf.RoundToInt(augmentedEyeUpgrade.buyPrice * additionalBuyPricePercentage);
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
        gameInstance.TryAddItemToInventory(gameInstance.inventories.stash, new(this));
    }
    
#endif
}
