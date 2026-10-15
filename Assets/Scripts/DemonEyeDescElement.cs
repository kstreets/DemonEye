using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using static Game;

public class DemonEyeDescElement : MonoBehaviour  {
    
    public Styles styles;
    public LayoutGroup layoutGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI descText;
    public RectTransform bodyLayout;

    private Sequence slideSequence;

    public float CurSlideOffset => layoutGroup.padding.left;

    // Replaces whatever slide was playing, so a new slide or bump never fights an old one over the offset
    public void PlaySlide(Sequence sequence) {
        StopSlide();
        slideSequence = sequence;
    }

    public void StopSlide() {
        if (slideSequence.isAlive) slideSequence.Stop();
    }

    private Tween punchTween;

    // Quick scale pop as feedback that this element changed
    public void PunchScale() {
        StopPunch();
        punchTween = Tween.PunchScale(transform, Vector3.one * 0.02f, 0.2f);
    }

    public void StopPunch() {
        if (punchTween.isAlive) punchTween.Stop();
        // A punch that gets cut off would otherwise leave the element stuck at whatever scale it was at
        transform.localScale = Vector3.one;
    }

    // Shifts the content sideways through the root layout group's padding, so layout rebuilds apply the offset
    // instead of resetting it like they would if we moved the content's position directly
    public void SetSlideOffset(float offset) {
        int offsetPixels = Mathf.RoundToInt(offset);
        if (layoutGroup.padding.left == offsetPixels) return;
        layoutGroup.padding.left = offsetPixels;
        layoutGroup.padding.right = -offsetPixels;
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
    }

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
