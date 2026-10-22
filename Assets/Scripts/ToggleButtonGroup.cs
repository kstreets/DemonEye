using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ToggleButtonGroup : MonoBehaviour {

    public enum NavigationMode {
        None,
        Primary,
        Secondary,
    }

    public NavigationMode navigationMode;
    public TextMeshProUGUI leftInputPrompt;
    public TextMeshProUGUI rightInputPrompt;
    public Styles styles;
    public Sprite selectedSprite;
    public Sprite nonSelectedSprite;
    public List<ToggleButton> toggles = new();
    public DynamicClip pressedClip;
    
    private Dictionary<ToggleButton, UnityAction> callbacks = new();
    private bool isLocked;
    private bool isHidden;

    public bool TogglesUsable => !isHidden && !isLocked;

    private void Awake() {
        if (toggles.Count <= 0) return;
        
        foreach (ToggleButton toggle in toggles) {
            InitializeToggle(toggle);
        } 
        OnButtonClicked(toggles[0]);
    }
    
    public bool IsSelected(ToggleButton toggle) {
        return toggle.image.sprite == selectedSprite;
    }
    
    public ToggleButton GetSelected() {
        foreach (ToggleButton toggle in toggles) {
            if (IsSelected(toggle)) {
                return toggle;
            }
        }
        return null;
    }
    
    public void ManualyToggle(ToggleButton toggle) {
        OnButtonClicked(toggle);
    }

    public void ManualyToggleCosmetically(ToggleButton toggle) {
        OnButtonClicked(toggle, false);
    }
    
    public void SetTogglesHidden(bool hide) {
        isHidden = hide;
        foreach (ToggleButton toggleButton in toggles) {
            toggleButton.gameObject.SetActive(!hide);
        }
    }

    public void SetTogglesLocked(bool locked) {
        isLocked = locked;
        foreach (ToggleButton toggle in toggles) {
            toggle.button.interactable = !locked;
        }
    }

    public void Move(int step) {
        if (toggles.Count == 0) return;
        int curIndex = toggles.IndexOf(GetSelected());
        
        for (int i = 1; i <= toggles.Count; i++) {
            int index = ((curIndex + step * i) % toggles.Count + toggles.Count) % toggles.Count;
            if (index == curIndex) return;
            
            ToggleButton toggle = toggles[index];
            if (toggle.gameObject.activeInHierarchy && toggle.button.IsInteractable()) {
                toggle.button.onClick.Invoke();
                return;
            }
        }
    }
    
    public void Add(ToggleButton toggle) {
        InitializeToggle(toggle);
        toggles.Add(toggle);
        toggle.button.interactable = !isLocked;

        // New toggles get created as the last child, so keep them before the right input prompt when they share a layout
        if (rightInputPrompt && rightInputPrompt.transform.parent == toggle.transform.parent) {
            toggle.transform.SetSiblingIndex(rightInputPrompt.transform.GetSiblingIndex());
        }
        if (toggles.Count == 1) {
            OnButtonClicked(toggle);
        }
    }

    private void InitializeToggle(ToggleButton toggle) {
        // Toggles added before Awake (e.g. quest toggles created while the panel is inactive) are already set up,
        // and adding the onClick listener again would make each click play the sound and select twice
        if (callbacks.ContainsKey(toggle)) return;
        
        UnityAction callback = () => {
            // Place sound here because we only want it to play when we actually click on it.
            // OnButtonClicked can be manually called without the player pressing the button.
            Game.gameInstance.PlayAudioClip(pressedClip);
            OnButtonClicked(toggle);
        };
        callbacks.Add(toggle, callback);
        toggle.button.onClick.AddListener(callback);
    }

    private void OnButtonClicked(ToggleButton clickedToggle, bool invokeCallbacks = true) {
        foreach (ToggleButton toggle in toggles) {
            bool selected = toggle == clickedToggle;
            toggle.image.sprite = selected ? selectedSprite : nonSelectedSprite;
            toggle.text.margin = selected ? styles.selectedHideoutTabMargin : styles.nonSelectedHideoutTabMargin;
        }

        if (invokeCallbacks) {
            clickedToggle.InvokeListenerCallback();
        }
    }

}
