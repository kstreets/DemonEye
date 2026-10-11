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

    [Header("Equipment")]
    [Tooltip("Eye Upgrades or Augments forged into the equipped Demon Eye. Leave all empty to not equip a Demon Eye")]
    public UuidScriptableObject[] demonEyeUpgrades = new UuidScriptableObject[GameData.Config.demonEyeCoreUpgradeCount];
    public BackpackItem backpack;
    public Trinket trinket;

    [Header("Items")]
    public List<TestItemInstance> items;

    public bool HasDemonEye {
        get {
            foreach (UuidScriptableObject upgrade in demonEyeUpgrades) {
                if (upgrade != null) return true;
            }
            return false;
        }
    }

    private void OnValidate() {
        if (demonEyeUpgrades == null || demonEyeUpgrades.Length != GameData.Config.demonEyeCoreUpgradeCount) {
            Array.Resize(ref demonEyeUpgrades, GameData.Config.demonEyeCoreUpgradeCount);
        }

        for (int i = 0; i < demonEyeUpgrades.Length; i++) {
            if (demonEyeUpgrades[i] == null || demonEyeUpgrades[i] is IEyeUpgrade) continue;
            Debug.LogWarning($"{demonEyeUpgrades[i].name} is not an Eye Upgrade or Augment", this);
            demonEyeUpgrades[i] = null;
        }
    }

    [VInspector.Button]
    public void EquipOnPlayer() {
        if (!Application.isPlaying) {
            Debug.Log("Can only equip testing inventory when game is playing");
            return;
        }

        Game.gameInstance.LoadTestingInventoryOntoPlayer(this);
    }

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
