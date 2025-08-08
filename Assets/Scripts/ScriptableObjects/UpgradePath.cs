using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradePath", menuName = "Scriptable Objects/UpgradePath")]
public class UpgradePath : ScriptableObject {

    [Serializable]
    public class Requirement {
        public Item item;
        public int count;
    }
    
    [Serializable]
    public class UpgradeRequirements {
        public List<Requirement> requirements;
    }

    public List<UpgradeRequirements> pathUpgrades;
}
