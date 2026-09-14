using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SkillLevelUpRow : MonoBehaviour {

    public Sprite emptyLevelProgressDotSprite;
    public Sprite filledLevelProgressDotSprite;
    public Image burnEffectImage;
    public TextMeshProUGUI levelProgressText;
    public TextMeshProUGUI statModifiersDesc;
    public TextMeshProUGUI levelUpCostText;
    public ButtonFeel levelUpButton;
    public Image[] levelProgressDots;
    
    public float burnNoiseScale;
    public float burnEdgeSize;
    public float burnPixelDensity;
    
    private static readonly int dissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int aspectRatioId = Shader.PropertyToID("_AspectRatio");
    private static readonly int offsetSizeId = Shader.PropertyToID("_Offset_Size");
    private static readonly int noiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int edgeSizeId = Shader.PropertyToID("_EdgeSize");
    private static readonly int pixelDensityId = Shader.PropertyToID("_PixelDensity");

    public void Init(int maxLevel, string statDesc) {
        Assert.IsTrue(levelProgressDots.Length >= maxLevel, $"Need to up to {maxLevel} level dots, currently have {levelProgressDots.Length}");
        statModifiersDesc.text = statDesc;
        for (int i = 0; i < levelProgressDots.Length; i++) {
            levelProgressDots[i].gameObject.SetActive(i < maxLevel);
            levelProgressDots[i].sprite = emptyLevelProgressDotSprite;
        }
        
        burnEffectImage.material = new(burnEffectImage.material);
        burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
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

    public void Burn(float duration, AnimationCurve curve) {
        float aspectRatio = (transform as RectTransform).AspectRatio();
        // burnMask.graphic.materialForRendering.SetFloat(aspectRatioId, aspectRatio);
        burnEffectImage.material.SetFloat(aspectRatioId, aspectRatio);
        burnEffectImage.material.SetFloat(edgeSizeId, burnEdgeSize);
        burnEffectImage.material.SetFloat(noiseScaleId, burnNoiseScale);
        burnEffectImage.material.SetFloat(pixelDensityId, burnPixelDensity);
        
        // burnData.burnMask = burnMask;
        burnData.burnEffectImage = burnEffectImage;
        burnData.edgeCurve = curve;
        
        Tween.Custom(burnData, 0f, 1f, duration, onValueChange: static (data, comp) => {
            // Material maskMat = data.burnMask.graphic.materialForRendering;
            Material burnMat = data.burnEffectImage.material;
            Vector4 offsetAndSize = data.burnEffectImage.OffsetAndSizeInTexture();
            comp = data.edgeCurve.Evaluate(comp);
            
            // maskMat.SetFloat(dissolveAmountId, comp);
            burnMat.SetFloat(dissolveAmountId, comp);
            // maskMat.SetVector(offsetSizeId, offsetAndSize);
            burnMat.SetVector(offsetSizeId, offsetAndSize);
        })
        .OnComplete(burnData, static (data) => {
            // data.burnMask.graphic.materialForRendering.SetFloat(dissolveAmountId, 0f);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
        });
    }
    
}
