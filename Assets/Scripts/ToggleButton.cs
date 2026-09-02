using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour {

    public Button button;
    public Image image;
    public GameObject notifier;
    public TextMeshProUGUI text;
    public Action buttonListenerCallback;
    
    private void Awake() {
        notifier.SetActive(false);
    }
    
    public void AddListener(Action callback) {
        buttonListenerCallback = callback;
    }

    public void InvokeListenerCallback() {
        buttonListenerCallback?.Invoke();
    }
    
}
