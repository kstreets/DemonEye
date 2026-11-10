using UnityEngine;

public static class Masks {
    
    public static LayerMask StaticLevelMask { get; }
    public static LayerMask PlayerHurtMask { get; }
    public static LayerMask EnemyMask { get; }
    public static LayerMask EnemyWorldColliderMask { get; }
    public static LayerMask DamagableMask { get; }
    public static LayerMask MineableMask { get; }
    public static LayerMask ItemMask { get; }
    
    static Masks() {
        StaticLevelMask = CreateLayerMask("Default");
        PlayerHurtMask = CreateLayerMask("PlayerHurtBox");
        EnemyMask = CreateLayerMask("Enemy", "EnemyNonTarget");
        EnemyWorldColliderMask = CreateLayerMask("EnemyWorldCollider");
        DamagableMask = CreateLayerMask("Enemy", "Mineable");
        MineableMask = CreateLayerMask("Mineable");
        ItemMask = CreateLayerMask("Item");
    }

    private static LayerMask CreateLayerMask(params string[] names) {
        int mask = 0;
        foreach (string name in names) {
            mask |= 1 << LayerMask.NameToLayer(name);
        }
        return new() { value = mask };
    }
    
}
