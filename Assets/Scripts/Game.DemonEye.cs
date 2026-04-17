using System.Collections.Generic;
using System.Linq;

public partial class Game {
    
    public struct EquipedUpgradeInstance {
        public int uuid;
        public int stackCount;
        
        public EyeUpgradeItem EyeUpgradeItem => gameInstance.resourceLookup[uuid] as EyeUpgradeItem;
        public void ApplyToEnemy(Enemy enemy) => EyeUpgradeItem.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => EyeUpgradeItem.AddInstanceToEye(eyeInstance, stackCount);
    }

    public struct EquipedAugmentInstance {
        public int uuid;
        public int stackCount;
        
        public Augment Augment => gameInstance.resourceLookup[uuid] as Augment;
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
        EyeUpgradeSet eyeUpgradeSet = ConstructEyeUpgradeSet(itemInstance.nestedUuids);
        
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
    
    public struct EyeUpgradeSet {
        
        public struct Element {
            public EyeUpgradeItem eyeUpgradeItem; 
            public int upgradeCount;
            public List<(Augment, int)> augmentsAndCount;
            public bool HasAugments => augmentsAndCount != null && augmentsAndCount.Count > 0;
        }
        
        public List<Element> elements;
    }
    
    public EyeUpgradeSet ConstructEyeUpgradeSet(List<int> uuids) {
        Dictionary<EyeUpgradeItem, int> upgradeCountFromItem = new();
        Dictionary<EyeUpgradeItem, Dictionary<Augment, int>> augmentsPerUpgrade = new();
        
        foreach (int uuid in uuids) {
            UuidScriptableObject nestedObject = resourceLookup[uuid];
            ExtractUpgradeAndAugment(nestedObject, out EyeUpgradeItem upgrade, out Augment augment);
            
            if (augment != null) {
                if (augmentsPerUpgrade.TryGetValue(upgrade, out var augmentCountDictionary)) {
                    if (!augmentCountDictionary.TryAdd(augment, 1)) {
                        augmentCountDictionary[augment]++;
                    }
                }
                else {
                    augmentsPerUpgrade.Add(upgrade, new() { {augment, 1} });
                }
            }
            
            if (upgrade != null) {
                if (!upgradeCountFromItem.TryAdd(upgrade, 1)) {
                    upgradeCountFromItem[upgrade]++;
                }
            }
        }
        
        List<(EyeUpgradeItem, int)> sortedUpgradeList = SortUpgradesFromDictionary(upgradeCountFromItem);
        
        EyeUpgradeSet eyeUpgradeSet = new() { elements = new() };
        foreach ((EyeUpgradeItem upgrade, int upgradeCount) in sortedUpgradeList) {
            EyeUpgradeSet.Element element = new() {
                eyeUpgradeItem = upgrade,
                upgradeCount = upgradeCount,
                augmentsAndCount = new(),
            };
            
            if (augmentsPerUpgrade.TryGetValue(upgrade, out var augmentCountDictionary)) {
                foreach ((Augment augment, int augmentCount) in augmentCountDictionary) {
                    element.augmentsAndCount.Add((augment, augmentCount));
                }
            }
            eyeUpgradeSet.elements.Add(element);
        }
        
        return eyeUpgradeSet;
    }

    private List<(EyeUpgradeItem, int)> SortUpgradesFromDictionary(Dictionary<EyeUpgradeItem, int> upgradesAndStackCount) {
        List<(EyeUpgradeItem, int)> eyeUpgrades = new();
        foreach (KeyValuePair<EyeUpgradeItem, int> pair in upgradesAndStackCount) {
            eyeUpgrades.Add(new(pair.Key, pair.Value));
        }
        eyeUpgrades = eyeUpgrades.OrderByDescending(m => m.Item1.GetRarity()).ThenBy(m => m.Item1.displayName).ToList();
        return eyeUpgrades;
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
