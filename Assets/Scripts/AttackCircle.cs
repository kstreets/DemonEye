using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

public class AttackCircle : MonoBehaviour {
    
    public SpriteRenderer spriteRenderer;
    private Tween tween;
    
    private const float defaultRadius = 0.5f;
    private float defaultPixelDensity;
    
    private void Awake() {
        spriteRenderer.material = new(spriteRenderer.material);
        defaultPixelDensity = spriteRenderer.material.GetFloat(ShaderIds.pixelDensity);
    }
    
    public void Animate(float radius, float animateInTime) {
        Assert.IsTrue(radius > Mathf.Epsilon, "Radius needs to be greater than or equal to 0.");
        
        float scale = radius / defaultRadius;
        transform.localScale = new(scale, scale, 1f);
        
        float pixelDensity = defaultPixelDensity * scale;
        spriteRenderer.material.SetFloat(ShaderIds.pixelDensity, pixelDensity);
        
        tween.Stop();
        tween = Tween.Custom(this, 0f, 1f, animateInTime, static (attackCircle, comp) => {
            attackCircle.spriteRenderer.material.SetFloat(ShaderIds.completion, comp);
        });
    }
    
    public void Hide() {
        tween.Complete();
        transform.localScale = Vector3.one;
    }
    
}
