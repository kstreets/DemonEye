using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public Sprite inventorySprite;
    public ItemType type;
    
    public int buyPrice;
    public int sellPrice;
    public int traderXp;
    [Range(0f, 1f)] public float chanceToSpawn;

    public bool modifiesStats;
    [ShowIf(nameof(modifiesStats))]
    public int agilityStatAdjustment;
    public int strengthStatAdjustment;
    [EndIf]
    
    public ItemGroupProperties itemGroupProps;
    [ShowIf(nameof(itemGroupProps), null)]
    [SerializeField] private int maxStackCount;
    [SerializeField] [Range(0, 10)] private int weight;
    [SerializeField] [TextArea] private string description;
    [EndIf]
    
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
