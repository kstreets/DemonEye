using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour {

    public bool disallowItemStacking;
    public ItemType onlyAcceptedItemType;
    public Image slotImage;
    public Image itemImage;
    public Sprite activeSlotSprite;
    public Sprite inactiveSlotSprite;
    public TextMeshProUGUI countText;
    
    public bool SlotIsInactive => slotImage.sprite == inactiveSlotSprite;
    public bool AcceptsAllTypes => onlyAcceptedItemType == null;

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
        itemImage.sprite = data.inventorySprite;
        itemImage.enabled = true;
        countText.text = count.ToString();
    }
    
    public void ClearItem() {
        itemImage.sprite = null;
        itemImage.enabled = false;
        countText.text = "";
    }
    
}
