using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class ResourceSpawn : MonoBehaviour {

    [Serializable]
    public class Element {
        public GameObject prefab;
        public float spawnChance;
    }
    
    public List<Element> elements;
    
    public GameObject GetPrefabToSpawn() {
        float total = 0f;
        foreach (Element element in elements) {
            total += element.spawnChance;
        }
        
        total = total < 1f ? 1f : total;
        
        float prefixedSum = 0f;
        float random = Random.Range(0f, total);
        foreach (Element element in elements) {
            prefixedSum += element.spawnChance;
            if (random < prefixedSum) {
                return element.prefab;
            }
        }
        
        return null;
    }
    
#if UNITY_EDITOR
    
    private static Material outlineMaterial;
    
    private void Update() {
        if (!TryGetComponent(out SpriteRenderer spriteRenderer)) {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        var prefabSpriteRenderer = elements[0]?.prefab.GetComponent<SpriteRenderer>();
        if (prefabSpriteRenderer == null) return;
        
        if (outlineMaterial == null) {
            outlineMaterial = new(Shader.Find("Shader Graphs/OutlineShader"));
            outlineMaterial.SetColor("_Color", Color.coral);
        }
        
        spriteRenderer.color = new(1f, 1f, 1f, 0.4f);
        spriteRenderer.material = outlineMaterial;
        spriteRenderer.sprite = prefabSpriteRenderer.sprite;
        spriteRenderer.sortingLayerID = prefabSpriteRenderer.sortingLayerID;
        spriteRenderer.sortingOrder = prefabSpriteRenderer.sortingOrder;
    }
    
#endif

}