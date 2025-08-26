using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public enum ItemType { Standard, Eye, Soulcard, DemonEye, Backpack, Trinket }
    
    public ItemType type;
    public Sprite inventorySprite;
    public ItemGroupProperties itemGroupProps;
    
    [ShowIf(nameof(itemGroupProps), null)]
    [SerializeField] private int maxStackCount;
    [SerializeField] [Range(0, 10)] private int weight;
    [SerializeField] [TextArea] private string description;
    [EndIf]
    
    public int buyPrice;
    public int sellPrice;
    public int traderXp;

    public bool modifiesStats;

    [ShowIf(nameof(modifiesStats))]
    public int agilityStatAdjustment;
    public int strengthStatAdjustment;
    
    public int Weight => itemGroupProps ? itemGroupProps.weight : weight;
    public int MaxStackCount => itemGroupProps ? itemGroupProps.maxStackCount : maxStackCount;

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
