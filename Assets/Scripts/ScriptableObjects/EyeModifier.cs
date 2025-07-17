using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "EyeModifier", menuName = "Scriptable Objects/EyeModifier")]
public class EyeModifier : UuidScriptableObject {

    public bool alwaysActive;
    
    [ShowIf("alwaysActive", false)] [Range(0f, 1f)]
    public float activationProbability;

}
