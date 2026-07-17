using UnityEngine;

[CreateAssetMenu(fileName = "ItemType", menuName = "Scriptable Objects/ItemType")]
public class ItemType : ScriptableObject {
    
    public string displayName;
    public ItemType[] derivativeItemTypes;
    
}