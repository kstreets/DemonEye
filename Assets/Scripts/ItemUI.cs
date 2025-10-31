using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour {
    
    public Styles styles;
    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI countText;
    public GameObject grayOverlay;
    
    public bool IsGrayedOut => grayOverlay.activeInHierarchy;

    private void Awake() {
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
        grayOverlay.SetActive(false);
        image.color = Color.white;
    }

    public void ToggleGray() {
        grayOverlay.SetActive(true);
        image.color = styles.grayedOutItemTint;
    }
    
}
