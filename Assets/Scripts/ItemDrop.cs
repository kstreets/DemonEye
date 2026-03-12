using System;
using UnityEngine;
using static Game;

public class ItemDrop : MonoBehaviour {

    public CircleCollider2D circleCollider;
    public SpriteRenderer spriteRenderer;
    public Item itemRef;
    
    [NonSerialized] private ItemInstance itemInstance;
    
    public int dropCount {
        get => itemInstance.count <= 0 ? 1 : itemInstance.count;
        set => itemInstance.count = value;
    }

    public void Init(ItemInstance _itemInstance) {
        itemInstance = _itemInstance;
        itemRef = _itemInstance.ItemRef;
        spriteRenderer.sprite = itemRef.dropSprite == null ? itemRef.inventorySprite : itemRef.dropSprite;
        circleCollider.radius = itemRef.pickupRadius;
        circleCollider.enabled = true;
    }
    
}
