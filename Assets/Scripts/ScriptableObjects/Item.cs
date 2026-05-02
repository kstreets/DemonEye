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
    public MapData firstSpawnMap;
    public List<MapData> spawnsOnMaps;
    [EndIf] 
    
    [Space]
    
    #if UNITY_EDITOR
    [ReadOnly] [SerializeField] private Rarity rarity;
    #endif
    
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
    [SerializeField] private float critChance;
    [SerializeField] private float critMultiplier;
    [SerializeField] private float damageMultiplier;
    [SerializeField] private float fireratePercentage;
    [SerializeField] private float movementSpeedPercentage;
    [SerializeField] private float projectileCount;
    [SerializeField] private float rangePercentage;
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
        float maxRarity = Mathf.Max(
            chanceToSpawnOnBody   > 0 ? chanceToSpawnOnBody   : float.MinValue,
            chanceToSpawnOnTrader > 0 ? chanceToSpawnOnTrader : float.MinValue,
            chanceToSpawnFromRock > 0 ? chanceToSpawnFromRock : float.MinValue,
            chanceToSpawnFromEnemy > 0 ? chanceToSpawnFromEnemy : float.MinValue,
            chanceToSpawnFromBush > 0 ? chanceToSpawnFromBush : float.MinValue
        );
        if (maxRarity <= 0.06f) return Rarity.Legendary;
        if (maxRarity <= 0.12f) return Rarity.Epic;
        if (maxRarity <= 0.25f) return Rarity.Rare;
        if (maxRarity <= 0.5f) return Rarity.Uncommon;
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
    
    public virtual string GetDescription(int stackCount = 1) {
        string desc = string.Empty;
        
        if (modifiesStats) {
            if (!Mathf.Approximately(critChance, 0f))              desc += $"\n{DisplayProbIncDec(GetCritChance(stackCount))} Crit Chance";
            if (!Mathf.Approximately(critMultiplier, 0f))          desc += $"\n{DisplayMultiplierIncDec(GetCritMultiplier(stackCount))} Crit Multiplier";
            if (!Mathf.Approximately(damageMultiplier, 0f))        desc += $"\n{DisplayMultiplierIncDec(GetDamageMultiplier(stackCount))} Damage";
            if (!Mathf.Approximately(fireratePercentage, 0f))      desc += $"\n{DisplayProbIncDec(GetFireratePercentage(stackCount))} Firerate";
            if (!Mathf.Approximately(movementSpeedPercentage, 0f)) desc += $"\n{DisplayProbIncDec(GetMovementSpeedPercentage(stackCount))} Movement Speed";
            if (!Mathf.Approximately(projectileCount, 0f))         desc += $"\n{DisplayIncDec(GetProjectileCount(stackCount))} Projectile Count";
            if (!Mathf.Approximately(rangePercentage, 0f))         desc += $"\n{DisplayProbIncDec(GetRangePercentage(stackCount))} Range";
        }

        bool removeFirstNewLine = !string.IsNullOrEmpty(desc);
        if (removeFirstNewLine) {
            desc = desc.Remove(0, 1);
        }

        string upgradeDesc = GetUpgradeDescription(stackCount);
        if (!string.IsNullOrEmpty(upgradeDesc)) {
            desc += string.IsNullOrEmpty(desc) ? upgradeDesc : $"\n{upgradeDesc}"; 
        }
        
        if (!string.IsNullOrEmpty(description)) {
            desc += string.IsNullOrEmpty(desc) ? description : $"\n{description}";
        }
        else if (!string.IsNullOrEmpty(itemGroupProps?.description)) {
            desc += string.IsNullOrEmpty(desc) ? itemGroupProps.description : $"\n{itemGroupProps.description}";
        }
        
        return !string.IsNullOrEmpty(desc) ? desc : "Item is missing description";
    }
    
    public float GetCritChance(int stackCount) => critChance * stackCount;
    public float GetCritMultiplier(int stackCount) => critMultiplier * stackCount;
    public float GetDamageMultiplier(int stackCount) => damageMultiplier * stackCount;
    public float GetFireratePercentage(int stackCount) => fireratePercentage * stackCount;
    public float GetMovementSpeedPercentage(int stackCount) => movementSpeedPercentage * stackCount;
    public float GetProjectileCount(int stackCount) => projectileCount * stackCount;
    public float GetRangePercentage(int stackCount) => rangePercentage * stackCount;

    protected virtual string GetUpgradeDescription(int stackCount) {
        return string.Empty;
    }
    
#if UNITY_EDITOR
    
    private void OnValidate() {
        rarity = GetRarity();
    }
    
    [OnValueChanged(nameof(buyPrice))]
    private void AutoSetSellPriceOnBuyPriceChanged() {
        if (this is EyeUpgradeItem) {
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
        gameInstance.TryAddItemToInventory(gameInstance.stashInventory, this, 1);
    }

#endif

}
