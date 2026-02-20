using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class QuestObjectiveUI : MonoBehaviour {

    public TextMeshProUGUI taskDesc;
    public GameObject progressBar;
    public Image fillImage;
    public TextMeshProUGUI progressText;
    
    public void UpdateDisplay(Quest.Objective objective) {
        if (taskDesc.text != objective.GetTaskDescription()) {
            taskDesc.text = objective.GetTaskDescription();
        }

        bool showProgressUI = objective.type != Quest.Objective.Type.Teleport;
        progressBar.gameObject.SetActive(showProgressUI);
        progressText.gameObject.SetActive(showProgressUI);

        fillImage.fillAmount = Mathf.Clamp01(objective.progressValue / (float)objective.targetValue);
        progressText.text = $"{objective.progressValue}/{objective.targetValue}";
    }

}
