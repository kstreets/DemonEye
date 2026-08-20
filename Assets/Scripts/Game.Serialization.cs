using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using static GameData;

public partial class Game {
    
    public class GameState {
        public PersistentFlags persistentFlags;
        
        public PlayerState playerState;
        public Trader.State traderState;
        
        public List<ItemInstance> playerInventoryItems;
        public List<ItemInstance> stashInventoryItems;
        public List<ItemInstance> traderInventoryItems;
        public List<ItemInstance> forgeInventoryItems;
        
        public List<MapData.State> mapStates; 
        public List<Quest.State> questStates; 
    }

    public string GetGameStateSavePath() {
        return $"{Application.persistentDataPath}/save";
    }
    
    public void SaveGameState() {
        using FileStream stream = File.Open(GetGameStateSavePath(), FileMode.OpenOrCreate); 
        using BinaryWriter binWriter = new(stream);
        
        SerializeInt(binWriter, (int)persistentFlags);
        SerializePlayerState(binWriter, player);
        SerializeTraderState(binWriter, config.trader.state);
        
        SerializeInventory(binWriter, inventories.player);
        SerializeInventory(binWriter, inventories.stash);
        SerializeInventory(binWriter, inventories.trader);
        SerializeInventory(binWriter, inventories.eyeForge);

        // Maps
        {
            using var _ = ListPool<MapData.State>.Get(out var mapStates);
            foreach (MapData map in config.maps) {
                mapStates.Add(map.state);
            }
            SerializeList(binWriter, mapStates, SerializeMapState);
        }
        // Quests
        { 
            using var _ = ListPool<Quest.State>.Get(out var questStates);
            foreach (Quest quest in quests.graph.unorderedQuests) {
                questStates.Add(quest.state);
            }
            SerializeList(binWriter, questStates, SerializeQuestState);
        }
    }

    public GameState LoadGameState() {
        string savePath = GetGameStateSavePath();
        if (!File.Exists(savePath)) {
            return null;
        } 
        
        using FileStream stream = File.Open(savePath, FileMode.Open); 
        using BinaryReader binReader = new(stream);
        
        return new() {
            persistentFlags = (PersistentFlags)DeserializeInt(binReader),
            playerState = DeserializePlayerState(binReader),
            traderState = DeserializeTraderState(binReader),
            playerInventoryItems = DeserializeInventory(binReader),
            stashInventoryItems = DeserializeInventory(binReader),
            traderInventoryItems = DeserializeInventory(binReader),
            forgeInventoryItems = DeserializeInventory(binReader),
            mapStates = DeserializeList(binReader, DeserializeMapState),
            questStates = DeserializeList(binReader, DeserializeQuestState)
        };
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

    private void SerializeTraderState(BinaryWriter binWriter, Trader.State traderState) {
        binWriter.Write(traderState.reputation);
        binWriter.Write(traderState.raidsUntilRestock);
    }
    
    private Trader.State DeserializeTraderState(BinaryReader binReader) {
        return new() {
            reputation = binReader.ReadInt32(),
            raidsUntilRestock = binReader.ReadInt32(),
        };
    }
    
    private void SerializeInventory(BinaryWriter writer, Inventory inventory) {
        using var _ = ListPool<ItemInstance>.Get(out var items);
        foreach (InventorySlot slot in inventory.slots) {
            items.Add(slot.itemInstance);
        }
        SerializeList(writer, items, SerializeItemInstance);
    }

    private List<ItemInstance> DeserializeInventory(BinaryReader reader) {
        return DeserializeList(reader, DeserializeItemInstance);
    }

    private void SerializeMapState(BinaryWriter binWriter, MapData.State mapState) {
        binWriter.Write(mapState.isUnlocked);
        SerializeList(binWriter, mapState.bloodMushroomSpawns, SerializeVector2);
    }

    private MapData.State DeserializeMapState(BinaryReader binReader) {
        return new() {
            isUnlocked = binReader.ReadBoolean(),
            bloodMushroomSpawns = DeserializeList(binReader, DeserializeVector2)
        };
    }
    
    private void SerializeQuestState(BinaryWriter binWriter, Quest.State questState) {
        binWriter.Write(questState.associatedQuestUuid);
        binWriter.Write(questState.submitted);
        SerializeList(binWriter, questState.objectiveProgresses, SerializeInt);
    }

    private Quest.State DeserializeQuestState(BinaryReader binReader) {
        return new() {
            associatedQuestUuid = binReader.ReadInt32(),
            submitted = binReader.ReadBoolean(),
            objectiveProgresses = DeserializeList(binReader, DeserializeInt)
        };
    }
    
    private void SerializeItemInstance(BinaryWriter binWriter, ItemInstance itemInstance) {
        binWriter.Write(itemInstance.itemOrInstanceUuid);
        SerializeList(binWriter, itemInstance.nestedUuids, SerializeInt);
        binWriter.Write(itemInstance.count);
        binWriter.Write(itemInstance.isDemonEye);
        SerializeString(binWriter, itemInstance.demonEyeName);
        binWriter.Write(itemInstance.demonEyeXp);
        binWriter.Write(itemInstance.demonEyeUpgradesAvailable);
    }
    
    private ItemInstance DeserializeItemInstance(BinaryReader binReader) {
        return new() {
            itemOrInstanceUuid = binReader.ReadInt32(),
            nestedUuids = DeserializeList(binReader, DeserializeInt),
            count = binReader.ReadInt32(),
            isDemonEye = binReader.ReadBoolean(),
            demonEyeName = DeserializeString(binReader),
            demonEyeXp = binReader.ReadInt32(),
            demonEyeUpgradesAvailable = binReader.ReadInt32(),
        };
    }
    
    private void SerializeList<T>(BinaryWriter writer, List<T> list, Action<BinaryWriter, T> serializeCallback) {
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
    
    private List<T> DeserializeList<T>(BinaryReader reader, Func<BinaryReader, T> deserializeCallback) {
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
    
    private void SerializeString(BinaryWriter writer, string str) {
        if (string.IsNullOrEmpty(str)) {
            writer.Write(0);
            return;
        }
        writer.Write(str.Length);
        writer.Write(str);
    }
    
    private string DeserializeString(BinaryReader reader) {
        int strLen = reader.ReadInt32();
        return strLen > 0 ? reader.ReadString() : string.Empty;
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
