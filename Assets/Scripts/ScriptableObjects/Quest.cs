using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "Scriptable Objects/Quest")]
public class Quest : ScriptableObject {

    [Serializable]
    public class ItemWithCount {
        public Item item;
        public int count;
    }

    [Serializable]
    public class SaveState {
        public List<int> objectiveCounts;
        public bool canComplete;
    }
    
    [Header("Info")]
    public string title;
    [TextArea] public string description;

    [Header("Rewards")]
    public int traderReputationReward;
    public List<ItemWithCount> itemRewards;

    // Assigned at runtime based on what quest line this quest is apart of to reduce editing
    [NonSerialized] public Trader questGiver;
    [NonSerialized] public bool canCompleteQuestFlag;

    public void Init(Trader thisQuestsGiver) {
        questGiver = thisQuestsGiver;
        OnInit();
    }
    
    public void LoadSaveState(SaveState saveState) {
        canCompleteQuestFlag = saveState.canComplete;
        OnLoadSaveState(saveState);
    }
    
    public SaveState GetSaveState() {
        SaveState saveState = new() {
            objectiveCounts = new(),
            canComplete = canCompleteQuestFlag,
        };
        OnWriteToSaveState(saveState);
        return saveState;
    }

    protected virtual void OnInit() { }
    public virtual void OnComplete() { }
    public virtual void UpdateQuest(Game game) { }
    protected virtual void OnLoadSaveState(SaveState saveState) { }
    protected virtual void OnWriteToSaveState(SaveState saveState) { }

}