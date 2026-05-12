using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    [Serializable]
    public class DropOrigin {
        public DropPool dropPool;
        public float chanceToSpawn;
    }
    
    [NonSerialized] public List<DropPool> globalDropPools = new();
    [NonSerialized] public List<DropPool> mapSpecificDropPools = new();
    
    private void InitDropPools() {
        foreach (DropPool dropPool in allDropPools) {
            dropPool.items = new();
            var dropPoolList = dropPool.isMapSpecific ? mapSpecificDropPools : globalDropPools;
            dropPoolList.Add(dropPool);
        }
        
        foreach (Item item in allItems) {
            RegisterItemToDropPools(item, globalDropPools);
        }
    }
    
    public void CreateDropPoolsForMap(MapData map) { 
        foreach (DropPool dropPool in mapSpecificDropPools) {
            dropPool.items.Clear();
            dropPool.lastDroppedItem = null;
        }
        
        foreach (Item item in allItems) {
            if (!ItemCanSpawnOnMap(item, map)) continue;
            RegisterItemToDropPools(item, mapSpecificDropPools);
        }
    }
    
    private void RegisterItemToDropPools(Item item, List<DropPool> dropPools) {
        foreach (DropPool dropPool in dropPools) {
            foreach (DropOrigin dropOrigin in item.dropOrigins) {
                if (dropPool != dropOrigin.dropPool) continue;
                dropPool.items.Add(item);
            }
        }
    }
    
    private Item GetItemFromDropPool(DropPool dropPool) => GetItemFromDropPool(dropPool, loadedMapData);
    
    public Item GetItemFromDropPool(DropPool dropPool, [CanBeNull] MapData map) {
        Assert.IsNotNull(dropPool, "Droppool cannot be null");
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling");
        
        const int smallPoolThreshold = 4;
        bool useShufflePickMethod = dropPool.items.Count <= smallPoolThreshold;
        Item rolledItem = useShufflePickMethod ? PerformShufflePick(dropPool, map) : PerformWeightedPick(dropPool, map);
        
        rolledItem = RollForAugmentedVersion(rolledItem, dropPool, map);
        dropPool.lastDroppedItem = rolledItem;
        
        return rolledItem;
    }
    
    private Item PerformShufflePick(DropPool dropPool, [CanBeNull] MapData map) {
        using var _ = ListPool<float>.Get(out var dropChances);
        GetTotalDropChances(dropPool, ref dropChances, map);
        
        dropPool.items.Shuffle();
        
        for (int i = 0; i < dropPool.items.Count; i++) {
            float dropChance = dropChances[i];
            if (RollProbability(dropChance)) {
                return dropPool.items[i];
            }
        }
        
        return dropPool.items[^1];
    }
    
    private Item PerformWeightedPick(DropPool dropPool, [CanBeNull] MapData map) {
        using var _ = ListPool<float>.Get(out var dropChances);
        float roll = Random.value * GetTotalDropChances(dropPool, ref dropChances, map);
        float dropThreshold = 0f;
        
        for (int i = 0; i < dropPool.items.Count; i++) {
            float dropChance = dropChances[i];
            dropThreshold += dropChance;
            if (roll < dropThreshold) {
                return dropPool.items[i];    
            }
        }
        
        return dropPool.items[^1];
    }

    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, ref List<Item> items) {
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling");

        using var _ = ListPool<Item>.Get(out var tempDropPoolItems);
        foreach (Item item in dropPool.items) {
            tempDropPoolItems.Add(item);
        }
        
        var restoreList = dropPool.items;
        dropPool.items = tempDropPoolItems;
        
        int selectCount = Mathf.Min(maxCount, dropPool.items.Count);
        for (int i = 0; i < selectCount; i++) {
            Item itemDrop = GetItemFromDropPool(dropPool);
            items.Add(itemDrop);
            dropPool.items.Remove(itemDrop);
        }
        
        dropPool.items = restoreList;
    }
    
    private Item RollForAugmentedVersion(Item item, DropPool dropPool, [CanBeNull] MapData map) {
        if (item is not EyeUpgradeItem upgradeItem || !augmentsPerModifierItemLookup.TryGetValue(upgradeItem, out var possibleAugments)) {
            return item;
        }

        possibleAugments.Shuffle();
        
        foreach (Augment possibleAugment in possibleAugments) {
            if (map != null && !AugmentCanSpawnOnMap(possibleAugment, map)) continue;
            
            float augmentingChance = GetDropChanceOfItem(possibleAugment.augmentedEyeUpgradeItem, dropPool, map);
            if (RollProbability(augmentingChance)) {
                return possibleAugment.augmentedEyeUpgradeItem;
            }
        }

        return item;
    }
    
    private float GetTotalDropChances(DropPool dropPool, ref List<float> dropChances, [CanBeNull] MapData map) {
        float total = 0f;
        foreach (Item item in dropPool.items) {
            float dropChance = GetDropChanceOfItem(item, dropPool, map);

            bool itemIsRepeat = dropPool.lastDroppedItem == item; 
            if (dropPool.reduceDuplicates && itemIsRepeat) {
                const float defaultPercentReduction = 0.5f;
                dropChance *= defaultPercentReduction;
            }
            
            dropChances.Add(dropChance);
            total += dropChance;
        }
        return total;
    }
    
    private float GetDropChanceOfItem(Item item, DropPool dropPool, [CanBeNull] MapData map) {
        float addChanceToSpawn = 0f;
        
        if (map != null) {
            addChanceToSpawn = item.GetRarity() switch {
                Item.Rarity.Common    => map.commonLootRarityIncrease,
                Item.Rarity.Uncommon  => map.uncommonLootRarityIncrease,
                Item.Rarity.Rare      => map.rareLootRarityIncrease,
                Item.Rarity.Epic      => map.epicLootRarityIncrease,
                Item.Rarity.Legendary => map.legendaryLootRarityIncrease,
                _                     => throw new ArgumentOutOfRangeException(),
            };
        }
        
        DropOrigin dropOrigin = GetItemDropOrigin(item, dropPool);
        return Mathf.Clamp01(dropOrigin.chanceToSpawn + addChanceToSpawn);
    }
    
    private DropOrigin GetItemDropOrigin(Item item, DropPool dropPool) {
        foreach (DropOrigin dropOrigin in item.dropOrigins) {
            if (dropPool != dropOrigin.dropPool) continue;
            return dropOrigin;
        }
        return null;
    }
    
    private bool ItemCanSpawnOnMap(Item item, [NotNull] MapData map) {
        Assert.IsNotNull(map);
        if (item.spawnsOnAllMaps) return true;
        if (MapIsEqualOrGreater(map, item.firstSpawnMap)) return true;
        return item.spawnsOnMaps.Contains(map);
    }
    
    private bool AugmentCanSpawnOnMap(Augment augment, [NotNull] MapData map) {
        Assert.IsNotNull(map);
        if (augment.spawnsOnAllMaps) return true;
        if (MapIsEqualOrGreater(map, augment.firstSpawnMap)) return true;
        return augment.spawnsOnMaps.Contains(map);
    }
    
    private bool MapIsEqualOrGreater(MapData left, MapData right) {
        if (left == null || right == null) return false;
        int leftIndex = maps.IndexOf(left);
        int rightIndex = maps.IndexOf(right);
        if (leftIndex == -1 || rightIndex == -1) return false;
        return leftIndex >= rightIndex;
    }
    
}