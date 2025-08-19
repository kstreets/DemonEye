using UnityEngine;

[CreateAssetMenu(fileName = "BaseCharacterStats", menuName = "Scriptable Objects/BaseCharacterStats")]
public class BaseCharacterStats : ScriptableObject {
    
    public const int maxStatValue = 10;
    
    [Range(1, maxStatValue)] public int agility;
    [Range(1, maxStatValue)] public int strength;
    [Range(1, maxStatValue)] public int health;
    [Range(1, maxStatValue)] public int luck;

}
