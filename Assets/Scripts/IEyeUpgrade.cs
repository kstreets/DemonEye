using UnityEngine;
using static ItemComponents;

public interface IEyeUpgrade {
    
    public MapSpawning MapSpawning { get; }
    public UuidScriptableObject UuidObject { get; }
    public Sprite InventorySprite { get; }
    public string DisplayName { get; }
    
    public Augment Augment => UuidObject as Augment;
    public bool IsAugment => Augment != null;
    
}