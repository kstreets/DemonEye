using UnityEngine;

[CreateAssetMenu(fileName = "DemonClaw", menuName = "Scriptable Objects/DemonClaw")]
public class DemonClaw : PassiveItem {

    public int bleedDamageIncreasePerItem;

    public override string GetStackDescription(int stackCount) {
        return $"+{GetBleedDamageIncrease(stackCount)} bleed damage per item";
    }

    public int GetBleedDamageIncrease(int stackCount) {
        return bleedDamageIncreasePerItem * stackCount;
    }
    
}
