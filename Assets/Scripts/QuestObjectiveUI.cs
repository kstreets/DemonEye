using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using static Game;

public class QuestObjectiveUI : MonoBehaviour {

    public TextMeshProUGUI taskDesc;
    public GameObject progressBar;
    public Image fillImage;
    public TextMeshProUGUI progressText;
    
    public void UpdateDisplay(ObjectiveData objective) {
        string desc = GetObjectiveDescription(objective);
        Assert.IsFalse(string.IsNullOrEmpty(desc), "Getting an empty objective description");
        
        if (taskDesc.text != desc) {
            taskDesc.text = desc;
        }

        bool showProgressUI = objective.type != QuestObjectiveTypes.Teleport;
        progressBar.gameObject.SetActive(showProgressUI);
        progressText.gameObject.SetActive(showProgressUI);

        fillImage.fillAmount = Mathf.Clamp01(objective.progressValue / (float)objective.targetValue);
        progressText.text = $"{objective.progressValue}/{objective.targetValue}";
    }

}
