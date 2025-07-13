using UnityEngine;

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Objects/ItemData")]
public class ItemData : UuidScriptableObject {

    public enum ItemType { Standard, Eye, BaseAttack, Rune, DemonEye }
    
    public ItemType itemType;
    public Sprite inventorySprite;
    public int maxStackCount;

}
