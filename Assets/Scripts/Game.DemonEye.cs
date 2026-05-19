using System.Collections.Generic;
using UnityEngine.Pool;

public partial class Game {
    
    public struct EquipedUpgradeInstance {
        public int uuid;
        public int stackCount;
        
        public EyeUpgrade EyeUpgrade => gameInstance.res.lookup[uuid] as EyeUpgrade;
        public void ApplyToEnemy(Enemy enemy) => EyeUpgrade.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => EyeUpgrade.AddInstanceToEye(eyeInstance, stackCount);
    }

    public struct EquipedAugmentInstance {
        public int uuid;
        public int stackCount;
        
        public Augment Augment => gameInstance.res.lookup[uuid] as Augment;
        public void ApplyToEnemy(Enemy enemy) => Augment.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Augment.AddInstanceToEye(eyeInstance, stackCount);
    }
    
    public class DemonEyeInstance {
        public List<EquipedUpgradeInstance> upgradeInstances = new();
        public List<EquipedAugmentInstance> augmentInstances = new();
        
        public FirerateEyeUpgrade.InstanceData? firerate;
        public TrishotEyeUpgrade.InstanceData? trishot;
        public RangeEyeUpgrade.InstanceData? range;
        public PenetrationEyeUpgrade.InstanceData? penetration;
        public BackwardsShotEyeUpgrade.InstanceData? backwardShot;
        public ExplosionEyeUpgrade.InstanceData? explosion;
        public OverheatBlast.InstanceData? blast;
        public BoneShatterEyeUpgrade.InstanceData? boneShatter;
        public StoppingPowerEyeUpgrade.InstanceData? stoppingPower;
        public ProjectileCountEyeUpgrade.InstanceData? projectileCount;
        
        public BleedCritAugment.InstanceData? bleedCritAugment;
        public DoubleCritAugment.InstanceData? doubleCritAugment;
        public DistanceDamageAugment.InstanceData? distanceDamage;
        public PenetrationDamageAugment.InstanceData? penetrationDamageAugment;
        public DoubleTapAugment.InstanceData? doubleTapAugment;
        public BackwardsPiercingAugment.InstanceData? backwardsPiercingAugment;
        public MultiProjectileCritAugment.InstanceData? multiProjectileCritAugment;
    }
    
    private void InitDemonEye() {
        demonEye.equiped = demonEye.empty;
    }
    
    private void OnEquipDemonEye(DemonEyeInstance newDemonEye) {
        demonEye.equiped = newDemonEye;
        curRaid.temp.damagingData.Reset();
        if (newDemonEye != demonEye.empty) {
            customQuestEvent?.Invoke("FirstDemonEyeEquiped");
        }
    }

    private void BuildAndRegisterEye(ItemInstance itemInstance) {
        itemInstance.itemOrInstanceUuid = GenerateNewItemUuid();
        
        List<EquipedUpgradeInstance> equipedUpgrades = new();
        List<EquipedAugmentInstance> equipedAugments = new();
        EyeUpgradeSet eyeUpgradeSet = EyeUpgradeSetFromIds(itemInstance.nestedUuids);
        
        foreach (EyeUpgradeSet.Element upgradeSetElm in eyeUpgradeSet.elements) {
            equipedUpgrades.Add(new() {
                uuid = upgradeSetElm.EyeUpgrade.uuid,
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
        
        demonEye.instanceFromItemId.Add(itemInstance.itemOrInstanceUuid, newDemonEye);
    }
    
    public class EyeUpgradeSet {
        
        public class Element {
            public EyeUpgrade EyeUpgrade; 
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
        
        using var autoRelease1 = DictionaryPool<EyeUpgrade, int>.Get(out var upgradeCountFromItem);
        // Need to release manually because we need to release its value dictionaries first
        var augmentsPerUpgradeDict = DictionaryPool<EyeUpgrade, Dictionary<Augment, int>>.Get();
        
        foreach (int uuid in uuids) {
            UuidScriptableObject nestedObject = res.lookup[uuid];
            ExtractUpgradeAndAugment(nestedObject, out EyeUpgrade upgrade, out Augment augment);
            
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
        
        using var autoRelease2 = ListPool<(EyeUpgrade, int)>.Get(out var sortedUpgradeList);
        SortUpgradesFromDictionaryIntoList(upgradeCountFromItem, sortedUpgradeList);
        
        foreach ((EyeUpgrade upgrade, int upgradeCount) in sortedUpgradeList) {
            var element = GenericPool<EyeUpgradeSet.Element>.Get();
            element.EyeUpgrade = upgrade;
            element.upgradeCount = upgradeCount;
            element.augmentsAndCount = ListPool<(Augment, int)>.Get();
            
            if (augmentsPerUpgradeDict.TryGetValue(upgrade, out var augmentCountDictionary)) {
                foreach ((Augment augment, int augmentCount) in augmentCountDictionary) {
                    element.augmentsAndCount.Add((augment, augmentCount));
                }
            }
            _eyeUpgradeSet.elements.Add(element);
        }
        
        foreach ((EyeUpgrade _, Dictionary<Augment, int> augmentCountDict) in augmentsPerUpgradeDict) {
            DictionaryPool<Augment, int>.Release(augmentCountDict);
        }
        DictionaryPool<EyeUpgrade, Dictionary<Augment, int>>.Release(augmentsPerUpgradeDict);
        
        return _eyeUpgradeSet;
    }

    private void SortUpgradesFromDictionaryIntoList(Dictionary<EyeUpgrade, int> upgradesAndStackCount, List<(EyeUpgrade, int)> outputList) {
        foreach (KeyValuePair<EyeUpgrade, int> pair in upgradesAndStackCount) {
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
    
    private void ExtractUpgradeAndAugment(UuidScriptableObject uuidObject, out EyeUpgrade upgrade, out Augment aug) {
        if (uuidObject is Augment augment) {
            aug = augment;
            upgrade = augment.derivedFrom;
            return;
        }
        if (uuidObject is EyeUpgrade upgradeItem) {
            aug = null;
            upgrade = upgradeItem;
            return;
        }
        aug = null;
        upgrade = null;
    }
    
    public int GetDemonEyeSellPrice(ItemInstance demonEyeItemInstance) {
        // We need to use the InventoryItem's ID because the Item's ID is the demon eye Scriptable Object
        DemonEyeInstance demonEyeInst = demonEye.instanceFromItemId[demonEyeItemInstance.itemOrInstanceUuid]; 
        
        int sellPrice = 0;
        foreach (EquipedUpgradeInstance upgradeInstance in demonEyeInst.upgradeInstances) {
            sellPrice += upgradeInstance.EyeUpgrade.GetSellPrice() * upgradeInstance.stackCount;
        }
        return sellPrice;
    } 

}
