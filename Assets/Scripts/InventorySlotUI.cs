using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour {

    public bool disallowItemStacking;
    public bool acceptsAllTypes = true;
    public Image slotImage;
    public Image itemImage;
    public Sprite activeSlotSprite;
    public Sprite inactiveSlotSprite;
    public TextMeshProUGUI countText;
    
    [VInspector.ShowIf("acceptsAllTypes", false)]
    public Item.ItemType onlyAcceptedItemType;

    public bool SlotIsInactive => slotImage.sprite == inactiveSlotSprite;

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
