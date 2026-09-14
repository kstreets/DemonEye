using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using PrimeTween;
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
    
    private static readonly int dissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int aspectRatioId = Shader.PropertyToID("_AspectRatio");
    private static readonly int offsetSizeId = Shader.PropertyToID("_Offset_Size");
    
    public void Init() {
        burnMask.graphic.material = new(burnMask.graphic.material);
        burnEffectImage.material = new(burnEffectImage.material);
        burnMask.graphic.materialForRendering.SetFloat(dissolveAmountId, 0f);
        burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
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
    
    public class BurnData {
        public Mask burnMask;
        public Image burnEffectImage;
        public BurnEdgeEmberSpawner emberSpawner;
        public AnimationCurve edgeCurve;
        public AnimationCurve particleCurve;
    }
    private BurnData burnData = new();
    
    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particleCurve) {
        float aspectRatio = rectTransform.AspectRatio();
        burnMask.graphic.materialForRendering.SetFloat(aspectRatioId, aspectRatio);
        burnEffectImage.material.SetFloat(aspectRatioId, aspectRatio);
        emberSpawner.emberParticles.Play();
        
        burnData.burnMask = burnMask;
        burnData.burnEffectImage = burnEffectImage;
        burnData.edgeCurve = edgeCurve;
        burnData.emberSpawner = emberSpawner;
        burnData.particleCurve = particleCurve;
        
        Tween.Custom(burnData, 0f, 1f, duration, onValueChange: static (data, comp) => {
            Material maskMat = data.burnMask.graphic.materialForRendering;
            Material burnMat = data.burnEffectImage.material;
            Vector4 offsetAndSize = data.burnEffectImage.OffsetAndSizeInTexture();
            float edgeComp = data.edgeCurve.Evaluate(comp);
            
            maskMat.SetFloat(dissolveAmountId, edgeComp);
            burnMat.SetFloat(dissolveAmountId, edgeComp);
            maskMat.SetVector(offsetSizeId, offsetAndSize);
            burnMat.SetVector(offsetSizeId, offsetAndSize);
            
            data.emberSpawner.BurnProgress = data.particleCurve.Evaluate(comp);
        })
        .OnComplete(burnData, static (data) => {
            data.burnMask.graphic.materialForRendering.SetFloat(dissolveAmountId, 0f);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
            data.emberSpawner.emberParticles.Stop();
        });
    }
    
}
