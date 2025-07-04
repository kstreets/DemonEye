using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject {

    [SerializeField] public string uuid;
    [SerializeField] public Sprite inventorySprite;

    private void OnValidate() {
        if (string.IsNullOrEmpty(uuid)) {
            uuid = Guid.NewGuid().ToString();
        }
    }

}
