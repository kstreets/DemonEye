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
        preferredHeight = nameText.preferredHeight + bodyVerticalLayout.preferredHeight;
        minHeight = preferredHeight;
    }
    
    public void CalculateLayoutInputHorizontal() { }
    
    public float minWidth { get; private set; }
    public float preferredWidth { get; private set; }
    public float flexibleWidth => 0f; 
    
    public float minHeight { get; private set; }
    public float preferredHeight { get; private set; }
    public float flexibleHeight => 0f;
    
    public int layoutPriority => 0;
}
