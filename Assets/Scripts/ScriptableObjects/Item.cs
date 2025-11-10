using System.Collections.Generic;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public string displayName;
    public Sprite inventorySprite;
    public Sprite dropSprite;
    public float pickupRadius = 0.07f;
    public ItemType type;

    [Space]

    public Trader associatedTrader;
    public int buyPrice;
    public int sellPrice;
    public int traderXp;

    [Space]

    public bool spawnsOnAllMaps;
    [ShowIf(nameof(spawnsOnAllMaps), false)]
    public List<MapData> spawnsOnMaps;
    [EndIf] 
    [Range(0f, 1f)] public float chanceToSpawnOnBody;
    [Range(0f, 1f)] public float chanceToSpawnOnTrader;
    [Range(0f, 1f)] public float chanceToSpawnFromRock;
    [Range(0f, 1f)] public float chanceToSpawnFromEnemy;

    [HideIf(nameof(chanceToSpawnFromEnemy), 0f)]
    public List<EnemyData> spawnsFromEnemies;
    [EndIf]

    [Space]
    
    public bool modifiesStats;
    [ShowIf(nameof(modifiesStats))]
    public int agilityStatAdjustment;
    public int strengthStatAdjustment;
    [EndIf]
    
    [Space]
    
    public ItemGroupProperties itemGroupProps;
    [ShowIf(nameof(itemGroupProps), null)]
    [SerializeField] private int maxStackCount;
    [SerializeField] [Range(0, 10)] private int weight;
    [SerializeField] [TextArea] private string description;
    [EndIf]
    
    public int Weight => itemGroupProps ? itemGroupProps.weight : weight;
    public int MaxStackCount => itemGroupProps ? itemGroupProps.maxStackCount : maxStackCount;
    
    public enum Rarity { Common, Uncommon, Rare, Legendary }

    public Rarity GetRarity() {
        float minRarity = Mathf.Max(
            chanceToSpawnOnBody   > 0 ? chanceToSpawnOnBody   : float.MinValue,
            chanceToSpawnOnTrader > 0 ? chanceToSpawnOnTrader : float.MinValue,
            chanceToSpawnFromRock > 0 ? chanceToSpawnFromRock : float.MinValue,
            chanceToSpawnFromEnemy > 0 ? chanceToSpawnFromEnemy : float.MinValue
        );
        if (minRarity <= 0.08f) return Rarity.Legendary;
        if (minRarity <= 0.20f) return Rarity.Rare;
        if (minRarity <= 0.50f) return Rarity.Uncommon;
        return Rarity.Common;
    }

    public virtual string GetDescription() {
        if (!string.IsNullOrEmpty(description)) {
            return description;
        }

        if (!string.IsNullOrEmpty(itemGroupProps?.description)) {
            return itemGroupProps.description;
        }
        
        return "Item is missing description";
    }
    
}
