using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
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

    public class GameState {
        public PlayerState playerState;
        
        public List<ItemInstance> playerInventoryItems;
        public List<ItemInstance> stashInventoryItems;
        public List<ItemInstance> traderInventoryItems;
        public List<ItemInstance> forgeInventoryItems;
        
        public PersistentFlags persistentFlags;
        
        public List<MapData.State> mapStates; 
        public List<Quest.State> questStates; 
    }

    public string GetGameStateSavePath() {
        return $"{Application.persistentDataPath}/save";
    }
    
    public void SaveGameState() {
        using FileStream stream = File.Open(GetGameStateSavePath(), FileMode.OpenOrCreate); 
        using BinaryWriter binWriter = new(stream);
        
        SerializePlayerState(binWriter, player);
        
        SaveInventory(binWriter, inventories.player);
        SaveInventory(binWriter, inventories.stash);
        SaveInventory(binWriter, inventories.trader);
        SaveInventory(binWriter, inventories.eyeForge);
        
        SerializeInt(binWriter, (int)persistentFlags);

        {
            using var _ = ListPool<MapData.State>.Get(out var mapStates);
            foreach (MapData map in config.maps) {
                mapStates.Add(map.state);
            }
            SaveList(binWriter, mapStates, SerializeMapState);
        }
        {
            using var _ = ListPool<Quest.State>.Get(out var questStates);
            foreach (Quest quest in quests.graph.unorderedQuests) {
                questStates.Add(quest.state);
            }
            SaveList(binWriter, questStates, SerializeQuestState);
        }
    }

    public GameState LoadGameState() {
        string savePath = GetGameStateSavePath();
        if (!File.Exists(savePath)) {
            return null;
        } 
        
        using FileStream stream = File.Open(savePath, FileMode.Open); 
        using BinaryReader binReader = new(stream);
        GameState gameState = new();
        
        gameState.playerState = DeserializePlayerState(binReader);
        
        gameState.playerInventoryItems = LoadInventory(binReader);
        gameState.stashInventoryItems = LoadInventory(binReader);
        gameState.traderInventoryItems = LoadInventory(binReader);
        gameState.forgeInventoryItems = LoadInventory(binReader);
        
        gameState.persistentFlags = (PersistentFlags)DeserializeInt(binReader);
        
        gameState.mapStates = LoadList(binReader, DeserializeMapState);
        gameState.questStates = LoadList(binReader, DeserializeQuestState);
        
        return gameState;
    }

    private void SerializePlayerState(BinaryWriter binWriter, Player player) {
        binWriter.Write(player.health);
        binWriter.Write(player.state.soulCurrency);
        binWriter.Write(player.state.coinCurrency);
        binWriter.Write(player.state.hasteSkillLevel);
        binWriter.Write(player.state.intellectSkillLevel);
        binWriter.Write(player.state.lifeBloodSkillLevel);
        binWriter.Write(player.state.strengthSkillLevel);
    }

    private PlayerState DeserializePlayerState(BinaryReader binReader) {
        return new() {
            initHealth = binReader.ReadInt32(),
            soulCurrency = binReader.ReadInt32(),
            coinCurrency = binReader.ReadInt32(),
            hasteSkillLevel = binReader.ReadInt32(),
            intellectSkillLevel = binReader.ReadInt32(),
            lifeBloodSkillLevel = binReader.ReadInt32(),
            strengthSkillLevel = binReader.ReadInt32(),
        };
    }
    
    private void SaveInventory(BinaryWriter writer, Inventory inventory) {
        using var _ = ListPool<ItemInstance>.Get(out var items);
        foreach (InventorySlot slot in inventory.slots) {
            items.Add(slot.itemInstance);
        }
        SaveList(writer, items, SerializeItemInstance);
    }

    private List<ItemInstance> LoadInventory(BinaryReader reader) {
        return LoadList(reader, DeserializeItemInstance);
    }

    private void SerializeMapState(BinaryWriter binWriter, MapData.State mapState) {
        binWriter.Write(mapState.isUnlocked);
        SaveList(binWriter, mapState.bloodMushroomSpawns, SerializeVector2);
    }

    private MapData.State DeserializeMapState(BinaryReader binReader) {
        MapData.State state = new();
        state.isUnlocked = binReader.ReadBoolean();
        state.bloodMushroomSpawns = LoadList(binReader, DeserializeVector2);
        return state;
    }

    private void SerializeQuestState(BinaryWriter binWriter, Quest.State questState) {
        binWriter.Write(questState.associatedQuestUuid);
        binWriter.Write(questState.submitted);
        SaveList(binWriter, questState.objectiveProgresses, SerializeInt);
    }

    private Quest.State DeserializeQuestState(BinaryReader binReader) {
        Quest.State state = new();
        state.associatedQuestUuid = binReader.ReadInt32();
        state.submitted = binReader.ReadBoolean();
        state.objectiveProgresses = LoadList(binReader, DeserializeInt);
        return state;
    }
    
    private void SaveList<T>(BinaryWriter writer, List<T> list, Action<BinaryWriter, T> serializeCallback) {
        int count = list?.Count ?? 0; 
        writer.Write(count);

        if (list == null) return;
        
        foreach (T elm in list) {
            bool isNull = elm == null;
            writer.Write(isNull);
            if (isNull) continue;
            serializeCallback(writer, elm);
        }
    }
    
    private List<T> LoadList<T>(BinaryReader reader, Func<BinaryReader, T> deserializeCallback) {
        int count = reader.ReadInt32();
        List<T> list = new(count);
        for (int i = 0; i < count; i++) {
            bool isNull = reader.ReadBoolean();
            if (isNull) {
                list.Add(default);
                continue;
            }
            list.Add(deserializeCallback(reader));
        }
        return list;
    }
    
    private void SerializeItemInstance(BinaryWriter binWriter, ItemInstance itemInstance) {
        binWriter.Write(itemInstance.itemOrInstanceUuid);
        SaveList(binWriter, itemInstance.nestedUuids, SerializeInt);
        binWriter.Write(itemInstance.count);
        binWriter.Write(itemInstance.isDemonEye);
    }
    
    private ItemInstance DeserializeItemInstance(BinaryReader binReader) {
        ItemInstance newInstance = new();
        newInstance.itemOrInstanceUuid = binReader.ReadInt32();
        newInstance.nestedUuids = LoadList(binReader, DeserializeInt);
        newInstance.count = binReader.ReadInt32();
        newInstance.isDemonEye = binReader.ReadBoolean();
        return newInstance;
    }

    private void SerializeVector2(BinaryWriter binWriter, Vector2 value) {
        binWriter.Write(value.x);
        binWriter.Write(value.y);
    }
    
    private Vector2 DeserializeVector2(BinaryReader binReader) {
        float x = binReader.ReadSingle();
        float y = binReader.ReadSingle();
        return new(x, y);
    }
    
    private void SerializeInt(BinaryWriter binWriter, int value) {
        binWriter.Write(value);
    }
    
    private int DeserializeInt(BinaryReader binReader) {
        return binReader.ReadInt32();
    }

}
