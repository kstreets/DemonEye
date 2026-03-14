using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

public partial class Game {
    
    private enum DropOrigin { Rock, Body, Trader, Enemy, ExistsInLevel, Bush }

    private class DropPool {
        public List<Item> items;
        public DropOrigin dropOrigin;
        public bool HasItems => items.Count > 0;
    }

    private DropPool rockStonesDropPool;
    private DropPool eyeUpgradesDropPool;
    private DropPool bodyDropPool;
    private DropPool traderDropPool;
    private DropPool enemyDropPool;
    private DropPool foragingDropPool;
    private DropPool bushesDropPool;

    private void CreateDropPools() {
        rockStonesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        eyeUpgradesDropPool = new() { items = new(), dropOrigin = DropOrigin.Rock };
        bodyDropPool = new() { items = new(), dropOrigin = DropOrigin.Body };
        traderDropPool = new() { items = new(), dropOrigin = DropOrigin.Trader };
        enemyDropPool = new() { items = new(), dropOrigin = DropOrigin.Enemy };
        foragingDropPool = new() { items = new(), dropOrigin = DropOrigin.ExistsInLevel };
        bushesDropPool = new() { items = new(), dropOrigin = DropOrigin.Bush };

        foreach (Item item in allItems) {
            if (item.chanceToSpawnOnTrader > 0f) {
                traderDropPool.items.Add(item); 
            }

            if (item.chanceToSpawnFromEnemy > 0f) {
                enemyDropPool.items.Add(item);
            }
        }
    }
    
    private void CreateDropPoolsForMap(MapData map) { 
        rockStonesDropPool.items.Clear();
        eyeUpgradesDropPool.items.Clear();
        bodyDropPool.items.Clear();
        foragingDropPool.items.Clear();
        bushesDropPool.items.Clear();
        
        foreach (Item item in allItems) {
            bool spawnsOnCurrentMap = item.spawnsOnAllMaps || item.spawnsOnMaps.Contains(map);
            if (!spawnsOnCurrentMap) continue;

            if (item.chanceToSpawnFromRock > 0f) {
                if (item.type == eyeModifierType) {
                    eyeUpgradesDropPool.items.Add(item);
                }
                else {
                    rockStonesDropPool.items.Add(item);
                }
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

    private Item GetItemFromEnemyDropPool(EnemyData enemy) {
        DropPool tempEnemyPool = new() {
            items = ListPool<Item>.Get(),
            dropOrigin = DropOrigin.Enemy,
        };
        
        foreach (Item enemyItem in enemyDropPool.items) {
            if (enemyItem.spawnsFromEnemies.Contains(enemy)) {
                tempEnemyPool.items.Add(enemyItem);
            }
        }

        if (tempEnemyPool.items.Count <= 0) {
            ListPool<Item>.Release(tempEnemyPool.items);
            return null;
        }
        
        Item item = GetItemFromDropPool(tempEnemyPool);
        ListPool<Item>.Release(tempEnemyPool.items);
        return item;
    }

    private Item GetItemFromDropPool(DropPool dropPool) {
        Assert.IsFalse(dropPool.items == enemyDropPool.items, $"Use {nameof(GetItemFromEnemyDropPool)} for enemies");
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling"); 
        
        dropPool.items.Shuffle();
        
        foreach (Item drop in dropPool.items) {
            float dropChance = GetDropChanceOfItem(drop, dropPool.dropOrigin);
            if (RollProbability(dropChance)) {
                return RollForAugmentedVersion(drop, dropPool.dropOrigin);
            }
        }
        
        return RollForAugmentedVersion(dropPool.items[^1], dropPool.dropOrigin); 
    }
    
    private void GetUniqueItemsFromDropPool(DropPool dropPool, int maxCount, List<Item> items, float raritySkew = 0f) {
        Assert.IsFalse(dropPool.items.Count == 0, $"No items in drop pool, use {nameof(DropPool.HasItems)} before calling"); 
        
        dropPool.items.Shuffle();
        
        foreach (Item item in dropPool.items) {
            float itemDropChance = GetDropChanceOfItem(item, dropPool.dropOrigin) + raritySkew;
            if (RollProbability(itemDropChance)) {
                items.Add(RollForAugmentedVersion(item, dropPool.dropOrigin));
            }
        }
        
        bool itemListNeedsTrimming = items.Count > maxCount;
        if (itemListNeedsTrimming) {
            items.RemoveRange(maxCount, items.Count - maxCount);
        }
    }
    
    private Item RollForAugmentedVersion(Item item, DropOrigin origin) {
        if (item is not ModifierItem modifierItem || !augmentsPerModifierItemLookup.TryGetValue(modifierItem, out var possibleAugments)) {
            return item;
        }

        possibleAugments.Shuffle();
        
        foreach (Augment possibleAugment in possibleAugments) {
            float augmentingChance = GetDropChanceOfItem(possibleAugment.augmentedModifierItem, origin);
            if (RollProbability(augmentingChance)) {
                return possibleAugment.augmentedModifierItem;
            }
        }

        return item;
    }
    
    private float GetDropChanceOfItem(Item item, DropOrigin origin) {
        float addChanceToSpawnFromLuck = 0f;
        
        if (origin != DropOrigin.Trader) {
            float raritySkewIncreaseFromMap = loadedMapData.increasedLootRarityChance;
            addChanceToSpawnFromLuck = item.GetRarity() switch {
                // Scaling the luck increase exponentionally (the adding/subtracting 1 is because rarity skew is a decimal)
                Item.Rarity.Uncommon  => Mathf.Pow(1f + raritySkewIncreaseFromMap, 1.1f) - 1f,
                Item.Rarity.Rare      => Mathf.Pow(1f + raritySkewIncreaseFromMap, 1.2f) - 1f,
                Item.Rarity.Legendary => Mathf.Pow(1f + raritySkewIncreaseFromMap, 1.3f) - 1f,
                _                     => 0f,
            };
        }
        
        return origin switch {
            DropOrigin.Rock   => Mathf.Clamp01(item.chanceToSpawnFromRock + addChanceToSpawnFromLuck),
            DropOrigin.Body   => Mathf.Clamp01(item.chanceToSpawnOnBody + addChanceToSpawnFromLuck),
            DropOrigin.Trader => Mathf.Clamp01(item.chanceToSpawnOnTrader + addChanceToSpawnFromLuck),
            DropOrigin.Enemy  => Mathf.Clamp01(item.chanceToSpawnFromEnemy + addChanceToSpawnFromLuck),
            DropOrigin.Bush   => Mathf.Clamp01(item.chanceToSpawnFromBush + addChanceToSpawnFromLuck),
            _                 => 0f,
        };
    }
    
}
