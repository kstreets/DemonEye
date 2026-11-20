using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using static GameData;

public static partial class Game {

    public class CameraLookahead {
        public const float maxDistance = 0.35f;
        public const float speed = 0.3f;
        public const float rampBackUpDuration = 0.6f;
        public Tween fadeOutTween;
        public float curRampBackUpSpeed;
    }

    static void UpdateAimLookAhead() {
        return;
        bool overridingSequenceOccuring = raidEnterSequence.isAlive || camera.lookAhead.fadeOutTween.isAlive || cutscene.playing;
        if (overridingSequenceOccuring && !MenuNavigationNeeded) {
            camera.lookAhead.curRampBackUpSpeed = 0f;
            return;
        }
        
        camera.lookAhead.curRampBackUpSpeed += CameraLookahead.rampBackUpDuration * Time.deltaTime;
        camera.lookAhead.curRampBackUpSpeed = Mathf.Clamp01(camera.lookAhead.curRampBackUpSpeed);
        
        bool inventoryOpen = PlayerInventoryIsOpen || LootInventoryIsOpen;
        Vector2 target = Vector2.zero;
        if (!inventoryOpen && TryGetAimVector(out Vector2 aim)) {
            Vector2 mouseDistance = aim - camera.aimOffset.offset;
            float mouseRange = (Screen.height / 4f) / 100f;
            float strength = UsingController ? Mathf.InverseLerp(ControllerNavigation.stickDeadzone, 1f, aim.magnitude) : Mathf.Clamp01(mouseDistance.magnitude / mouseRange);
            target = aim.normalized * (strength * CameraLookahead.maxDistance);
        }
        
        Vector2 current = camera.aimOffset.offset;
        float lookSpeed = CameraLookahead.speed * camera.lookAhead.curRampBackUpSpeed * Time.deltaTime;
        Vector2 next = Vector2.MoveTowards(current, target, lookSpeed);
        camera.aimOffset.offset = next;
    }

    static void FadeoutAimLookAhead() {
        return;
        const float fadeOutTime = 0.35f;
        camera.lookAhead.fadeOutTween = Tween.Custom(camera.aimOffset.offset, Vector2.zero, fadeOutTime, val => camera.aimOffset.offset = val, Ease.OutQuad);
    }

    static void ResetAimLookAhead() {
        camera.aimOffset.offset = Vector2.zero;
    }
    
    public class CameraShake {
        public enum Falloff { Linear, Sine, Quad, }

        public struct Instance {
            public float jitter;
            public float magnitude;
            public float duration;
            public float elapsed;
            public float curJitter;
            public Vector2 randomSeed;
            public float falloffScale;
        }

        public readonly List<Instance> active = new(5);
    }

    public static void ShakeCamera(float jitter, float magnitude, float duration) {
        AddCameraShake(jitter, magnitude, duration, 1f);
    }

    public static void ShakeCamera(float jitter, float magnitude, float duration, Vector2 sourcePosition, float falloffStartRange, float falloffDistance, CameraShake.Falloff falloff) {
        float distance = Vector2.Distance(camera.main.transform.position, sourcePosition);
        AddCameraShake(jitter, magnitude, duration, CalculateShakeFalloffScale(distance, falloffStartRange, falloffDistance, falloff));
    }

    static void AddCameraShake(float jitter, float magnitude, float duration, float falloffScale) {
        camera.shake.active.Add(new() {
            jitter = jitter,
            magnitude = magnitude,
            duration = duration,
            randomSeed = new(Random.Range(0f, 10000f), Random.Range(0f, 10000f)),
            falloffScale = falloffScale,
        });
    }

    // The shake shifts the finished image in the final blit rather than moving the camera, so it moves
    // in whole screen pixels without re-rasterizing the pixel art, letting it go below one art pixel
    static void UpdateCameraShake() {
        List<CameraShake.Instance> shakes = camera.shake.active;
        Vector2 combinedOffset = Vector2.zero;

        for (int i = shakes.Count - 1; i >= 0; i--) {
            CameraShake.Instance shake = shakes[i];
            if (shake.elapsed >= shake.duration) {
                shakes.RemoveAt(i);
                continue;
            }

            shake.curJitter = (shake.curJitter + shake.jitter * Time.deltaTime) % 1f;
            float x = (Mathf.PerlinNoise(shake.randomSeed.x, shake.curJitter) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(shake.randomSeed.y, shake.curJitter + 100f) - 0.5f) * 2f;

            float remaining = 1f - Mathf.Clamp01(shake.elapsed / shake.duration);
            combinedOffset += new Vector2(x, y) * (shake.magnitude * remaining * remaining * shake.falloffScale);

            shake.elapsed += Time.deltaTime;
            shakes[i] = shake;
        }

        RenderManager.shakeOffset = combinedOffset;
    }

    static float CalculateShakeFalloffScale(float distance, float falloffStartRange, float falloffDistance, CameraShake.Falloff falloff) {
        if (distance <= falloffStartRange) return 1f;
        if (distance >= falloffStartRange + falloffDistance) return 0f;

        float normalizedDistance = (distance - falloffStartRange) / falloffDistance;
        return falloff switch {
            CameraShake.Falloff.Linear => 1f - normalizedDistance,
            CameraShake.Falloff.Sine => Mathf.Cos((normalizedDistance * Mathf.PI) / 2f),
            CameraShake.Falloff.Quad => 1f - Mathf.Pow(normalizedDistance, 2f),
            _ => 1f - normalizedDistance,
        };
    }

    static Tween _cameraPunchZoomTween;

    static void CameraPunchZoom(float punch, float inDuration, float outDuration, Ease easeIn = Ease.OutQuad, Ease easeOut = Ease.InOutQuad) {
        float zoomIn = 1f + (punch / 100f);
        _cameraPunchZoomTween.Complete();
        _cameraPunchZoomTween = Tween.Custom(1f, zoomIn, inDuration, val => RenderManager.punchZoom = val, easeIn);
        _cameraPunchZoomTween.Chain(Tween.Custom(zoomIn, 1f, outDuration, val => RenderManager.punchZoom = val, easeOut));
    }
    
}
