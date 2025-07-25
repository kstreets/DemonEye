using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "ItemPool", menuName = "Scriptable Objects/ItemPool")]
public class ItemPool : ScriptableObject {
    
    [Serializable]
    public class ItemSpawn {
        public Item item;
        public float dropChance;
    }

    public List<ItemSpawn> itemDrops;

    public Item GetItemFromPool() {
        float dropTotal = 0f;
        foreach (ItemSpawn itemSpawn in itemDrops) {
            dropTotal += itemSpawn.dropChance;
        }

        float randomChance = Random.Range(0f, dropTotal);
        float prefixSum = 0f;
        
        foreach (ItemSpawn itemSpawn in itemDrops) {
            prefixSum += itemSpawn.dropChance;
            if (randomChance < prefixSum) {
                return itemSpawn.item;
            }
        }
        
        return itemDrops[^1].item;
    }

}
