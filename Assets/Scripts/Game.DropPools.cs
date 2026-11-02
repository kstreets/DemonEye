using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

public partial class Game {
    
    [Serializable]
    public class DropOrigin {
        public DropPool dropPool;
        public float chanceToSpawn;
        public int maxStackCount;
    }
    
    public void CreateDropPoolsForMap(MapData map) { 
        foreach (DropPool dropPool in res.mapSpecificDropPools) {
            dropPool.Reset();
        }
        
        foreach (Item item in res.items) {
            if (!ItemCanSpawnOnMap(item, map)) continue;
            RegisterItemToDropPools(item, res.mapSpecificDropPools);
        }
    }
    
    private void RegisterItemToDropPools(Item item, List<DropPool> dropPools) {
        foreach (DropPool dropPool in dropPools) {
            foreach (DropOrigin dropOrigin in item.dropOrigins) {
                if (dropPool != dropOrigin.dropPool) continue;
                dropPool.AddItem(item);
            }
        }
    }
    
    private Item GetItemFromDropPool(DropPool dropPool) => GetItemFromDropPool(dropPool, curRaid.map);
    
    public Item GetItemFromDropPool(DropPool dropPool, [CanBeNull] MapData map) {
        Assert.IsNotNull(dropPool, "Droppool cannot be null");
        Assert.IsFalse(dropPool.itemRarityTable.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling");
        
        // Start rolling from highest rarity down to lowest
        for (int i = Item.rarityCount - 1; i >= 0; i--) {
            Item.Rarity curRarity = (Item.Rarity)i;
            float rarityRoll = GetDropChanceOfRarity(curRarity, map); 
            if (RollProbability(rarityRoll)) {
                if (dropPool.TryGetItemOfRarity(curRarity, out Item item)) {
                    return item;
                }
            }
        } 
        
        return null;
    }
    
    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, ref List<Item> items) => GetUniqueItemsFromDropPool(dropPool, maxCount, ref items, curRaid.map);

    public void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, ref List<Item> items, [CanBeNull] MapData map) {
        Assert.IsNotNull(dropPool, "Droppool cannot be null");
        Assert.IsFalse(dropPool.itemRarityTable.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling");

        // Copy of each rarity's items that haven't been picked yet, items already in the list count as picked
        using var _ = ListPool<List<Item>>.Get(out var remainingByRarity);
        for (int i = 0; i < Item.rarityCount; i++) {
            List<Item> remaining = ListPool<Item>.Get();
            foreach (Item item in dropPool.itemRarityTable[i]) {
                if (!items.Contains(item)) {
                    remaining.Add(item);
                }
            }
            remainingByRarity.Add(remaining);
        }

        for (int pickCount = 0; pickCount < maxCount; pickCount++) {
            // Same cascade as a single drop, from highest rarity down to lowest, but rarities that have run out
            // of items are skipped so they can't eat the roll and it falls back to the lowest one that still has items
            int chosenRarity = -1;
            for (int i = Item.rarityCount - 1; i >= 0; i--) {
                if (remainingByRarity[i].Count == 0) continue;
                
                chosenRarity = i;
                float rarityRoll = GetDropChanceOfRarity((Item.Rarity)i, map);
                if (RollProbability(rarityRoll)) break;
            }

            // Every item in the pool has been picked
            if (chosenRarity == -1) break;

            List<Item> rarityItems = remainingByRarity[chosenRarity];
            int pickedIndex = UnityEngine.Random.Range(0, rarityItems.Count);
            items.Add(rarityItems[pickedIndex]);

            // Swap remove since the order doesn't matter
            rarityItems[pickedIndex] = rarityItems[^1];
            rarityItems.RemoveAt(rarityItems.Count - 1);
        }

        foreach (List<Item> remaining in remainingByRarity) {
            ListPool<Item>.Release(remaining);
        }
    }
    
    private float GetDropChanceOfItem(Item item, [CanBeNull] MapData map) => GetDropChanceOfRarity(item.GetRarity(), map);
    
    private float GetDropChanceOfRarity(Item.Rarity rarity, [CanBeNull] MapData map) {
        float addChanceToSpawn = 0f;
        if (map != null) {
            addChanceToSpawn = rarity switch {
                Item.Rarity.Common    => map.commonLootRarityIncrease,
                Item.Rarity.Uncommon  => map.uncommonLootRarityIncrease,
                Item.Rarity.Rare      => map.rareLootRarityIncrease,
                Item.Rarity.Epic      => map.epicLootRarityIncrease,
                Item.Rarity.Legendary => map.legendaryLootRarityIncrease,
                _                     => throw new ArgumentOutOfRangeException(),
            };
        }
        return Mathf.Clamp01(Item.RollValueOfRarity(rarity) + addChanceToSpawn);
    }
    
    private bool ItemCanSpawnOnMap(Item item, MapData map) {
        Assert.IsNotNull(map);
        if (item.mapSpawning.spawnsOnAll) return true;
        if (MapIsEqualOrGreater(map, item.mapSpawning.firstSpawnMap)) return true;
        return item.mapSpawning.spawnsOnMaps.Contains(map);
    }
    
    private bool MapIsEqualOrGreater(MapData left, MapData right) {
        if (left == null || right == null) return false;
        int leftIndex = config.maps.IndexOf(left);
        int rightIndex = config.maps.IndexOf(right);
        if (leftIndex == -1 || rightIndex == -1) return false;
        return leftIndex >= rightIndex;
    }
    
}