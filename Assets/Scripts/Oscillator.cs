using PrimeTween;
using UnityEngine;

public class Oscillator : MonoBehaviour {
    
    public float duration;
    public float distance;
    [Range(0f, 1f)] 
    public float startingOffset;
    public Ease ease;
    
    private Vector3 startingPos;
    private Tween tween;
    
    public void OnEnable() {
        startingPos = transform.localPosition;
        transform.localPosition = startingPos.Offset(y: distance);
        tween = Tween.LocalPosition(transform, startingPos.Offset(y: -distance), duration, ease, cycleMode: CycleMode.Yoyo, cycles: -1);
        tween.progress = startingOffset;
    }
    
    public void OnDisable() {
        tween.Stop();
        transform.localPosition = startingPos;
    }
    
}
