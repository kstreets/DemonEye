using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class InputPrompt : MonoBehaviour {
    
    public bool isSimplePrompt;
    
    public enum ShowWithDevice { KeyboardMouse, Controller, All }
    public ShowWithDevice showWhenUsingDevice;
    [Tooltip("Keep showing while a higher input layer has taken the action, e.g. the hotbar while the inventory is open")]
    public bool ignoreLayers;

    public Image primaryGlyphImage;
    
    [HideIf("isSimplePrompt")]
    public Image secondaryGlyphImage;
    public Image compositePlusImage;
    public TextMeshProUGUI text;
    [EndIf]
    
    public readonly List<GameAction> boundActions = new();
    public RectTransform rectTransform => (RectTransform)transform;

    public GameAction GetWinningAction() {
        GameAction winner = null;
        foreach (GameAction action in boundActions) {
            if (ignoreLayers ? !action.hasBindingsForCurrentDevice : !action.isActive) continue;
            if (winner == null || action.layerIndex > winner.layerIndex) {
                winner = action;
            }
        }
        return winner;
    }
    
    public bool ShouldBeShowing(bool usingController) {
        return showWhenUsingDevice switch {
            ShowWithDevice.KeyboardMouse => !usingController,
            ShowWithDevice.Controller => usingController,
            _ => true,
        };
    }
    
    // Simple prompts are glyph only, so they have no label
    public void SetLabel(string label) {
        if (isSimplePrompt || text == null || text.text == label) return;
        text.text = label;
    }

    public void Set(Sprite[] glyphs) {
        Sprite primary = glyphs.Length > 0 ? glyphs[0] : null;
        
        if (isSimplePrompt) {
            if (primaryGlyphImage.sprite != primary) {
                primaryGlyphImage.sprite = primary;
            } 
            return; 
        }
        
        if (primaryGlyphImage.sprite == glyphs[0] && secondaryGlyphImage.sprite == glyphs[1]) return;
        
        Sprite secondary = glyphs.Length > 1 ? glyphs[1] : null;
        bool isComposite = secondary != null;

        primaryGlyphImage.sprite = primary;
        secondaryGlyphImage.sprite = secondary;
        compositePlusImage.gameObject.SetActive(isComposite);
        secondaryGlyphImage.gameObject.SetActive(isComposite);
    }
    
}
