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
        public List<int> objectiveProgresses;
    }
    
    public State state;

#if UNITY_EDITOR
    
    [Button]
    private void CompleteForTesting() {
        if (!Application.isPlaying) {
            Debug.Log("Need to be in playmode to complete quest");
            return;
        }

        for (int i = 0; i < objectives.Count; i++) {
            ObjectiveData obj = objectives[i];
            state.objectiveProgresses[i] = obj.targetValue;
            // For fetch quests we need to actually have the items to complete it properly
            if (obj.type == QuestObjectiveTypes.FetchByItem) {
                gameInstance.TryAddItemToInventory(gameInstance.inventories.stash, obj.targetItem, obj.targetValue);
            }
        }

        gameInstance.RefreshQuestDisplays();
    }
    
#endif
    
}