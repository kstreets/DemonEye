using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameData;

public partial class Game {
    
    private const int gameStateSaveVersion = 1;

    public class GameState {
        public int version;
        public PersistentFlags persistentFlags;
        
        public HideoutState hideoutState;
        public PlayerState playerState;
        public Trader.State traderState;
        
        public List<ItemInstance> playerInventoryItems;
        public List<ItemInstance> stashInventoryItems;
        public List<ItemInstance> traderInventoryItems;
        public List<ItemInstance> forgeInventoryItems;
        
        public List<MapData.State> mapStates;
        public List<Quest.State> questStates;
    }
    
    public string GetSettingsSavePath() {
        return $"{Application.persistentDataPath}/settings.json";
    }

    public string GetGameStateSavePath() {
        return $"{Application.persistentDataPath}/save";
    }

    public string GetBindingsSavePath() {
        return $"{Application.persistentDataPath}/bindings.json";
    }

    private void SaveBindingOverrides() {
        File.WriteAllText(GetBindingsSavePath(), InputSystem.actions.SaveBindingOverridesAsJson());
    }

    private void LoadBindingOverrides() {
        string savePath = GetBindingsSavePath();
        if (!File.Exists(savePath)) return;
        string json = File.ReadAllText(savePath);
        if (string.IsNullOrEmpty(json)) return; // Saved with no overrides
        InputSystem.actions.LoadBindingOverridesFromJson(json);
    }
    
    public void SaveSettings() {
        File.WriteAllText(GetSettingsSavePath(), JsonUtility.ToJson(settings.curSettingsState, prettyPrint: true));
    }

    // Fields missing from the file keep their defaults
    public SettingsState LoadSettings() {
        string savePath = GetSettingsSavePath();
        if (!File.Exists(savePath)) return null;
        SettingsState state = new();
        JsonUtility.FromJsonOverwrite(File.ReadAllText(savePath), state);
        return state;
    }
    
    public void SaveGameState() {
        // The tutorial starts over if it's quit partway, so nothing from it is saved until it's completed
        if (InTutorial) return;
        WriteBinaryFileSafely(GetGameStateSavePath(), WriteGameState);
    }

    private void WriteGameState(BinaryWriter binWriter) {
        SerializeInt(binWriter, gameStateSaveVersion);
        SerializeInt(binWriter, (int)persistentFlags);
        SerializeHideoutState(binWriter, hideoutState);
        SerializePlayerState(binWriter, player);
        SerializeTraderState(binWriter, config.trader.state);
        
        SerializeInventory(binWriter, inventories.player);
        SerializeInventory(binWriter, inventories.stash);
        SerializeInventory(binWriter, inventories.trader);
        SerializeInventory(binWriter, inventories.eyeForge);

        // Maps
        {
            using var _ = UnityEngine.Pool.ListPool<MapData.State>.Get(out var mapStates);
            foreach (MapData map in config.maps) {
                mapStates.Add(map.state);
            }
            SerializeList(binWriter, mapStates, SerializeMapState);
        }
        // Quests
        { 
            using var _ = UnityEngine.Pool.ListPool<Quest.State>.Get(out var questStates);
            foreach (Quest quest in quests.graph.unorderedQuests) {
                questStates.Add(quest.state);
            }
            SerializeList(binWriter, questStates, SerializeQuestState);
        }
    }

    // failed is set when there was a save but it couldn't be loaded, so a new game starts
    public GameState LoadGameState(out bool failed) {
        return ReadBinaryFileSafely(GetGameStateSavePath(), ReadGameState, out failed);
    }

    private GameState ReadGameState(BinaryReader binReader) {
        int version = DeserializeInt(binReader);
        return new() {
            version = version,
            persistentFlags = (PersistentFlags)DeserializeInt(binReader),
            hideoutState = DeserializeHideoutState(binReader),
            playerState = DeserializePlayerState(binReader),
            traderState = DeserializeTraderState(binReader),
            playerInventoryItems = DeserializeInventory(binReader),
            stashInventoryItems = DeserializeInventory(binReader),
            traderInventoryItems = DeserializeInventory(binReader),
            forgeInventoryItems = DeserializeInventory(binReader),
            mapStates = DeserializeList(binReader, DeserializeMapState),
            questStates = DeserializeList(binReader, DeserializeQuestState),
        };
    }
    
    private void SerializeHideoutState(BinaryWriter binWriter, HideoutState hideoutState) {
        binWriter.Write(hideoutState.pentagramLevelIndex);
    }
    
    private HideoutState DeserializeHideoutState(BinaryReader binReader) {
        return new() {
            pentagramLevelIndex = binReader.ReadInt32(),
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
        using var _ = UnityEngine.Pool.ListPool<ItemInstance>.Get(out var items);
        foreach (InventorySlot slot in inventory.slots) {
            items.Add(slot.itemInstance);
        }
        SerializeList(writer, items, SerializeItemInstance);
    }

    private List<ItemInstance> DeserializeInventory(BinaryReader reader) {
        return DeserializeList(reader, DeserializeItemInstance);
    }

    private void SerializeMapState(BinaryWriter binWriter, MapData.State mapState) {
        binWriter.Write(mapState.mapUuid);
        binWriter.Write(mapState.isUnlocked);
        SerializeList(binWriter, mapState.bloodMushroomSpawns, SerializeVector2);
        binWriter.Write(mapState.unlockRevealPending);
    }

    private MapData.State DeserializeMapState(BinaryReader binReader) {
        MapData.State state = new() {
            mapUuid = binReader.ReadInt32(),
            isUnlocked = binReader.ReadBoolean(),
            bloodMushroomSpawns = DeserializeList(binReader, DeserializeVector2),
            unlockRevealPending = binReader.ReadBoolean(),
        };
        return state;
    }
    
    private void SerializeQuestState(BinaryWriter binWriter, Quest.State questState) {
        binWriter.Write(questState.associatedQuestUuid);
        binWriter.Write(questState.submitted);
        SerializeList(binWriter, new List<KeyValuePair<int, int>>(questState.objectiveProgresses), SerializeIntPair);
    }

    private Quest.State DeserializeQuestState(BinaryReader binReader) {
        Quest.State state = new() {
            associatedQuestUuid = binReader.ReadInt32(),
            submitted = binReader.ReadBoolean(),
            objectiveProgresses = new(),
        };
        foreach ((int objectiveId, int progress) in DeserializeList(binReader, DeserializeIntPair)) {
            state.objectiveProgresses[objectiveId] = progress;
        }
        return state;
    }

    private void SerializeIntPair(BinaryWriter binWriter, KeyValuePair<int, int> pair) {
        binWriter.Write(pair.Key);
        binWriter.Write(pair.Value);
    }

    private KeyValuePair<int, int> DeserializeIntPair(BinaryReader binReader) {
        int key = binReader.ReadInt32();
        return new(key, binReader.ReadInt32());
    }
    
    private void SerializeItemInstance(BinaryWriter binWriter, ItemInstance itemInstance) {
        binWriter.Write(itemInstance.itemOrInstanceUuid);
        SerializeList(binWriter, itemInstance.nestedUuids, SerializeInt);
        binWriter.Write(itemInstance.count);
        binWriter.Write(itemInstance.isDemonEye);
        SerializeString(binWriter, itemInstance.demonEyeName);
    }
    
    private ItemInstance DeserializeItemInstance(BinaryReader binReader) {
        return new() {
            itemOrInstanceUuid = binReader.ReadInt32(),
            nestedUuids = DeserializeList(binReader, DeserializeInt),
            count = binReader.ReadInt32(),
            isDemonEye = binReader.ReadBoolean(),
            demonEyeName = DeserializeString(binReader),
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
        // Every element takes at least a byte, so a corrupt count fails here instead of allocating a huge list
        if (count < 0 || count > reader.BaseStream.Length - reader.BaseStream.Position) {
            throw new InvalidDataException($"Bad list count {count}");
        }
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

    private static void WriteBinaryFileSafely(string path, Action<BinaryWriter> write) {
        WriteFileSafely(path, tempPath => {
            using FileStream stream = File.Open(tempPath, FileMode.Create);
            using BinaryWriter binWriter = new(stream);
            write(binWriter);
        });
    }

    private static T ReadBinaryFileSafely<T>(string path, Func<BinaryReader, T> read, out bool failed) where T : class {
        return ReadFileSafely(path, filePath => {
            using FileStream stream = File.Open(filePath, FileMode.Open);
            using BinaryReader binReader = new(stream);
            T result = read(binReader);
            // Reads are positional, so leftover data means the file doesn't match what was read
            if (stream.Position != stream.Length) throw new InvalidDataException("Unread data at the end of the file");
            return result;
        }, out failed);
    }

    // Written to a temp file and swapped in, so a crash mid-save keeps the last good file. The one before it is kept as .bak
    private static void WriteFileSafely(string path, Action<string> writeTo) {
        string tempPath = path + ".tmp";
        writeTo(tempPath);
        if (File.Exists(path)) {
            File.Replace(tempPath, path, path + ".bak");
        }
        else {
            File.Move(tempPath, path);
        }
    }

    // Tries the file, then the .bak from the save before it. Returns null when there's nothing to load, or nothing loads.
    // failed is set when a file was there but couldn't be loaded, which is kept as .corrupt.
    private static T ReadFileSafely<T>(string path, Func<string, T> readFrom, out bool failed) where T : class {
        failed = false;
        foreach (string filePath in new[] { path, path + ".bak" }) {
            if (!File.Exists(filePath)) continue;
            try {
                return readFrom(filePath);
            }
            catch (Exception e) {
                Debug.LogWarning($"Couldn't load {filePath}: {e.Message}");
                if (filePath == path) {
                    File.Copy(path, path + ".corrupt", overwrite: true);
                }
                failed = true;
            }
        }
        return null;
    }

}
