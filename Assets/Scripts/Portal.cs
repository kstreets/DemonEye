using System;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class Portal : MonoBehaviour {
    
    public enum State { Inactive, BeingSummoned, Open, Closed }
    [NonSerialized] public State state = State.Inactive;
    
    public ParticleSystem summoningParticles;
    public GameObject crystalExplosion;
    
    [Header("Crystal")]
    public Transform crystalTrans;
    public SpriteRenderer crystalSpriteRenderer;
    public Oscillator crystalOscillator;
    public AnimationCurve crystalShakeMagnitudeCurve;
    public AnimationCurve crystalShakeJitterCurve;
    public Transform[] crystalFragments;
    
    [Header("Opened Portal")]
    public SpriteRenderer openPortalSpriteRenderer;
    public float presentDelay;
    public float rotationSpeed;
    public AnimationCurve openPortalAnimationCurve; 
    
    private float rotation;
    private Limiter presentLimiter;
    private Tween openPortalAnimationTween;
    
    private Sequence openCloseSequence;
    private float particleStartSpeed;
    
    private static readonly int aspectRatioId = Shader.PropertyToID("_AspectRatio");
    private static readonly int offsetSizeId = Shader.PropertyToID("_Offset_Size");
    private static readonly int rotationId = Shader.PropertyToID("_Rotation");
    private static readonly int fillId = Shader.PropertyToID("_Fill");
    
    public void Init() {
        crystalSpriteRenderer.material = new(crystalSpriteRenderer.sharedMaterial);
        openPortalSpriteRenderer.material = new(openPortalSpriteRenderer.sharedMaterial);
        openPortalSpriteRenderer.gameObject.SetActive(false);
        crystalExplosion.SetActive(false);
        
        crystalSpriteRenderer.material.SetFloat(fillId, 0f);
        crystalSpriteRenderer.material.SetVector(offsetSizeId, crystalSpriteRenderer.sprite.OffsetAndSizeInTexture());
        
        summoningParticles.gameObject.SetActive(false);
        particleStartSpeed = summoningParticles.velocityOverLifetime.radialMultiplier;
        
        foreach (Transform fragTrans in crystalFragments) {
            fragTrans.gameObject.SetActive(false);
        }
    }
    
    public void StartOpenCloseSequence(float openDelay, float openDuration) {
        state = State.BeingSummoned;
        
        crystalTrans.DoTweenShake(15f, 0.015f, openDelay, crystalShakeMagnitudeCurve, crystalShakeJitterCurve);
        summoningParticles.gameObject.SetActive(true);
        
        Tween.PunchScale(crystalTrans, new(0.2f, 0.2f, 0f), 0.2f, 12f);
        
        // Summoning
        const float particleRampUpPercentage = 0.8f;
        TweenSettings particleSettings = new() { duration = openDelay * particleRampUpPercentage, ease = Ease.InSine };
        Tween.Custom(this, 0f, 1f, particleSettings, static (portal, comp) => {
            const float particleRampingSpeed = 1.7f;
            float emissionMultiplier = Mathf.Lerp(5f, 30f, comp);
            float speedMultiplier = Mathf.Lerp(portal.particleStartSpeed, portal.particleStartSpeed * particleRampingSpeed, comp);
            
            ParticleSystem.VelocityOverLifetimeModule velocity = portal.summoningParticles.velocityOverLifetime;
            ParticleSystem.EmissionModule emission = portal.summoningParticles.emission;
            emission.rateOverTimeMultiplier = emissionMultiplier; 
            velocity.radialMultiplier = speedMultiplier;
        })
        .OnComplete(this, static (portal) => portal.summoningParticles.Stop());
        
        TweenSettings crystalSettings = new() { duration = openDelay * particleRampUpPercentage, ease = Ease.InCubic };
        Tween.Custom(crystalSpriteRenderer, 0f, 1f, crystalSettings, static (crystalSpriteRenderer, comp) => {
            crystalSpriteRenderer.material.SetFloat(fillId, comp);
        });
        
        // Open
        openCloseSequence = Sequence.Create();
        openCloseSequence.ChainDelay(openDelay);
        openCloseSequence.ChainCallback(this, static (portal) => {
            portal.crystalExplosion.SetActive(true);
            portal.crystalOscillator.enabled = false;
            
            portal.openPortalSpriteRenderer.gameObject.SetActive(true);
            portal.StartAnimating();
            
            Tween.Custom(portal, portal.rotationSpeed * 6f, portal.rotationSpeed, 3f, onValueChange: static (portal, speed) => {
                portal.rotationSpeed = speed;
            });
            
            foreach (Transform fragTrans in portal.crystalFragments) {
                fragTrans.gameObject.SetActive(true);
                float randomAngle = Random.Range(0, 2) == 0 ? Random.Range(-40f, 0f) : Random.Range(-140f, -180f);
                Vector3 endPos = fragTrans.position + Game.RotationVector(randomAngle, 0.3f, 0.4f);
                AddBounceEffect(fragTrans, endPos, 0.55f, 0.75f);
            }
            
        });
        
        openCloseSequence.Chain(Tween.Custom(this, 0f, 1f, 0.9f, onValueChange: static (portal, comp) => {
            comp = portal.openPortalAnimationCurve.Evaluate(comp);
            portal.openPortalSpriteRenderer.material.SetFloat(fillId, comp);
        }));
        
        openCloseSequence.Group(Tween.Delay(this, 0.1f, static (portal) => {
            portal.crystalTrans.GetComponent<SpriteRenderer>().enabled = false;
            portal.state = State.Open; 
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

    private static void AddBounceEffect(Transform trans, Vector3 pos, float minDuration, float maxDuration) {
        Vector3 initialPos = trans.position;
        Tween.Custom(trans, 0f, 1f, Random.Range(minDuration, maxDuration), ease: Ease.Linear, onValueChange: (trans, val) => {
            float yPos = Game.gameInstance.curves.bounce.Evaluate(val);
            Vector2 newPos = Vector2.Lerp(initialPos, pos, val);
            trans.position = new(newPos.x, newPos.y + yPos, trans.position.z);
        });
    }
    
}
