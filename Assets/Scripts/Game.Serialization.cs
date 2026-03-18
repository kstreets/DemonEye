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
    private string hideoutDataSavePath;
    private string raidDataSavePath;
    private string playerSavePath;
    private string questSavePath;
    private string traderSavePath;
    private string traderInventorySavePath;
    private string tutorialSavePath;
    private List<ItemInstance> cachedInventoryForSaving = new(50);
    
    private void BuildSavePaths() {
        playerInventorySavePath = $"{Application.persistentDataPath}/inventory";
        stashSavePath = $"{Application.persistentDataPath}/stash";
        crucibleSavePath = $"{Application.persistentDataPath}/crucible";
        hideoutDataSavePath = $"{Application.persistentDataPath}/hideoutData"; 
        raidDataSavePath = $"{Application.persistentDataPath}/raidStateData";
        playerSavePath = $"{Application.persistentDataPath}/player";
        questSavePath = $"{Application.persistentDataPath}/quests";
        traderSavePath = $"{Application.persistentDataPath}/traders";
        traderInventorySavePath = $"{Application.persistentDataPath}/traderInventory";
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
    
    private int GenerateNewItemUuid() {
        int newItemId = UuidScriptableObject.GetIntUuid();
        while (resourceLookup.ContainsKey(newItemId)) {
            newItemId = UuidScriptableObject.GetIntUuid();
        }
        return newItemId;
    }
    
    public Dictionary<int, UuidScriptableObject> resourceLookup = new();
    public Dictionary<ModifierItem, List<Augment>> augmentsPerModifierItemLookup = new();
    public List<Item> allItems = new();
    
    private void LoadAllResources() {
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        foreach (UuidScriptableObject res in resourceObjects) {
            resourceLookup.Add(res.uuid, res);
            if (res is Item item) {
                allItems.Add(item);
            }
            if (res is Augment augment) {
                augment.CreateAugmentItemFromDerived();
                if (augmentsPerModifierItemLookup.TryGetValue(augment.modifierDerivedFrom, out var augmentList)) {
                    augmentList.Add(augment);
                }
                else {
                    augmentsPerModifierItemLookup.Add(augment.modifierDerivedFrom, new() { augment });
                }
            }
        }
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
            crucibleLevel = player.crucibleLevel,
            soulCurrency = player.soulCurrency,
            coinCurrency = player.coinCurrency,
            hasteSkillLevel = player.hasteSkillLevel,
            intellectSkillLevel = player.intellectSkillLevel,
            lifeBloodSkillLevel = player.lifeBloodSkillLevel,
            strengthSkillLevel = player.strengthSkillLevel,
        };
        SaveToFile(playerSavePath, data);
    }

    private void LoadAndAssignPlayerSaveData(Player instancedPlayer) {
        PlayerSaveData data = LoadFromFile<PlayerSaveData>(playerSavePath);
        if (data != null) {
            instancedPlayer.health = data.health;
            instancedPlayer.crucibleLevel = data.crucibleLevel;
            instancedPlayer.soulCurrency = data.soulCurrency;
            instancedPlayer.coinCurrency = data.coinCurrency;
            instancedPlayer.hasteSkillLevel = data.hasteSkillLevel;
            instancedPlayer.intellectSkillLevel = data.intellectSkillLevel;
            instancedPlayer.lifeBloodSkillLevel = data.lifeBloodSkillLevel;
            instancedPlayer.strengthSkillLevel = data.strengthSkillLevel;
        }
        
        // We want to make sure that the player health is never <= zero
        instancedPlayer.health = player.health <= 0f ? FullPlayerHealth : player.health;
    }

}
