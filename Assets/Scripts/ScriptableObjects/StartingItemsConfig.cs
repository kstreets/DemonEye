using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StartingItemsConfig", menuName = "Scriptable Objects/StartingItemsConfig")]
public class StartingItemsConfig : ScriptableObject {

    [Serializable]
    public class ItemConfig {
        public Item item;
        public int count;
    }
    
    public List<ItemConfig> configs;
    
}