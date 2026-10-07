using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "PortalBlastTrinket", menuName = "Scriptable Objects/Trinkets/PortalBlastTrinket")]
public class PortalBlastTrinket : Trinket {

    public float killRadius = 1f;
    public string activationPopUpText;

    public override string GetDescription(int stackCount = 1) {
        return $"Summoning an exit portal kills all enemies within {DisplayDistance(killRadius)} of it";
    }

}
