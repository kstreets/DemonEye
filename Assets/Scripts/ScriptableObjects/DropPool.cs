using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropPool", menuName = "Scriptable Objects/DropPool")]
public class DropPool : ScriptableObject {
    
    public bool isMapSpecific;
    public bool usesMapRarityIncrease;
    public float[] pityBonusPerMiss = new float[Item.rarityCount]; // Each drop in a row below a rarity multiplies that rarity's chance by this much more

    [NonSerialized] public List<List<Item>> itemRarityTable;
    [NonSerialized] public int[] pityCounts;

    public bool HasItems => itemRarityTable.Exists(items => items.Count > 0);
    public int Count => itemRarityTable.Count;
    public bool UsesPity => pityBonusPerMiss != null && Array.Exists(pityBonusPerMiss, x => x > 0f);

    public float GetPityMultiplier(Item.Rarity rarity) {
        int i = (int)rarity;
        if (pityBonusPerMiss == null || i >= pityBonusPerMiss.Length) {
            return 1f;
        }
        return 1f + pityCounts[i] * pityBonusPerMiss[i];
    }

    public void UpdatePity(Item.Rarity droppedRarity) {
        if (!UsesPity) return;
        for (int i = 0; i < pityCounts.Length; i++) {
            if (i <= (int)droppedRarity) {
                pityCounts[i] = 0;
            }
            else if (itemRarityTable[i].Count > 0) { // Rarities missing from this map's pool hold their count
                pityCounts[i]++;
            }
        }
    }

    public void Init() {
        pityCounts = new int[Item.rarityCount];
        itemRarityTable = new();
        for (int i = 0; i < Item.rarityCount; i++) {
            itemRarityTable.Add(new());     
        }
    }

    public void Reset() {
        foreach (List<Item> items in itemRarityTable) {
            items.Clear();
        }
    }

    public void AddItem(Item item) {
        itemRarityTable[(int)item.GetRarity()].Add(item);
    }
    
    public bool TryGetItemOfRarity(Item.Rarity rarity, out Item item) {
        List<Item> items = itemRarityTable[(int)rarity];
        item = items.GetRandom();
        return item != null;
    }
    
}