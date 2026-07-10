using System;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class Portal : MonoBehaviour {
    
    public enum State { Inactive, BeingSummoned, Open, Closed }
    [NonSerialized] public State state = State.Inactive;
    
    [Header("Crystal")]
    public Transform crystalTrans;
    public Oscillator crystalOscillator;
    public AnimationCurve crystalShakeMagnitudeCurve;
    public AnimationCurve crystalShakeJitterCurve;
    public Transform[] crystalFragments;
    
    [Header("Opened Portal")]
    public SpriteRenderer openPortalSpriteRenderer;
    public float presentDelay;
    public float rotationSpeed;
    
    private float rotation;
    private Limiter presentLimiter;
    private Tween openPortalAnimationTween;
    
    private Sequence openCloseSequence;
    
    private static readonly int aspectRatioId = Shader.PropertyToID("_AspectRatio");
    private static readonly int offsetSizeId = Shader.PropertyToID("_Offset_Size");
    private static readonly int rotationId = Shader.PropertyToID("_Rotation");
    private static readonly int fillId = Shader.PropertyToID("_Fill");
    
    public void Init() {
        openPortalSpriteRenderer.material = new(openPortalSpriteRenderer.sharedMaterial);
        openPortalSpriteRenderer.gameObject.SetActive(false);
    }
    
    public void StartOpenCloseSequence(float openDelay, float openDuration) {
        state = State.BeingSummoned;
        
        crystalOscillator.enabled = false;
        crystalTrans.DoTweenShake(12f, 0.01f, openDelay, crystalShakeMagnitudeCurve, crystalShakeJitterCurve);
        
        openCloseSequence = Sequence.Create();
        // Summoning
        openCloseSequence.ChainDelay(openDelay);
        
        // Open
        openCloseSequence.ChainCallback(this, static (portal) => {
            portal.crystalTrans.GetComponent<SpriteRenderer>().enabled = false;
            foreach (Transform fragTrans in portal.crystalFragments) {
                fragTrans.gameObject.SetActive(true);
                Vector3 endPos = fragTrans.position + Game.RotationVector(Random.Range(0f, 360f), 0.25f, 0.65f);
                AddBounceEffect(fragTrans, endPos, 0.9f);
            }
            
            portal.openPortalSpriteRenderer.gameObject.SetActive(true);
            portal.StartAnimating();
            portal.state = State.Open;
        });
        openCloseSequence.Chain(Tween.Custom(this, 0f, 1f, 2f, ease: Ease.InOutCubic, onValueChange: static (portal, comp) => {
            portal.openPortalSpriteRenderer.material.SetFloat(fillId, comp);
        }));
        
        // Staying Open
        openCloseSequence.ChainDelay(openDuration);
        
        // Close
        openCloseSequence.ChainCallback(this, static (portal) => portal.state = State.Closed);
        openCloseSequence.Chain(Tween.Custom(this, 1f, 0f, 1f, static (portal, comp) => {
            portal.openPortalSpriteRenderer.material.SetFloat(fillId, comp);
        }));
        openCloseSequence.ChainCallback(this, static (portal) => {
            portal.StopAnimating();
            portal.openPortalSpriteRenderer.gameObject.SetActive(false);
        });
    }
    
    public void StopClosingSequence() {
        openCloseSequence.Stop();
    }
    
    private void StartAnimating() {
        openPortalAnimationTween = Tween.Custom(this, 0f, 0f, 1f, cycles: -1, onValueChange: static (portal, _) => {
            portal.UpdateAnimation();
        });
    }
    
    private void StopAnimating() {
        openPortalAnimationTween.Stop();
    }
    
    private void UpdateAnimation() {
        rotation += rotationSpeed * Time.deltaTime;
        rotation %= 360f;
        if (!presentLimiter.TimeHasPassed(presentDelay)) return;
        
        openPortalSpriteRenderer.sharedMaterial.SetFloat(aspectRatioId, openPortalSpriteRenderer.sprite.AspectRatio());
        openPortalSpriteRenderer.sharedMaterial.SetVector(offsetSizeId, openPortalSpriteRenderer.sprite.OffsetAndSizeInTexture());
        openPortalSpriteRenderer.sharedMaterial.SetFloat(rotationId, rotation);
    }

    private static void AddBounceEffect(Transform trans, Vector3 pos, float duration) {
        Vector3 initialPos = trans.position;
        Tween.Custom(trans, 0f, 1f, duration, ease: Ease.Linear, onValueChange: (trans, val) => {
            float yPos = Game.gameInstance.curves.bounce.Evaluate(val);
            Vector2 newPos = Vector2.Lerp(initialPos, pos, val);
            trans.position = new(newPos.x, newPos.y + yPos, trans.position.z);
        });
    }
    
}
