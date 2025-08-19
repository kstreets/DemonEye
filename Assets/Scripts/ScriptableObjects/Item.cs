using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public enum ItemType { Standard, Eye, Soulcard, DemonEye, Backpack, Trinket }
    
    public ItemType type;
    public Sprite inventorySprite;
    public int maxStackCount;
    public int buyPrice;
    public int sellPrice;
    public int traderXp;
    [Range(0, 10)] public int weight;
    [TextArea] public string description;

    public bool modifiesStats;

    [ShowIf(nameof(modifiesStats))]
    public int agilityStatAdjustment;
    public int strengthStatAdjustment;

    public virtual string GetDescription() {
        return !string.IsNullOrEmpty(description) ? description : "Item is missing description";
    }

}
