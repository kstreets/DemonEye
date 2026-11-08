using PrimeTween;
using UnityEngine;

public partial class Game {

    private void SetDeathWipe(float fill, float alpha) {
        Shader.SetGlobalFloat(ShaderIds.deathWipeFill, fill);
        Shader.SetGlobalFloat(ShaderIds.deathWipeAlpha, alpha);
        UpdateDeathWipePassActive();
    }

    private Tween TweenDeathWipeFill(float startValue, float endValue, float duration, Ease ease) {
        return Tween.Custom(startValue, endValue, duration, static val => {
            Shader.SetGlobalFloat(ShaderIds.deathWipeFill, val);
            UpdateDeathWipePassActive();
        }, ease);
    }

    private Tween TweenDeathWipeAlpha(float startValue, float endValue, float duration, Ease ease) {
        return Tween.Custom(startValue, endValue, duration, static val => {
            Shader.SetGlobalFloat(ShaderIds.deathWipeAlpha, val);
            UpdateDeathWipePassActive();
        }, ease);
    }

    private static void UpdateDeathWipePassActive() {
        bool visible = Shader.GetGlobalFloat(ShaderIds.deathWipeFill) > 0f && Shader.GetGlobalFloat(ShaderIds.deathWipeAlpha) > 0f;
        RenderManager.SetDeathWipePassActive(visible);
    }
    
    private Tween damageVignetteTween;

    private void AnimateDamageVignette(float intensity) {
        damageVignetteTween.Stop();
        damageVignetteTween = Tween.Custom(intensity, 0f, 0.5f, static val => SetDamageVignetteIntensity(val), Ease.OutQuad);
    }

    private void ClearDamageVignette() {
        damageVignetteTween.Stop();
        SetDamageVignetteIntensity(0f);
    }

    private static void SetDamageVignetteIntensity(float intensity) {
        Shader.SetGlobalFloat(ShaderIds.damageIntensity, intensity);
        RenderManager.SetDamageVignettePassActive(intensity > 0f);
    }

}
