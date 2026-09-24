using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static Game;

public class DemonEyeDescElement : MonoBehaviour  {
    
    public Styles styles;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI descText;
    public RectTransform bodyLayout;
    
    public void UpdateDisplay(EyeUpgradeSet.Element modifierSetElm, List<AugmentDescription> augmentDescriptions, bool showCountsAsIncrease) {
        Assert.IsTrue(!modifierSetElm.HasAugments || modifierSetElm.augmentsAndCount.Count == augmentDescriptions.Count,
            "Parent should be supplying the correct number of augment descriptions");
        
        Color countTextColor = showCountsAsIncrease ? styles.increaseDescColor : styles.headerTextColor; 
        nameText.text = ColorText($"{modifierSetElm.EyeUpgrade.displayName} ", styles.headerTextColor);
        countText.text = ColorText($"x{modifierSetElm.upgradeCount}", countTextColor);
        descText.text = modifierSetElm.EyeUpgrade.GetDescription(modifierSetElm.upgradeCount);
        
        for (int i = 0; i < augmentDescriptions.Count; i++) {
            AugmentDescription augmentDesc = augmentDescriptions[i];
            (Augment augment, int augmentStackCount) = modifierSetElm.augmentsAndCount[i];
            augmentDesc.descTextMesh.text = augment.GetDescription(augmentStackCount);
            // BUG: There is a purely visual bug with Unity's new hierarchy where it might appear that this set parent call isn't working
            augmentDesc.transform.SetParent(bodyLayout);
            // Augments show at the top of the body content and we make sure to preserve the sorted order
            augmentDesc.transform.SetSiblingIndex(i);
        }
    }
    
}
