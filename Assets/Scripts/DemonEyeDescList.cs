using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static Game;

public class DemonEyeDescList : MonoBehaviour {
    
    public GameObject augmentDescPrefab;
    public DemonEyeDescElement[] elements;
    
    private List<AugmentDescription> synergyDescTrackingList = new();
    private List<List<AugmentDescription>> childAugmentDescTrackingList;
    private ObjectPool<AugmentDescription> augmentDescObjectPool;
    
    private void Awake() {
        childAugmentDescTrackingList = new();
        for (int i = 0; i < elements.Length; i++) {
            childAugmentDescTrackingList.Add(new(capacity: 3));
        }
        
        const int preCachingSize = 10;
        augmentDescObjectPool = new(OnCreateAugmentDescription, OnGetAugmentDescription, OnReleaseAugmentDescription, defaultCapacity: preCachingSize);
        augmentDescObjectPool.CreateObjects(preCachingSize);
    }
    
    public void UpdateDisplay(EyeUpgradeSet eyeUpgradeSet) {
        for (int i = 0; i < elements.Length; i++) {
            DemonEyeDescElement demonEyeDescElm = elements[i];
            List<AugmentDescription> curAugmentDescList = childAugmentDescTrackingList[i];
            
            if (!eyeUpgradeSet.elements.IndexInRange(i)) {
                demonEyeDescElm.gameObject.SetActive(false);
                ReleaseElementsAugmentDescriptions(curAugmentDescList);
                continue;
            }
            
            EyeUpgradeSet.Element upgradeElm = eyeUpgradeSet.elements[i];
            int numberOfDifferentAugments = upgradeElm.HasAugments ? upgradeElm.augmentsAndCount.Count : 0;
            
            bool elementAugmentDescMismatch = numberOfDifferentAugments != curAugmentDescList.Count;
            if (elementAugmentDescMismatch) {
                ReleaseElementsAugmentDescriptions(curAugmentDescList);
                FillElmentsAugmentDescriptions(curAugmentDescList, numberOfDifferentAugments); 
            }
            
            demonEyeDescElm.gameObject.SetActive(true);
            demonEyeDescElm.UpdateDisplay(upgradeElm, curAugmentDescList);
        }
        
        if (eyeUpgradeSet.synergies.Count != synergyDescTrackingList.Count) {
            ReleaseElementsAugmentDescriptions(synergyDescTrackingList);
            FillElmentsAugmentDescriptions(synergyDescTrackingList, eyeUpgradeSet.synergies.Count); 
        }
        for (int i = 0; i < eyeUpgradeSet.synergies.Count; i++) {
            synergyDescTrackingList[i].transform.SetSiblingIndex(i);
            synergyDescTrackingList[i].descTextMesh.text = eyeUpgradeSet.synergies[i].GetDescription();
            synergyDescTrackingList[i].stackCountTextMesh.gameObject.SetActive(false);
        }
    }
    
    public void HideAllElements() {
        foreach (DemonEyeDescElement demonEyeDescElm in elements) {
            demonEyeDescElm.gameObject.SetActive(false);
        }
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
