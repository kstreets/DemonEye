using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropPool", menuName = "Scriptable Objects/DropPool")]
public class DropPool : ScriptableObject {
    
    public bool reduceDuplicates;
    public bool isMapSpecific;
    
    [NonSerialized] public List<List<Item>> itemRarityTable;
    [NonSerialized] public Item lastDroppedItem;
    
    public bool HasItems => itemRarityTable.Count > 0;
    
    public void Init() {
        itemRarityTable = new();
        for (int i = 0; i < Item.rarityCount; i++) {
            itemRarityTable.Add(new());     
        }
    }

    public void Reset() {
        foreach (List<Item> items in itemRarityTable) {
            items.Clear();
        }
        lastDroppedItem = null;
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