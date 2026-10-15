using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Pool;
using static Game;

public class DemonEyeDescList : MonoBehaviour {

    public GameObject augmentDescPrefab;
    public DemonEyeDescElement[] elements;

    private List<List<AugmentDescription>> childAugmentDescTrackingList;
    private ObjectPool<AugmentDescription> augmentDescObjectPool;

    // Elements stay bound to the upgrade they show rather than to a list position, so a removed upgrade's element
    // can keep its content while it slides out, and the others don't swap content when the list reorders
    private readonly Dictionary<EyeUpgrade, int> elementIndexFromUpgrade = new();
    private EyeUpgrade[] shownUpgrades;
    private int[] shownUpgradeCounts;
    private bool[] slidingOut; // Still showing a removed upgrade while it slides away, so it can't be given a new one yet

    private const float SlideInDelayPerElement = 0.06f;

    private void Awake() {
        childAugmentDescTrackingList = new();
        for (int i = 0; i < elements.Length; i++) {
            childAugmentDescTrackingList.Add(new(capacity: 3));
        }

        shownUpgrades = new EyeUpgrade[elements.Length];
        shownUpgradeCounts = new int[elements.Length];
        slidingOut = new bool[elements.Length];

        const int preCachingSize = 10;
        augmentDescObjectPool = new(OnCreateAugmentDescription, OnGetAugmentDescription, OnReleaseAugmentDescription, defaultCapacity: preCachingSize);
        augmentDescObjectPool.CreateObjects(preCachingSize);
    }

    private bool resetPending;

    private void OnDisable() {
        // Everything counts as new when shown again, rather than comparing against whatever was showing ages ago.
        // Can't hide them here, Unity doesn't allow changing the hierarchy while it's in the middle of deactivating it.
        resetPending = true;
    }

    // animateChanges slides in new upgrades, slides out removed ones, and punches the scale of ones whose count went up.
    // Without it everything just appears and disappears.
    public void UpdateDisplay(EyeUpgradeSet eyeUpgradeSet, bool showCountsAsIncrease, bool animateChanges = false) {
        if (resetPending) {
            resetPending = false;
            HideAllElements();
        }

        using var _ = HashSetPool<EyeUpgrade>.Get(out HashSet<EyeUpgrade> upgradesInSet);
        using var __ = ListPool<int>.Get(out List<int> newElementIndices);
        using var ___ = ListPool<int>.Get(out List<int> stackCountChangedElementIndices);

        foreach (EyeUpgradeSet.Element upgradeElm in eyeUpgradeSet.elements) {
            upgradesInSet.Add(upgradeElm.EyeUpgrade);
        }

        float listWidth = animateChanges ? GetLaidOutListWidth() : 0f;

        // Upgrades that are no longer in the set
        for (int i = 0; i < elements.Length; i++) {
            if (slidingOut[i]) {
                if (!animateChanges) HideElement(i);
                continue;
            }
            EyeUpgrade shownUpgrade = shownUpgrades[i];
            if (shownUpgrade == null || upgradesInSet.Contains(shownUpgrade)) continue;

            elementIndexFromUpgrade.Remove(shownUpgrade);
            if (animateChanges) {
                SlideOutElement(i, listWidth);
            }
            else {
                HideElement(i);
            }
        }

        foreach (EyeUpgradeSet.Element upgradeElm in eyeUpgradeSet.elements) {
            if (elementIndexFromUpgrade.TryGetValue(upgradeElm.EyeUpgrade, out int i)) {
                if (upgradeElm.upgradeCount != shownUpgradeCounts[i]) {
                    stackCountChangedElementIndices.Add(i);
                }
            }
            else {
                i = ClaimElementFor(upgradeElm.EyeUpgrade);
                if (i == -1) continue; // More upgrades than elements, nothing we can show it in
                elementIndexFromUpgrade.Add(upgradeElm.EyeUpgrade, i);
                newElementIndices.Add(i);
            }

            shownUpgrades[i] = upgradeElm.EyeUpgrade;
            shownUpgradeCounts[i] = upgradeElm.upgradeCount;

            List<AugmentDescription> curAugmentDescList = childAugmentDescTrackingList[i];
            int numberOfDifferentAugments = upgradeElm.HasAugments ? upgradeElm.augmentsAndCount.Count : 0;
            bool elementAugmentDescMismatch = numberOfDifferentAugments != curAugmentDescList.Count;
            if (elementAugmentDescMismatch) {
                ReleaseElementsAugmentDescriptions(curAugmentDescList);
                FillElmentsAugmentDescriptions(curAugmentDescList, numberOfDifferentAugments);
            }

            elements[i].gameObject.SetActive(true);
            elements[i].UpdateDisplay(upgradeElm, curAugmentDescList, showCountsAsIncrease);
        }

        OrderElements(eyeUpgradeSet);

        if (!animateChanges) return;

        for (int k = 0; k < newElementIndices.Count; k++) {
            SlideInElement(elements[newElementIndices[k]], listWidth, k * SlideInDelayPerElement);
        }
        foreach (int i in stackCountChangedElementIndices) {
            elements[i].PunchScale();
        }
    }

    public void SlideInElements() {
        float listWidth = GetLaidOutListWidth();

        int shownIndex = 0;
        foreach (Transform child in transform) { // Sibling order, so they slide in top to bottom
            if (!child.gameObject.activeSelf || !child.TryGetComponent(out DemonEyeDescElement demonEyeDescElm)) continue;
            SlideInElement(demonEyeDescElm, listWidth, shownIndex * SlideInDelayPerElement);
            shownIndex++;
        }
    }

    public void HideAllElements() {
        for (int i = 0; i < elements.Length; i++) {
            HideElement(i);
        }
    }

    private static void SlideInElement(DemonEyeDescElement demonEyeDescElm, float slideDistance, float delay) {
        const float slideDuration = 0.35f;

        // An element caught partway through sliding out carries on back in from where it is, rather than jumping off screen first
        float startOffset = demonEyeDescElm.CurSlideOffset < 0f ? demonEyeDescElm.CurSlideOffset : -slideDistance;

        // Set the offset now rather than waiting on the tween, so delayed elements are already hidden off to the left on this frame
        demonEyeDescElm.SetSlideOffset(startOffset);
        demonEyeDescElm.PlaySlide(Sequence.Create()
            .Chain(Tween.Delay(delay))
            .Chain(Tween.Custom(demonEyeDescElm, startOffset, 0f, slideDuration, static (elm, offset) => elm.SetSlideOffset(offset), Ease.OutCubic)));
    }

    // The reverse of SlideInElement, then the element is hidden and freed up for another upgrade
    private void SlideOutElement(int elementIndex, float slideDistance) {
        const float slideDuration = 0.25f;

        slidingOut[elementIndex] = true;
        DemonEyeDescElement demonEyeDescElm = elements[elementIndex];
        demonEyeDescElm.StopPunch();
        demonEyeDescElm.PlaySlide(Sequence.Create()
            .Chain(Tween.Custom(demonEyeDescElm, demonEyeDescElm.CurSlideOffset, -slideDistance, slideDuration, static (elm, offset) => elm.SetSlideOffset(offset), Ease.InCubic))
            .ChainCallback(() => HideElement(elementIndex, stopSlide: false))); // Stopping the sequence from inside its own callback isn't allowed
    }

    private void HideElement(int elementIndex, bool stopSlide = true) {
        DemonEyeDescElement demonEyeDescElm = elements[elementIndex];
        if (stopSlide) demonEyeDescElm.StopSlide();
        demonEyeDescElm.StopPunch();
        demonEyeDescElm.SetSlideOffset(0f);
        demonEyeDescElm.gameObject.SetActive(false);
        ReleaseElementsAugmentDescriptions(childAugmentDescTrackingList[elementIndex]);

        EyeUpgrade shownUpgrade = shownUpgrades[elementIndex];
        if (shownUpgrade != null && !slidingOut[elementIndex]) {
            elementIndexFromUpgrade.Remove(shownUpgrade);
        }
        shownUpgrades[elementIndex] = null;
        shownUpgradeCounts[elementIndex] = 0;
        slidingOut[elementIndex] = false;
    }

    // Returns the element to show a newly added upgrade in, or -1 if every element is in use
    private int ClaimElementFor(EyeUpgrade upgrade) {
        // If it was taken out and put straight back in, take back the element that's still sliding away with it
        for (int i = 0; i < elements.Length; i++) {
            if (slidingOut[i] && shownUpgrades[i] == upgrade) {
                slidingOut[i] = false;
                return i;
            }
        }
        for (int i = 0; i < elements.Length; i++) {
            if (shownUpgrades[i] == null) return i;
        }
        // Out of free elements, so cut short one that's sliding away
        for (int i = 0; i < elements.Length; i++) {
            if (!slidingOut[i]) continue;
            HideElement(i);
            return i;
        }
        return -1;
    }

    // Puts the elements in the same order as the set. Elements that are sliding out keep their spot relative to
    // the element that was above them, so they leave from where they were instead of jumping to the end.
    private void OrderElements(EyeUpgradeSet eyeUpgradeSet) {
        using var _ = ListPool<DemonEyeDescElement>.Get(out List<DemonEyeDescElement> currentOrder);
        using var __ = ListPool<DemonEyeDescElement>.Get(out List<DemonEyeDescElement> desiredOrder);

        foreach (Transform child in transform) {
            if (child.gameObject.activeSelf && child.TryGetComponent(out DemonEyeDescElement demonEyeDescElm)) {
                currentOrder.Add(demonEyeDescElm);
            }
        }

        foreach (EyeUpgradeSet.Element upgradeElm in eyeUpgradeSet.elements) {
            if (elementIndexFromUpgrade.TryGetValue(upgradeElm.EyeUpgrade, out int i)) {
                desiredOrder.Add(elements[i]);
            }
        }

        for (int k = 0; k < currentOrder.Count; k++) {
            if (!slidingOut[Array.IndexOf(elements, currentOrder[k])]) continue;
            int insertAt = 0;
            for (int above = k - 1; above >= 0; above--) {
                int aboveIndex = desiredOrder.IndexOf(currentOrder[above]);
                if (aboveIndex == -1) continue;
                insertAt = aboveIndex + 1;
                break;
            }
            desiredOrder.Insert(insertAt, currentOrder[k]);
        }

        // Only touch the hierarchy when the order actually changes, since it dirties the layout
        bool inOrder = true;
        for (int k = 1; k < desiredOrder.Count; k++) {
            if (desiredOrder[k].transform.GetSiblingIndex() < desiredOrder[k - 1].transform.GetSiblingIndex()) {
                inOrder = false;
                break;
            }
        }
        if (inOrder) return;

        foreach (DemonEyeDescElement demonEyeDescElm in desiredOrder) {
            demonEyeDescElm.transform.SetAsLastSibling();
        }
    }

    private float GetLaidOutListWidth() {
        Canvas.ForceUpdateCanvases(); // The panel may have only just been turned on, so make sure its width is up to date
        return ((RectTransform)transform).rect.width;
    }

    private void ReleaseElementsAugmentDescriptions(List<AugmentDescription> list) {
        for (int i = list.Count - 1; i >= 0; i--) {
            augmentDescObjectPool.Release(list[i]);
            list.RemoveAt(i);
        }
    }
    
    private void FillElmentsAugmentDescriptions(List<AugmentDescription> list, int count) {
        for (int i = 0; i < count; i++) {
            list.Add(augmentDescObjectPool.Get());
        }
    }
    
    private AugmentDescription OnCreateAugmentDescription() {
        var augmentDescInstance = Instantiate(augmentDescPrefab, transform).GetComponent<AugmentDescription>();        
        augmentDescInstance.gameObject.SetActive(false);
        return augmentDescInstance;
    }
    
    private void OnReleaseAugmentDescription(AugmentDescription augmendDesc) {
        augmendDesc.transform.SetParent(transform);
        augmendDesc.gameObject.SetActive(false);
    }
    
    private void OnGetAugmentDescription(AugmentDescription augmendDesc) {
        augmendDesc.gameObject.SetActive(true);
    }

}
