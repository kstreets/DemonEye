using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestingInventory", menuName = "Scriptable Objects/TestingInventory")]
public class TestingInventory : ScriptableObject {

    [Serializable]
    public struct TestInventoryItem {
        public Item item;
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
            gameManager?.TryAddItemToInventory(gameManager.playerInventory, inventoryItem.item, inventoryItem.count);
        }
    }
    
    [VInspector.Button]
    public void AddToStashInventory() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to stash inventory when game is playing");
            return;
        }
        
        GameManager gameManager = FindAnyObjectByType(typeof(GameManager)) as GameManager;
        foreach (TestInventoryItem inventoryItem in items) {
            gameManager?.TryAddItemToInventory(gameManager.stashInventory, inventoryItem.item, inventoryItem.count);
            gameManager?.RefreshInventoryDisplay(gameManager.stashInventory);
        }
    }

}
