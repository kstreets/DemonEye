using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "UuidScriptableObject", menuName = "Scriptable Objects/UuidScriptableObject")]
public class UuidScriptableObject : ScriptableObject {

    [VInspector.ReadOnly] public string uuid;

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
    
#endif
    
}
