using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour {

    public Button button;
    public Image image;
    public TextMeshProUGUI text;
    public Action buttonListenerCallback;
    
    public void AddListener(Action callback) {
        buttonListenerCallback = callback;
    }

    public void InvokeListenerCallback() {
        buttonListenerCallback?.Invoke();
    }
    
}
