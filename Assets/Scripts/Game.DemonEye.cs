using System.Collections.Generic;
using UnityEngine.Pool;

public partial class Game {
    
    public struct EquipedUpgradeInstance {
        public int uuid;
        public int stackCount;
        
        public EyeUpgradeItem EyeUpgradeItem => gameInstance.gameData.res.lookup[uuid] as EyeUpgradeItem;
        public void ApplyToEnemy(Enemy enemy) => EyeUpgradeItem.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => EyeUpgradeItem.AddInstanceToEye(eyeInstance, stackCount);
    }

    public struct EquipedAugmentInstance {
        public int uuid;
        public int stackCount;
        
        public Augment Augment => gameInstance.gameData.res.lookup[uuid] as Augment;
        public void ApplyToEnemy(Enemy enemy) => Augment.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Augment.AddInstanceToEye(eyeInstance, stackCount);
    }
    
    public class DemonEyeInstance {
        public List<EquipedUpgradeInstance> upgradeInstances = new();
        public List<EquipedAugmentInstance> augmentInstances = new();
        
        public FirerateEyeUpgradeItem.InstanceData? firerate;
        public TrishotEyeUpgradeItem.InstanceData? trishot;
        public RangeEyeUpgradeItem.InstanceData? range;
        public PenetrationEyeUpgradeItem.InstanceData? penetration;
        public BackwardsShotEyeUpgradeItem.InstanceData? backwardShot;
        public ExplosionEyeUpgradeItem.InstanceData? explosion;
        public OverheatBlast.InstanceData? blast;
        public BoneShatterEyeUpgradeItem.InstanceData? boneShatter;
        public StoppingPowerEyeUpgradeItem.InstanceData? stoppingPower;
        public ProjectileCountEyeUpgradeItem.InstanceData? projectileCount;
        
        public BleedCritAugment.InstanceData? bleedCritAugment;
        public DoubleCritAugment.InstanceData? doubleCritAugment;
        public DistanceDamageAugment.InstanceData? distanceDamage;
        public PenetrationDamageAugment.InstanceData? penetrationDamageAugment;
        public DoubleTapAugment.InstanceData? doubleTapAugment;
        public BackwardsPiercingAugment.InstanceData? backwardsPiercingAugment;
        public MultiProjectileCritAugment.InstanceData? multiProjectileCritAugment;
    }
    
    private DemonEyeInstance equipedEye;
    public Dictionary<int, DemonEyeInstance> eyeInstanceFromItemId = new();
    private readonly DemonEyeInstance emptyDemonEye = new();
    private Limiter attackLimiter;

    private void BuildAndRegisterEye(ItemInstance itemInstance) {
        itemInstance.itemOrInstanceUuid = GenerateNewItemUuid();
        
        List<EquipedUpgradeInstance> equipedUpgrades = new();
        List<EquipedAugmentInstance> equipedAugments = new();
        EyeUpgradeSet eyeUpgradeSet = EyeUpgradeSetFromIds(itemInstance.nestedUuids);
        
        foreach (EyeUpgradeSet.Element upgradeSetElm in eyeUpgradeSet.elements) {
            equipedUpgrades.Add(new() {
                uuid = upgradeSetElm.eyeUpgradeItem.uuid,
                stackCount = upgradeSetElm.upgradeCount,
            });
            
            if (upgradeSetElm.HasAugments) {
                foreach ((Augment augment, int count) in upgradeSetElm.augmentsAndCount) {
                    equipedAugments.Add(new() {
                        uuid = augment.uuid,
                        stackCount = count,
                    }); 
                }
            }
        }
        
        DemonEyeInstance newDemonEye = new() {
            upgradeInstances = equipedUpgrades,
            augmentInstances = equipedAugments,
        };
        
        foreach (EquipedUpgradeInstance upgradeInstance in equipedUpgrades) { 
            upgradeInstance.ApplyToEye(newDemonEye); 
        }
        foreach (EquipedAugmentInstance augmentInstance in equipedAugments) { 
            augmentInstance.ApplyToEye(newDemonEye); 
        }
        
        eyeInstanceFromItemId.Add(itemInstance.itemOrInstanceUuid, newDemonEye);
    }
    
    public class EyeUpgradeSet {
        
        public class Element {
            public EyeUpgradeItem eyeUpgradeItem; 
            public int upgradeCount;
            public List<(Augment, int)> augmentsAndCount;
            public bool HasAugments => augmentsAndCount != null && augmentsAndCount.Count > 0;
        }
        
        public List<Element> elements = new();
    }
    
    private EyeUpgradeSet _eyeUpgradeSet = new();
    
    public EyeUpgradeSet EyeUpgradeSetFromIds(List<int> uuids) {
        foreach (EyeUpgradeSet.Element element in _eyeUpgradeSet.elements) { 
            ListPool<(Augment, int)>.Release(element.augmentsAndCount);
            GenericPool<EyeUpgradeSet.Element>.Release(element);
        }
        _eyeUpgradeSet.elements.Clear();
        
        using var autoRelease1 = DictionaryPool<EyeUpgradeItem, int>.Get(out var upgradeCountFromItem);
        // Need to release manually because we need to release its value dictionaries first
        var augmentsPerUpgradeDict = DictionaryPool<EyeUpgradeItem, Dictionary<Augment, int>>.Get();
        
        foreach (int uuid in uuids) {
            UuidScriptableObject nestedObject = gameData.res.lookup[uuid];
            ExtractUpgradeAndAugment(nestedObject, out EyeUpgradeItem upgrade, out Augment augment);
            
            if (augment != null) {
                if (augmentsPerUpgradeDict.TryGetValue(upgrade, out Dictionary<Augment, int> augmentCountDict)) {
                    if (!augmentCountDict.TryAdd(augment, 1)) {
                        augmentCountDict[augment]++;
                    }
                }
                else {
                    augmentCountDict = DictionaryPool<Augment, int>.Get();
                    augmentCountDict.Add(augment, 1);
                    augmentsPerUpgradeDict.Add(upgrade, augmentCountDict);
                }
            }
            
            if (upgrade != null) {
                if (!upgradeCountFromItem.TryAdd(upgrade, 1)) {
                    upgradeCountFromItem[upgrade]++;
                }
            }
        }
        
        using var autoRelease2 = ListPool<(EyeUpgradeItem, int)>.Get(out var sortedUpgradeList);
        SortUpgradesFromDictionaryIntoList(upgradeCountFromItem, sortedUpgradeList);
        
        foreach ((EyeUpgradeItem upgrade, int upgradeCount) in sortedUpgradeList) {
            var element = GenericPool<EyeUpgradeSet.Element>.Get();
            element.eyeUpgradeItem = upgrade;
            element.upgradeCount = upgradeCount;
            element.augmentsAndCount = ListPool<(Augment, int)>.Get();
            
            if (augmentsPerUpgradeDict.TryGetValue(upgrade, out var augmentCountDictionary)) {
                foreach ((Augment augment, int augmentCount) in augmentCountDictionary) {
                    element.augmentsAndCount.Add((augment, augmentCount));
                }
            }
            _eyeUpgradeSet.elements.Add(element);
        }
        
        foreach ((EyeUpgradeItem _, Dictionary<Augment, int> augmentCountDict) in augmentsPerUpgradeDict) {
            DictionaryPool<Augment, int>.Release(augmentCountDict);
        }
        DictionaryPool<EyeUpgradeItem, Dictionary<Augment, int>>.Release(augmentsPerUpgradeDict);
        
        return _eyeUpgradeSet;
    }

    private void SortUpgradesFromDictionaryIntoList(Dictionary<EyeUpgradeItem, int> upgradesAndStackCount, List<(EyeUpgradeItem, int)> outputList) {
        foreach (KeyValuePair<EyeUpgradeItem, int> pair in upgradesAndStackCount) {
            outputList.Add(new(pair.Key, pair.Value));
        }
        outputList.Sort(static (x, y) => {
            int rarityCompare = x.Item1.GetRarity().CompareTo(y.Item1.GetRarity()) * -1; // Flip for sorting in descending order
            if (rarityCompare != 0) {
                return rarityCompare;
            }
            return x.Item1.displayName.CompareTo(y.Item1.displayName);
        });
    }
    
    private void ExtractUpgradeAndAugment(UuidScriptableObject uuidObject, out EyeUpgradeItem upgrade, out Augment aug) {
        if (uuidObject is Augment augment) {
            aug = augment;
            upgrade = augment.eyeUpgradeDerivedFrom;
            return;
        }
        if (uuidObject is EyeUpgradeItem upgradeItem) {
            aug = null;
            upgrade = upgradeItem;
            return;
        }
        aug = null;
        upgrade = null;
    }
    
    public int GetDemonEyeSellPrice(ItemInstance demonEyeItemInstance) {
        // We need to use the InventoryItem's ID because the Item's ID is the demon eye Scriptable Object
        DemonEyeInstance demonEye = eyeInstanceFromItemId[demonEyeItemInstance.itemOrInstanceUuid]; 
        
        int sellPrice = 0;
        foreach (EquipedUpgradeInstance upgradeInstance in demonEye.upgradeInstances) {
            sellPrice += upgradeInstance.EyeUpgradeItem.GetSellPrice() * upgradeInstance.stackCount;
        }
        return sellPrice;
    } 

}
