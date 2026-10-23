using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BlackHoleModifier", menuName = "Scriptable Objects/Modifiers/BlackHoleModifier")]
public class BlackHoleEyeUpgrade : EyeUpgrade {

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        // TODO
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return "";
    }
    
}
