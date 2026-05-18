using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Game {
    
    private string playerInventorySavePath;
    private string stashSavePath;
    private string crucibleSavePath;
    private string playerSavePath;
    private string questSavePath;
    private string traderSavePath;
    private string traderInventorySavePath;
    private string mapUnlocksSavePath;
    private string tutorialSavePath;
    private List<ItemInstance> cachedInventoryForSaving = new(50);
    
    private void BuildSavePaths() {
        playerInventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
        crucibleSavePath = $"{Application.persistentDataPath}/crucible";
        playerSavePath = $"{Application.persistentDataPath}/player";
        questSavePath = $"{Application.persistentDataPath}/quests";
        traderSavePath = $"{Application.persistentDataPath}/traders";
        traderInventorySavePath = $"{Application.persistentDataPath}/traderInventory";
        mapUnlocksSavePath = $"{Application.persistentDataPath}/maps";
        tutorialSavePath = $"{Application.persistentDataPath}/tutorial";
    }

    private string GetInventorySavePath(Inventory inventory) {
        if (inventory == playerInventory) return playerInventorySavePath;
        if (inventory == stashInventory) return stashSavePath;
        if (inventory == crucibleInventory) return crucibleSavePath;
        if (inventory == traderInventory) return traderInventorySavePath;
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
    private class PlayerSaveData {
        public int health;
        public int crucibleLevel;
        public int soulCurrency;
        public int coinCurrency;
        
        public int hasteSkillLevel;
        public int intellectSkillLevel;
        public int lifeBloodSkillLevel;
        public int strengthSkillLevel;
    }

    private void SavePlayerData() {
        PlayerSaveData data = new() {
            health = player.health,
            soulCurrency = player.soulCurrency,
            coinCurrency = player.coinCurrency,
            hasteSkillLevel = player.hasteSkillLevel,
            intellectSkillLevel = player.intellectSkillLevel,
            lifeBloodSkillLevel = player.lifeBloodSkillLevel,
            strengthSkillLevel = player.strengthSkillLevel,
        };
        SaveToFile(playerSavePath, data);
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
        SaveToFile(mapUnlocksSavePath, mapSaves);
    }
    
    private void LoadAndAssignMapSaves(List<MapData> mapDatas) {
        MapSaves mapSaves = LoadFromFile<MapSaves>(mapUnlocksSavePath);
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
