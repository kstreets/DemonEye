using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ArmorItem", menuName = "Scriptable Objects/ArmorItem")]
public class ArmorItem : Item {

    protected override string GetModifierDescription(int stackCount) {
        return $"Reduces incoming damage by {DisplayProb(armorPercent)}";
    }
    
}