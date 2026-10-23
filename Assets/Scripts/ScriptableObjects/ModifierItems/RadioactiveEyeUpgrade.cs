using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "RadioactiveModifier", menuName = "Scriptable Objects/Modifiers/RadioactiveModifier")]
public class RadioactiveEyeUpgrade : EyeUpgrade {

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        // TODO
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return "";
    }
    
}
