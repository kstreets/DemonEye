using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class Soulcard : Item {

    public MechanicDesc relativeMechanicDesc;
    
    public virtual void AddInstanceToEnemy(Game.Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(Game.DemonEyeInstance eyeInstance, int stackCount) { }

    public override string GetDescription() {
        return GetStackDescription(1);
    }

    public virtual string GetStackDescription(int stackCount) {
        return "Modifier has no description";
    }

    protected int TaperInteger(int value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return Mathf.RoundToInt(value * Mathf.Pow(stackCount, taper));
    }

    protected float TaperFloat(float value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return value * Mathf.Pow(stackCount, taper);
    }
    
#if UNITY_EDITOR
    
    [Button]
    private void AutoCalculateSellPrice() {
        sellPrice = Mathf.RoundToInt(buyPrice * 0.25f);     
        EditorUtility.SetDirty(this);
    }
    
#endif



}