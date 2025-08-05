using System;
using UnityEditor;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "UuidScriptableObject", menuName = "Scriptable Objects/UuidScriptableObject")]
public class UuidScriptableObject : ScriptableObject {

    [ReadOnly] public string uuid;

#if UNITY_EDITOR
    
    private void OnEnable() {
        TryCreateUuid();
    }

    private void OnValidate() {
        TryCreateUuid();
    }
    
    private void TryCreateUuid() {
        if (string.IsNullOrEmpty(uuid)) {
            uuid = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
    }

    [Button]
    private void CreateNewUuid() {
        uuid = Guid.NewGuid().ToString();
        EditorUtility.SetDirty(this);
    }
    
#endif
    
}
