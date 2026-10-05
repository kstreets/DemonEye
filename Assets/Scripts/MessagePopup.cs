using System;
using PrimeTween;
using TMPro;
using UnityEngine;

public class MessagePopup : MonoBehaviour {

    public RectTransform rectTransform;
    public TextMeshProUGUI messageText;
    public ButtonFeel yesButton;
    public ButtonFeel noButton;

    private Action onYes;
    private Action onNo;
    private bool listenersAdded;

    public bool IsShowing => gameObject.activeInHierarchy;

    public void Show(string message, string yesText, string noText, Action onYes, Action onNo = null) {
        if (!listenersAdded) {
            yesButton.AddListener(OnYesPressed);
            noButton.AddListener(OnNoPressed);
            listenersAdded = true;
        }

        this.onYes = onYes;
        this.onNo = onNo;
        messageText.text = message;
        yesButton.text.text = yesText;
        noButton.text.text = noText;
        
        gameObject.SetActive(true);
        Tween.Scale(rectTransform, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack, useUnscaledTime: true);
    }

    public void Hide() {
        onYes = null;
        onNo = null;
        gameObject.SetActive(false);
    }

    public void Cancel() {
        OnNoPressed();
    }

    private void OnYesPressed() {
        Action callback = onYes;
        Hide();
        callback?.Invoke();
    }

    private void OnNoPressed() {
        Action callback = onNo;
        Hide();
        callback?.Invoke();
    }

}
