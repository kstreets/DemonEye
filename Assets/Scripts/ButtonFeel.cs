using System;
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
    private Action buttonListenerCallback;

    private void OnDisable() {
        OnPointerExit(null);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (isDisabled) return;
        image.sprite = pressedSprite;
        SetMargin(styles.pressedButtonTextMargin);
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        if (isDisabled || beingKeptPressed) return;
        image.sprite = highlightedSprite && beingHovered ? GetHighlightedSprite() : GetNonHighlightedSprite();
        SetMargin(styles.normalButtonTextMargin);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        beingHovered = true;
        if (!highlightedSprite) return;
        image.sprite = GetHighlightedSprite();
        if (isDisabled || beingKeptPressed) {
            SetMargin(styles.pressedButtonTextMargin);
        }
        else {
            SetMargin(styles.normalButtonTextMargin);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData) {
        beingHovered = false;
        if (!highlightedSprite) return;
        image.sprite = GetNonHighlightedSprite();
        if (!(isDisabled || beingKeptPressed)) {
            SetMargin(styles.normalButtonTextMargin);
        }
    }
    
    public void SetClickableState(bool clickable) {
        if (clickable) Enable(); else Disable();
    }

    public void Disable() {
        if (isDisabled) return;
        isDisabled = true;
        image.sprite = disabledSprite;
        SetMargin(styles.pressedButtonTextMargin);
    }

    public void Enable() {
        if (!isDisabled) return;
        isDisabled = false;
        image.sprite = unpressedSprite;
        SetMargin(styles.normalButtonTextMargin);
    }

    public void AddListener(Action callback) {
        buttonListenerCallback -= callback;
        buttonListenerCallback += callback;
        button.onClick.AddListener(OnButtonClicked);
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

    private void SetMargin(Vector4 margin) {
        if (!text) return;
        text.margin = margin;
    }

    private void OnButtonClicked() {
        if (isDisabled) return;
        buttonListenerCallback?.Invoke();
    }

}
