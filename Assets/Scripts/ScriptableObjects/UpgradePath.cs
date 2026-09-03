using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradePath", menuName = "Scriptable Objects/UpgradePath")]
public class UpgradePath : ScriptableObject {

    [Serializable]
    public class UpgradeRequirements {
        public List<ItemWithCount> requirements;
    }

    public List<UpgradeRequirements> pathUpgrades;
    
}
