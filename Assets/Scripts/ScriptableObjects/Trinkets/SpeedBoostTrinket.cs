using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "SpeedBoostTrinket", menuName = "Scriptable Objects/Trinkets/SpeedBoostTrinket")]
public class SpeedBoostTrinket : Trinket {
    
    public float duration;
    public float percentSpeedIncrease;
    public int killsPerBoost;
    
    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayProbIncrease(percentSpeedIncrease)} speed for {DisplaySeconds(duration)} for every {DisplayNumber(killsPerBoost)} kills";
    }
    
}