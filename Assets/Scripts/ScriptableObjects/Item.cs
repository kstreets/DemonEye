using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public string displayName;
    public Sprite inventorySprite;
    public ItemType type;

    [Space]

    public Trader associatedTrader;
    public int buyPrice;
    public int sellPrice;
    public int traderXp;
    
    [Space]
    
    [Range(0f, 1f)] public float chanceToSpawnOnBody;
    [Range(0f, 1f)] public float chanceToSpawnOnTrader;
    [Range(0f, 1f)] public float chanceToSpawnFromRock;

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
            chanceToSpawnFromRock > 0 ? chanceToSpawnFromRock : float.MinValue
        );
        if (minRarity <= 0.10) return Rarity.Legendary;
        if (minRarity <= 0.25) return Rarity.Rare;
        if (minRarity <= 0.50) return Rarity.Uncommon;
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
