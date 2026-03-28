using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ImageTextGroup : MonoBehaviour {
    
    [Serializable]
    public class Margins {
        public float left;
        public float right;
        public float top;
        public float bottom;
    }
    
    public enum FollowMode { TextRenderBounds, TextRectTransform }
    
    public Margins margins;
    public FollowMode imageWidthFollowFollowMode;
    public FollowMode imageHeightFollowFollowMode;
    
    private LayoutElement layoutElement;
    private Image image;
    private TextMeshProUGUI textMesh;
    
    private void Awake() {
        layoutElement = GetComponent<LayoutElement>();
        image = GetComponentInChildren<Image>();
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update() {
        Recalculate();
    }

    public void Recalculate() {
        float preferredHeight = textMesh.preferredHeight;
        preferredHeight += margins.top;
        preferredHeight += margins.bottom;
        layoutElement.preferredHeight = preferredHeight;
        
        float imageWidth = imageWidthFollowFollowMode switch {
            FollowMode.TextRenderBounds  => textMesh.preferredWidth,
            FollowMode.TextRectTransform => textMesh.rectTransform.rect.width,
            _ => 0f,
        };
        
        float imageHeight = imageHeightFollowFollowMode switch {
            FollowMode.TextRenderBounds  => textMesh.preferredHeight,
            FollowMode.TextRectTransform => textMesh.rectTransform.rect.height,
            _ => 0f,
        };
        
        imageWidth += margins.left + margins.right;
        imageHeight += margins.left + margins.right;
        image.rectTransform.sizeDelta = new(imageWidth, imageHeight);
    }
    
}
