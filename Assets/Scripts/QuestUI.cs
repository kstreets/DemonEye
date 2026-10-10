using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using static Game;

public class QuestUI : MonoBehaviour {
    
    public RectTransform rectTransform;
    public ButtonFeel completeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI repRewardText;
    public List<QuestObjectiveUI> objectiveUIs;
    public Mask burnMask;
    public Image burnEffectImage;
    public BurnEdgeEmberSpawner emberSpawner;
    
    private UIBurnEffect burnEffect;

    public void Init() {
        burnEffect = new(burnEffectImage, emberSpawner, burnMask: burnMask);
    }
    
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
    
    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particleCurve) {
        burnEffect.SetAspectRatio(rectTransform.AspectRatio());
        burnEffect.Burn(duration, edgeCurve, particleCurve);
    }
    
}
