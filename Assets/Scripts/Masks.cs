using UnityEngine;

public static class Masks {
    
    public static LayerMask EnemyMask { get; }
    
    static Masks() {
        string[] enemy = { "Enemy" };
        EnemyMask = CreateLayerMask(enemy);
    }

    private static LayerMask CreateLayerMask(string[] names) {
        int mask = 0;
        foreach (string name in names) {
            mask |= 1 << LayerMask.NameToLayer(name);
        }
        return new() { value = mask };
    }
    
}
