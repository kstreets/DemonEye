using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public enum ItemType { Standard, Eye, Core, Vein, DemonEye }
    
    public ItemType type;
    public Sprite inventorySprite;
    public int maxStackCount;

    public virtual string GetDescription() {
        return "Item is missing description";
    }

}
