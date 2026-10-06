using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler  {

    public Styles styles;
    public bool disallowItemStacking;
    public ItemType onlyAcceptedItemType;
    public bool acceptDerivativeTypes = true;
    public Image slotImage;
    public Image rarityFrameImage;
    public Image overlayImage;
    public Image underlayImage;
    public Image outlineImage;
    public Sprite activeSlotSprite;
    public Sprite inactiveSlotSprite;
    public Sprite highlightedSlotSprite;
    public ItemUI itemUI;
    public GameObject searchingCircle;
    
    public bool SlotIsInactive => isInactive;
    public bool AcceptsAllTypes => onlyAcceptedItemType == null;
    public bool IsGrayedOut => overlayImage.gameObject.activeInHierarchy;

    private RectTransform _rectTransform;
    private bool isHovered;
    private bool isInactive;

    public RectTransform rectTransform {
        get {
            if (!_rectTransform) {
                _rectTransform = GetComponent<RectTransform>();
            }
            return _rectTransform;
        }
    }

    private void Awake() {
        ClearItem(); // Prevent single frame flickering
    }

    public void OnPointerEnter(PointerEventData eventData) {
        SetHovered(eventData, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        SetHovered(eventData, false);
    }

    private void OnDisable() {
        SetHovered(null, false);
    }

    private void SetHovered(PointerEventData eventData, bool hovered) {
        if (eventData != null && !eventData.FromDominantInputDevice()) return;
        isHovered = hovered;
        RefreshSlotSprite();
    }

    private void RefreshSlotSprite() {
        if (isHovered) {
            slotImage.sprite = highlightedSlotSprite;
        }
        else {
            slotImage.sprite = isInactive ? inactiveSlotSprite : activeSlotSprite;
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
        isInactive = false;
        RefreshSlotSprite();
    }

    public void MakeSlotInactive() {
        isInactive = true;
        RefreshSlotSprite();
    }

    public void MakeSlotSearching() {
        isInactive = true;
        RefreshSlotSprite();
        searchingCircle.SetActive(true);
    }

    public void StopSlotSearching() {
        isInactive = false;
        RefreshSlotSprite();
        searchingCircle.SetActive(false);
    }

    public void SetItem(Item item, int count) {
        itemUI.SetItem(item, count);
        rarityFrameImage.color = styles.GetSlotColorForRarity(item.GetRarity());
    }

    public void SetItem(Game.ItemInstance itemInstance) {
        itemUI.SetItem(itemInstance.ItemRef, itemInstance.count);
        rarityFrameImage.color = styles.GetSlotColorForRarity(itemInstance.GetRarity());
    }

    public void SetPlaceHolderItemImage(Item item) {
        itemUI.SetPlaceholderItem(item);
        rarityFrameImage.color = styles.GetSlotColorForRarity(item.GetRarity());
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

    // Draws an outline around the slot, e.g. to point the player at it during the tutorial
    public void SetOutlined(bool outlined) {
        outlineImage.gameObject.SetActive(outlined);
    }
    
    public void ClearItem() {
        overlayImage.gameObject.SetActive(false);
        rarityFrameImage.color = Color.clear;
        itemUI.ClearItem();
    }
    
}
