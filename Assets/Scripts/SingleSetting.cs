using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SingleSetting : MonoBehaviour {
    
    public Image[] breadcrumbs;
    public Sprite unselectedBreadcrumb;
    public Sprite selectedBreadcrumb;
    public TextMeshProUGUI curSettingText;
    public ButtonFeel leftButton;
    public ButtonFeel rightButton;
    
    public int appliedIndex;
    public int curIndex;
    public int numOfSettings;
    
    private Action<int> onSettingIndexChanged;
    private Action<int> onApplyCallback;
    
    public SingleSetting Init(int startingIndex, int settingsCount) {
        numOfSettings = settingsCount;
        curIndex = startingIndex;
        appliedIndex = int.MinValue;
        SetBreadcrumbIndex(startingIndex); 
        leftButton.AddListener(OnDecrement);
        rightButton.AddListener(OnIncrement);
        return this;
    }
    
    public SingleSetting OnChange(Action<int> callback) {
        onSettingIndexChanged = callback; 
        onSettingIndexChanged?.Invoke(curIndex);
        return this;
    }
    
    public void OnApply(Action<int> applyCallback) {
        onApplyCallback = applyCallback;
    }
    
    public void Display(string curSetting) {
        curSettingText.text = curSetting;
    }
    
    public void Apply() {
        if (curIndex == appliedIndex) return;
        onApplyCallback?.Invoke(curIndex);
        appliedIndex = curIndex;
    }
    
    public bool HasChangesToApply() {
        return curIndex != appliedIndex;
    }
    
    public void ForceChangeWithoutApplying(int index) {
        curIndex = index;
        appliedIndex = index;
        SetBreadcrumbIndex(index);
        onSettingIndexChanged?.Invoke(index);
    }
    
    private void SetBreadcrumbIndex(int index) {
        for (int i = 0; i < breadcrumbs.Length; i++) {
            if (i >= numOfSettings) {
                breadcrumbs[i].gameObject.SetActive(false);
                continue;
            }
            breadcrumbs[i].sprite = unselectedBreadcrumb; 
            breadcrumbs[i].gameObject.SetActive(true);
        }
        
        if (numOfSettings > breadcrumbs.Length) {
            index = Mathf.RoundToInt((curIndex / (float)numOfSettings) * (breadcrumbs.Length - 1));
        }
        breadcrumbs[index].sprite = selectedBreadcrumb;
    }
    
    private void OnIncrement() {
        curIndex = (curIndex + 1) % numOfSettings;
        if (onApplyCallback == null) {
            appliedIndex = curIndex;
        }
        
        SetBreadcrumbIndex(curIndex);
        onSettingIndexChanged?.Invoke(curIndex);
        TweenArrowButton(rightButton.rectTransform);
        Game.OnAnySettingsChanged();
    }
    
    private void OnDecrement() {
        curIndex = (curIndex - 1 + numOfSettings) % numOfSettings;
        if (onApplyCallback == null) {
            appliedIndex = curIndex;
        }
        
        SetBreadcrumbIndex(curIndex);
        onSettingIndexChanged?.Invoke(curIndex);
        TweenArrowButton(leftButton.rectTransform);
        Game.OnAnySettingsChanged();
    }
    
    private void TweenArrowButton(RectTransform arrowButton) {
        Tween.PunchScale(arrowButton, Vector3.one * 0.15f, 0.125f);
    }
    
}
