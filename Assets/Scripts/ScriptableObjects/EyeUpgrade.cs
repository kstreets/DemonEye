using UnityEngine;
using static Game;

[CreateAssetMenu(fileName = "EyeUpgrade", menuName = "Scriptable Objects/EyeUpgrade")]
public class EyeUpgrade : Item {

    public MechanicDesc relativeMechanicDesc;
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }
    
}