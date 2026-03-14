using System;
using UnityEngine;
using static Game;

public class ItemDrop : MonoBehaviour {

    public CircleCollider2D circleCollider;
    public SpriteRenderer spriteRenderer;
    [SerializeField] private Item item;
    
    [NonSerialized] private ItemInstance itemInstance;
    
    public ItemInstance ItemInstance {
        get {
            // If we instantiate a prefab with this component it would not have an item instance.
            // Example, spawning a mushroom on a map
            itemInstance ??= new(item);
            return itemInstance;
        }
    }
    
    public void Init(ItemInstance _itemInstance) {
        itemInstance = _itemInstance;
        Item itemRef = _itemInstance.ItemRef;
        spriteRenderer.sprite = itemRef.dropSprite == null ? itemRef.inventorySprite : itemRef.dropSprite;
        circleCollider.radius = itemRef.pickupRadius;
        circleCollider.enabled = true;
    }
    
}
