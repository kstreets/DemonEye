using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "SoulSuckerTrinket", menuName = "Scriptable Objects/Trinkets/SoulSuckerTrinket")]
public class SoulSuckerTrinket : Trinket {

    public float soulsPerKillMultiplier = 1.5f;

    public override string GetDescription(int stackCount = 1) {
        return $"{DisplayMultiplier(soulsPerKillMultiplier)} souls earned per kill";
    }

}
