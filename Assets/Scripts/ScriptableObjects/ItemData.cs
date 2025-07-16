using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Objects/ItemData")]
public class ItemData : UuidScriptableObject {

    public enum ItemType { Standard, Eye, Core, Vein, DemonEye }
    
    public ItemType itemType;
    public Sprite inventorySprite;
    public int maxStackCount;

    [ShowIf("itemType", ItemType.Core)]
    public CoreAttack coreAttack;
    
    [ShowIf("itemType", ItemType.Vein)]
    public EyeModifier eyeModifier;

}
