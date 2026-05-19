using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestingInventory", menuName = "Scriptable Objects/TestingInventory")]
public class TestingInventory : ScriptableObject {

    [Serializable]
    public struct TestItemInstance {
        public Item item;
        public int count;
    }
    
    public List<TestItemInstance> items;

    [VInspector.Button]
    public void AddToPlayerInventory() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to player inventory when game is playing");
            return;
        }
        
        Game gameManager = FindAnyObjectByType(typeof(Game)) as Game;
        foreach (TestItemInstance inventoryItem in items) {
            gameManager?.TryAddItemToInventory(gameManager.inventories.player, inventoryItem.item, inventoryItem.count);
        }
    }
    
    [VInspector.Button]
    public void AddToStashInventory() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to stash inventory when game is playing");
            return;
        }
        
        Game gameManager = FindAnyObjectByType(typeof(Game)) as Game;
        foreach (TestItemInstance inventoryItem in items) {
            gameManager?.TryAddItemToInventory(gameManager.inventories.stash, inventoryItem.item, inventoryItem.count);
        }
    }

}
