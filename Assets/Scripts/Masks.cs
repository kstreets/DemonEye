using UnityEngine;

public static class Masks {
    
    public static LayerMask PlayerMask { get; }
    public static LayerMask EnemyMask { get; }
    public static LayerMask DamagableMask { get; }
    public static LayerMask ItemMask { get; }
    
    static Masks() {
        string[] player = { "Player" };
        PlayerMask = CreateLayerMask(player);
        
        string[] enemy = { "Enemy" };
        EnemyMask = CreateLayerMask(enemy);
        
        string[] damagable = { "Enemy", "Mineable" };
        DamagableMask = CreateLayerMask(damagable);
        
        string[] item = { "Item" };
        ItemMask = CreateLayerMask(item);
    }

    private static LayerMask CreateLayerMask(string[] names) {
        int mask = 0;
        foreach (string name in names) {
            mask |= 1 << LayerMask.NameToLayer(name);
        }
        return new() { value = mask };
    }
    
}
