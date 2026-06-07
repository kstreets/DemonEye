using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;
using static Game;

[CreateAssetMenu(fileName = "Quest", menuName = "Scriptable Objects/Quest")]
public class Quest : ScriptableObject {

    [Header("Info")]
    public string title;
    [TextArea(minLines: 6, maxLines: 10)] 
    public string description;
    
    [Header("Rewards")]
    public int traderReputationReward;
    
    public List<ObjectiveData> objectives;
    
#if UNITY_EDITOR
    
    [Button]
    private void CompleteForTesting() {
        if (!Application.isPlaying) {
            Debug.Log("Need to be in playmode to complete quest");
            return;
        }
        foreach (ObjectiveData obj in objectives) {
            obj.progressValue = obj.targetValue;
            // For fetch quests we need to actually have the items to complete it properly
            if (obj.type == QuestObjectiveTypes.FetchByItem) {
                gameInstance.TryAddItemToInventory(gameInstance.inventories.stash, obj.targetItem, obj.targetValue);
            }
        }
        gameInstance.RefreshQuestDisplays();
    }
    
#endif

}