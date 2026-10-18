using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VInspector;

public class ScreenScalingMaintainer : MonoBehaviour {
    
    [SerializeField] [ReadOnly] 
    private Vector2 defaultAnchorPos;
    private RectTransform rectTransform => transform as RectTransform;
    
    public void OnScreenSizeChanged(float canvasScaleFactor) {
        float scaler = (Screen.height / 1080f) / canvasScaleFactor;
        rectTransform.anchoredPosition = defaultAnchorPos * scaler;
    }
    
#if UNITY_EDITOR

    private void OnValidate() {
        CanvasScaler canvasScaler = GetComponentInParent<CanvasScaler>();
        if (canvasScaler == null || canvasScaler.scaleFactor != 1f) return;
        defaultAnchorPos = rectTransform.anchoredPosition;
        EditorUtility.SetDirty(this);        
    }

#endif
    
}