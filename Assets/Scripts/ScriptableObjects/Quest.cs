using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using static Game;

[CreateAssetMenu(fileName = "Quest", menuName = "Scriptable Objects/Quest")]
public class Quest : ScriptableObject {

    [Serializable]
    public class Objective {
        public enum Type { Kill, Fetch, Teleport, Sell, ForgeEye }
        
        public Type type;
        public QuestObjectiveUI.Display display;
        public int targetValue = 1;
        [TextArea] public string task;

        [Header("Kill")]
        public EnemyData targetEnemy;
        
        [Header("Fetch / Sell")]
        public Item targetItem;
        
        [Header("Teleport")]
        public MapData teleportMap;

        [NonSerialized] public int progressValue;
        public bool completed => progressValue >= targetValue;
    }
    
    [Serializable]
    public class SaveState {
        public List<int> objectiveProgressValues;
    }

    [Header("Info")]
    public string title;
    [TextArea] public string description;
    
    [Header("Objectives")]
    public List<Objective> objectives;

    [Header("Rewards")]
    public int traderReputationReward;

    public void Init() {
        onEnemyDeath += OnEnemyDeath;
        onTeleportToMap += OnTeleportToMap;
        onEyeForged += OnEyeForged;
        onSoldItemsToTrader += OnSoldItemsToTrader;
    }

    public void Deinit() {
        onEnemyDeath -= OnEnemyDeath;
        onTeleportToMap -= OnTeleportToMap;
        onEyeForged -= OnEyeForged;
        onSoldItemsToTrader -= OnSoldItemsToTrader;
    }

    public void LoadSaveState(SaveState saveState) {
        Assert.IsTrue(objectives.Count == saveState.objectiveProgressValues.Count, "Save state does not match objectives");
        for (int i = 0; i < objectives.Count; i++) {
            objectives[i].progressValue = saveState.objectiveProgressValues[i];
        }
    }
    
    public SaveState GetSaveState() {
        List<int> progressValues = new();
        
        foreach (Objective obj in objectives) {
            progressValues.Add(obj.progressValue);
        }
        
        return new() {
            objectiveProgressValues = progressValues,
        };
    }

    public bool IsComplete() {
        foreach (Objective obj in objectives) {
            if (!obj.completed) {
                return false;
            }
        }
        return true;
    }
    
    private void OnEnemyDeath(Enemy enemy) {
        foreach (Objective obj in objectives) {
            if (obj.type == Objective.Type.Kill && obj.targetEnemy == enemy.data) {
                obj.progressValue = Mathf.Clamp(++obj.progressValue, 0, obj.targetValue);
            }
        }
    }

    private void OnTeleportToMap(MapData map) {
        foreach (Objective obj in objectives) {
            if (obj.type == Objective.Type.Teleport && obj.teleportMap == map) {
                Assert.IsTrue(obj.targetValue == 1, "Teleport objectives need to have a target value of 1");
                obj.progressValue = Mathf.Clamp(++obj.progressValue, 0, obj.targetValue);
            }
        }
    }

    private void OnEyeForged(DemonEyeInstance demonEye) {
        foreach (Objective obj in objectives) {
            if (obj.type == Objective.Type.ForgeEye) {
                Assert.IsTrue(obj.targetValue == 1, "Eye forge objectives need to have a target value of 1");
                obj.progressValue = Mathf.Clamp(++obj.progressValue, 0, obj.targetValue);
            }
        }
    }

    private void OnSoldItemsToTrader(InventorySlot[] transactionInventorySlots) {
        foreach (Objective obj in objectives) {
            if  (obj.type != Objective.Type.Sell) continue;
            
            foreach (InventorySlot slot in transactionInventorySlots) {
                if (slot.item == null) continue;
                
                if (obj.targetItem.uuid == slot.item.ItemRef.uuid) {
                    obj.progressValue = Mathf.Clamp(obj.progressValue + slot.item.count, 0, obj.targetValue);
                }
            }
        }
    }

}