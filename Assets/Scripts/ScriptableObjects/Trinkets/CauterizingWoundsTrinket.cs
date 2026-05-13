using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "CauterizingWoundsTrinket", menuName = "Scriptable Objects/Trinkets/CauterizingWoundsTrinket")]
public class CauterizingWoundsTrinket : Trinket {
    
    public float chancePerShotToStopBleeding;
    public string activationText;
    
    public override string GetDescription(int stackCount = 1) {
        return $"Every shot has a {DisplayProb(chancePerShotToStopBleeding)} chance to stop an active bleed";
    }
    
}