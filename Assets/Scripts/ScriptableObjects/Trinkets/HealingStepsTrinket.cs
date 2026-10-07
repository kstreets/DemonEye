using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "HealingStepsTrinket", menuName = "Scriptable Objects/Trinkets/HealingStepsTrinket")]
public class HealingStepsTrinket : Trinket {
    
    public int healing;
    public int stepsPerHeal;
    public string activationPopUpText;
    
    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayIncrease(healing)} health for every {DisplayNumber(stepsPerHeal)} steps taken";
    }
    
}
