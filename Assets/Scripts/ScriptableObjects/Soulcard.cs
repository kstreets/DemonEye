using UnityEngine;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class Soulcard : Item {

    public virtual void AddInstanceToEnemy(GameManager.Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }

    public override string GetDescription() {
        return GetModifierDescription(1);
    }

    public virtual string GetModifierDescription(int stackCount) {
        return "Modifier has no description";
    }
    
}