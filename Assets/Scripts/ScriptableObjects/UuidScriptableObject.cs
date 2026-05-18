using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using GameResLookup = System.Collections.Generic.Dictionary<int, UuidScriptableObject>;

[CreateAssetMenu(fileName = "UuidScriptableObject", menuName = "Scriptable Objects/UuidScriptableObject")]
public class UuidScriptableObject : ScriptableObject {

    [HideInInspector] public int uuid;

    public static int GetIntUuid() {
        return Random.Range(int.MinValue, int.MaxValue);
    }
    
#if UNITY_EDITOR
    
    public void CreateUuid() {
        Item[] itemsFoundInFolder = Resources.LoadAll<Item>(string.Empty);
        HashSet<int> existingUuids = new();
        foreach (Item item in itemsFoundInFolder) {
            existingUuids.Add(item.uuid);
        }

        int newId = GetIntUuid();
        while (existingUuids.Contains(newId)) {
            newId = GetIntUuid();
        }

        uuid = newId;
        EditorUtility.SetDirty(this);
    }

#endif
    
}

#if UNITY_EDITOR

public class UuidAssetProcessor : AssetModificationProcessor {
    
    // Check to see if the asset we created is a UuidScriptableObject for uuid generation
    public static void OnWillCreateAsset(string path) {
        if (!path.EndsWith(".asset")) return;
        EditorApplication.delayCall += () => {
            UuidScriptableObject asset = AssetDatabase.LoadAssetAtPath<UuidScriptableObject>(path);
            if (!asset) return;
            asset.CreateUuid();
        };
    }
    
}

#endif