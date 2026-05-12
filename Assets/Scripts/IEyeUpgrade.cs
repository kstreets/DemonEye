using System.Collections.Generic;
using UnityEngine;

public interface IEyeUpgrade {
    
    public bool IsAugment { get; }
    public bool SpawnsOnAllMaps { get; }
    public MapData FirstSpawnMap { get; }
    public List<MapData> SpawnsOnMaps { get; }
    public UuidScriptableObject UuidObject { get; }
    public Sprite InventorySprite { get; }
    public string DisplayName { get; }
    
}