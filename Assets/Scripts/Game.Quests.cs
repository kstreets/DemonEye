using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using static GameData;

public partial class Game {
    
    [Serializable]
    public class QuestSaveData {
        public Progress[] progressSaves;
        public bool[] submissionStates;
        
        [Serializable]
        public class Progress {
            public List<int> values;
        }
    }
    
    // Explicitly define each value so we can remove or
    // re-arrange without breaking the serialization
    public enum QuestObjectiveTypes {
        Kill                   = 0,
        FetchByItem            = 10,
        FetchByType            = 20,
        Teleport               = 30, 
        Sell                   = 40,
        Extract                = 50,
        UpgradeSkills          = 60,
        StoppingBleeds         = 70,
        InRaidHealing          = 80,
        OverweightExtract      = 90,
        PickPocket             = 100,
        MedicalBushes          = 110,
        EquipADemonEye         = 120,
        BloodDropsForMushrooms = 130,
    }
    
    [Serializable]
    public class ObjectiveData {
        public QuestObjectiveTypes type;
        public string description;
        public int targetValue = 1;
        public EnemyData targetEnemy;
        public Item targetItem;
        public ItemType targetItemType;
        public MapData teleportMap;
        public bool keepFetchedItems;
        
        [NonSerialized] public int progressValue;
        public bool completed => progressValue >= targetValue || type == QuestObjectiveTypes.Teleport;
    }
    
    public static string GetObjectiveDescription(ObjectiveData obj) {
        return obj.type switch {
            QuestObjectiveTypes.Kill        => $"Kill {obj.targetValue} {obj.targetEnemy.displayName}s",
            QuestObjectiveTypes.FetchByItem => GetFetchDesc(),
            QuestObjectiveTypes.FetchByType => GetFetchDesc(),
            QuestObjectiveTypes.Teleport    => $"Teleport to {obj.teleportMap?.displayName}",
            QuestObjectiveTypes.Sell        => $"Sell {obj.targetValue} {obj.targetItem.displayName} to the trader",
            _                               => obj.description,
        };
        
        string GetFetchDesc() {
            string displayName = obj.type == QuestObjectiveTypes.FetchByItem ? obj.targetItem.displayName : obj.targetItemType.displayName;
            if (obj.targetValue == 1) {
                return displayName.StartsWithVowel() ? $"Return with an {displayName}" : $"Return with a {displayName}";
            }
            return $"Return with {obj.targetValue} {displayName}s";
        }
    }
    
    public static bool QuestIsComplete(Quest quest) {
        foreach (ObjectiveData obj in quest.objectives) {
            if (!obj.completed) {
                return false;
            }
        }
        return true;
    }
    
    private void InitQuests() {
        quests.saveData = LoadFromFile<QuestSaveData>(savePaths.quest);
        
        if (quests.saveData == null) {
            quests.saveData = new() {
                progressSaves = new QuestSaveData.Progress[quests.graph.questCount],
                submissionStates = new bool[quests.graph.questCount],
            };
            quests.saveData.progressSaves.InitalizeWithDefault();
            SaveToFile(savePaths.quest, quests.saveData);
        }
        
        HashSet<QuestGraphRuntime.Node> initialQuestNodes = new();
        foreach (QuestGraphRuntime.Node node in quests.graph.rootNode.nextNodes) {
            FindStartingQuestNodes(initialQuestNodes, node);
        }
        
        const int questUiPoolSize = 6;
        for (int i = 0; i < questUiPoolSize; i++) {
            ReleaseQuestPackage(CreateQuestPackage());
        }
        
        foreach (QuestGraphRuntime.Node questNode in initialQuestNodes) {
            QuestSaveData.Progress save = quests.saveData.progressSaves[questNode.saveIndex];
            SetQuestProgressSave(questNode.curQuest, save);
            ActivateQuest(questNode); 
        }
        
        RefreshQuestDisplays();
    }

    private void FindStartingQuestNodes(HashSet<QuestGraphRuntime.Node> nodes, QuestGraphRuntime.Node curNode) {
        bool questHasBeenSubmitted = quests.saveData.submissionStates[curNode.saveIndex];
        
        if (!questHasBeenSubmitted) {
            nodes.Add(curNode);
            return;
        }
        
        foreach (QuestGraphRuntime.Node nextNode in curNode.nextNodes) {
            FindStartingQuestNodes(nodes, nextNode);
        }
    }
    
    private void UpdateQuests() {
        foreach (QuestPackage questsActivePkg in quests.activePkgs) {
            Quest quest = questsActivePkg.questNode.curQuest;
            foreach (ObjectiveData obj in quest.objectives) {
                UpdateQuestObjective(obj);
            }
        }
    }
    
    private void UpdateQuestObjective(ObjectiveData obj) {
        switch (obj.type) {
            case QuestObjectiveTypes.Kill: {
                if (thisFrame.enemyKillCount.TryGetValue(obj.targetEnemy, out int kills)) {
                    IncreaseObjective(obj, kills);
                }
                break;
            }
            case QuestObjectiveTypes.FetchByItem: {
                UpdateObjectiveFetchItem(obj);
                break;
            }
            case QuestObjectiveTypes.FetchByType: {
                UpdateObjectiveFetchType(obj);
                break;
            }
            case QuestObjectiveTypes.Teleport:
                break;
            case QuestObjectiveTypes.Sell:
                break;
            case QuestObjectiveTypes.Extract: {
                IncreaseObjectiveFromFlags(obj, FrameFlags.ExitTaken | FrameFlags.EarlyExitTaken);
                break;
            }
            case QuestObjectiveTypes.UpgradeSkills: {
                IncreaseObjectiveFromFlags(obj, FrameFlags.SkillUpgraded);
                break;
            }
            case QuestObjectiveTypes.StoppingBleeds: {
                IncreaseObjectiveFromFlags(obj, FrameFlags.BleedStopped);
                break;
            }
            case QuestObjectiveTypes.InRaidHealing: {
                if (!InRaid) break;
                IncreaseObjective(obj, thisFrame.data.healing);
                break;
            }
            case QuestObjectiveTypes.OverweightExtract: {
                if (GetOverweightCompletion() >= 1f) {
                    IncreaseObjectiveFromFlags(obj, FrameFlags.ExitTaken | FrameFlags.EarlyExitTaken);
                }
                break;
            }
            case QuestObjectiveTypes.PickPocket: {
                if (thisFrame.flags.HasFlag(FrameFlags.PostRaidInit)) {
                    SpawnQuestItemOnDeadBody(obj);
                }
                UpdateObjectiveFetchItem(obj);
                break;
            }
            case QuestObjectiveTypes.MedicalBushes: {
                Item foundItem = thisFrame.data.foundSearchItem?.ItemRef;
                if (foundItem == null) break;
                bool searchingInBush = curRaid.data.interactions.curLootOrigin == LootInventoryOrigin.Bush;
                if (searchingInBush && foundItem.IsMedical()) {
                    IncreaseObjective(obj, 1);
                }
                break;
            }
            case QuestObjectiveTypes.EquipADemonEye: {
                if (thisFrame.flags.HasFlag(FrameFlags.DemonEyeChanged)) {
                    if (demonEye.equiped != demonEye.empty) {
                        IncreaseObjective(obj, 1);
                    }
                } 
                break;
            }
            case QuestObjectiveTypes.BloodDropsForMushrooms: {
                persistentFlags |= PersistentFlags.BloodMushroomsUnlocked;
                IncreaseObjective(obj, thisFrame.data.enemyBloodDropped);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void IncreaseObjectiveFromFlags(ObjectiveData obj, FrameFlags flag) {
        if (thisFrame.flags.HasAnyFlag(flag)) {
            obj.progressValue = Mathf.Clamp(++obj.progressValue, 0, obj.targetValue);
        }
    }
    
    private void IncreaseObjective(ObjectiveData obj, int value) {
        obj.progressValue = Mathf.Clamp(obj.progressValue + value, 0, obj.targetValue);
    }
    
    private void UpdateObjectiveFetchItem(ObjectiveData obj) {
        obj.progressValue = Mathf.Clamp(GetOwnedCountOfItem(obj.targetItem), 0, obj.targetValue);
    }
    
    private void UpdateObjectiveFetchType(ObjectiveData obj) {
        obj.progressValue = Mathf.Clamp(GetOwnedCountOfItem(obj.targetItemType), 0, obj.targetValue);
    }
    
    private void SpawnQuestItemOnDeadBody(ObjectiveData obj) {
        if (obj.completed) return;
        
        InventorySlot[] chosenDeadbody = curRaid.deadBodySlotsLookup.RandomValue();
        if (chosenDeadbody == null) return;
        
        foreach (InventorySlot slot in chosenDeadbody) {
            if (slot.itemInstance != null) continue;
            slot.itemInstance = new() {
                itemOrInstanceUuid = obj.targetItem.uuid,
                count = 1,
                notDiscovered = true,
            };
            break;
        }
    }
    
    private void SaveActiveQuestProgresses() {
        foreach (QuestPackage questPackage in quests.activePkgs) {
            QuestGraphRuntime.Node node = questPackage.questNode;
            quests.saveData.progressSaves[node.saveIndex] = GetQuestProgressSave(node.curQuest);
        }
        SaveToFile(savePaths.quest, quests.saveData);
    }

    private void SaveAndMarkQuestAsSubmitted(QuestGraphRuntime.Node questNode) {
        quests.saveData.submissionStates[questNode.saveIndex] = true;
        quests.saveData.progressSaves[questNode.saveIndex] = GetQuestProgressSave(questNode.curQuest);
        SaveToFile(savePaths.quest, quests.saveData);
    }
    
    private QuestSaveData.Progress GetQuestProgressSave(Quest quest) {
        QuestSaveData.Progress progress = new() { values = new() };
        foreach (ObjectiveData obj in quest.objectives) {
            progress.values.Add(obj.progressValue);
        }
        return progress;
    }
    
    private void SetQuestProgressSave(Quest quest, QuestSaveData.Progress save) {
        if (save.values == null) return;
        Assert.IsTrue(quest.objectives.Count == save.values.Count, "Save state does not match objectives");
        for (int i = 0; i < quest.objectives.Count; i++) {
            quest.objectives[i].progressValue = save.values[i];
        }
    }
    
}
