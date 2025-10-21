using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    public Styles styles;
    public Image image;
    public TextMeshProUGUI text;
    public Sprite pressedSprite;
    public Sprite unpressedSprite;

    public void OnPointerDown(PointerEventData eventData) {
        image.sprite = pressedSprite;
        text.margin = styles.pressedButtonTextMargin;
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        image.sprite = unpressedSprite;
        text.margin = styles.normalButtonTextMargin;
    }
    
}
