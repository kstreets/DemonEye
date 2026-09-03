using System.Collections.Generic;
using UnityEngine;
using static Game;

public class ResourceRequirementList : MonoBehaviour {
    
    public List<ResourceRequirement> resourceRequirements;
    
    public void HideAll() {
        foreach (ResourceRequirement resReq in resourceRequirements) {
            resReq.gameObject.SetActive(false);
        }
    }
    
    public void Show(List<ItemWithCount> displayList) {
        HideAll();
        for (int i = 0; i < displayList.Count; i++) {
            ItemWithCount itemWithCount = displayList[i];
            ResourceRequirement resReq = resourceRequirements[i];
            resReq.gameObject.SetActive(true);
            resReq.Set(itemWithCount.item, itemWithCount.count, gameInstance.GetOwnedCountOfItem(itemWithCount.item));
        }
    }
    
}
