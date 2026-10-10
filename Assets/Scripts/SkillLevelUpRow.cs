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
    
    public void Init(int maxLevel, string statDesc) {
        Assert.IsTrue(levelProgressDots.Length >= maxLevel, $"Need to up to {maxLevel} level dots, currently have {levelProgressDots.Length}");
        statModifiersDesc.text = statDesc;
        for (int i = 0; i < levelProgressDots.Length; i++) {
            levelProgressDots[i].gameObject.SetActive(i < maxLevel);
            levelProgressDots[i].sprite = emptyLevelProgressDotSprite;
        }

        burnEffect = new(burnEffectImage, emberSpawner, scortchedImage, emberStopPoint: 0.45f);
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

    public void RefreshAtMaxLevel(int maxLevel) {
        levelUpButton.Disable();
        levelProgressText.text = $"{maxLevel}/{maxLevel}";
        levelUpCostText.text = "Max";

        for (int i = 0; i < maxLevel; i++) {
            levelProgressDots[i].sprite = filledLevelProgressDotSprite;
        }
    }
    
    private UIBurnEffect burnEffect;

    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particlesCurve) {
        burnEffect.Burn(duration, edgeCurve, particlesCurve);
    }
    
}
