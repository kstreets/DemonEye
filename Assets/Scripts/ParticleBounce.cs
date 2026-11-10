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

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    // x = height currently applied, y = bounce duration, z = height multiplier
    private readonly List<Vector4> bounceData = new();

    private void Awake() {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    private void LateUpdate() {
        int count = ps.GetParticles(particles);
        if (count == 0) return;

        ps.GetCustomParticleData(bounceData, ParticleSystemCustomData.Custom1);
        AnimationCurve curve = heightCurve is { length: > 0 } ? heightCurve : Game.gameInstance.curves.bounce;

        for (int i = 0; i < count; i++) {
            ref ParticleSystem.Particle particle = ref particles[i];
            Vector4 data = bounceData[i];
            if (data.y <= 0f) {
                float speed = particle.velocity.magnitude;
                data.y = Mathf.Max(speed * durationPerSpeed, 0.01f);
                data.z = speed * heightPerSpeed;
            }

            float t = Mathf.Clamp01((particle.startLifetime - particle.remainingLifetime) / data.y);
            float height = curve.Evaluate(t) * data.z;
            particle.position += new Vector3(0f, height - data.x, 0f);
            data.x = height;
            if (t >= 1f) {
                particle.velocity = Vector3.zero;
            }
            bounceData[i] = data;
        }

        ps.SetParticles(particles, count);
        ps.SetCustomParticleData(bounceData, ParticleSystemCustomData.Custom1);
    }

}
