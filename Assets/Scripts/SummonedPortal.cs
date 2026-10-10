using PrimeTween;
using UnityEngine;

public class SummonedPortal : MonoBehaviour {
    
    public SpriteRenderer spriteRenderer;
    public float presentDelay;
    public float rotationSpeed;
    public AnimationCurve openAnimationCurve; 
    public AnimationCurve closeAnimationCurve; 
    
    private float rotation;
    private Limiter presentLimiter;
    private Tween openPortalAnimationTween;
    private Sequence openingTween;
    private Tween closingTween;
    private bool activeStateAfterClose;
    
    public void Init() {
        spriteRenderer.material = new(spriteRenderer.sharedMaterial);
    }
    
    public void Open() {
        closingTween.Stop();
        StartAnimating();
        
        openingTween = Tween.Custom(this, 0f, 1f, 0.9f, onValueChange: static (portal, comp) => {
            comp = portal.openAnimationCurve.Evaluate(comp);
            portal.spriteRenderer.material.SetFloat(ShaderIds.fill, comp);
        })
        .Group(
            Tween.Custom(this, rotationSpeed * 6f, rotationSpeed, 3f, onValueChange: static (portal, speed) => {
                portal.rotationSpeed = speed;
            })
        );
    }
    
    public void Close(bool activeStateOnComplete) {
        openingTween.Stop();
        activeStateAfterClose = activeStateOnComplete;
        
        closingTween = Tween.Custom(this, 1f, 0f, 1f, onValueChange: static (portal, comp) => {
            comp = portal.closeAnimationCurve.Evaluate(comp);
            portal.spriteRenderer.material.SetFloat(ShaderIds.fill, comp);
        })
        .OnComplete(this, static (portal) => {
            portal.StopAnimating();
            portal.gameObject.SetActive(portal.activeStateAfterClose); 
        });
    }
    
    public void StartAnimating() {
        openPortalAnimationTween = Tween.Custom(this, 0f, 0f, 1f, cycles: -1, onValueChange: static (portal, _) => {
            portal.UpdateAnimation();
        });
    }
    
    public void StopAnimating() {
        openPortalAnimationTween.Stop();
    }
    
    private void UpdateAnimation() {
        rotation += rotationSpeed * Time.deltaTime;
        rotation %= 360f;
        if (!presentLimiter.TimeHasPassed(presentDelay)) return;
        
        spriteRenderer.sharedMaterial.SetFloat(ShaderIds.aspectRatio, spriteRenderer.sprite.AspectRatio());
        spriteRenderer.sharedMaterial.SetVector(ShaderIds.offsetSize, spriteRenderer.sprite.OffsetAndSizeInTexture());
        spriteRenderer.sharedMaterial.SetFloat(ShaderIds.rotation, rotation);
    }
    
}
