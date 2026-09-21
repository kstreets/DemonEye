using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class ItemUI : MonoBehaviour {
    
    public Sprite placeholderSprite;
    public Styles styles;
    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI countText;
    public ForgeEffect forgeEffect; // Only pentagram inventory slots will have this
    
    private Vector2 defaultCountTextPosition;
    
    private void Awake() {
        ClearItem();
        defaultCountTextPosition = countText.rectTransform.localPosition;
    }
    
    public void SetItem(Item data, int count) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        UpdateCount(count);
        image.color = Color.white;
        forgeEffect?.SetActive(true);
    }
    
    public void SetPlaceholderItem(Item data) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.gameObject.SetActive(false);
        image.color = Color.white;
        forgeEffect?.SetActive(true);
    }

    public void UpdateCount(int count) {
        countText.gameObject.SetActive(true); 
        countText.text = count.ToString();
        countText.rectTransform.localPosition = defaultCountTextPosition;
    }
    
    public void UpdateOwnedVsRequiredCount(int owned, int required) {
        countText.gameObject.SetActive(true);
        Color textColor = owned >= required ? styles.increaseDescColor : styles.decreaseDescColor;
        countText.text = $"{ColorText(owned.ToString(), textColor)}/{required}";
        countText.rectTransform.localPosition = defaultCountTextPosition.Offset(x: 15);
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
