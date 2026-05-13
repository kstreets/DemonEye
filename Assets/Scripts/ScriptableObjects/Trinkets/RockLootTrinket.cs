using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "RockLootTrinket", menuName = "Scriptable Objects/Trinkets/RockLootTrinket")]
public class RockLootTrinket : Trinket {
    
    public float chanceForSecondDrop;
    
    public override string GetDescription(int stackCount = 1) {
        return $"Rocks have a {DisplayProb(chanceForSecondDrop)} chance to drop a second item";
    }
    
}
