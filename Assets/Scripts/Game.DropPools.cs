using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    public enum DropOrigin { Rock, Altar, Body, Trader, Enemy, ExistsInLevel, Bush }

    public class DropPool {
        public List<Item> items = new();
        public DropOrigin dropOrigin;
        public Item lastDroppedItem;
        public bool HasItems => items.Count > 0;
    }

    public DropPool rockStonesDropPool;
    public DropPool eyeUpgradesDropPool;
    public DropPool bodyDropPool;
    public DropPool traderDropPool;
    public DropPool enemyDropPool;
    public DropPool foragingDropPool;
    public DropPool bushesDropPool;
    
    private DropPool[] mapSpecificDropPools;
    private readonly DropOrigin[] reducedDuplicateDropPools = { DropOrigin.Altar };
    
    private void CreateDropPools() {
        rockStonesDropPool = new() { dropOrigin = DropOrigin.Rock };
        eyeUpgradesDropPool = new() { dropOrigin = DropOrigin.Altar };
        bodyDropPool = new() { dropOrigin = DropOrigin.Body };
        traderDropPool = new() { dropOrigin = DropOrigin.Trader };
        enemyDropPool = new() { dropOrigin = DropOrigin.Enemy };
        foragingDropPool = new() { dropOrigin = DropOrigin.ExistsInLevel };
        bushesDropPool = new() { dropOrigin = DropOrigin.Bush };
        
        mapSpecificDropPools = new[] { rockStonesDropPool, eyeUpgradesDropPool, bodyDropPool, foragingDropPool, bushesDropPool };
        
        foreach (Item item in allItems) {
            if (item.chanceToSpawnOnTrader > 0f) {
                traderDropPool.items.Add(item);
            }
            if (item.chanceToSpawnFromEnemy > 0f) {
                enemyDropPool.items.Add(item);
            }
        }
    }
    
    public void CreateDropPoolsForMap(MapData map) { 
        foreach (DropPool dropPool in mapSpecificDropPools) {
            dropPool.items.Clear();
            dropPool.lastDroppedItem = null;
        }
        
        foreach (Item item in allItems) {
            if (!ItemCanSpawnOnMap(item, map)) continue;
            
            if (item.chanceToSpawnFromAltar > 0f) {
                eyeUpgradesDropPool.items.Add(item);
            }
            if (item.chanceToSpawnFromRock > 0f) {
                rockStonesDropPool.items.Add(item);
            }
            if (item.chanceToSpawnOnBody > 0f) {
                bodyDropPool.items.Add(item);
            }
            if (item.chanceToExistInLevel > 0f) {
                foragingDropPool.items.Add(item);
            }
            if (item.chanceToSpawnFromBush > 0f) {
                bushesDropPool.items.Add(item);
            }
        }
    }
    
    private Item GetItemFromDropPool(DropPool dropPool) => GetItemFromDropPool(dropPool, loadedMapData);
    
    public Item GetItemFromDropPool(DropPool dropPool, [CanBeNull] MapData map) {
        Assert.IsFalse(dropPool.items == enemyDropPool.items, $"Use {nameof(GetItemFromEnemyDropPool)} for enemies");
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling");
        
        const int smallPoolThreshold = 4;
        bool useShufflePickMethod = dropPool.items.Count <= smallPoolThreshold;
        Item rolledItem = useShufflePickMethod ? PerformShufflePick(dropPool, map) : PerformWeightedPick(dropPool, map);
        
        rolledItem = RollForAugmentedVersion(rolledItem, dropPool.dropOrigin, map);
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

    private Item GetItemFromEnemyDropPool(EnemyData enemy) {
        using var _ = GenericPool<DropPool>.Get(out var tempDropPool);
        tempDropPool.items.Clear();
        tempDropPool.dropOrigin = DropOrigin.Enemy;
        
        foreach (Item enemyItem in enemyDropPool.items) {
            if (enemyItem.spawnsFromEnemies.Contains(enemy)) {
                tempDropPool.items.Add(enemyItem);
            }
        }

        if (tempDropPool.items.Count <= 0) {
            return null;
        }
        
        Item item = GetItemFromDropPool(tempDropPool);
        return item;
    }

    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, List<Item> items) {
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling"); 
        
        using var _ = GenericPool<DropPool>.Get(out var tempDropPool);
        tempDropPool.dropOrigin = dropPool.dropOrigin;
        tempDropPool.items.Clear();
        foreach (Item item in dropPool.items) {
            tempDropPool.items.Add(item);
        }
        
        int selectCount = Mathf.Min(maxCount, tempDropPool.items.Count);
        for (int i = 0; i < selectCount; i++) {
            Item itemDrop = GetItemFromDropPool(tempDropPool);
            items.Add(itemDrop);
            tempDropPool.items.Remove(itemDrop);
        }
    }
    
    private Item RollForAugmentedVersion(Item item, DropOrigin origin, [CanBeNull] MapData map) {
        if (item is not EyeUpgradeItem upgradeItem || !augmentsPerModifierItemLookup.TryGetValue(upgradeItem, out var possibleAugments)) {
            return item;
        }

        possibleAugments.Shuffle();
        
        foreach (Augment possibleAugment in possibleAugments) {
            if (map != null && !AugmentCanSpawnOnMap(possibleAugment, map)) continue;
            
            float augmentingChance = GetDropChanceOfItem(possibleAugment.augmentedEyeUpgradeItem, origin, map);
            if (RollProbability(augmentingChance)) {
                return possibleAugment.augmentedEyeUpgradeItem;
            }
        }

        return item;
    }
    
    private float GetTotalDropChances(DropPool dropPool, ref List<float> dropChances, [CanBeNull] MapData map) {
        float total = 0f;
        foreach (Item item in dropPool.items) {
            float dropChance = GetDropChanceOfItem(item, dropPool.dropOrigin, map);
            
            bool reduceForDropPool = reducedDuplicateDropPools.Contains(dropPool.dropOrigin);
            bool itemIsRepeat = dropPool.lastDroppedItem == item; 
            if (reduceForDropPool && itemIsRepeat) {
                const float defaultPercentReduction = 0.5f;
                dropChance *= defaultPercentReduction;
            }
            
            dropChances.Add(dropChance);
            total += dropChance;
        }
        return total;
    }
    
    private float GetDropChanceOfItem(Item item, DropOrigin origin, [CanBeNull] MapData map) {
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
        
        return origin switch {
            DropOrigin.Altar         => Mathf.Clamp01(item.chanceToSpawnFromAltar + addChanceToSpawn),
            DropOrigin.Rock          => Mathf.Clamp01(item.chanceToSpawnFromRock + addChanceToSpawn),
            DropOrigin.Body          => Mathf.Clamp01(item.chanceToSpawnOnBody + addChanceToSpawn),
            DropOrigin.Trader        => Mathf.Clamp01(item.chanceToSpawnOnTrader + addChanceToSpawn),
            DropOrigin.Enemy         => Mathf.Clamp01(item.chanceToSpawnFromEnemy + addChanceToSpawn),
            DropOrigin.Bush          => Mathf.Clamp01(item.chanceToSpawnFromBush + addChanceToSpawn),
            DropOrigin.ExistsInLevel => Mathf.Clamp01(item.chanceToExistInLevel + addChanceToSpawn),
            _                        => throw new ArgumentOutOfRangeException(),
        };
    }
    
    private bool ItemCanSpawnOnMap(Item item, [NotNull] MapData map) {
        Assert.IsNotNull(map);
        if (item.spawnsOnAllMaps) return true;
        if (MapIsOnOrPassed(item.firstSpawnMap, map)) return true;
        return item.spawnsOnMaps.Contains(map);
    }
    
    private bool AugmentCanSpawnOnMap(Augment augment, [NotNull] MapData map) {
        Assert.IsNotNull(map);
        if (augment.spawnsOnAllMaps) return true;
        if (MapIsOnOrPassed(augment.firstSpawnMap, map)) return true;
        return augment.spawnsOnMaps.Contains(loadedMapData);
    }
    
    private bool MapIsOnOrPassed(MapData map, MapData currentMap) {
        if (map == null || currentMap == null) return false;
        return maps.IndexOf(currentMap) >= maps.IndexOf(map);
    }
    
}