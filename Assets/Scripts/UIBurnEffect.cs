using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class UIBurnEffect {

    private readonly Image burnEffectImage;
    private readonly BurnEdgeEmberSpawner emberSpawner;
    private readonly Image scortchedImage;
    private readonly Mask burnMask;
    private readonly float emberStopPoint;

    private AnimationCurve edgeCurve;
    private AnimationCurve particleCurve;
    private Tween emberTween;
    private Tween scortchedTween;
    private Tween burningTween;

    public UIBurnEffect(Image burnEffectImage, BurnEdgeEmberSpawner emberSpawner, Image scortchedImage = null, Mask burnMask = null, float emberStopPoint = 1f) {
        this.burnEffectImage = burnEffectImage;
        this.emberSpawner = emberSpawner;
        this.scortchedImage = scortchedImage;
        this.burnMask = burnMask;
        this.emberStopPoint = emberStopPoint;

        burnEffectImage.material = new(burnEffectImage.material);

        if (scortchedImage != null) {
            scortchedImage.material = new(scortchedImage.material);
            scortchedImage.material.SetFloat(ShaderIds.opacity, 0f);
        }

        if (burnMask != null) {
            burnMask.graphic.material = new(burnMask.graphic.material);
        }

        SetCompletion(0f);
    }

    public void SetAspectRatio(float aspectRatio) {
        burnEffectImage.material.SetFloat(ShaderIds.aspectRatio, aspectRatio);
        if (burnMask != null) {
            burnMask.graphic.materialForRendering.SetFloat(ShaderIds.aspectRatio, aspectRatio);
        }
    }

    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particleCurve) {
        emberTween.Complete();
        scortchedTween.Complete();
        burningTween.Complete();

        this.edgeCurve = edgeCurve;
        this.particleCurve = particleCurve;

        emberSpawner.Play();
        emberTween = Tween.Delay(emberSpawner, duration * emberStopPoint, static (emberSpawner) => emberSpawner.Stop());

        if (scortchedImage != null) {
            scortchedImage.material.SetFloat(ShaderIds.opacity, 1f);
            scortchedTween = Tween.Custom(scortchedImage, 1f, 0f, duration, startDelay: duration * 0.35f, onValueChange: static (scortchedImage, comp) => {
                scortchedImage.material.SetFloat(ShaderIds.opacity, comp);
            })
            .OnComplete(scortchedImage, static (scortchedImage) => scortchedImage.material.SetFloat(ShaderIds.opacity, 0f));
        }

        burningTween = Tween.Custom(this, 0f, 1f, duration, onValueChange: static (burn, comp) => {
            burn.SetCompletion(burn.edgeCurve.Evaluate(comp));
            if (burn.burnMask != null) {
                burn.SetOffsetSize();
            }
            burn.emberSpawner.BurnProgress = burn.particleCurve.Evaluate(comp);
        })
        .OnComplete(this, static (burn) => burn.SetCompletion(0f));
    }

    private void SetCompletion(float completion) {
        burnEffectImage.material.SetFloat(ShaderIds.completion, completion);
        if (scortchedImage != null) {
            scortchedImage.material.SetFloat(ShaderIds.completion, completion);
        }
        if (burnMask != null) {
            burnMask.graphic.materialForRendering.SetFloat(ShaderIds.completion, completion);
        }
    }

    private void SetOffsetSize() {
        Vector4 offsetAndSize = burnEffectImage.OffsetAndSizeInTexture();
        burnEffectImage.material.SetVector(ShaderIds.offsetSize, offsetAndSize);
        burnMask.graphic.materialForRendering.SetVector(ShaderIds.offsetSize, offsetAndSize);
    }
}
