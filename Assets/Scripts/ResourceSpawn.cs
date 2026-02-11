using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

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
    
    [Button]
    private void NormalizeSpawnChances() {
        float total = 0f;
        foreach (Element element in elements) total += element.spawnChance;
        foreach (Element element in elements) element.spawnChance /= total;
    }

}