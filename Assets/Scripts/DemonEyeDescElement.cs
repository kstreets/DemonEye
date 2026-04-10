using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class DemonEyeDescElement : MonoBehaviour, ILayoutElement {
    
    public Styles styles;
    public TextMeshProUGUI nameText;
    public VerticalLayoutGroup bodyVerticalLayout;
    public TextMeshProUGUI descText;
    public ImageTextGroup augmentImageTextGroup;
    
    public void UpdateDisplay(ModifierSet.Element modifierSetElm) {
        nameText.text = ColorText($"{modifierSetElm.modifierItem.displayName} <size=87%>x{modifierSetElm.modifierCount}</size>", styles.headerTextColor);
        descText.text = modifierSetElm.modifierItem.GetDescription(modifierSetElm.modifierCount);
        
        augmentImageTextGroup.gameObject.SetActive(modifierSetElm.HasUniqueAugments);
        if (modifierSetElm.HasUniqueAugments) {
            augmentImageTextGroup.textMesh.text = modifierSetElm.uniqueAugments[0].GetDescription();
        }
    }

    public void CalculateLayoutInputVertical() {
        float descHeight = descText.GetPreferredValues(400, 0f).y;
        Debug.Log($"{nameText.preferredHeight} {descText.preferredHeight} {augmentImageTextGroup.preferredHeight}");
        preferredHeight = nameText.preferredHeight + augmentImageTextGroup.preferredHeight + descHeight;
        minHeight = preferredHeight;
    }
    
    public void CalculateLayoutInputHorizontal() { }
    
    public float minWidth { get; private set; }
    public float preferredWidth { get; private set; }
    public float flexibleWidth => 1f; 
    
    public float minHeight { get; private set; }
    public float preferredHeight { get; private set; }
    public float flexibleHeight => 1f;
    
    public int layoutPriority => 0;
}
