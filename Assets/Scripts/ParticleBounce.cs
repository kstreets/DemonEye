using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleBounce : MonoBehaviour {

    [Tooltip("Height over the bounce. Leave empty to use the game's bounce curve")]
    public AnimationCurve heightCurve;
    [Tooltip("Bounce duration per unit of start speed, so slower particles land sooner")]
    public float durationPerSpeed = 1.6f;
    [Tooltip("Height curve multiplier per unit of start speed, so slower particles hop lower")]
    public float heightPerSpeed = 2.5f;
    [Tooltip("Seconds to fade out once the particle's ground point leaves walkable space")]
    public float offMapFadeDuration = 0.15f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    // x = height currently applied, y = bounce duration, z = height multiplier
    private readonly List<Vector4> bounceData = new();
    // x = age when the fade started + 1 (0 = not fading), y = alpha before fading
    private readonly List<Vector4> fadeData = new();

    private void LateUpdate() {
        Apply();
    }

    // Also called by ParticleBounceEditor to preview outside play mode
    public void Apply(AnimationCurve fallbackCurve = null) {
        if (!ps) {
            ps = GetComponent<ParticleSystem>();
        }
        
        if (particles == null || particles.Length < ps.main.maxParticles) {
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
        }

        int count = ps.GetParticles(particles);
        if (count == 0) return;

        AnimationCurve curve = GetHeightCurve() ?? fallbackCurve;
        if (curve == null) return;

        ps.GetCustomParticleData(bounceData, ParticleSystemCustomData.Custom1);
        ps.GetCustomParticleData(fadeData, ParticleSystemCustomData.Custom2);
        CoolerGrid coolGrid = GetGrid();

        for (int i = 0; i < count; i++) {
            ref ParticleSystem.Particle particle = ref particles[i];
            Vector4 data = bounceData[i];
            if (data.y <= 0f) {
                float speed = particle.velocity.magnitude;
                data.y = Mathf.Max(speed * durationPerSpeed, 0.01f);
                data.z = speed * heightPerSpeed;
            }

            float age = particle.startLifetime - particle.remainingLifetime;
            float t = Mathf.Clamp01(age / data.y);
            float height = curve.Evaluate(t) * data.z;
            particle.position += new Vector3(0f, height - data.x, 0f);
            data.x = height;
            if (t >= 1f) {
                particle.velocity = Vector3.zero;
            }
            bounceData[i] = data;
            
            Vector4 fade = fadeData[i];
            Vector2 particlesGroundPos = new(particle.position.x, particle.position.y - height);
            bool startFade = fade.x <= 0f && coolGrid && !coolGrid.IsWalkable(particlesGroundPos);
            if (startFade) {
                fade.x = age + 1f;
                fade.y = particle.startColor.a;
            }
            
            bool updateFade = fade.x > 0f;
            if (updateFade) {
                float fadeT = (age - (fade.x - 1f)) / Mathf.Max(offMapFadeDuration, 0.001f);
                if (fadeT >= 1f) {
                    particle.remainingLifetime = 0f;
                }
                else {
                    Color32 color = particle.startColor;
                    color.a = (byte)(fade.y * (1f - fadeT));
                    particle.startColor = color;
                }
                fadeData[i] = fade;
            }
        }

        // Custom data first since SetParticles removes particles whose fade finished
        ps.SetCustomParticleData(bounceData, ParticleSystemCustomData.Custom1);
        ps.SetCustomParticleData(fadeData, ParticleSystemCustomData.Custom2);
        ps.SetParticles(particles, count);
    }

    private static CoolerGrid GetGrid() {
        if (!Game.gameInstance) {
            return null;
        }
        MapInstance map = Game.gameInstance.curRaid.mapInstance;
        return map ? map.grid : null;
    }

    public AnimationCurve GetHeightCurve() {
        if (heightCurve is { length: > 0 }) return heightCurve;
        // gameInstance isn't set outside play mode
        Game game = Game.gameInstance ? Game.gameInstance : FindAnyObjectByType<Game>();
        return game ? game.curves.bounce : null;
    }

}
