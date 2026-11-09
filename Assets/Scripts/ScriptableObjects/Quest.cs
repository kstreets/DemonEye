using System.Collections.Generic;
using UnityEngine;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "Quest", menuName = "Scriptable Objects/Quest")]
public class Quest : UuidScriptableObject {

    [Header("Info")]
    public string title;
    [TextArea(minLines: 6, maxLines: 10)] 
    public string description;
    
    [Header("Rewards")]
    public int traderReputationReward;
    
    public List<ObjectiveData> objectives;
    
    public class State {
        public int associatedQuestUuid;
        public bool submitted;
        public Dictionary<int, int> objectiveProgresses; // Keyed by objective id
    }

    public State state;

#if UNITY_EDITOR

    // New objectives get an id no other objective in the quest has, including ones the + button copies from the last
    private void OnValidate() {
        if (objectives == null) return;
        int nextId = 1;
        foreach (ObjectiveData obj in objectives) {
            nextId = Mathf.Max(nextId, obj.id + 1);
        }
        HashSet<int> usedIds = new();
        foreach (ObjectiveData obj in objectives) {
            if (obj.id > 0 && usedIds.Add(obj.id)) continue;
            obj.id = nextId++;
            usedIds.Add(obj.id);
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    [Button]
    private void CompleteForTesting() {
        if (!Application.isPlaying) {
            Debug.Log("Need to be in playmode to complete quest");
            return;
        }

        foreach (ObjectiveData obj in objectives) {
            state.objectiveProgresses[obj.id] = obj.targetValue;
            // For fetch quests we need to actually have the items to complete it properly
            if (obj.type == QuestObjectiveTypes.FetchByItem) {
                gameInstance.TryAddItemToInventory(gameInstance.inventories.stash, obj.targetItem, obj.targetValue);
            }
        }

        gameInstance.RefreshQuestDisplays();
    }
    
#endif
    
}