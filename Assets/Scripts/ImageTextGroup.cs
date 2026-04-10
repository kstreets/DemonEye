using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageTextGroup : UIBehaviour, ILayoutGroup, ILayoutElement {
    
    [Header("Refs")]
    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI textMesh;
    
    [Header("Settings")]
    public float horizontalMargin;
    public float verticalMargin;
    public float fixedHeight;
    
    private void Awake() {
        image = GetComponentInChildren<Image>();
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }
    
    protected override void OnEnable() {
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    public void SetLayoutHorizontal() {
        rectTransform.ForceUpdateRectTransforms();
        if (fixedHeight > Mathf.Epsilon) {
            image.rectTransform.ResizeWidth(preferredWidth);
        }
        else {
            image.rectTransform.ResizeWidth(rectTransform.rect.width + horizontalMargin);
            // textMesh.rectTransform.ResizeWidth(rectTransform.rect.width);
        }
    }
    
    public void SetLayoutVertical() {
        if (fixedHeight > Mathf.Epsilon) {
            image.rectTransform.ResizeHeight(fixedHeight + verticalMargin);
        }
        else {
            image.rectTransform.ResizeHeight(preferredHeight);
        }
    }

    public void CalculateLayoutInputHorizontal() {
        if (fixedHeight > Mathf.Epsilon) {
            preferredWidth = textMesh.GetPreferredValues(0f, fixedHeight).x + horizontalMargin;
        }
        else {
            preferredWidth = 400;
        }
        minWidth = 0f;
    }
    
    public void CalculateLayoutInputVertical() {
        preferredHeight = textMesh.preferredHeight + verticalMargin;
        minHeight = 0f;
        Debug.Break();
    }

    public float minWidth { get; private set; }
    public float preferredWidth { get; private set; }
    public float flexibleWidth => 0f;
    
    public float minHeight { get; private set; }
    public float preferredHeight { get; private set; }
    public float flexibleHeight => 0f;
    
    public int layoutPriority => 0;
    
}
