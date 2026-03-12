using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BackpackItem", menuName = "Scriptable Objects/BackpackItem")]
public class BackpackItem : Item {

    public int additionalStorageSlots;
    
    protected override string GetModifierDescription(int stackCount) {
        return $"{DisplayIncrease(additionalStorageSlots)} inventory slots";
    }
    
}
