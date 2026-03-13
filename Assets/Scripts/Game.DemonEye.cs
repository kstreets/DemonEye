using System.Collections.Generic;
using System.Linq;

public partial class Game {
    
    public struct EquipedModInstance {
        public int uuid;
        public int stackCount;
        
        public ModifierItem ModifierItem => resourceLookup[uuid] as ModifierItem;
        public void ApplyToEnemy(Enemy enemy) => ModifierItem.AddInstanceToEnemy(enemy, stackCount);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => ModifierItem.AddInstanceToEye(eyeInstance, stackCount);
    }

    public struct EquipedAugmentInstance {
        public int uuid;
        
        public Augment Augment => resourceLookup[uuid] as Augment;
        public void ApplyToEnemy(Enemy enemy) => Augment.AddInstanceToEnemy(enemy);
        public void ApplyToEye(DemonEyeInstance eyeInstance) => Augment.AddInstanceToEye(eyeInstance);
    }

    public class DemonEyeInstance {
        public List<EquipedModInstance> modInstances = new();
        public List<EquipedAugmentInstance> augmentInstances = new();
        
        public FirerateModifierItem.InstanceData? firerate;
        public TrishotModifierItem.InstanceData? trishot;
        public RangeModifierItem.InstanceData? range;
        public PenetrationModifierItem.InstanceData? penetration;
        public BackwardsShotModifierItem.InstanceData? backwardShot;
        public ExplosionModifierItem.InstanceData? explosion;
        public OverheatBlast.InstanceData? blast;
        public BoneShatterModifierItem.InstanceData? boneShatter;
        public StoppingPowerModifierItem.InstanceData? stoppingPower;
        public ProjectileCountModifierItem.InstanceData? projectileCount;
        
        public BleedCritAugment.InstanceData? bleedCritAugment;
        public DoubleCritAugment.InstanceData? doubleCritAugment;
        public DistanceDamageAugment.InstanceData? distanceDamage;
        public PenetrationDamageAugment.InstanceData? penetrationDamageAugment;
        public DoubleTapAugment.InstanceData? doubleTapAugment;
        public BackwardsPiercingAugment.InstanceData? backwardsPiercingAugment;
        public MultiProjectileCritAugment.InstanceData? multiProjectileCritAugment;
    }
    
    public class DemonEyeRaidStats {
        public int consecutiveCriticalHits;
        public float lastDoubleCritActivationTime;
    }

    // Need to reset this at the beginning of every raid
    private DemonEyeRaidStats demonEyeRaidStats;

    public Dictionary<int, DemonEyeInstance> eyeInstanceFromItemId = new();
    private readonly DemonEyeInstance emptyDemonEye = new();
    private DemonEyeInstance equipedEye;
    private Limiter attackLimiter;

    private void BuildAndRegisterEye(ItemInstance itemInstance) {
        itemInstance.itemOrInstanceUuid = GenerateNewItemUuid();
        itemInstance._itemRef = demonEyeItem;
        
        Dictionary<ModifierItem, int> modCountFromItem = new();
        List<EquipedAugmentInstance> equipedAugments = new();
        
        foreach (int modUuid in itemInstance.nestedUuids) {
            UuidScriptableObject nestedObject = resourceLookup[modUuid];
            ExtractModAndAugment(nestedObject, out ModifierItem modifier, out Augment augment);
            if (augment != null) {
                equipedAugments.Add(new() { uuid = augment.uuid });
            }
            if (modifier != null) {
                if (!modCountFromItem.TryAdd(modifier, 1)) {
                    modCountFromItem[modifier]++;
                }
            }
        }

        List<EquipedModInstance> equipedMods = new();
        foreach ((ModifierItem modItem, int stackCount) in SortModsFromDictionary(modCountFromItem)) {
            equipedMods.Add(new() {
                uuid = modItem.uuid,
                stackCount = stackCount,
            });
        }
        
        DemonEyeInstance newDemonEye = new() {
            modInstances = equipedMods,
            augmentInstances = equipedAugments,
        };
        
        foreach (EquipedModInstance modInstance in equipedMods) { 
            modInstance.ApplyToEye(newDemonEye); 
        }
        foreach (EquipedAugmentInstance augmentInstance in equipedAugments) { 
            augmentInstance.ApplyToEye(newDemonEye); 
        }
        
        eyeInstanceFromItemId.Add(itemInstance.itemOrInstanceUuid, newDemonEye);
    }
    
    private struct ModifierTree {
        public ModifierItem modifierItem; 
        public int modifierCount;
        public List<Augment> uniqueAugments;
    }
    
    private struct ModifierSet {
        public List<ModifierTree> elements;
    }
    
    private ModifierSet ConsturctModifierSet(List<int> uuids) {
        Dictionary<ModifierItem, int> modCountFromItem = new();
        Dictionary<ModifierItem, HashSet<Augment>> uniqueAugmentsPerModifier = new();
        
        foreach (int uuid in uuids) {
            UuidScriptableObject nestedObject = resourceLookup[uuid];
            ExtractModAndAugment(nestedObject, out ModifierItem modifier, out Augment augment);
            
            if (augment != null) {
                if (uniqueAugmentsPerModifier.TryGetValue(modifier, out var augmentSet)) {
                    augmentSet.Add(augment);
                }
                else {
                    uniqueAugmentsPerModifier.Add(modifier, new() { augment });
                }
            }
            
            if (modifier != null) {
                if (!modCountFromItem.TryAdd(modifier, 1)) {
                    modCountFromItem[modifier]++;
                }
            }
        }
        
        List<(ModifierItem, int)> sortedModsList = SortModsFromDictionary(modCountFromItem);
        
        ModifierSet modifierSet = new() { elements = new() };
        foreach ((ModifierItem mod, int count) in sortedModsList) {
            ModifierTree tree = new() {
                modifierItem = mod,
                modifierCount = count,
                uniqueAugments = new(),
            };
            
            if (uniqueAugmentsPerModifier.TryGetValue(mod, out var augmentSet)) {
                foreach (Augment augment in augmentSet) {
                    tree.uniqueAugments.Add(augment);
                }
            }
            modifierSet.elements.Add(tree);
        }
        
        return modifierSet;
    }

    private List<(ModifierItem, int)> SortModsFromDictionary(Dictionary<ModifierItem, int> soulcardsAndStackCount) {
        List<(ModifierItem, int)> eyeModifiers = new();
        foreach (KeyValuePair<ModifierItem, int> pair in soulcardsAndStackCount) {
            eyeModifiers.Add(new(pair.Key, pair.Value));
        }
        eyeModifiers = eyeModifiers.OrderByDescending(m => m.Item1.GetRarity()).ThenBy(m => m.Item1.displayName).ToList();
        return eyeModifiers;
    }
    
    private void ExtractModAndAugment(UuidScriptableObject uuidObject, out ModifierItem mod, out Augment aug) {
        if (uuidObject is Augment augment) {
            aug = augment;
            mod = augment.modifierDerivedFrom;
            return;
        }
        if (uuidObject is ModifierItem modifierItem) {
            aug = null;
            mod = modifierItem;
            return;
        }
        aug = null;
        mod = null;
    }

    public int GetDemonEyeSellPrice(ItemInstance demonEyeItemInstance) {
        // We need to use the InventoryItem's ID because the Item's ID is the demon eye Scriptable Object
        DemonEyeInstance demonEye = eyeInstanceFromItemId[demonEyeItemInstance.itemOrInstanceUuid]; 
        
        int sellPrice = 0;
        foreach (EquipedModInstance modInstance in demonEye.modInstances) {
            sellPrice += modInstance.ModifierItem.GetSellPrice() * modInstance.stackCount;
        }
        return sellPrice;
    } 

    
    public string GetDemonEyeModDescription(ModifierItem modifierItem, int count, List<Augment> augments) {
        string title = ColorText($"<size=108%>{modifierItem.displayName}</size> <size=87%>x{count}</size>", styles.headerTextColor);
        string desc = $"<line-height=95%>{title}\n{modifierItem.GetDescription(count)}<line-height=140%>\n";
        if (augments == null || augments.Count <= 0) {
            return desc;
        }
        
        string augmentDesc = string.Empty;
        foreach (Augment augment in augments) {
            augmentDesc += $"{augment.GetDescription()}\n";
        }
        return desc + augmentDesc;
    }

}
