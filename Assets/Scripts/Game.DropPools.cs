using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public partial class Game {
    
    private enum DropOrigin { Rock, Body, Trader, Enemy, ExistsInLevel, Bush }

    private class DropPool {
        public List<Item> items = new();
        public DropOrigin dropOrigin;
        public Item lastDroppedItem;
        public bool HasItems => items.Count > 0;
    }

    private DropPool rockStonesDropPool;
    private DropPool eyeUpgradesDropPool;
    private DropPool bodyDropPool;
    private DropPool traderDropPool;
    private DropPool enemyDropPool;
    private DropPool foragingDropPool;
    private DropPool bushesDropPool;
    
    private DropPool[] mapSpecificDropPools;
    
    private void CreateDropPools() {
        rockStonesDropPool = new() { dropOrigin = DropOrigin.Rock };
        eyeUpgradesDropPool = new() { dropOrigin = DropOrigin.Rock };
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
    
    private void CreateDropPoolsForCurrentMap() { 
        foreach (DropPool dropPool in mapSpecificDropPools) {
            dropPool.items.Clear();
            dropPool.lastDroppedItem = null;
        }
        
        foreach (Item item in allItems) {
            if (!ItemCanSpawnOnCurrentMap(item)) continue;
            
            if (item.type == eyeUpgradeType) {
                eyeUpgradesDropPool.items.Add(item);
                continue;
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
    
    private Item GetItemFromDropPool(DropPool dropPool) {
        Assert.IsFalse(dropPool.items == enemyDropPool.items, $"Use {nameof(GetItemFromEnemyDropPool)} for enemies");
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling");
        
        using var _ = ListPool<float>.Get(out var dropChances);
        
        Item rolledItem = null;
        float roll = Random.value * GetTotalDropChances(dropPool, ref dropChances);
        float dropThreshold = 0f;

        for (int i = 0; i < dropPool.items.Count; i++) {
            Item drop = dropPool.items[i];
            float dropChance = dropChances[i];
            
            dropThreshold += dropChance;
            if (roll < dropThreshold) {
                rolledItem = RollForAugmentedVersion(drop, dropPool.dropOrigin);
                break;
            }
        }
        
        rolledItem ??= RollForAugmentedVersion(dropPool.items[^1], dropPool.dropOrigin);
        dropPool.lastDroppedItem = rolledItem;
        return rolledItem;
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

    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, List<Item> items, float raritySkew = 0f) {
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
    
    private Item RollForAugmentedVersion(Item item, DropOrigin origin) {
        if (item is not EyeUpgradeItem upgradeItem || !augmentsPerModifierItemLookup.TryGetValue(upgradeItem, out var possibleAugments)) {
            return item;
        }

        possibleAugments.Shuffle();
        
        foreach (Augment possibleAugment in possibleAugments) {
            if (!AugmentCanSpawnOnCurrentMap(possibleAugment)) continue;
            
            float augmentingChance = GetDropChanceOfItem(possibleAugment.augmentedEyeUpgradeItem, origin);
            if (RollProbability(augmentingChance)) {
                return possibleAugment.augmentedEyeUpgradeItem;
            }
        }

        return item;
    }
    
    private float GetDropChanceOfItem(Item item, DropOrigin origin) {
        float addChanceToSpawn = 0f;
        
        if (origin != DropOrigin.Trader) {
            addChanceToSpawn = item.GetRarity() switch {
                Item.Rarity.Common    => loadedMapData.commonLootRarityIncrease,
                Item.Rarity.Uncommon  => loadedMapData.uncommonLootRarityIncrease,
                Item.Rarity.Rare      => loadedMapData.rareLootRarityIncrease,
                Item.Rarity.Epic      => loadedMapData.epicLootRarityIncrease,
                Item.Rarity.Legendary => loadedMapData.legendaryLootRarityIncrease,
                _                     => throw new ArgumentOutOfRangeException(),
            };
        }
        
        return origin switch {
            DropOrigin.Rock   => Mathf.Clamp01(item.chanceToSpawnFromRock + addChanceToSpawn),
            DropOrigin.Body   => Mathf.Clamp01(item.chanceToSpawnOnBody + addChanceToSpawn),
            DropOrigin.Trader => Mathf.Clamp01(item.chanceToSpawnOnTrader + addChanceToSpawn),
            DropOrigin.Enemy  => Mathf.Clamp01(item.chanceToSpawnFromEnemy + addChanceToSpawn),
            DropOrigin.Bush   => Mathf.Clamp01(item.chanceToSpawnFromBush + addChanceToSpawn),
            _                 => 0f,
        };
    }
    
    private float GetTotalDropChances(DropPool dropPool, ref List<float> dropChances) {
        float total = 0f;
        foreach (Item item in dropPool.items) {
            float dropChance = GetDropChanceOfItem(item, dropPool.dropOrigin);
            
            bool reduceChanceForRepeatItem = dropPool.lastDroppedItem == item; 
            if (reduceChanceForRepeatItem) {
                const float defaultPercentReduction = 0.5f;
                dropChance *= defaultPercentReduction;
            }
            
            dropChances.Add(dropChance);
            total += dropChance;
        }
        return total;
    }
    
    private bool ItemCanSpawnOnCurrentMap(Item item) {
        if (item.spawnsOnAllMaps) return true;
        if (CurrentlyOnOrPassedMap(item.firstSpawnMap)) return true;
        return item.spawnsOnMaps.Contains(loadedMapData);
    }
    
    private bool AugmentCanSpawnOnCurrentMap(Augment augment) {
        if (augment.spawnsOnAllMaps) return true;
        if (CurrentlyOnOrPassedMap(augment.firstSpawnMap)) return true;
        return augment.spawnsOnMaps.Contains(loadedMapData);
    }
    
    private bool CurrentlyOnOrPassedMap(MapData map) {
        if (map == null || loadedMapData == null) return false;
        return maps.IndexOf(loadedMapData) >= maps.IndexOf(map);
    }
    
}
