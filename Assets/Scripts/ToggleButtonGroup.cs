using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleButtonGroup : MonoBehaviour {

    public Styles styles;
    public Sprite selectedSprite;
    public Sprite nonSelectedSprite;
    public List<ToggleButton> toggles = new();
    
    private Dictionary<ToggleButton, UnityAction> callbacks = new();

    private void Awake() {
        foreach (ToggleButton toggle in toggles) {
            Add(toggle);
        }
    }

    public void Add(ToggleButton toggle) {
        InitalizeToggle(toggle);
        if (toggles.Count <= 0) {
            OnButtonClicked(toggle);
        }
    }

    public void Remove(ToggleButton toggle) {
        UnityAction callback = callbacks[toggle];
        toggle.button.onClick.RemoveListener(callback);
        callbacks.Remove(toggle);
    }

    private void InitalizeToggle(ToggleButton toggle) {
        UnityAction callback = () => OnButtonClicked(toggle);
        callbacks.Add(toggle, callback);
        toggle.button.onClick.AddListener(callback);
    }

    private void OnButtonClicked(ToggleButton clickedToggle) {
        foreach (ToggleButton toggle in toggles) {
            bool selected = toggle == clickedToggle;
            toggle.image.sprite = selected ? selectedSprite : nonSelectedSprite;
            toggle.text.margin = selected ? styles.selectedHideoutTabMargin : styles.nonSelectedHideoutTabMargin;
        }
    }

}
