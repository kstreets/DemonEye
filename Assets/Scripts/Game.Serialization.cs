using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Game {
    
    private void BuildSavePaths() {
        gameData.savePaths.playerInventory = $"{Application.persistentDataPath}/playerInventory";
        gameData.savePaths.stashInventory = $"{Application.persistentDataPath}/stashInventory";
        gameData.savePaths.eyeForgeInventory = $"{Application.persistentDataPath}/eyeForgeInventory";
        gameData.savePaths.player = $"{Application.persistentDataPath}/player";
        gameData.savePaths.quest = $"{Application.persistentDataPath}/quests";
        gameData.savePaths.trader = $"{Application.persistentDataPath}/trader";
        gameData.savePaths.traderInventory = $"{Application.persistentDataPath}/traderInventory";
        gameData.savePaths.mapUnlocks = $"{Application.persistentDataPath}/maps";
    }

    private string GetInventorySavePath(Inventory inventory) {
        if (inventory == playerInventory) return gameData.savePaths.playerInventory;
        if (inventory == stashInventory) return gameData.savePaths.stashInventory;
        if (inventory == eyeForgeInventory) return gameData.savePaths.eyeForgeInventory;
        if (inventory == traderInventory) return gameData.savePaths.traderInventory;
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
    private class MapSaves {
        public List<bool> unlockStates;
    }
    
    private void SaveMaps() {
        MapSaves mapSaves = new() {
            unlockStates = new(maps.Count),
        };
        foreach (MapData mapData in maps) {
            mapSaves.unlockStates.Add(mapData.isUnlocked);    
        }
        SaveToFile(gameData.savePaths.mapUnlocks, mapSaves);
    }
    
    private void LoadAndAssignMapSaves(List<MapData> mapDatas) {
        MapSaves mapSaves = LoadFromFile<MapSaves>(gameData.savePaths.mapUnlocks);
        if (mapSaves == null) return;
        
        if (mapDatas.Count != mapSaves.unlockStates.Count) {
            Debug.Log("Maps save does not match current maps. Saves are not going to be loaded");
            return;
        }
        
        for (int i = 0; i < mapDatas.Count; i++) {
            mapDatas[i].isUnlocked = mapSaves.unlockStates[i];
        }
    }

}
