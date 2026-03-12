using System.Collections.Generic;
using UnityEngine;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : UuidScriptableObject {

    public string displayName;
    public Sprite inventorySprite;
    public Sprite dropSprite;
    public float pickupRadius = 0.07f;
    public ItemType type;

    [Space]

    public Trader associatedTrader;
    public PriceCategory priceCategory;
    public int buyPrice;
    public int sellPrice;

    [Space]

    public bool spawnsOnAllMaps;
    [ShowIf(nameof(spawnsOnAllMaps), false)]
    public List<MapData> spawnsOnMaps;
    [EndIf] 
    
    [Range(0f, 1f)] public float chanceToSpawnOnBody;
    [Range(0f, 1f)] public float chanceToSpawnOnTrader;
    [Range(0f, 1f)] public float chanceToSpawnFromRock;
    [Range(0f, 1f)] public float chanceToSpawnFromEnemy;
    [Range(0f, 1f)] public float chanceToExistInLevel;
    [Range(0f, 1f)] public float chanceToSpawnFromBush;

    [HideIf(nameof(chanceToSpawnOnTrader), 0f)]
    [Range(1, 10)] public int traderLevelRequired;
    [MinMaxSlider(1, 15)] public Vector2Int traderStockRange;
    public List<ItemWithCount> barterRequirements;
    [EndIf]

    [HideIf(nameof(chanceToSpawnFromEnemy), 0f)]
    public List<EnemyData> spawnsFromEnemies;
    [EndIf]

    [Space]
    
    public bool modifiesStats;
    [ShowIf(nameof(modifiesStats))]
    public float armorPercent;
    public float critChance;
    public float critMultiplier;
    public int damage;
    public float fireratePercentage;
    public float movementSpeedPercentage;
    public float projectileCount;
    public float rangeInSeconds;
    [EndIf]
    
    [Space]
    
    public ItemGroupProperties itemGroupProps;
    [ShowIf(nameof(itemGroupProps), null)]
    [SerializeField] private int maxStackCount;
    [SerializeField] [Range(0, 10)] private int weight;
    [SerializeField] [TextArea] private string description;
    [EndIf]
    
    public int Weight => itemGroupProps ? itemGroupProps.weight : weight;
    public int MaxStackCount => itemGroupProps ? itemGroupProps.maxStackCount : maxStackCount;
    
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public Rarity GetRarity() {
        float minRarity = Mathf.Max(
            chanceToSpawnOnBody   > 0 ? chanceToSpawnOnBody   : float.MinValue,
            chanceToSpawnOnTrader > 0 ? chanceToSpawnOnTrader : float.MinValue,
            chanceToSpawnFromRock > 0 ? chanceToSpawnFromRock : float.MinValue,
            chanceToSpawnFromEnemy > 0 ? chanceToSpawnFromEnemy : float.MinValue,
            chanceToSpawnFromBush > 0 ? chanceToSpawnFromBush : float.MinValue
        );
        if (minRarity <= 0.08f) return Rarity.Legendary;
        if (minRarity <= 0.20f) return Rarity.Epic;
        if (minRarity <= 0.38f) return Rarity.Rare;
        if (minRarity <= 0.50f) return Rarity.Uncommon;
        return Rarity.Common;
    }

    public int GetSellPrice() {
        if (priceCategory == null) {
            return sellPrice;
        }
        
        return GetRarity() switch {
            Rarity.Common    => priceCategory.commonPrice,
            Rarity.Uncommon  => priceCategory.uncommonPrice,
            Rarity.Rare      => priceCategory.rarePrice,
            Rarity.Epic      => priceCategory.epicPrice,
            Rarity.Legendary => priceCategory.legendaryPrice,
        };
    }
    
    public string GetDescription(int stackCount = 1) {
        string desc = string.Empty;
        
        if (modifiesStats) {
            if (!Mathf.Approximately(critChance, 0f))              desc += $"\n{DisplayIncDec(critChance)} Crit Chance";
            if (!Mathf.Approximately(critMultiplier, 0f))          desc += $"\n{DisplayIncDec(critMultiplier)} Crit Multiplier";
            if (damage != 0)                                       desc += $"\n{DisplayIncDec(damage)} Damage";
            if (!Mathf.Approximately(fireratePercentage, 0f))      desc += $"\n{DisplayProbIncDec(fireratePercentage)} Firerate";
            if (!Mathf.Approximately(movementSpeedPercentage, 0f)) desc += $"\n{DisplayProbIncDec(movementSpeedPercentage)} Movement Speed";
            if (!Mathf.Approximately(projectileCount, 0f))         desc += $"\n{DisplayIncDec(projectileCount)} Projectile Count";
            if (!Mathf.Approximately(rangeInSeconds, 0f))          desc += $"\n{DisplayIncDec(rangeInSeconds)} Range";
        }

        bool removeFirstNewLine = !string.IsNullOrEmpty(desc);
        if (removeFirstNewLine) {
            desc = desc.Remove(0, 1);
        }

        string modifierDesc = GetModifierDescription(stackCount);
        if (!string.IsNullOrEmpty(modifierDesc)) {
            desc += string.IsNullOrEmpty(desc) ? modifierDesc : $"\n{modifierDesc}"; 
        }
        
        if (!string.IsNullOrEmpty(description)) {
            desc += string.IsNullOrEmpty(desc) ? description : $"\n{description}";
        }
        else if (!string.IsNullOrEmpty(itemGroupProps?.description)) {
            desc += string.IsNullOrEmpty(desc) ? itemGroupProps.description : $"\n{itemGroupProps.description}";
        }
        
        return !string.IsNullOrEmpty(desc) ? desc : "Item is missing description";
    }

    protected virtual string GetModifierDescription(int stackCount) {
        return string.Empty;
    }
    
#if UNITY_EDITOR
    
    [OnValueChanged(nameof(buyPrice))]
    private void AutoSetSellPriceOnBuyPriceChanged() {
        if (this is ModifierItem) {
            sellPrice = Mathf.RoundToInt(buyPrice * 0.08f);
        }
        else {
            sellPrice = Mathf.RoundToInt(buyPrice * 0.28f);
        }
    }
    
    [Button]
    private void AddToStash() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to stash inventory when game is playing");
            return;
        }
        inst.TryAddItemToInventory(inst.stashInventory, this, 1);
    }

#endif

}
