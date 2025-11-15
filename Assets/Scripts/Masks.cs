using UnityEngine;

public static class Masks {
    
    public static LayerMask StaticLevelMask { get; }
    public static LayerMask PlayerMask { get; }
    public static LayerMask EnemyMask { get; }
    public static LayerMask DamagableMask { get; }
    public static LayerMask MineableMask { get; }
    public static LayerMask ItemMask { get; }
    public static LayerMask EnemySpacerMask { get; }
    
    static Masks() {
        StaticLevelMask = CreateLayerMask("Default");
        PlayerMask = CreateLayerMask("Player");
        EnemyMask = CreateLayerMask("Enemy");
        DamagableMask = CreateLayerMask("Enemy", "Mineable");
        MineableMask = CreateLayerMask("Mineable");
        ItemMask = CreateLayerMask("Item");
        EnemySpacerMask = CreateLayerMask("EnemySpacers");
    }

    private static LayerMask CreateLayerMask(params string[] names) {
        int mask = 0;
        foreach (string name in names) {
            mask |= 1 << LayerMask.NameToLayer(name);
        }
        return new() { value = mask };
    }
    
}
