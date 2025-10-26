using System;
using UnityEngine;

public class ItemDrop : MonoBehaviour {

    public CircleCollider2D circleCollider;
    public SpriteRenderer spriteRenderer;
    
    [NonSerialized] public Item item;
    [NonSerialized] private int _dropCount;
    
    public int dropCount {
        get => _dropCount <= 0 ? 1 : _dropCount;
        set => _dropCount = value;
    }

    public void Init(Item applyItem, int count) {
        _dropCount = count;
        item = applyItem;
        spriteRenderer.sprite = applyItem.dropSprite == null ? applyItem.inventorySprite : applyItem.dropSprite;
        circleCollider.radius = applyItem.pickupRadius;
    }
    
}
