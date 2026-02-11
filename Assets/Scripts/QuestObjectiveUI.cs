using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class QuestObjectiveUI : MonoBehaviour {

    public TextMeshProUGUI taskDesc;
    public Image fillImage;
    public TextMeshProUGUI progressText;
    
    public void UpdateDisplay(Quest.Objective objective) {
        if (taskDesc.text != objective.GetTaskDescription()) {
            taskDesc.text = objective.GetTaskDescription();
        }

        fillImage.fillAmount = Mathf.Clamp01(objective.progressValue / (float)objective.targetValue);
        progressText.text = $"{objective.progressValue}/{objective.targetValue}";
    }

}
