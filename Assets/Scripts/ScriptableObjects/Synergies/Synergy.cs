using UnityEngine;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "Synergy", menuName = "Scriptable Objects/Synergy")]
public class Synergy : UuidScriptableObject {
    
#if UNITY_EDITOR
    [ReadOnly] [SerializeField] 
    private Item.Rarity rarity;
#endif
    
    [Range(0f, 1f)]
    public float probability;
    public EyeUpgrade[] amongUpgrades;
    
    public virtual void AddInstanceToEnemy(Enemy enemy) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance) { }
    
    public virtual string GetDescription() {
        return "No description for synergy";
    }
    
#if UNITY_EDITOR
    private void OnValidate() {
        rarity = Item.GetRarity(probability);
    }
#endif
    
}
