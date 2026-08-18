using UnityEngine;

[CreateAssetMenu(fileName = "DemonEyeLevels", menuName = "Scriptable Objects/DemonEyeLevels")]
public class DemonEyeLevels : ScriptableObject {
    
    public const int numLevels = 3;
    public int[] prefixedLevels = new int[numLevels];
    
    public Sprite[] levelSprites;
    
}
