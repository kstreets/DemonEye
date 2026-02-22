using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BleedCritModifier", menuName = "Scriptable Objects/Modifiers/BleedCritModifier")]
public class BleedCritModifierItem : ModifierItem {
    
    public struct InstanceData {
        public float probability;
    }

    public float probability;

    public override void AddInstanceToTrinketPower(ref TrinketPowers trinkets, int stackCount) {
        trinkets.bleedCrit = new() {
            probability = GetProbability(stackCount),
        };
    }

    protected override string BuildDescription(int stackCount) {
        return $"{DisplayProbIncrease(GetProbability(stackCount))} critical strike chance on a bleeding enemy";
    }
    
    private float GetProbability(int stackCount) {
        return TaperFloat(probability, stackCount, 0.5f);
    }
    
}
