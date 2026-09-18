using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class UIHintPopUp : MonoBehaviour, ILayoutSelfController {
    
    public RectTransform rectTransform;
    public TextMeshProUGUI descText;
    
    public void Show(Vector2 position, string description) {
        gameObject.SetActive(true);
        rectTransform.position = position;
        descText.text = description;
        TweenPopUp(rectTransform);
        descText.ForceMeshUpdate(); // Needed so rendered width is up to date when calculating horizontal layout
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
    
    public void Hide() {
        gameObject.SetActive(false);
    }
    
    public void SetLayoutVertical() {
        Rect newRect = rectTransform.rect;
        newRect.height = descText.preferredHeight;
        rectTransform.sizeDelta = new(newRect.width, newRect.height);
    }
    
    public void SetLayoutHorizontal() {
        Rect newRect = rectTransform.rect;
        newRect.width = descText.renderedWidth + descText.margin.x + descText.margin.z;
        rectTransform.sizeDelta = new(newRect.width, newRect.height);
    }
    
}
