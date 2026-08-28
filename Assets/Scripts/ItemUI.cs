using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour {
    
    public Styles styles;
    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI countText;
    public PixelFillManager pixelFillManager; // Only pentagram inventory slots will have this
    
    private void Awake() {
        ClearItem();
    }
    
    public void SetItem(Item data, int count) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.gameObject.SetActive(true);
        countText.text = count.ToString();
    }
    
    public void SetPlaceholderItem(Item data) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.gameObject.SetActive(false);
        image.color = Color.gray2;
    }

    public void UpdateCount(int count) {
        countText.text = count.ToString();
    }
    
    public void ClearItem() {
        image.sprite = null;
        image.enabled = false;
        countText.text = "";
        countText.color = styles.itemCountColor; 
        image.color = Color.white;
    }

    public void ToggleGray() {
        image.color = styles.grayedOutItemTint;
    }
    
    public void ToggleOutOfStock() {
        ToggleGray();
        countText.color = styles.outOfStockCountColor;
    }
    
}
