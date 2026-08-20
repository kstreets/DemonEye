using UnityEngine;

[CreateAssetMenu(fileName = "TraderLevels", menuName = "Scriptable Objects/TraderLevels")]
public class TraderLevels : ScriptableObject {

    // public const int numTraderLevels = 3;
    // public int[] prefixedSumRepForLevel = new int[numTraderLevels];
    public PrefixedLevels prefixedLevels;

}
