using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using PrimeTween;
using VInspector;
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
    public ParticleSystem burnParticles;
    
    [Range(0f, 1f)]
    public float editorBurnComp;
    
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
    
    private class BurnData {
        public Mask burnMask;
        public Image burnEffectImage;
        public AnimationCurve curve;
    }
    private BurnData burnData = new();
    
    public void Burn(float duration, AnimationCurve curve) {
        float aspectRation = rectTransform.rect.width / rectTransform.rect.height;
        burnMask.graphic.materialForRendering.SetFloat(aspectRatioId, aspectRation);
        burnEffectImage.material.SetFloat(aspectRatioId, aspectRation);
        
        burnData.burnMask = burnMask;
        burnData.burnEffectImage = burnEffectImage;
        burnData.curve = curve;
        
        burnParticles.Play();
        
        Tween.Custom(burnData, 0f, 1f, duration, onValueChange: static (data, comp) => {
            Material maskMat = data.burnMask.graphic.materialForRendering;
            Material burnMat = data.burnEffectImage.material;
            Vector4 offsetAndSize = GetOffsetAndSizeIntoTexture(data.burnEffectImage);
            comp = data.curve.Evaluate(comp);
            
            maskMat.SetFloat(dissolveAmountId, comp);
            burnMat.SetFloat(dissolveAmountId, comp);
            maskMat.SetVector(offsetSizeId, offsetAndSize);
            burnMat.SetVector(offsetSizeId, offsetAndSize);
        })
        .OnComplete(burnData, static (data) => {
            data.burnMask.graphic.materialForRendering.SetFloat(dissolveAmountId, 0f);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
        });
    }
    
    [OnValueChanged(nameof(editorBurnComp))]
    private void UpdateBurnForEdtitor() {
        burnMask.graphic.materialForRendering.SetFloat(dissolveAmountId, editorBurnComp);
        burnEffectImage.material.SetFloat(dissolveAmountId, editorBurnComp);
        burnEffectImage.material.SetVector(offsetSizeId, GetOffsetAndSizeIntoTexture(burnEffectImage));
    }

}
