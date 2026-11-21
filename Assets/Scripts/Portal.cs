using System;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class Portal : MonoBehaviour {

    public enum State { Inactive, BeingSummoned, Open, Closed }
    [NonSerialized] public State state = State.Inactive;
    [NonSerialized] public bool found; // Seen on screen by the player this raid

    public ParticleSystem summoningParticles;
    public GameObject crystalExplosion;
    public SummonedPortal summonedPortal;

    [Header("Crystal")]
    public Transform crystalTrans;
    public SpriteRenderer crystalSpriteRenderer;
    public Oscillator crystalOscillator;
    public AnimationCurve crystalShakeMagnitudeCurve;
    public AnimationCurve crystalShakeJitterCurve;
    public Transform[] crystalFragments;
    public float crystalKnockbackDistance = 0.08f;
    public float crystalKnockbackOutDuration = 0.06f;
    public float crystalKnockbackReturnDuration = 0.3f;

    [Header("SFX")]
    public DynamicClip crystalHitClip;
    public DynamicClip portalSummoningClip;
    Sequence openCloseSequence;
    float particleStartSpeed;

    public void Init() {
        found = false;
        summonedPortal.Init();
        summonedPortal.gameObject.SetActive(false);

        crystalSpriteRenderer.material = new(crystalSpriteRenderer.sharedMaterial);
        crystalExplosion.SetActive(false);

        crystalSpriteRenderer.material.SetFloat(ShaderIds.fill, 0f);
        crystalSpriteRenderer.material.SetVector(ShaderIds.offsetSize, crystalSpriteRenderer.sprite.OffsetAndSizeInTexture());

        summoningParticles.gameObject.SetActive(false);
        particleStartSpeed = summoningParticles.velocityOverLifetime.radialMultiplier;

        foreach (Transform fragTrans in crystalFragments) {
            fragTrans.gameObject.SetActive(false);
        }
    }

    public void StartOpenCloseSequence(float openDelay, float openDuration, Vector2 hitFrom) {
        state = State.BeingSummoned;

        crystalTrans.DoTweenShake(15f, 0.015f, openDelay, crystalShakeMagnitudeCurve, crystalShakeJitterCurve);
        summoningParticles.gameObject.SetActive(true);

        KnockCrystalAwayFrom(hitFrom);
        Game.PlayAudioClip(crystalHitClip, crystalTrans.position);

        // Summoning
        Game.PlayAudioClip(portalSummoningClip, crystalTrans.position, cannotInterrupt: true);

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
            crystalSpriteRenderer.material.SetFloat(ShaderIds.fill, comp);
        });

        // Open
        openCloseSequence = Sequence.Create();
        openCloseSequence.ChainDelay(openDelay);
        openCloseSequence.ChainCallback(this, static (portal) => {
            portal.crystalExplosion.SetActive(true);
            portal.crystalOscillator.enabled = false;

            portal.summonedPortal.gameObject.SetActive(true);
            portal.summonedPortal.Open();
            Game.PlayerOnExitPortalSummoned(portal);

            Game.ShakeCamera(5f, 0.1f, 0.5f, portal.transform.position, falloffStartRange: 0.7f, falloffDistance: 2f, Game.CameraShake.Falloff.Linear);

            foreach (Transform fragTrans in portal.crystalFragments) {
                fragTrans.gameObject.SetActive(true);
                float randomAngle = Random.Range(0, 2) == 0 ? Random.Range(-40f, 0f) : Random.Range(-140f, -180f);
                Vector3 endPos = fragTrans.position + Game.RotationVector(randomAngle, 0.3f, 0.4f);
                AddBounceEffect(fragTrans, endPos, 0.55f, 0.75f);
            }
        });
        openCloseSequence.Group(Tween.Delay(this, 0.1f, static (portal) => {
            portal.crystalTrans.GetComponent<SpriteRenderer>().enabled = false;
            portal.state = State.Open;
        }));

        // Staying Open
        openCloseSequence.ChainDelay(openDuration);

        // Close
        openCloseSequence.ChainCallback(this, static (portal) => {
            portal.state = State.Closed;
            portal.summonedPortal.Close(activeStateOnComplete: false);
        });
    }

    // Applied as an offset so it stacks with the shake, which also moves the crystal
    void KnockCrystalAwayFrom(Vector2 hitFrom) {
        Vector2 dir = (Vector2)crystalTrans.position - hitFrom;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        Vector2 knockback = dir * crystalKnockbackDistance;

        Vector2 appliedOffset = Vector2.zero;
        void ApplyOffset(float val) {
            Vector2 offset = knockback * val;
            crystalTrans.position += (Vector3)(offset - appliedOffset);
            appliedOffset = offset;
        }

        Sequence.Create()
            .Chain(Tween.Custom(0f, 1f, crystalKnockbackOutDuration, ApplyOffset, Ease.OutQuad))
            .Chain(Tween.Custom(1f, 0f, crystalKnockbackReturnDuration, ApplyOffset, Ease.InOutQuad));
    }

    public void OnPlayerTook() {
        openCloseSequence.Stop();
        summonedPortal.Close(activeStateOnComplete: false);
    }

    static void AddBounceEffect(Transform trans, Vector3 pos, float minDuration, float maxDuration) {
        Vector3 initialPos = trans.position;
        Tween.Custom(trans, 0f, 1f, Random.Range(minDuration, maxDuration), ease: Ease.Linear, onValueChange: (trans, val) => {
            float yPos = Game.curves.bounce.Evaluate(val);
            Vector2 newPos = Vector2.Lerp(initialPos, pos, val);
            trans.position = new(newPos.x, newPos.y + yPos, trans.position.z);
        });
    }

}
