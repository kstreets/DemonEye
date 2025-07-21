using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public enum ItemType { Standard, Eye, Core, Vein, DemonEye }
    
    public ItemType type;
    public Sprite inventorySprite;
    public int maxStackCount;
    [TextArea] public string description;

    public virtual string GetDescription() {
        return !string.IsNullOrEmpty(description) ? description : "Item is missing description";
    }

}
