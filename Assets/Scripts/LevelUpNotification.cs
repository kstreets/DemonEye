using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpNotification : MonoBehaviour {
    
    public Image burnEffectImage;
    public TextMeshProUGUI textMesh;
    
    private Sequence sequence;
    private static readonly int completion = Shader.PropertyToID("_Completion");
    
    public void Init() {
        gameObject.SetActive(false);
    }
    
    public void Show(string text, float delay = 0.6f) {
        sequence.Complete();
        
        textMesh.text = text;
        burnEffectImage.material.SetFloat(completion, 0f); 
        
        sequence = Sequence.Create();
        sequence.ChainDelay(delay);
        sequence.ChainCallback(this, static (notification) => notification.gameObject.SetActive(true));
        sequence.Chain(Tween.Custom(this, 0f, 1f, 2.6f, static (notification, comp) => { 
            notification.burnEffectImage.material.SetFloat(completion, comp); 
        }));
        sequence.Group(Tween.Alpha(textMesh, 0f, 1f, 1f));
        sequence.Group(Tween.Alpha(textMesh, 1f, 0f, 0.4f, startDelay: 2.2f));
        sequence.OnComplete(this, static (notification)  => notification.gameObject.SetActive(false));
    }
    
}
