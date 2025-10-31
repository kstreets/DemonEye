using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour {

    public bool disallowItemStacking;
    public ItemType onlyAcceptedItemType;
    public Image slotImage;
    public Sprite activeSlotSprite;
    public Sprite inactiveSlotSprite;
    public ItemUI itemUI;
    
    public bool SlotIsInactive => slotImage.sprite == inactiveSlotSprite;
    public bool AcceptsAllTypes => onlyAcceptedItemType == null;

    private RectTransform _rectTransform;
    
    public RectTransform rectTransform {
        get {
            if (!_rectTransform) {
                _rectTransform = GetComponent<RectTransform>();
            }
            return _rectTransform;
        }
    }

    public bool AcceptsItem(Item item) {
        if (AcceptsAllTypes) {
            return true;
        }
        return item.type == onlyAcceptedItemType;
    }

    public bool OnlyAcceptsType(ItemType itemType) {
        if (AcceptsAllTypes) {
            return false;
        }
        return onlyAcceptedItemType == itemType;
    }

    public void MakeSlotActive() {
        slotImage.sprite = activeSlotSprite;     
    }

    public void MakeSlotInactive() {
        slotImage.sprite = inactiveSlotSprite;
    }
    
    public void SetItem(Item data, int count) {
        itemUI.SetItem(data, count);
    }
    
    public void ClearItem() {
        itemUI.ClearItem();
    }
    
}
