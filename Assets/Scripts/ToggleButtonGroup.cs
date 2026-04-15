using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ToggleButtonGroup : MonoBehaviour {

    public Styles styles;
    public Sprite selectedSprite;
    public Sprite nonSelectedSprite;
    public List<ToggleButton> toggles = new();
    
    private Dictionary<ToggleButton, UnityAction> callbacks = new();

    private void Awake() {
        callbacks.Clear();
        
        if (toggles.Count <= 0) return;
        
        foreach (ToggleButton toggle in toggles) {
            InitializeToggle(toggle);
        } 
        OnButtonClicked(toggles[0]);
    }
    
    public void ManualyToggle(ToggleButton toggle) {
        OnButtonClicked(toggle);
    }

    public void ManualyToggleCosmetically(ToggleButton toggle) {
        OnButtonClicked(toggle, false);
    }

    public void Add(ToggleButton toggle) {
        InitializeToggle(toggle);
        toggles.Add(toggle);
        if (toggles.Count == 1) {
            OnButtonClicked(toggle);
        }
    }

    public void Remove(ToggleButton toggle) {
        UnityAction callback = callbacks[toggle];
        toggle.button.onClick.RemoveListener(callback);
        callbacks.Remove(toggle);
        toggles.Remove(toggle);
        if (toggles.Count == 1) {
            OnButtonClicked(toggles[0]);
        }
    }

    private void InitializeToggle(ToggleButton toggle) {
        UnityAction callback = () => OnButtonClicked(toggle);
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
