using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour {
    
    public Styles styles;
    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI countText;
    public PixelFillManager pixelFillManager; // Only pentagram inventory slots will have this
    
    private Color originalTextColor;
    
    private void Awake() {
        originalTextColor = countText.color;
        ClearItem();
    }
    
    public void SetItem(Item data, int count) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.text = count.ToString();
    }

    public void UpdateCount(int count) {
        countText.text = count.ToString();
    }
    
    public void ClearItem() {
        image.sprite = null;
        image.enabled = false;
        countText.text = "";
        countText.color = originalTextColor; 
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
