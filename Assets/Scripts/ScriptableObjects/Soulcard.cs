using UnityEngine;
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

    
}