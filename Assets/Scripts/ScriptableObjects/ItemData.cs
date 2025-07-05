using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject {

    [SerializeField] public string uuid;
    [SerializeField] public Sprite inventorySprite;

#if UNITY_EDITOR
    
    private void OnValidate() {
        if (string.IsNullOrEmpty(uuid)) {
            uuid = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
    
#endif 

}
