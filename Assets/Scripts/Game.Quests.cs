using System;
using System.Collections.Generic;
using UnityEngine;
using static GameData;

public static partial class Game {
    
    // Explicitly define each value so we can remove or
    // re-arrange without breaking the serialization
    public enum QuestObjectiveTypes {
        Kill                   = 0,
        FetchByItem            = 10,
        FetchByType            = 20,
        Teleport               = 30, 
        Sell                   = 40,
        Extract                = 50,
        ClearingMap            = 55,
        UpgradeSkills          = 60,
        StoppingBleeds         = 70,
        InRaidHealing          = 80,
        OverweightExtract      = 90,
        PickPocket             = 100,
        MedicalBushes          = 110,
        EquipADemonEye         = 120,
        BloodDropsForMushrooms = 130,
        SoulsBanked            = 140,
        ExtractAfterWave       = 150,
        MultiKill              = 160,
        CraftEyeWithStacks     = 170,
        CraftEyeWithUpgrades   = 180,
    }
    
    [Serializable]
    public class ObjectiveData {
        [HideInInspector] public int id; // Unique within its quest, saved progress is matched by it
        public QuestObjectiveTypes type;
        public string description;
        public int targetValue = 1;
        public EnemyData targetEnemy;
        public Item targetItem;
        public ItemType targetItemType;
        public MapData targetMap;
        public bool keepFetchedItems;
        [Tooltip("Multi kill: kills at once. Extract after wave: the wave to reach. Craft eye: stacks of one upgrade, or different upgrades")]
        public int requiredCount;
    }
    
    public static string GetObjectiveDescription(ObjectiveData obj) {
        return obj.type switch {
            QuestObjectiveTypes.Kill when obj.targetValue == 1 && obj.targetEnemy.isBoss => $"Kill the {obj.targetEnemy.displayName}",
            QuestObjectiveTypes.Kill        => $"Kill {obj.targetValue} {obj.targetEnemy.displayName}s",
            QuestObjectiveTypes.SoulsBanked => $"Bank {obj.targetValue} souls from one extraction{OnMap()}",
            QuestObjectiveTypes.ExtractAfterWave => $"Reach wave {obj.requiredCount}{OnMap()} and extract",
            QuestObjectiveTypes.MultiKill   => $"Kill {obj.requiredCount} enemies at once" + (obj.targetValue > 1 ? $", {obj.targetValue} times" : ""),
            QuestObjectiveTypes.CraftEyeWithStacks   => $"Craft a Demon Eye with {obj.requiredCount} of the same upgrade",
            QuestObjectiveTypes.CraftEyeWithUpgrades => $"Craft a Demon Eye with {obj.requiredCount} different upgrades",
            QuestObjectiveTypes.FetchByItem => GetFetchDesc(),
            QuestObjectiveTypes.FetchByType => GetFetchDesc(),
            QuestObjectiveTypes.Teleport    => $"Teleport to {obj.targetMap?.displayName}",
            QuestObjectiveTypes.Sell        => $"Sell {obj.targetValue} {obj.targetItem.displayName} to the trader",
            _                               => obj.description,
        };
        
        string OnMap() => obj.targetMap != null ? $" on {obj.targetMap.displayName}" : "";

        string GetFetchDesc() {
            string displayName = obj.type == QuestObjectiveTypes.FetchByItem ? obj.targetItem.displayName : obj.targetItemType.displayName;
            if (obj.targetValue == 1) {
                return displayName.StartsWithVowel() ? $"Return with an {displayName}" : $"Return with a {displayName}";
            }
            return $"Return with {obj.targetValue} {displayName}s";
        }
    }

    // Objectives added since the save have no progress yet, and a lowered target caps what was saved
    public static int GetObjectiveProgress(Quest quest, ObjectiveData obj) {
        return Mathf.Min(quest.state.objectiveProgresses.GetValueOrDefault(obj.id), obj.targetValue);
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
        return GetObjectiveProgress(quest, obj) >= obj.targetValue;
    }

    static void InitQuests(GameState gameState) {
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

    static void AssignNewEmptyState(Quest quest) {
        quest.state = new() {
            associatedQuestUuid = quest.uuid,
            submitted = false,
            objectiveProgresses = new(),
        };
    }

    static void UpdateQuests() {
        if (InTutorial) return;
        UpdateRecentKills();
        
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

    static void UpdateQuestObjective(Quest quest, ObjectiveData obj) {
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
            case QuestObjectiveTypes.ClearingMap: {
                if (curRaid.state == RaidState.PostFinalWave && curRaid.stateSwitchedThisFrame) {
                    int increase = curRaid.map == obj.targetMap ? 1 : 0;
                    IncreaseProgressValue(quest, obj, increase);
                }
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
                if (thisFrame.flags.HasFlag(FrameFlags.PostInitRaid)) {
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
            case QuestObjectiveTypes.SoulsBanked: {
                // Progress is the most souls banked from a single extraction
                if (JustExtracted() && OnTargetMap(obj)) {
                    SetProgressValue(quest, obj, Mathf.Max(GetObjectiveProgress(quest, obj), curRaid.data.souls));
                }
                break;
            }
            case QuestObjectiveTypes.ExtractAfterWave: {
                if (JustExtracted() && OnTargetMap(obj) && spawnManager.CurWaveNumber >= obj.requiredCount) {
                    IncreaseProgressValue(quest, obj, 1);
                }
                break;
            }
            case QuestObjectiveTypes.MultiKill: {
                if (recentKillTimes.Count >= obj.requiredCount) {
                    recentKillTimes.Clear(); // One burst of kills only counts once
                    IncreaseProgressValue(quest, obj, 1);
                }
                break;
            }
            case QuestObjectiveTypes.CraftEyeWithStacks:
            case QuestObjectiveTypes.CraftEyeWithUpgrades: {
                ItemInstance forgedEye = thisFrame.data.forgedDemonEye;
                if (forgedEye == null) break;
                EyeUpgradeSet upgradeSet = EyeUpgradeSetFromIds(forgedEye.nestedUuids);
                int value = upgradeSet.elements.Count;
                if (obj.type == QuestObjectiveTypes.CraftEyeWithStacks) {
                    value = 0;
                    foreach (EyeUpgradeSet.Element element in upgradeSet.elements) {
                        value = Mathf.Max(value, element.upgradeCount);
                    }
                }
                if (value >= obj.requiredCount) {
                    IncreaseProgressValue(quest, obj, 1);
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static bool JustExtracted() => thisFrame.flags.HasAnyFlag(FrameFlags.ExitTaken | FrameFlags.EarlyExitTaken);
    static bool OnTargetMap(ObjectiveData obj) => obj.targetMap == null || curRaid.map == obj.targetMap;

    // Kills spread over a few frames, like an explosion's, still count as at once
    const float multiKillWindow = 0.25f;
    static readonly List<float> recentKillTimes = new();

    static void UpdateRecentKills() {
        if (!InRaid) {
            recentKillTimes.Clear();
            return;
        }
        foreach (int kills in thisFrame.enemyKillCount.Values) {
            for (int i = 0; i < kills; i++) {
                recentKillTimes.Add(Time.time);
            }
        }
        recentKillTimes.RemoveAll(static time => Time.time - time > multiKillWindow);
    }

    static void IncreaseObjectiveFromFlags(Quest quest, ObjectiveData obj, FrameFlags flag) {
        if (thisFrame.flags.HasAnyFlag(flag)) {
            IncreaseProgressValue(quest, obj, 1);
        }
    }

    static void UpdateObjectiveFetchItem(Quest quest, ObjectiveData obj) {
        UpdateObjectiveFetch(quest, obj, GetOwnedCountOfItem(obj.targetItem));
    }

    static void UpdateObjectiveFetchType(Quest quest, ObjectiveData obj) {
        UpdateObjectiveFetch(quest, obj, GetOwnedCountOfItem(obj.targetItemType));
    }

    // Items only count as owned outside of a raid, once they're in the inventory or stash
    static void UpdateObjectiveFetch(Quest quest, ObjectiveData obj, int ownedCount) {
        if (InRaid) return;
        
        // Kept items are the player's to use, so the progress never drops. Brought back, bought or crafted away later, they still count
        if (obj.keepFetchedItems) {
            SetProgressValue(quest, obj, Mathf.Max(GetObjectiveProgress(quest, obj), ownedCount));
            return;
        }
        
        // The trader takes these on completion, so they all need to be owned at once
        SetProgressValue(quest, obj, ownedCount);
    }

    static void SetProgressValue(Quest quest, ObjectiveData obj, int value) {
        quest.state.objectiveProgresses[obj.id] = Mathf.Clamp(value, 0, obj.targetValue);
    }

    static void IncreaseProgressValue(Quest quest, ObjectiveData obj, int value) {
        SetProgressValue(quest, obj, GetObjectiveProgress(quest, obj) + value);
    }

    static void SpawnQuestItemOnDeadBody(Quest quest, ObjectiveData obj) {
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
