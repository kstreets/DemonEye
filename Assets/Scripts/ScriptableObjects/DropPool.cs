using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "DropPool", menuName = "Scriptable Objects/DropPool")]
public class DropPool : ScriptableObject {
    
    [Serializable]
    public class ItemDrop {
        public GameObject itemPrefab;
        public float dropChance;
    }

    public List<ItemDrop> itemDrops;

    public GameObject GetDropFromPool() {
        float dropTotal = 0f;
        foreach (ItemDrop drop in itemDrops) {
            dropTotal += drop.dropChance;
        }

        float randomChance = Random.Range(0f, dropTotal);
        float prefixSum = 0f;
        
        foreach (ItemDrop itemSpawn in itemDrops) {
            prefixSum += itemSpawn.dropChance;
            if (randomChance < prefixSum) {
                return itemSpawn.itemPrefab;
            }
        }
        
        return itemDrops[^1].itemPrefab;
    }
}
