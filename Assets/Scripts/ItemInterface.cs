using UnityEngine;
using static ItemComponents;

public interface ItemInterface {
    
    public bool IsAugment { get; }
    public MapSpawning MapSpawning { get; }
    public TraderSpawning TraderSpawning { get; }
    public UuidScriptableObject UuidObject { get; }
    public Sprite InventorySprite { get; }
    public string DisplayName { get; }
    
}