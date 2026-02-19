using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "Quest", menuName = "Scriptable Objects/Quest")]
public class Quest : ScriptableObject {

    [Serializable]
    public class Objective {
        public enum Type { None, Custom, Kill, Fetch, Teleport, Sell }
        
        public Type type;
        public string taskDescription;
        public int targetValue = 1;
        public EnemyData targetEnemy;
        public Item targetItem;
        public MapData teleportMap;
        public string customCode;
        
        [NonSerialized] public int progressValue;
        public bool completed => progressValue >= targetValue;

        public string GetTaskDescription() {
            return (type) switch {
                Type.Kill => $"Kill {targetValue} {targetEnemy.displayName}s", 
                Type.Fetch => targetValue == 1 ? $"Return with a {targetItem.displayName}" : $"Return with {targetValue} {targetItem.displayName}s",
                Type.Teleport => $"Teleport to {teleportMap?.displayName}", 
                Type.Sell => $"Sell {targetValue} {targetItem.displayName} to {targetItem.associatedTrader.traderName}", 
                _ => taskDescription,
            };
        }
    }

    [Serializable]
    public class ProgressSave {
        public List<int> progressValues;
    }
    
    [Header("Info")]
    public string title;
    [TextArea] public string description;
    
    [Header("Rewards")]
    public int traderReputationReward;
    
    [Space]
    public List<Objective> objectives;
    
    public void Init() {
        onEnemyDeath += OnEnemyDeath;
        onTeleportToMap += OnTeleportToMap;
        onSoldItemsToTrader += OnSoldItemsToTrader;
        onReturnedFromRaid += OnReturnFromRaid;
        customQuestEvent += OnCustomEvent;
    }

    public void Deinit() {
        onEnemyDeath -= OnEnemyDeath;
        onTeleportToMap -= OnTeleportToMap;
        onSoldItemsToTrader -= OnSoldItemsToTrader;
        onReturnedFromRaid -= OnReturnFromRaid;
        customQuestEvent -= OnCustomEvent;
    }

    public void LoadProgressSave(ProgressSave progressSave) {
        if (progressSave.progressValues == null) return;
        Assert.IsTrue(objectives.Count == progressSave.progressValues.Count, "Save state does not match objectives");
        for (int i = 0; i < objectives.Count; i++) {
            objectives[i].progressValue = progressSave.progressValues[i];
        }
    }
    
    public ProgressSave GetProgressSave() { 
        List<int> progressValues = new();
        
        foreach (Objective obj in objectives) {
            progressValues.Add(obj.progressValue);
        }
        
        return new() {
            progressValues = progressValues,
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

    private void OnReturnFromRaid(InventorySlot[] playerInventory) {
        foreach (Objective obj in objectives) {
            if  (obj.type != Objective.Type.Fetch) continue;
            
            foreach (InventorySlot slot in playerInventory) {
                if (slot.item == null) continue;
                
                if (obj.targetItem.uuid == slot.item.ItemRef.uuid) {
                    obj.progressValue = Mathf.Clamp(obj.progressValue + slot.item.count, 0, obj.targetValue);
                }
            }
        }
    }

    private void OnCustomEvent(string code) {
        foreach (Objective obj in objectives) {
            if  (obj.type != Objective.Type.Custom || obj.customCode != code) continue;
            obj.progressValue = Mathf.Clamp(++obj.progressValue, 0, obj.targetValue);
            return;
        }
        Assert.IsTrue(true, $"Could not find a matching code for {code}");
    }

}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(Quest.Objective))]
public class ObjectiveDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        
        position.height = EditorGUIUtility.singleLineHeight;
        
        DisplayPropertyField(property, "type", ref position);
        Quest.Objective.Type actualType = (Quest.Objective.Type)property.FindPropertyRelative("type").enumValueIndex;

        if (actualType != Quest.Objective.Type.None) {
            DisplayTextArea(property, "taskDescription", ref position);
        }
        
        if (actualType == Quest.Objective.Type.Custom) {
            DisplayPropertyField(property, "customCode", ref position);
            DisplayPropertyField(property, "targetValue", ref position);
        }
        else if (actualType == Quest.Objective.Type.Kill) {
            DisplayPropertyField(property, "targetEnemy", ref position);
            DisplayPropertyField(property, "targetValue", ref position);
        }
        else if (actualType == Quest.Objective.Type.Fetch) {
            DisplayPropertyField(property, "targetItem", ref position);
            DisplayPropertyField(property, "targetValue", ref position);
        }
        else if (actualType == Quest.Objective.Type.Teleport) {
            DisplayPropertyField(property, "teleportMap", ref position);
            DisplayPropertyField(property, "targetValue", ref position);
        }
        else if (actualType == Quest.Objective.Type.Sell) {
            DisplayPropertyField(property, "targetItem", ref position);
            DisplayPropertyField(property, "targetValue", ref position);
        }

        EditorGUI.EndProperty();
    }

    private void DisplayPropertyField(SerializedProperty main, string relative, ref Rect position) {
        SerializedProperty relativeProp = main.FindPropertyRelative(relative);
        EditorGUI.PropertyField(position, relativeProp);
        position.y += EditorGUIUtility.singleLineHeight;
    }
    
    private void DisplayTextArea(SerializedProperty main, string relative, ref Rect position) {
        SerializedProperty relativeProp = main.FindPropertyRelative(relative);

        float line = EditorGUIUtility.singleLineHeight;
        Rect labelRect = new(position.x, position.y, position.width, line);
        Rect textRect  = new(position.x, position.y + line, position.width, line * 3);
        
        EditorGUI.PrefixLabel(labelRect, new(relativeProp.displayName));
        
        EditorGUI.BeginChangeCheck();
        string textString = EditorGUI.TextArea(textRect, relativeProp.stringValue);
        if (EditorGUI.EndChangeCheck()) {
            relativeProp.stringValue = textString;
        }
        
        position.y += line * 4;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        int lines = 1; // type is always shown

        SerializedProperty typeProp = property.FindPropertyRelative("type");
        Quest.Objective.Type type = (Quest.Objective.Type)typeProp.enumValueIndex;

        if (type != Quest.Objective.Type.None) {
            lines += 4;
        }

        switch (type) {
            case Quest.Objective.Type.Custom:
            case Quest.Objective.Type.Kill:
            case Quest.Objective.Type.Fetch:
            case Quest.Objective.Type.Sell:
            case Quest.Objective.Type.Teleport:
                lines += 2; 
                break;
        }

        return lines * EditorGUIUtility.singleLineHeight;
    }
    
}

#endif