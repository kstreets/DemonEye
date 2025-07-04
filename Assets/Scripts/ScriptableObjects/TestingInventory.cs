using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestingInventory", menuName = "Scriptable Objects/TestingInventory")]
public class TestingInventory : ScriptableObject {

    [Serializable]
    public struct TestInventoryItem {
        public ItemData itemData;
        public int count;
    }
    
    public List<TestInventoryItem> items;

}
