using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Assertions;
using static GameData;

public partial class Game {
    
    private void BuildSavePaths() {
        savePaths.playerInventory = $"{Application.persistentDataPath}/playerInventory";
        savePaths.stashInventory = $"{Application.persistentDataPath}/stashInventory";
        savePaths.eyeForgeInventory = $"{Application.persistentDataPath}/eyeForgeInventory";
        savePaths.player = $"{Application.persistentDataPath}/player";
        savePaths.quest = $"{Application.persistentDataPath}/quests";
        savePaths.trader = $"{Application.persistentDataPath}/trader";
        savePaths.traderInventory = $"{Application.persistentDataPath}/traderInventory";
        savePaths.mapUnlocks = $"{Application.persistentDataPath}/maps";
        savePaths.persistentFlags = $"{Application.persistentDataPath}/persistentFlags";
    }

    private string GetInventorySavePath(Inventory inventory) {
        if (inventory == inventories.player) return savePaths.playerInventory;
        if (inventory == inventories.stash) return savePaths.stashInventory;
        if (inventory == inventories.eyeForge) return savePaths.eyeForgeInventory;
        if (inventory == inventories.trader) return savePaths.traderInventory;
        Assert.IsTrue(false, "Inventory does not have associated save path");
        return string.Empty;
    }

    private void SaveToFile(string path, object obj) {
        if (obj == null) return;
        BinaryFormatter bf = new();
        using FileStream file = File.Create(path);
        bf.Serialize(file, obj);
    }

    private T LoadFromFileOrCreateNew<T>(string path) where T : class, new() {
        return LoadFromFile<T>(path) ?? new T();
    }
    
    private T LoadFromFile<T>(string path) where T : class {
        if (File.Exists(path)) {
            BinaryFormatter bf = new();
            using FileStream file = File.Open(path, FileMode.Open);
            return (T)bf.Deserialize(file);
        }
        return null;
    }
    
    [Serializable]
    private class PersistentFlagsSaveWrapper {
        public int persistentFlags;
    }

    private void SavePersistentFlags() {
        PersistentFlagsSaveWrapper wrapper = new() {
            persistentFlags = (int)persistentFlags,
        };
        SaveToFile(savePaths.persistentFlags, wrapper);
    }
    
    private void LoadPersistentFlags() {
        PersistentFlagsSaveWrapper wrapper = LoadFromFileOrCreateNew<PersistentFlagsSaveWrapper>(savePaths.persistentFlags);
        persistentFlags = (PersistentFlags)wrapper.persistentFlags;
    }

}
