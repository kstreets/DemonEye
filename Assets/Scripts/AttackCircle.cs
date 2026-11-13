using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

public class AttackCircle : MonoBehaviour {
    
    public SpriteRenderer spriteRenderer;
    private Tween tween;
    
    private const float defaultRadius = 0.5f;
    private float defaultPixelDensity;
    private float defaultBorder;

    private void Awake() {
        spriteRenderer.material = new(spriteRenderer.material);
        defaultPixelDensity = spriteRenderer.material.GetFloat(ShaderIds.pixelDensity);
        defaultBorder = spriteRenderer.material.GetFloat(ShaderIds.border);
    }
    
    public void Animate(float radius, float animateInTime, float startDelay = 0f) {
        Assert.IsTrue(radius > Mathf.Epsilon, "Radius needs to be greater than or equal to 0.");
        
        float scale = radius / defaultRadius;
        transform.localScale = new(scale, scale, 1f);
        
        float pixelDensity = defaultPixelDensity * scale;
        spriteRenderer.material.SetFloat(ShaderIds.pixelDensity, pixelDensity);

        // The border is in uv units, dividing by the scale keeps it the same number of pixels thick at any radius
        spriteRenderer.material.SetFloat(ShaderIds.border, defaultBorder / scale);
        
        if (startDelay > 0f) {
            gameObject.SetActive(false);
        }
        
        tween.Stop();
        tween = Tween.Custom(this, 0f, 1f, animateInTime, startDelay: startDelay, onValueChange: static (attackCircle, comp) => {
            if (!attackCircle.gameObject.activeInHierarchy) {
                attackCircle.gameObject.SetActive(true);
            }
            attackCircle.spriteRenderer.material.SetFloat(ShaderIds.completion, comp);
        });
    }
    
    public void Hide() {
        tween.Complete();
        transform.localScale = Vector3.one;
    }
    
}
