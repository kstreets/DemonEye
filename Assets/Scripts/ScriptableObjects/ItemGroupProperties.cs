using UnityEngine;

[CreateAssetMenu(fileName = "ItemGroupProperties", menuName = "Scriptable Objects/ItemGroupProperties")]
public class ItemGroupProperties : ScriptableObject {
    
    public int maxStackCount;
    [Range(0, 10)] public int weight;
    [TextArea] public string description;
    
}
