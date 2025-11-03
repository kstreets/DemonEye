using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public static class DemonEyeTween {
    
    public enum Curve { Linear, EaseOut, EaseIn, EaseInOut }
    public enum Type { Scale, Shake, Delay, Callback, Custom, }

    public class TweenObject {
        public Type type;
        public Transform transform;
        public RectTransform rectTransform;
        public Curve curve;
        public AnimationCurve animationCurve;
        public Action callback;
        public Action<float> completionCallback;
        public Vector2 randomSeed;
        public float float1;
        public float float2;
        public float float3;
        public float time;
        public float countdown;
    }

    public struct TweenHandle {
        public TweenObject singleTween;
        public List<TweenObject> sequence;
    }

    public static List<TweenObject> singleTweens;
    public static List<List<TweenObject>> sequences;
    public static ObjectPool<TweenObject> tweenObjectPool;
    
    public static void Init() {
        singleTweens = new(30);
        sequences = new(30);
        tweenObjectPool = new(() => new());
    }

    public static void Update() {
        for (int i = singleTweens.Count - 1; i >= 0; i--) {
            bool tweenIsFinished = singleTweens[i].countdown < 0f;
            if (tweenIsFinished) {
                Release(singleTweens[i]);
                singleTweens.RemoveAt(i);
                continue;
            }
            UpdateIndividualTween(singleTweens[i]);
        }

        for (int i = sequences.Count - 1; i >= 0; i--) {
            int curTweenIndex = GetCurrentTweenIndexOfSequence(sequences[i]);
            bool sequenceIsFinished = curTweenIndex < 0;
            if (sequenceIsFinished) {
                Release(sequences[i]);
                sequences.RemoveAt(i);
                continue;
            }
            UpdateIndividualTween(sequences[i][curTweenIndex]);
        } 
    }
    
    public static void UpdateIndividualTween(TweenObject tween) {
        switch (tween.type) {
            case Type.Scale:
                UpdateScale(tween);
                break;
            case Type.Shake:
                UpdateShake(tween);
                break;
            case Type.Delay:
                UpdateDelay(tween);
                break;
            case Type.Callback:
                UpdateCallback(tween);
                break;
            case Type.Custom:
                UpdateCustom(tween);
                break;
        } 
    }
    
    public static int GetCurrentTweenIndexOfSequence(List<TweenObject> sequence) {
        for (int i = 0; i < sequence.Count; i++) {
            if (sequence[i].countdown > 0f) {
                return i;
            }
        }
        return -1;
    }
    
    public static List<TweenObject> CreateSequence(out TweenHandle handle) {
        List<TweenObject> sequence = ListPool<TweenObject>.Get();
        sequences.Add(sequence);
        handle = new() { sequence = sequence };
        return sequence;
    }

    public static void Release(TweenObject tween) {
        tweenObjectPool.Release(tween);
    }
    
    public static void Release(List<TweenObject> sequence) {
        foreach (TweenObject tween in sequence) {
            Release(tween);
        }
        ListPool<TweenObject>.Release(sequence);
    }

    public static void Stop(TweenHandle handle) {
        if (handle.singleTween != null) {
            if (singleTweens.Remove(handle.singleTween)) {
                Release(handle.singleTween);
            }
        }
        if (handle.sequence != null) {
            for (int i = sequences.Count - 1; i >= 0; i--) {
                if (sequences[i] == handle.sequence) {
                    Release(sequences[i]);
                    sequences.RemoveAt(i);
                    return;
                }
            }
        }
    }

    public static float ConvertCompletion(float comp, Curve curve) {
        return curve switch {
            Curve.EaseOut   => 1 - Mathf.Pow(1 - comp, 3),
            Curve.EaseIn    => Mathf.Pow(comp, 3),
            Curve.EaseInOut => Mathf.SmoothStep(0f, 1f, comp),
            _               => comp,
        };
    }

    public static float UpdateAndGetCompletion(TweenObject tween) {
        tween.countdown -= Time.deltaTime;
        return ConvertCompletion(Mathf.Clamp01((tween.time - tween.countdown) / tween.time), tween.curve);
    }

    
    public static TweenObject TweenDelay(float delay) { 
        TweenObject tween = tweenObjectPool.Get();
        tween.type = Type.Delay;
        tween.countdown = delay;
        tween.time = delay;
        return tween;
    }

    public static void UpdateDelay(TweenObject tween) {
        UpdateAndGetCompletion(tween);
    }

    
    public static TweenObject Callback(Action callback) {
        TweenObject tween = tweenObjectPool.Get();
        tween.type = Type.Callback;
        tween.callback = callback;
        tween.countdown = 1f;
        tween.time = 1f;
        return tween;
    }

    public static void UpdateCallback(TweenObject tween) {
        tween.callback?.Invoke();
        tween.countdown = 0f;
        tween.time = 0f;
    }

    
    public static TweenObject TweenScale(RectTransform rectTransform, float startSize, float endSize, float time, Curve curve) {
        TweenObject tween = tweenObjectPool.Get();
        tween.type = Type.Scale;
        tween.rectTransform = rectTransform;
        tween.float1 = startSize;
        tween.float2 = endSize;
        tween.time = time;
        tween.countdown = time;
        tween.curve = curve;
        return tween;
    }
    
    public static TweenHandle DoTweenScale(this RectTransform rectTransform, float startSize, float endSize, float time, Curve curve) {
        singleTweens.Add(TweenScale(rectTransform, startSize, endSize, time, curve));
        return new() { singleTween = singleTweens[^1] };
    }

    public static void UpdateScale(TweenObject tween) {
        tween.countdown -= Time.deltaTime;
        float comp = UpdateAndGetCompletion(tween);
        float size = Mathf.Lerp(tween.float1, tween.float2, comp);
        tween.rectTransform.localScale = new(size, size, size);
    }


    public static TweenObject TweenShake(RectTransform rectTransform, float jitter, float magnitude, float time, AnimationCurve animCurve) {
        TweenObject tween = tweenObjectPool.Get();
        tween.type = Type.Shake;
        tween.rectTransform = rectTransform;
        tween.randomSeed = new(Random.Range(int.MinValue, int.MaxValue), Random.Range(int.MinValue, int.MaxValue));
        tween.animationCurve = animCurve;
        tween.float1 = jitter;
        tween.float2 = magnitude;
        tween.time = time;
        tween.countdown = time;
        tween.curve = Curve.Linear;
        return tween;
    }
    
    public static TweenHandle DoTweenShake(this RectTransform rectTransform, float jitter, float magnitude, float time, AnimationCurve animCurve) {
        singleTweens.Add(TweenShake(rectTransform, jitter, magnitude, time, animCurve));
        return new() { singleTween = singleTweens[^1] };
    }

    public static void UpdateShake(TweenObject tween) {
        float comp = UpdateAndGetCompletion(tween);
        float magnitude = tween.animationCurve.Evaluate(comp) * tween.float2;
        
        tween.float3 = (tween.float3 + tween.float1 * Time.deltaTime) % 1f;
        float x = (Mathf.PerlinNoise(tween.randomSeed.x, tween.float3) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(tween.randomSeed.y, tween.float3 + 100f) - 0.5f) * 2f;
        Vector3 targetVector = new Vector3(x, y, tween.rectTransform.position.z) * magnitude;
        tween.rectTransform.anchoredPosition = targetVector; 
    }


    public static TweenObject TweenCustom(float time, Action<float> completionCallback) {
        TweenObject tween = tweenObjectPool.Get();
        tween.type = Type.Custom;
        tween.completionCallback = completionCallback;
        tween.countdown = time;
        tween.time = time;
        tween.curve = Curve.Linear;
        return tween;
    }

    public static TweenHandle DoTweenCustom(float time, Action<float> completionCallback) {
        singleTweens.Add(TweenCustom(time, completionCallback));
        return new() { singleTween = singleTweens[^1] };
    }

    public static void UpdateCustom(TweenObject tween) {
        float comp = UpdateAndGetCompletion(tween);
        tween.completionCallback?.Invoke(comp); 
    }

}
