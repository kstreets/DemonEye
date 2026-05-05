using System.Collections.Generic;
using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "EyeUpgrade", menuName = "Scriptable Objects/EyeUpgrade")]
public class EyeUpgradeItem : Item, IEyeUpgrade {

    public MechanicDesc relativeMechanicDesc;
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }
    
    // IEyeUpgradeInterface
    public bool IsAugment => false;
    public bool SpawnsOnAllMaps => spawnsOnAllMaps;
    public MapData FirstSpawnMap => firstSpawnMap;
    public List<MapData> SpawnsOnMaps => spawnsOnMaps;
    public UuidScriptableObject GetUuidObject => this;
    public Sprite InventorySprite => inventorySprite;
    public string DisplayName => displayName;
}