using UnityEngine;

public static class Masks {
    
    public static LayerMask EnemyMask { get; }
    public static LayerMask ItemMask { get; }
    
    static Masks() {
        string[] enemy = { "Enemy" };
        EnemyMask = CreateLayerMask(enemy);
        
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
