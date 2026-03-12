using System.Collections.Generic;
using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class ModifierItem : Item {

    public MechanicDesc relativeMechanicDesc;
    public List<Augment> augments;
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }

}