using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour {

    public Image image;
    public TextMeshProUGUI countText;

    public void Set(Item data, int count) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.text = count.ToString();
    }
    
    public void Clear() {
        image.sprite = null;
        image.enabled = false;
        countText.text = "";
    }
    
}
