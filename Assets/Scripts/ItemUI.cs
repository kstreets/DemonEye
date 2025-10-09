using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour {

    public Image image;
    public TextMeshProUGUI countText;

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
    }
}
