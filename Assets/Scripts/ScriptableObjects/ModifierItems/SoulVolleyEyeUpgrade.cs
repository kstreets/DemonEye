using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "SoulVolley", menuName = "Scriptable Objects/EyeUpgrade/SoulVolley")]
public class SoulVolleyEyeUpgrade : EyeUpgrade {
    
    public Styles styles;
    public int volleyCount;
    public int soulsNeededPerVolley;
    public float projDamageMultiplier;
    
    public struct InstanceData {
        public int volleyCount;
        public int soulsNeededPerVolley;
        public float damageMultiplier;
        public int curSoulsTowardsVolley;
    }
    
    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.soulVolley = new InstanceData {
            volleyCount = GetVolleyCount(stackCount),
            soulsNeededPerVolley = GetSoulsNeeded(stackCount),
            damageMultiplier = GetProjDamageMultiplier(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        int soulsNeeded = GetSoulsNeeded(stackCount);
        int count = GetVolleyCount(stackCount);
        float damageMulti = GetProjDamageMultiplier(stackCount);
        return $"Every <sprite=1>{DisplayNumber(soulsNeeded, styles.soulCurrencyColor)} gained releases a volley of {DisplayNumber(count)} burning souls, each dealing {DisplayMultiplier(damageMulti)} damage";
    }
    
    private int GetVolleyCount(int stackCount) {
        return TaperInteger(volleyCount, stackCount, 0.75f);
    }
    
    private int GetSoulsNeeded(int stackCount) {
        return TaperInteger(soulsNeededPerVolley, stackCount, 0.15f);
    }
    
    private float GetProjDamageMultiplier(int stackCount) {
        return TaperFloat(projDamageMultiplier, stackCount, 0.35f);
    }
    
}
