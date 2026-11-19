using PrimeTween;
using UnityEngine;

public static partial class Game {

    // Keep animating shaders while the game is paused
    static void UpdateGlobalShaders() {
        Shader.SetGlobalFloat(ShaderIds.unscaledTime, Time.unscaledTime);
    }

    static void SetDeathWipe(float fill, float alpha) {
        Shader.SetGlobalFloat(ShaderIds.deathWipeFill, fill);
        Shader.SetGlobalFloat(ShaderIds.deathWipeAlpha, alpha);
        UpdateDeathWipePassActive();
    }

    static Tween TweenDeathWipeFill(float startValue, float endValue, float duration, Ease ease) {
        return Tween.Custom(startValue, endValue, duration, static val => {
            Shader.SetGlobalFloat(ShaderIds.deathWipeFill, val);
            UpdateDeathWipePassActive();
        }, ease);
    }

    static Tween TweenDeathWipeAlpha(float startValue, float endValue, float duration, Ease ease) {
        return Tween.Custom(startValue, endValue, duration, static val => {
            Shader.SetGlobalFloat(ShaderIds.deathWipeAlpha, val);
            UpdateDeathWipePassActive();
        }, ease);
    }

    static void UpdateDeathWipePassActive() {
        bool visible = Shader.GetGlobalFloat(ShaderIds.deathWipeFill) > 0f && Shader.GetGlobalFloat(ShaderIds.deathWipeAlpha) > 0f;
        RenderManager.SetDeathWipePassActive(visible);
    }

    static Tween damageVignetteTween;

    static void AnimateDamageVignette(float intensity) {
        damageVignetteTween.Stop();
        damageVignetteTween = Tween.Custom(intensity, 0f, 0.5f, static val => SetDamageVignetteIntensity(val), Ease.OutQuad);
    }

    static void ClearDamageVignette() {
        damageVignetteTween.Stop();
        SetDamageVignetteIntensity(0f);
    }

    static void SetDamageVignetteIntensity(float intensity) {
        Shader.SetGlobalFloat(ShaderIds.damageIntensity, intensity);
        RenderManager.SetDamageVignettePassActive(intensity > 0f);
    }

}
