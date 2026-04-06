using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageTextGroup : MonoBehaviour, ILayoutSelfController, ILayoutElement {
    
    public enum FollowMode { TextRenderBounds, TextRectTransform }
    
    public float horizontalMargin;
    public float verticalMargin;
    public FollowMode imageWidthFollowMode;
    public FollowMode imageHeightFollowMode;
    
    [Header("Refs")]
    public Image image;
    public TextMeshProUGUI textMesh;
    
    private void Awake() {
        image = GetComponentInChildren<Image>();
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetLayoutHorizontal() {
        image.rectTransform.ResizeWidth(preferredWidth);
    }
    
    public void SetLayoutVertical() {
        image.rectTransform.ResizeHeight(preferredHeight);
    }

    public void CalculateLayoutInputHorizontal() {
        float width = imageWidthFollowMode switch {
            FollowMode.TextRenderBounds  => textMesh.preferredWidth,
            FollowMode.TextRectTransform => textMesh.rectTransform.rect.width,
            _                            => 0f,
        };
        preferredWidth = width + horizontalMargin;
        minWidth = preferredWidth;
    }
    
    public void CalculateLayoutInputVertical() {
        float height = imageHeightFollowMode switch {
            FollowMode.TextRenderBounds  => textMesh.preferredHeight,
            FollowMode.TextRectTransform => textMesh.rectTransform.rect.height,
            _                            => 0f,
        };
        preferredHeight = height + verticalMargin;
        minHeight = preferredHeight;
    }
    
    public float minWidth { get; private set; }
    public float preferredWidth { get; private set; }
    public float flexibleWidth => 0f;
    
    public float minHeight { get; private set; }
    public float preferredHeight { get; private set; }
    public float flexibleHeight => 0f;
    
    public int layoutPriority => 0;
    
}
