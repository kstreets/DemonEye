using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static Game;

public class DemonEyeDescElement : MonoBehaviour  {
    
    public Styles styles;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public RectTransform bodyLayout;

    public void UpdateDisplay(EyeUpgradeSet.Element modifierSetElm, List<AugmentDescription> augmentDescriptions) {
        Assert.IsTrue(!modifierSetElm.HasAugments || modifierSetElm.augmentsAndCount.Count == augmentDescriptions.Count,
            "Parent should be supplying the correct number of augment descriptions");
        
        nameText.text = ColorText($"{modifierSetElm.EyeUpgrade.displayName} <size=87%>x{modifierSetElm.upgradeCount}</size>", styles.headerTextColor);
        descText.text = modifierSetElm.EyeUpgrade.GetDescription(modifierSetElm.upgradeCount);
        
        for (int i = 0; i < augmentDescriptions.Count; i++) {
            AugmentDescription augmentDesc = augmentDescriptions[i];
            (Augment augment, int augmentStackCount) = modifierSetElm.augmentsAndCount[i];
            
            augmentDesc.descTextMesh.text = augment.GetDescription(augmentStackCount);
            bool showAugmentStackCount = augmentStackCount > 1;
            augmentDesc.stackCountTextMesh.gameObject.SetActive(showAugmentStackCount);
            if (showAugmentStackCount) {
                augmentDesc.stackCountTextMesh.text = $"x{augmentStackCount}";
            }
            
            // BUG: There is a purely visual bug with Unity's new hierarchy where it might appear that this set parent call isn't working
            augmentDesc.transform.SetParent(bodyLayout);
            
            // Augments show at the top of the body content and we make sure to preserve the sorted order
            augmentDesc.transform.SetSiblingIndex(i);
        }
    }
    
}
