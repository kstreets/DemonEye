using System.Collections.Generic;
using UnityEngine;

public class ParticleBatch : MonoBehaviour {

    public ParticleSystem prefab;
    public Vector2Int countRange = new(4, 6);
    public bool playOnEnable;
    [Min(0)] public float playOnEnableDelay;

    // Keys are ParticleSystem prefabs, values are the shared instances that emit them
    private static readonly Dictionary<ParticleSystem, ParticleSystem> instances = new();
    private static Transform instanceRoot;

    private float playTime = -1f;

    private void OnEnable() {
        if (playOnEnable) {
            playTime = Time.time + playOnEnableDelay;
        }
    }

    private void OnDisable() {
        playTime = -1f;   
    }

    // Deferred to LateUpdate so objects that are enabled then disabled in the same frame (pool warmup) don't emit
    private void LateUpdate() {
        if (playTime < 0f || Time.time < playTime) return;
        playTime = -1f;
        Play();
    }

    public void Play() => Play(transform.position);

    public void Play(Vector3 position) {
        if (!prefab) return;
        ParticleSystem particles = GetInstance(prefab);
        particles.transform.position = position;
        particles.Emit(RandomCount());
    }

    private int RandomCount() {
        return Random.Range(countRange.x, countRange.y + 1);  
    } 

    private static ParticleSystem GetInstance(ParticleSystem prefab) {
        if (instances.TryGetValue(prefab, out ParticleSystem particles) && particles) return particles;

        if (!instanceRoot) {
            instanceRoot = new GameObject("ParticleBatches").transform;
            DontDestroyOnLoad(instanceRoot.gameObject);
        }
        particles = Instantiate(prefab, instanceRoot);
        instances[prefab] = particles;
        return particles;
    }

}
