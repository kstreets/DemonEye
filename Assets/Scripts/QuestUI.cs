using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour {

    public ButtonFeel completeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI repRewardText;
    public RectTransform objectivesParent;
    public GameObject objectiveNumericalPrefab;
    public GameObject objectiveBinaryPrefab;

    private List<QuestObjectiveUI> objectiveUIs;
    
    public void Display(Quest quest) {
        titleText.text = quest.title;
        descText.text = quest.description;
        repRewardText.text = $"+{quest.traderReputationReward} Trader Rep";

        if (quest.IsComplete()) completeButton.Enable(); else completeButton.Disable();

        for (int i = 0; i < objectivesParent.childCount; i++) {
            Destroy(objectivesParent.GetChild(i).gameObject);
        } 
        
        foreach (Quest.Objective obj in quest.objectives) {
            GameObject objUIGameobject = Instantiate(obj.display == QuestObjectiveUI.Display.Numerical ? objectiveNumericalPrefab : objectiveBinaryPrefab, objectivesParent);
            objUIGameobject.GetComponent<QuestObjectiveUI>().UpdateDisplay(obj);
        }
    }

}
