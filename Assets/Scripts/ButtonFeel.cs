using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Game;

public class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {

    public static readonly List<ButtonFeel> activeWithControllerShortcuts = new();
    
    public Styles styles;
    public RectTransform rectTransform;
    public Button button;
    public Image image;
    public Image inputGlyph;
    public TextMeshProUGUI text;
    public Sprite pressedSprite;
    public Sprite nonHighlightedPressedSprite;
    public Sprite unpressedSprite;
    public Sprite highlightedSprite;
    public Sprite disabledSprite;
    public Sprite highlightedDisabledSprite;
    public bool isDisabled;
    public bool beingKeptPressed;
    public DynamicClip pressedClip;
    public DynamicClip hoveredClip;
    public InputActionReference controllerShortcut;

    [NonSerialized] public Action buttonListenerCallback;
    private bool beingHovered;

    private const float shortcutPressDuration = 0.1f;
    private Tween shortcutReleaseTween;

    private Vector2 inputGlyphRestPosition;

    private void Awake() {
        if (inputGlyph) {
            inputGlyphRestPosition = inputGlyph.rectTransform.anchoredPosition;
        }
    }

    private void OnEnable() {
        if (controllerShortcut == null) return;
        activeWithControllerShortcuts.Add(this);
    }

    private void OnDisable() {
        OnPointerExit(null);
        activeWithControllerShortcuts.Remove(this);

        // Don't leave the button stuck down if it's hidden mid press
        if (shortcutReleaseTween.isAlive) {
            shortcutReleaseTween.Stop();
            OnPointerUp(null);
        }
    }

    public void PlayShortcutPress() {
        if (isDisabled || beingKeptPressed) return;

        shortcutReleaseTween.Stop();
        OnPointerDown(null);
        shortcutReleaseTween = Tween.Delay(this, shortcutPressDuration, static target => target.OnPointerUp(null), useUnscaledTime: true);
    }

    public void SetInputGlyph(Sprite glyph) {
        if (!inputGlyph) return;

        bool show = glyph != null;
        if (inputGlyph.gameObject.activeSelf != show) {
            inputGlyph.gameObject.SetActive(show);
        }
        if (show && inputGlyph.sprite != glyph) {
            inputGlyph.sprite = glyph;
        }
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
        if (!EventCameFromCurrentPointerDevice(eventData)) return;
        
        beingHovered = true;
        if (!highlightedSprite) return;
        image.sprite = GetHighlightedSprite();
        if (hoveredClip != null) {
            gameInstance.PlayAudioClip(hoveredClip);
        }
        if (isDisabled || beingKeptPressed) {
            SetMargin(styles.pressedButtonTextMargin);
        }
        else {
            SetMargin(styles.normalButtonTextMargin);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData) {
        if (!EventCameFromCurrentPointerDevice(eventData)) return;
        
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
    
    public void TriggerClick() {
        if (!button.IsInteractable()) return;
        button.onClick.Invoke();
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
        if (inputGlyph) {
            // Margin is (left, top, right, bottom). Assumes left-middle aligned text.
            Vector4 delta = margin - styles.normalButtonTextMargin;
            Vector2 textOffset = new(delta.x, (delta.w - delta.y) * 0.5f);
            inputGlyph.rectTransform.anchoredPosition = inputGlyphRestPosition + textOffset;
        }
    }

    private void OnButtonClicked() {
        if (isDisabled || beingKeptPressed) return;
        gameInstance.PlayAudioClip(pressedClip);
        buttonListenerCallback?.Invoke();
    }

}
