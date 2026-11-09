using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class InputBindingCellUI : MonoBehaviour {

    public RectTransform rectTransform;
    public ButtonFeel button;
    public Image glyph;
    public TextMeshProUGUI keyText;
    public GameObject warningIcon;

    [NonSerialized] public BindingSlot slot;

    public void Show(Sprite glyphSprite, string text) {
        bool showGlyph = glyphSprite != null;
        if (glyph.gameObject.activeSelf != showGlyph) {
            glyph.gameObject.SetActive(showGlyph);
        }
        if (showGlyph && glyph.sprite != glyphSprite) {
            glyph.sprite = glyphSprite;
        }

        bool showText = !string.IsNullOrEmpty(text);
        if (keyText.gameObject.activeSelf != showText) {
            keyText.gameObject.SetActive(showText);
        }
        if (showText && keyText.text != text) {
            keyText.text = text;
        }
    }

}
