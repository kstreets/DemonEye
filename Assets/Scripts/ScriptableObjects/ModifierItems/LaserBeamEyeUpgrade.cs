using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "LaserBeamModifier", menuName = "Scriptable Objects/Modifiers/LaserBeamModifier")]
public class LaserBeamEyeUpgrade : EyeUpgrade {

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        // TODO
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return "";
    }
    
}
