using System;
using UnityEngine;
using static GameData;

public partial class Game {
    
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

    public static int GetObjectiveProgress(Quest quest, ObjectiveData obj) {
        return quest.state.objectiveProgresses[quest.objectives.IndexOf(obj)];
    }
    
    public static bool QuestIsComplete(Quest quest) {
        foreach (ObjectiveData obj in quest.objectives) {
            if (!ObjectiveIsComplete(quest, obj)) {
                return false;
            }
        }
        return true;
    }

    public static bool ObjectiveIsComplete(Quest quest, ObjectiveData obj) {
        int i = quest.objectives.IndexOf(obj);
        return quest.state.objectiveProgresses[i] >= quest.objectives[i].targetValue;
    }
    
    private void InitQuests(GameState gameState) {
        if (gameState == null) {
            foreach (Quest quest in quests.graph.unorderedQuests) {
                AssignNewEmptyState(quest);
                quests.stateLookupFromUuid.Add(quest.uuid, quest.state);
            }
            return;
        }
        
        foreach (Quest.State state in gameState.questStates) {
            quests.stateLookupFromUuid.Add(state.associatedQuestUuid, state);
        }

        foreach (Quest quest in quests.graph.unorderedQuests) {
            if (quests.stateLookupFromUuid.TryGetValue(quest.uuid, out Quest.State state)) {
                quest.state = state;
                continue;
            }
            AssignNewEmptyState(quest);
        }
    }

    private void AssignNewEmptyState(Quest quest) {
        quest.state = new() {
            associatedQuestUuid = quest.uuid,
            submitted = false,
            objectiveProgresses = new(),
        };
        for (int i = 0; i < quest.objectives.Count; i++) {
            quest.state.objectiveProgresses.Add(0);
        }
    }

    private void UpdateQuests() {
        foreach (QuestPackage questsActivePkg in quests.activePkgs) {
            Quest quest = questsActivePkg.questNode.curQuest;
            bool wasComplete = QuestIsComplete(quest);
            
            foreach (ObjectiveData obj in quest.objectives) {
                UpdateQuestObjective(quest, obj);
            }
            
            bool justCompletedQuest = !wasComplete && QuestIsComplete(quest);
            if (justCompletedQuest) {
                PushNotification($"Quest Completed\n {quest.title}");
            }
        }
    }
    
    private void UpdateQuestObjective(Quest quest, ObjectiveData obj) {
        switch (obj.type) {
            case QuestObjectiveTypes.Kill: {
                if (thisFrame.enemyKillCount.TryGetValue(obj.targetEnemy, out int kills)) {
                    IncreaseProgressValue(quest, obj, kills);
                }
                break;
            }
            case QuestObjectiveTypes.FetchByItem: {
                UpdateObjectiveFetchItem(quest, obj);
                break;
            }
            case QuestObjectiveTypes.FetchByType: {
                UpdateObjectiveFetchType(quest, obj);
                break;
            }
            case QuestObjectiveTypes.Teleport:
                break;
            case QuestObjectiveTypes.Sell:
                break;
            case QuestObjectiveTypes.Extract: {
                IncreaseObjectiveFromFlags(quest, obj, FrameFlags.ExitTaken | FrameFlags.EarlyExitTaken);
                break;
            }
            case QuestObjectiveTypes.UpgradeSkills: {
                IncreaseObjectiveFromFlags(quest, obj, FrameFlags.SkillUpgraded);
                break;
            }
            case QuestObjectiveTypes.StoppingBleeds: {
                IncreaseObjectiveFromFlags(quest, obj, FrameFlags.BleedStopped);
                break;
            }
            case QuestObjectiveTypes.InRaidHealing: {
                if (!InRaid) break;
                IncreaseProgressValue(quest, obj, thisFrame.data.healing);
                break;
            }
            case QuestObjectiveTypes.OverweightExtract: {
                if (GetOverweightCompletion() >= 1f) {
                    IncreaseObjectiveFromFlags(quest, obj, FrameFlags.ExitTaken | FrameFlags.EarlyExitTaken);
                }
                break;
            }
            case QuestObjectiveTypes.PickPocket: {
                if (thisFrame.flags.HasFlag(FrameFlags.PostRaidInit)) {
                    SpawnQuestItemOnDeadBody(quest, obj);
                }
                UpdateObjectiveFetchItem(quest, obj);
                break;
            }
            case QuestObjectiveTypes.MedicalBushes: {
                ItemInstance foundInstance = thisFrame.data.foundSearchItem;
                Item foundItem = foundInstance?.ItemRef;
                if (foundItem == null) break;
                bool searchingInBush = curRaid.data.interactions.curLootOrigin == LootInventoryOrigin.Bush;
                if (searchingInBush && foundItem.IsMedical()) {
                    IncreaseProgressValue(quest, obj, foundInstance.count);
                }
                break;
            }
            case QuestObjectiveTypes.EquipADemonEye: {
                if (thisFrame.flags.HasFlag(FrameFlags.DemonEyeChanged)) {
                    if (demonEye.equiped != demonEye.empty) {
                        IncreaseProgressValue(quest, obj, 1);
                    }
                } 
                break;
            }
            case QuestObjectiveTypes.BloodDropsForMushrooms: {
                persistentFlags |= PersistentFlags.BloodMushroomsUnlocked;
                IncreaseProgressValue(quest, obj, thisFrame.data.enemyBloodDropped);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void IncreaseObjectiveFromFlags(Quest quest, ObjectiveData obj, FrameFlags flag) {
        if (thisFrame.flags.HasAnyFlag(flag)) {
            IncreaseProgressValue(quest, obj, 1);
        }
    }
    
    private void UpdateObjectiveFetchItem(Quest quest, ObjectiveData obj) {
        int count = GetOwnedCountOfItem(obj.targetItem);
        SetProgressValue(quest, obj, count);
    }
    
    private void UpdateObjectiveFetchType(Quest quest, ObjectiveData obj) {
        int count = GetOwnedCountOfItem(obj.targetItemType);
        SetProgressValue(quest, obj, count);
    }

    private void SetProgressValue(Quest quest, ObjectiveData obj, int value) {
        int i = quest.objectives.IndexOf(obj);
        quest.state.objectiveProgresses[i] = Mathf.Clamp(value, 0, obj.targetValue);
    }

    private void IncreaseProgressValue(Quest quest, ObjectiveData obj, int value) {
        int i = quest.objectives.IndexOf(obj);
        int newValue = quest.state.objectiveProgresses[i] + value;
        quest.state.objectiveProgresses[i] = Mathf.Clamp(newValue, 0, obj.targetValue);
    }
    
    private void SpawnQuestItemOnDeadBody(Quest quest, ObjectiveData obj) {
        if (ObjectiveIsComplete(quest, obj)) return;
        
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
    
}
