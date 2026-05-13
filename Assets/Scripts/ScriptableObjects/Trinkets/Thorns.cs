using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "Thorns", menuName = "Scriptable Objects/Trinkets/Thorns")]
public class Thorns : Trinket {
    
    public float cooldownTime;
    public string activationPopUpText;
    
    public override string GetDescription(int stackCount = 1) {
        return $"Upon taking damage, deal the same amount of damage back to the enemy. Has a cooldown time of {DisplaySeconds(cooldownTime)}";
    }
    
}
