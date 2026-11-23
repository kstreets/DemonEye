using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class InputPrompt : MonoBehaviour {
    
    public bool isSimplePrompt;
    
    public enum ShowWithDevice { KeyboardMouse, Controller, All }
    public ShowWithDevice showWhenUsingDevice;
    public bool ignoreLayers;

    public Image primaryGlyphImage;
    
    [HideIf("isSimplePrompt")]
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
        if (primaryGlyphImage.sprite != primary) {
            primaryGlyphImage.sprite = primary;
        } 
    }
    
}
