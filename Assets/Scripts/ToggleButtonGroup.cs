using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ToggleButtonGroup : MonoBehaviour {

    // Which controller buttons switch this group. Primary uses the bumpers, Secondary uses the triggers.
    public enum NavigationMode {
        None,
        Primary,
        Secondary,
    }

    public NavigationMode navigationMode;
    // Optional, show the controller buttons that switch this group. Hidden when using mouse & keyboard.
    public TextMeshProUGUI leftInputPrompt;
    public TextMeshProUGUI rightInputPrompt;
    public Styles styles;
    public Sprite selectedSprite;
    public Sprite nonSelectedSprite;
    public List<ToggleButton> toggles = new();
    public DynamicClip pressedClip;
    
    private Dictionary<ToggleButton, UnityAction> callbacks = new();
    private readonly List<ToggleButton> togglesHiddenByGroup = new();

    public bool TogglesHidden { get; private set; }

    private void Awake() {
        callbacks.Clear();
        
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
    
    // Hides every toggle, so they can't be clicked or switched to with the controller, and hides the group's input prompts.
    // Which toggle is selected stays the same.
    public void SetTogglesHidden(bool hidden) {
        if (hidden == TogglesHidden) return;
        TogglesHidden = hidden;

        if (hidden) {
            togglesHiddenByGroup.Clear();
            foreach (ToggleButton toggle in toggles) {
                // Toggles already hidden by something else, e.g. unused pooled quest toggles, should stay hidden when showing them again
                if (!toggle.gameObject.activeSelf) continue;
                togglesHiddenByGroup.Add(toggle);
                toggle.gameObject.SetActive(false);
            }
            return;
        }

        foreach (ToggleButton toggle in togglesHiddenByGroup) {
            toggle.gameObject.SetActive(true);
        }
        togglesHiddenByGroup.Clear();
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

        // New toggles get created as the last child, so keep them before the right input prompt when they share a layout
        if (rightInputPrompt && rightInputPrompt.transform.parent == toggle.transform.parent) {
            toggle.transform.SetSiblingIndex(rightInputPrompt.transform.GetSiblingIndex());
        }
        if (toggles.Count == 1) {
            OnButtonClicked(toggle);
        }
    }

    public void Remove(ToggleButton toggle) {
        UnityAction callback = callbacks[toggle];
        toggle.button.onClick.RemoveListener(callback);
        callbacks.Remove(toggle);
        toggles.Remove(toggle);
        togglesHiddenByGroup.Remove(toggle);
        if (toggles.Count == 1) {
            OnButtonClicked(toggles[0]);
        }
    }

    private void InitializeToggle(ToggleButton toggle) {
        UnityAction callback = () => {
            // Place sound here because we only want it to play when we actually click on it.
            // OnButtonClicked can be manually called without the player pressing the button.
            Game.gameInstance.PlayAudioClip(pressedClip);
            OnButtonClicked(toggle);
        };
        callbacks.TryAdd(toggle, callback);
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
