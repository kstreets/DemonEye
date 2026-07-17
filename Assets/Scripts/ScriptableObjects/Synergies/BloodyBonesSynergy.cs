using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "BloodyBones", menuName = "Scriptable Objects/Synergy/BloodyBones")]
public class BloodyBonesSynergy : Synergy {
    
    public struct InstanceData { }
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance) {
        eyeInstance.bloodyBonesSynergy = new();
    }

    public override string GetDescription() {
        return "Bone fragments from bleeding enemies inflicts bleed";
    }
}
