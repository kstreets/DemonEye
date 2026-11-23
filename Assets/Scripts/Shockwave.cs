using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

public class Shockwave : MonoBehaviour {

    public SpriteRenderer spriteRenderer;
    Tween tween;
    const float defaultRadius = 0.5f;
    float defaultPixelDensity;
    float defaultThickness;

    void Awake() {
        spriteRenderer.material = new(spriteRenderer.material);
        defaultPixelDensity = spriteRenderer.material.GetFloat(ShaderIds.pixelDensity);
        defaultThickness = spriteRenderer.material.GetFloat(ShaderIds.thickness);
    }

    public void Animate(float radius, float duration) {
        Assert.IsTrue(radius > Mathf.Epsilon, "Radius needs to be greater than 0.");

        float scale = radius / defaultRadius;
        transform.localScale = new(scale, scale, 1f);
        spriteRenderer.material.SetFloat(ShaderIds.pixelDensity, defaultPixelDensity * scale);

        // The thickness is in uv units, dividing by the scale keeps it the same number of pixels thick at any radius
        spriteRenderer.material.SetFloat(ShaderIds.thickness, defaultThickness / scale);
        spriteRenderer.material.SetFloat(ShaderIds.completion, 0f);

        tween.Stop();
        tween = Tween.Custom(this, 0f, 1f, duration, ease: Ease.OutCubic, onValueChange: static (shockwave, comp) => {
            shockwave.spriteRenderer.material.SetFloat(ShaderIds.completion, comp);
            shockwave.spriteRenderer.color = shockwave.spriteRenderer.color.Alpha(1f - comp);
        });
    }

}
