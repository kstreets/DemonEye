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

    [VInspector.Button]
    public void AddToPlayerInventory() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to player inventory when game is playing");
            return;
        }
        
        GameManager gameManager = FindAnyObjectByType(typeof(GameManager)) as GameManager;
        foreach (TestInventoryItem inventoryItem in items) {
            gameManager?.AddItemToInventory(gameManager.playerInventory, inventoryItem.itemData, inventoryItem.count);
        }
    }

}
