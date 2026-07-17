using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour {

    public Styles styles;
    public bool disallowItemStacking;
    public ItemType onlyAcceptedItemType;
    public bool acceptDerivativeTypes = true;
    public Image slotImage;
    public Image overlayImage;
    public Image underlayImage;
    public Sprite activeSlotSprite;
    public Sprite inactiveSlotSprite;
    public ItemUI itemUI;
    public GameObject searchingCircle;
    
    public bool SlotIsInactive => slotImage.sprite == inactiveSlotSprite;
    public bool AcceptsAllTypes => onlyAcceptedItemType == null;
    public bool IsGrayedOut => overlayImage.gameObject.activeInHierarchy;
    public bool IsUnderlayed => underlayImage.gameObject.activeInHierarchy;

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
        return AcceptsItemType(item.type);
    }
    
    public bool AcceptsItemType(ItemType type) {
        if (AcceptsAllTypes) {
            return true;
        }
        if (type == onlyAcceptedItemType) {
            return true;
        }
        if (acceptDerivativeTypes) {
            foreach (ItemType derivative in onlyAcceptedItemType.derivativeItemTypes) {
                if (derivative == type) {
                    return true;
                }
            }
        }
        return false;
    }

    public void MakeSlotActive() {
        slotImage.sprite = activeSlotSprite;     
    }

    public void MakeSlotInactive() {
        slotImage.sprite = inactiveSlotSprite;
    }

    public void MakeSlotSearching() {
        slotImage.sprite = inactiveSlotSprite;
        searchingCircle.SetActive(true);
    }

    public void StopSlotSearching() {
        slotImage.sprite = activeSlotSprite;     
        searchingCircle.SetActive(false);
    }

    public void SetItem(Item data, int count) {
        itemUI.SetItem(data, count);
    }
    
    public void ToggleOutOfStock() {
        itemUI.ToggleOutOfStock();
        overlayImage.gameObject.SetActive(true);
        overlayImage.color = styles.grayedOutOverlay;
    }
    
    public void ToggleGray() {
        itemUI.ToggleGray();
        overlayImage.gameObject.SetActive(true);
        overlayImage.color = styles.grayedOutOverlay;
    }
    
    public void SetSelectionUnderlay() {
        underlayImage.gameObject.SetActive(true);
        underlayImage.color = styles.selectedUnderlay;
    }
    
    public void ClearSelectionUnderlay() {
        underlayImage.gameObject.SetActive(false);
    }
    
    public void ClearItem() {
        overlayImage.gameObject.SetActive(false);
        itemUI.ClearItem();
    }
    
}
