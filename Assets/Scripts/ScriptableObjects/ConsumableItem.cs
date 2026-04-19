using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "ConsumableItem", menuName = "Scriptable Objects/ConsumableItem")]
public class ConsumableItem : Item {

    public enum AnimationType { Drink, Eat, Bandage }

    public AnimationType animationType;
    public int healingAmount;
    public int bandageAmount;
    public float healingDuration;
    
    public override string GetDescription(int stackCount = 1) {
        return animationType switch {
            AnimationType.Drink => $"Drink and restore {DisplayHealth(healingAmount)} health over {DisplaySeconds(healingDuration)}",
            AnimationType.Eat => $"Eat and restore {DisplayHealth(healingAmount)} health over {DisplaySeconds(healingDuration)}",
            // We kinda just assume the bandage is the only one stopping bleeds
            AnimationType.Bandage => $"Stops bleeding and restores {DisplayHealth(healingAmount)} health over {DisplaySeconds(healingDuration)}", 
            _ => $"Restores {DisplayHealth(healingAmount)} health over {DisplaySeconds(healingDuration)}"
        };
    }

}
