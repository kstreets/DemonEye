using System;
using UnityEngine;

public class ItemDrop : MonoBehaviour {

    public CircleCollider2D circleCollider;
    public SpriteRenderer spriteRenderer;
    [SerializeField] private Item item;
    
    [NonSerialized] private Item _item;
    [NonSerialized] private int _dropCount;
    
    public Item Item => item != null ? item : _item;

    public int dropCount {
        get => _dropCount <= 0 ? 1 : _dropCount;
        set => _dropCount = value;
    }

    public void Init(Item applyItem, int count) {
        _dropCount = count;
        _item = applyItem;
        spriteRenderer.sprite = applyItem.dropSprite == null ? applyItem.inventorySprite : applyItem.dropSprite;
        circleCollider.radius = applyItem.pickupRadius;
        circleCollider.enabled = true;
    }
    
}
