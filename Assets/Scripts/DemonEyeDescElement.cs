using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class DemonEyeDescElement : MonoBehaviour  {
    
    public Styles styles;
    public TextMeshProUGUI nameText;
    public VerticalLayoutGroup bodyVerticalLayout;
    public ImageTextGroup augmentImageTextGroup;
    public TextMeshProUGUI descText;
    
    public float Height => nameText.preferredHeight + bodyVerticalLayout.preferredHeight;
    
    public void UpdateDisplay(EyeUpgradeSet.Element modifierSetElm) {
        nameText.text = ColorText($"{modifierSetElm.eyeUpgradeItem.displayName} <size=87%>x{modifierSetElm.upgradeCount}</size>", styles.headerTextColor);
        descText.text = modifierSetElm.eyeUpgradeItem.GetDescription(modifierSetElm.upgradeCount);
        
        augmentImageTextGroup.gameObject.SetActive(modifierSetElm.HasUniqueAugments);
        if (modifierSetElm.HasUniqueAugments) {
            augmentImageTextGroup.textMesh.text = modifierSetElm.uniqueAugments[0].GetDescription();
        }
    }
    
}
