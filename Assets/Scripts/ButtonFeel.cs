using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {

    public Styles styles;
    public Image image;
    public TextMeshProUGUI text;
    public Sprite pressedSprite;
    public Sprite unpressedSprite;
    public Sprite highlightedSprite;

    public void OnPointerDown(PointerEventData eventData) {
        image.sprite = pressedSprite;
        text.margin = styles.pressedButtonTextMargin;
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        image.sprite = highlightedSprite != null && eventData.hovered.Contains(gameObject) ? highlightedSprite : unpressedSprite;
        text.margin = styles.normalButtonTextMargin;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (highlightedSprite == null) return;
        image.sprite = highlightedSprite;
    }
    
    public void OnPointerExit(PointerEventData eventData) {
        if (highlightedSprite == null) return;
        image.sprite = unpressedSprite;
        text.margin = styles.normalButtonTextMargin;
    }
    
}
