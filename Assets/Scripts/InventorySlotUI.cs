using UnityEngine;

public class InventorySlotUI : MonoBehaviour {

    public bool disallowItemStacking;
    public bool acceptsAllTypes = true;
    [VInspector.ShowIf("acceptsAllTypes", false)]
    public ItemData.ItemType onlyAcceptedItemType;
    
}
