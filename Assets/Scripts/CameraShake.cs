using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : CinemachineExtension {
    
    public enum Falloff { Linear, Sine, Quad, }

    private struct ShakeData {
        public float jitter;
        public float magnitude;
        public float duration;
        public float elapsed;
        public float curJitter;
        public Vector2 randomSeed;
        public float falloffScale;
    }
    
    private List<ShakeData> activeShakes = new(5);
    private Vector2 appliedOffset;

    public void Shake(float jitter, float magnitude, float duration) {
        ShakeData shake = new() {
            jitter = jitter,
            magnitude = magnitude,
            duration = duration,
            elapsed = 0f,
            curJitter = 0f,
            randomSeed = GetRandomSeed(),
            falloffScale = 1f,
        };
        activeShakes.Add(shake);
    }

    public void Shake(float jitter, float magnitude, float duration, Vector2 sourcePosition, float falloffStartRange, float falloffDistance, Falloff falloff) {
        ShakeData shake = new() {
            jitter = jitter,
            magnitude = magnitude,
            duration = duration,
            elapsed = 0f,
            curJitter = 0f,
            randomSeed = GetRandomSeed(),
            falloffScale = CalculateFalloffScale(sourcePosition, falloffStartRange, falloffDistance, falloff),
        };
        activeShakes.Add(shake);
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime) {
        if (stage != CinemachineCore.Stage.Finalize) return;
        if (activeShakes.Count == 0) return;
        
        Vector3 combinedOffset = Vector3.zero;
        
        for (int i = activeShakes.Count - 1; i >= 0; i--) {
            ShakeData shake = activeShakes[i];
            
            if (shake.elapsed >= shake.duration) {
                activeShakes.RemoveAt(i);
                continue;
            }
            
            shake.curJitter = (shake.curJitter + shake.jitter * deltaTime) % 1f;
            float x = (Mathf.PerlinNoise(shake.randomSeed.x, shake.curJitter) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(shake.randomSeed.y, shake.curJitter + 100f) - 0.5f) * 2f;
            
            float comp = Mathf.Clamp01(shake.elapsed / shake.duration);
            float easeOutComp = Mathf.Pow(1f - comp, 3f);
            Vector3 shakeOffset = new Vector3(x, y, 0f) * shake.magnitude * easeOutComp * shake.falloffScale;
            
            combinedOffset += shakeOffset;
            shake.elapsed += deltaTime;
            
            // Apply modified struct properties
            activeShakes[i] = shake;
        }
        
        Vector3 origin = vcam.State.RawPosition - (Vector3)appliedOffset;
        vcam.ForceCameraPosition(origin + combinedOffset, Quaternion.identity);
        
        appliedOffset = combinedOffset;
    }
    
    private float CalculateFalloffScale(Vector2 sourcePosition, float falloffStartRange, float falloffDistance, Falloff falloff) {
        float distance = Vector2.Distance(transform.position, sourcePosition);
        
        if (distance <= falloffStartRange) {
            return 1f;
        }

        float maxRange = falloffStartRange + falloffDistance;
        if (distance >= maxRange) {
            return 0f;
        }

        float normalizedDistance = (distance - falloffStartRange) / falloffDistance;
        return falloff switch {
            Falloff.Linear => 1f - normalizedDistance,
            Falloff.Sine => Mathf.Cos((normalizedDistance * Mathf.PI) / 2f),
            Falloff.Quad => 1f - Mathf.Pow(normalizedDistance, 2f),
            _ => 1f - normalizedDistance,
        };
    }
    
    private Vector2 GetRandomSeed() {
        // Using a float range that still gives us enough precision
        return new(Random.Range(0f, 10000f), Random.Range(0f, 10000f));
    }

}
