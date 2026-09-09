using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour {
    
    public Sprite placeholderSprite;
    public Styles styles;
    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI countText;
    public ForgeEffect forgeEffect; // Only pentagram inventory slots will have this
    
    private void Awake() {
        ClearItem();
    }
    
    public void SetItem(Item data, int count) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.gameObject.SetActive(true); 
        countText.text = count.ToString();
        image.color = Color.white;
        forgeEffect?.SetActive(true);
    }
    
    public void SetPlaceholderItem(Item data) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.gameObject.SetActive(false);
        image.color = Color.gray2;
        forgeEffect?.SetActive(true);
    }

    public void UpdateCount(int count) {
        countText.text = count.ToString();
    }
    
    public void ClearItem() {
        bool usePlaceHolder = placeholderSprite != null;
        image.sprite = usePlaceHolder ? placeholderSprite : null;
        image.color = styles.itemPlaceholderColor;
        image.enabled = usePlaceHolder;
        countText.text = "";
        countText.color = styles.itemCountColor; 
        forgeEffect?.SetActive(false);
    }

    public void ToggleGray() {
        image.color = styles.grayedOutItemTint;
    }
    
    public void ToggleOutOfStock() {
        ToggleGray();
        countText.color = styles.outOfStockCountColor;
    }
    
}
