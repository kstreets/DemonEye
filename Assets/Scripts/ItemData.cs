using UnityEngine;

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject {
    
    [SerializeField] public Sprite inventorySprite;
    
}
