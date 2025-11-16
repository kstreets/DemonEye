using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {

    public Styles styles;
    public RectTransform rectTransform;
    public Button button;
    public Image image;
    public TextMeshProUGUI text;
    public Sprite pressedSprite;
    public Sprite nonHighlightedPressedSprite;
    public Sprite unpressedSprite;
    public Sprite highlightedSprite;
    public Sprite disabledSprite;
    public Sprite highlightedDisabledSprite;
    public bool isDisabled;
    public bool beingKeptPressed;

    private bool beingHovered;

    private void OnDisable() {
        OnPointerExit(null);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (isDisabled) return;
        image.sprite = pressedSprite;
        text.margin = styles.pressedButtonTextMargin;
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        if (isDisabled || beingKeptPressed) return;
        image.sprite = highlightedSprite && beingHovered ? GetHighlightedSprite() : GetNonHighlightedSprite();
        text.margin = styles.normalButtonTextMargin;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        beingHovered = true;
        if (!highlightedSprite) return;
        image.sprite = GetHighlightedSprite();
        text.margin = isDisabled || beingKeptPressed ? styles.pressedButtonTextMargin : styles.normalButtonTextMargin;
    }
    
    public void OnPointerExit(PointerEventData eventData) {
        beingHovered = false;
        if (!highlightedSprite) return;
        image.sprite = GetNonHighlightedSprite();
        text.margin = isDisabled || beingKeptPressed ? text.margin : styles.normalButtonTextMargin;
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

    public void KeepPressed() {
        beingKeptPressed = true;
        OnPointerDown(null);
    }

    public void StopKeepPressed() {
        beingKeptPressed = false;
        OnPointerUp(null);
    }

    private Sprite GetHighlightedSprite() {
        if (isDisabled) {
            return highlightedDisabledSprite;
        }
        if (beingKeptPressed) {
            return pressedSprite;
        }
        return highlightedSprite;
    }

    private Sprite GetNonHighlightedSprite() {
        if (isDisabled) {
            return disabledSprite;
        }
        if (beingKeptPressed) {
            return nonHighlightedPressedSprite;
        }
        return unpressedSprite;
    }

}
