using UnityEngine;
using UnityEngine.Assertions;
using static Game;

[CreateAssetMenu(fileName = "CannonShot", menuName = "Scriptable Objects/Modifiers/CannonShot")]
public class CannonShot : EyeUpgrade {
    
    public struct InstanceData {
        public float damageMulti;
        public float blastDamageMulti;
        public float blastRadius;
    }

    public float firerateMulti;
    public float damageMulti;
    public float blastDamageMulti;
    public float blastRadius;
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.cannonShot = new() {
            damageMulti = GetDamageMultiplier(stackCount),
            blastDamageMulti = GetBlastDamageMultiplier(stackCount),
            blastRadius = blastRadius,
        };
    }

    protected override string GetUpgradeDescription(int stackCount) {
        return $"Every shot releases a shockwave that deals {DisplayMultiplier(GetBlastDamageMultiplier(stackCount))} damage within a {DisplayDistance(blastRadius)} radius";
    }

    public override float GetDamageMultiplier(int stackCount) {
        return TaperFloat(damageMulti, stackCount, 0.35f);
    }

    public override float GetFireratePercentage(int stackCount) {
        Assert.IsTrue(firerateMulti < 0f, "Firerate for this eye upgrade should be negative as we want to reduce firerate");
        return TaperFloat(firerateMulti, stackCount, 0.35f);
    }

    float GetBlastDamageMultiplier(int stackCount) {
        return TaperFloat(blastDamageMulti, stackCount, 0.45f);
    }
    
}
