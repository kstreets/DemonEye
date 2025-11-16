using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableItem", menuName = "Scriptable Objects/ConsumableItem")]
public class ConsumableItem : Item {

    public enum AnimationType { Drink, Eat, Bandage }

    public AnimationType animationType;
    public int healingAmount;
    public int bandageAmount;

}
