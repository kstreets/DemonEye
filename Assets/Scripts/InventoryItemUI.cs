using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour {

    public Image image;
    public TextMeshProUGUI countText;

    public void Set(ItemData data, int count) {
        image.sprite = data.inventorySprite;
        countText.text = count.ToString();
    }
    
    public void Clear() {
        image.sprite = null;
        countText.text = "";
    }
    
}
