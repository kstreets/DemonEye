using UnityEngine;
using UnityEngine.Assertions;
using static Game;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class ModifierItem : Item {

    public MechanicDesc relativeMechanicDesc;
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }

}