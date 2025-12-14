using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatUpgradePath", menuName = "Scriptable Objects/StatUpgradePath")]
public class SkillUpgradePath : ScriptableObject {

    public List<int> soulsNeededPerLevel;
    public int MaxLevel => soulsNeededPerLevel.Count;

}
