using System;
using UnityEngine;
using static Game;

public class ItemDrop : MonoBehaviour {

    public CircleCollider2D circleCollider;
    public SpriteRenderer spriteRenderer;
    [SerializeField] private Item item;
    
    [NonSerialized] private ItemInstance itemInstance;
    // Set when summoned from an altar, so the altar can be told when the item is taken
    [NonSerialized] public Altar summoningAltar;
    
    // If we instantiate a prefab with this component it would not have an item instance.
    // Example, spawning a mushroom on a map
    public ItemInstance ItemInstance {
        get {
            itemInstance ??= new(item);
            return itemInstance;
        }
    }
    
    public void Init(ItemInstance passedItemInstance) {
        itemInstance = passedItemInstance;
        Item itemRef = passedItemInstance.ItemRef;
        spriteRenderer.sprite = itemRef.dropSprite == null ? itemRef.inventorySprite : itemRef.dropSprite;
        spriteRenderer.sortingOrder = 0;
        circleCollider.radius = itemRef.pickupRadius;
        circleCollider.enabled = true;
        summoningAltar = null;
    }
    
}
