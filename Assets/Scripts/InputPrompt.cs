using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class InputPrompt : MonoBehaviour {
    
    public bool isSimplePrompt;
    
    public enum ShowWithDevice { KeyboardMouse, Controller, All }
    public ShowWithDevice showWhenUsingDevice;
    
    public Image primaryGlyphImage;
    
    [HideIf("isSimplePrompt")]
    public Image secondaryGlyphImage;
    public Image compositePlusImage;
    public TextMeshProUGUI text;
    [EndIf]
    
    public GameAction boundAction;
    
    public bool ShouldBeShowing(bool usingController) {
        return showWhenUsingDevice switch {
            ShowWithDevice.KeyboardMouse => !usingController,
            ShowWithDevice.Controller => usingController,
            _ => true,
        };
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
