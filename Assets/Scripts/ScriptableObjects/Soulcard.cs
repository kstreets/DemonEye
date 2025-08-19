using UnityEngine;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class Soulcard : Item {

    public virtual void AddInstanceToEnemy(GameManager.Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) { }

    public override string GetDescription() {
        return GetStackDescription(1);
    }

    public virtual string GetStackDescription(int stackCount) {
        return "Modifier has no description";
    }

    public static string DisplayProbability(float probability) {
        return $"{Mathf.FloorToInt(probability * 100f)}%";
    }
    
}