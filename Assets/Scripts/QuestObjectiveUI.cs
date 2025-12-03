using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class QuestObjectiveUI : MonoBehaviour {

    public enum Display { Numerical, Binary }
    
    public Display display;
    public TextMeshProUGUI taskDesc;

    [ShowIf(nameof(display), Display.Numerical)]
    public Image fillImage;
    public TextMeshProUGUI progressText;
    [EndIf]
    
    [ShowIf(nameof(display), Display.Binary)]
    public TextMeshProUGUI completionText;

    public void UpdateDisplay(Quest.Objective objective) {
        if (taskDesc.text != objective.task) {
            taskDesc.text = objective.task;
        }
        
        if (display == Display.Numerical) {
            fillImage.fillAmount = Mathf.Clamp01(objective.progressValue / (float)objective.targetValue);
            progressText.text = $"{objective.progressValue}/{objective.targetValue}";
        }
        
        if (display == Display.Binary) {
            completionText.text = objective.completed ? "Complete" : "Not Completed";
        }
    }

}
