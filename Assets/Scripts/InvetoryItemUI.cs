using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InvetoryItemUI : MonoBehaviour {

    public Image image;
    public TextMeshProUGUI countText;

    public void Set(Sprite sprite, int count) {
        image.sprite = sprite;
        countText.text = count.ToString();
    }
    
    public void Clear() {
        image.sprite = null;
        countText.text = "";
    }
    
}
