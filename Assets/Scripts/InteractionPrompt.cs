using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPrompt : MonoBehaviour, ILayoutSelfController {
    
    public RectTransform rectTransform;
    public Image background;
    public TextMeshProUGUI promptText;
    
    public void Show(string prompt, string additionalInfo) {
        // Icon has built in margin but additional text does not, so we add right padding for it
        promptText.margin = string.IsNullOrEmpty(additionalInfo) ? Vector4.zero : new(0f, 0f, 4f, 0f);
        background.enabled = !string.IsNullOrEmpty(additionalInfo);
        promptText.text = prompt + additionalInfo;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
    
    public void SetLayoutHorizontal() {
        rectTransform.ResizeWidth(promptText.preferredWidth);
    }
    
    public void SetLayoutVertical() { }
    
}
