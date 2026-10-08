using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "Thorns", menuName = "Scriptable Objects/Trinkets/Thorns")]
public class Thorns : Trinket {
    
    public float cooldownTime;
    public string activationPopUpText;
    
    public override string GetDescription(int stackCount = 1) {
        return $"Damage enemies on contact without taking collision damage. Has a cooldown time of {DisplaySeconds(cooldownTime)}";
    }
    
}