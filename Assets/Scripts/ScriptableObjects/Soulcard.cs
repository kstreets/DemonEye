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

    protected string DisplayProb(float probability) {
        return ColorText($"{Mathf.FloorToInt(probability * 100f)}%", instance.styles.increaseDescColor);
    }
    
    protected string DisplayProbIncrease(float probability) {
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", instance.styles.increaseDescColor);
    }
    
    protected string DisplayProbDecrease(float probability) {
        return ColorText($"-{Mathf.FloorToInt(probability * 100f)}%", instance.styles.decreaseDescColor);
    }

    
    protected string DisplayNumber(int number) {
        return ColorText(number.ToString(), instance.styles.increaseDescColor);
    }
    
    protected string DisplayNumber(float number) {
        return ColorText(number.ToString("0.00"), instance.styles.increaseDescColor);
    }


    protected string DisplayIncrease(int amount) {
        return ColorText($"+{amount}", instance.styles.increaseDescColor);
    }
    
    protected string DisplayIncrease(float amount) {
        return ColorText($"+{amount:0.00}", instance.styles.increaseDescColor);
    }
    

    protected string DisplayMultiplier(float multiplier) {
        return ColorText($"{multiplier:0.00}x", instance.styles.increaseDescColor);
    }
    
}