using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BackpackItem", menuName = "Scriptable Objects/BackpackItem")]
public class BackpackItem : Item {

    public int additionalStorageSlots;
    
    public override string GetDescription() {
        return $"{DisplayIncrease(additionalStorageSlots)} inventory slots";
    }
    
}
