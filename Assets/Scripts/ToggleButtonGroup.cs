using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ToggleButtonGroup : MonoBehaviour {

    public Styles styles;
    public Sprite selectedSprite;
    public Sprite nonSelectedSprite;
    public List<ToggleButton> toggles = new();
    public DynamicClip pressedClip;
    
    private Dictionary<ToggleButton, UnityAction> callbacks = new();

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
    
    public void Move(int step) {
        if (toggles.Count == 0) return;
        int curIndex = toggles.IndexOf(GetSelected());
        
        for (int i = 1; i <= toggles.Count; i++) {
            int index = ((curIndex + step * i) % toggles.Count + toggles.Count) % toggles.Count;
            if (index == curIndex) return;
            
            ToggleButton toggle = toggles[index];
            if (toggle.button.IsInteractable()) {
                toggle.button.onClick.Invoke();
                return;
            }
        }
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
