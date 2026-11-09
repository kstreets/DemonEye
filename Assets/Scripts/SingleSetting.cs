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
    
    [NonSerialized] public int appliedIndex;
    [NonSerialized] public int curIndex;
    [NonSerialized] public int numOfSettings;

    private Action<int> onSettingIndexChanged;
    private Action<int> onApplyCallback;
    private Func<int> getDefaultIndex;
    
    public void OnHorizontalNav(bool right) {
        if (right) {
            rightButton.TriggerClick();
        }
        else {
            leftButton.TriggerClick();
        }
    }
    
    // Without a saved index the setting starts on its default, so OnDefault has to come before OnChange
    public SingleSetting Init(int settingsCount, int? savedIndex = null) {
        numOfSettings = settingsCount;
        curIndex = savedIndex ?? -1;
        appliedIndex = int.MinValue;
        leftButton.AddListener(OnDecrement);
        rightButton.AddListener(OnIncrement);
        return this;
    }
    
    public SingleSetting OnDefault(Func<int> callback) {
        getDefaultIndex = callback;
        if (curIndex < 0) {
            curIndex = callback();
        }
        return this;
    }
    
    public SingleSetting OnChange(Action<int> callback) {
        onSettingIndexChanged = callback;
        SetBreadcrumbIndex(curIndex);
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
        
        // More settings than breadcrumbs, so spread them out with the first and last setting on the first and last breadcrumb
        if (numOfSettings > breadcrumbs.Length) {
            index = Mathf.RoundToInt(index / (float)(numOfSettings - 1) * (breadcrumbs.Length - 1));
        }
        breadcrumbs[index].sprite = selectedBreadcrumb;
    }
    
    // Like the player picking it, so settings with an apply step still wait for apply
    public void ResetToDefault() {
        ChangeTo(getDefaultIndex());
    }
    
    private void ChangeTo(int index) {
        curIndex = index;
        if (onApplyCallback == null) {
            appliedIndex = curIndex;
        }
    
        SetBreadcrumbIndex(curIndex);
        onSettingIndexChanged?.Invoke(curIndex);
        Game.OnAnySettingsChanged();
    }
    
    private void OnIncrement() {
        ChangeTo((curIndex + 1) % numOfSettings);
        TweenArrowButton(rightButton.rectTransform);
    }
    
    private void OnDecrement() {
        ChangeTo((curIndex - 1 + numOfSettings) % numOfSettings);
        TweenArrowButton(leftButton.rectTransform);
    }
    
    private void TweenArrowButton(RectTransform arrowButton) {
        // Unscaled so it still plays in the pause menu
        Tween.PunchScale(arrowButton, Vector3.one * 0.15f, 0.125f, useUnscaledTime: true);
    }
    
}
