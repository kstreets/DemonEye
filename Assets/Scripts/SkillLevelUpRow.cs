using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SkillLevelUpRow : MonoBehaviour {

    public Sprite emptyLevelProgressDotSprite;
    public Sprite filledLevelProgressDotSprite;
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

}
