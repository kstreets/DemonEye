using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SkillLevelUpRow : MonoBehaviour {

    public Sprite emptyLevelProgressDotSprite;
    public Sprite filledLevelProgressDotSprite;
    public Image burnEffectImage;
    public Image scortchedImage;
    public BurnEdgeEmberSpawner emberSpawner;
    public TextMeshProUGUI levelProgressText;
    public TextMeshProUGUI statModifiersDesc;
    public TextMeshProUGUI levelUpCostText;
    public ButtonFeel levelUpButton;
    public Image[] levelProgressDots;
    
    private static readonly int dissolveAmountId = Shader.PropertyToID("_Completion");
    private static readonly int opacityId = Shader.PropertyToID("_Opacity");

    public void Init(int maxLevel, string statDesc) {
        Assert.IsTrue(levelProgressDots.Length >= maxLevel, $"Need to up to {maxLevel} level dots, currently have {levelProgressDots.Length}");
        statModifiersDesc.text = statDesc;
        for (int i = 0; i < levelProgressDots.Length; i++) {
            levelProgressDots[i].gameObject.SetActive(i < maxLevel);
            levelProgressDots[i].sprite = emptyLevelProgressDotSprite;
        }
        
        burnEffectImage.material = new(burnEffectImage.material);
        burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
        
        scortchedImage.material = new(scortchedImage.material);
        scortchedImage.material.SetFloat(opacityId, 0f);
        scortchedImage.material.SetFloat(dissolveAmountId, 0f);
    }

    public void Refresh(int curLevel, int maxLevel, int soulsNeeded, bool enableButton) {
        if (enableButton) {
            levelUpButton.Enable();
        }
        else {
            levelUpButton.Disable();
        }

        levelProgressText.text = $"{curLevel}/{maxLevel}";
        levelUpCostText.text = $"<sprite=1> {soulsNeeded:N0}";

        for (int i = 0; i < curLevel; i++) {
            levelProgressDots[i].sprite = filledLevelProgressDotSprite;
        }
    }
    
    private QuestUI.BurnData burnData = new();
    private Tween emberTween;
    private Tween scortchedTween;
    private Tween burningTween;

    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particlesCurve) {
        emberTween.Complete();
        scortchedTween.Complete();
        burningTween.Complete();
        
        burnData.burnEffectImage = burnEffectImage;
        burnData.scortchedImage = scortchedImage;
        burnData.emberSpawner = emberSpawner;
        burnData.edgeCurve = edgeCurve;
        burnData.particleCurve = particlesCurve;
        
        emberSpawner.Play();
        emberTween = Tween.Delay(emberSpawner, duration * 0.45f, static (emberSpawner) => emberSpawner.Stop());
        
        scortchedImage.material.SetFloat(opacityId, 1f);
        scortchedTween = Tween.Custom(scortchedImage, 1f, 0f, duration, startDelay: duration * 0.35f, onValueChange: static (scortchedImage, comp) => {
            scortchedImage.material.SetFloat(opacityId, comp);
        })
        .OnComplete(scortchedImage, static (scortchedImage) => scortchedImage.material.SetFloat(opacityId, 0f));
        
        burningTween = Tween.Custom(burnData, 0f, 1f, duration, onValueChange: static (data, comp) => {
            float burnComp = data.edgeCurve.Evaluate(comp);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, burnComp);
            data.scortchedImage.material.SetFloat(dissolveAmountId, burnComp);
            data.emberSpawner.BurnProgress = data.particleCurve.Evaluate(comp);
        })
        .OnComplete(burnData, static (data) => {
            data.scortchedImage.material.SetFloat(dissolveAmountId, 0f);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
        });
    }
    
}
