using UnityEngine;
using UnityEngine.Assertions;
using static Game;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class Soulcard : Item {

    public MechanicDesc relativeMechanicDesc;
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }

    protected int TaperInteger(int value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return Mathf.RoundToInt(value * Mathf.Pow(stackCount, taper));
    }

    protected float TaperFloat(float value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return value * Mathf.Pow(stackCount, taper);
    }
    
}