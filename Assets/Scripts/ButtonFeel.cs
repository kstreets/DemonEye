using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {

    public Styles styles;
    public Button button;
    public Image image;
    public TextMeshProUGUI text;
    public Sprite pressedSprite;
    public Sprite unpressedSprite;
    public Sprite highlightedSprite;
    public Sprite disabledSprite;
    public Sprite highlightedDisabledSprite;
    public bool isDisabled;

    private void OnDisable() {
        OnPointerExit(null);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (isDisabled) return;
        image.sprite = pressedSprite;
        text.margin = styles.pressedButtonTextMargin;
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        if (isDisabled) return;
        image.sprite = highlightedSprite != null && eventData.hovered.Contains(gameObject) ? highlightedSprite : unpressedSprite;
        text.margin = styles.normalButtonTextMargin;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (highlightedSprite == null) return;
        image.sprite = isDisabled ? highlightedDisabledSprite : highlightedSprite;
        text.margin = isDisabled ? styles.pressedButtonTextMargin : styles.normalButtonTextMargin;
    }
    
    public void OnPointerExit(PointerEventData eventData) {
        if (highlightedSprite == null) return;
        image.sprite = isDisabled ? disabledSprite : unpressedSprite;
        text.margin = isDisabled ? text.margin : styles.normalButtonTextMargin;
    }

    public void Disable() {
        isDisabled = true;
        image.sprite = disabledSprite;
        text.margin = styles.pressedButtonTextMargin;
    }

    public void Enable() {
        isDisabled = false;
        image.sprite = unpressedSprite;
        text.margin = styles.normalButtonTextMargin;
    }
    
}
