using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static Game;

public class QuestUI : MonoBehaviour {

    public ButtonFeel completeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI repRewardText;
    public List<QuestObjectiveUI> objectiveUIs;
    
    public void Display(Quest quest) {
        titleText.text = quest.title;
        descText.text = quest.description;
        repRewardText.text = $"+{quest.traderReputationReward} Trader Rep";

        if (QuestIsComplete(quest)) {
            completeButton.Enable();
        }
        else {
            completeButton.Disable();
        }
        
        foreach (QuestObjectiveUI objUI in objectiveUIs) {
            objUI.gameObject.SetActive(false);
        }
        
        Assert.IsTrue(quest.objectives.Count <= objectiveUIs.Count, "Not enough objective UIs for quest objectives");

        for (int i = 0; i < quest.objectives.Count; i++) {
            ObjectiveData obj = quest.objectives[i];
            QuestObjectiveUI objUI = objectiveUIs[i];
            objUI.gameObject.SetActive(true);
            objUI.UpdateDisplay(quest, obj);
        }
    }

}
