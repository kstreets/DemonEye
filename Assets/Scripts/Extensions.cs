using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class Extensions {

    public static bool StartsWithVowel(this string str) {
        if (string.IsNullOrEmpty(str)) {
            return false;
        }
        char firstChar = char.ToLower(str[0]); 
        return firstChar is 'a' or 'e' or 'i' or 'o' or 'u'; // Never Y in this case lol
    }
    
    public static Vector2 Offset(this Vector2 vec, float x = 0f, float y = 0f) {
        return vec + new Vector2(x, y);
    }
    
    public static Vector3 Offset(this Vector3 vec, float x = 0f, float y = 0f, float z = 0f) {
        return vec + new Vector3(x, y, z);
    }
    
    public static void ForceRecalculate(this ContentSizeFitter fitter) {
        fitter.SetLayoutVertical();
        fitter.SetLayoutHorizontal();
    }

    public static Rect WorldRect(this RectTransform rectTransform) {
        Rect rect = rectTransform.rect;
        Matrix4x4 ltw = rectTransform.localToWorldMatrix;
        return new (ltw.MultiplyPoint(new(rect.x, 1f, 1f)).x, ltw.MultiplyPoint(new(1f, rect.y, 1f)).y, rect.width, rect.height);
    } 
    
    public static Rect WorldRectIgnoreScale(this RectTransform rectTransform) {
        Rect rect = rectTransform.rect;
        Matrix4x4 ltw = Matrix4x4.TRS(rectTransform.position, rectTransform.rotation, Vector3.one);
        return new (ltw.MultiplyPoint(new(rect.x, 1f, 1f)).x, ltw.MultiplyPoint(new(1f, rect.y, 1f)).y, rect.width, rect.height);
    }
    
    public static void ResizeWidth(this RectTransform rectTransform, float width) {
        rectTransform.sizeDelta = new(width, rectTransform.sizeDelta.y);
    }
    
    public static void ResizeHeight(this RectTransform rectTransform, float height) {
        rectTransform.sizeDelta = new(rectTransform.sizeDelta.x, height);
    }
    
    public static void Reset<T>(this ref T structType) where T : struct {
        structType = new();
    }

    public static Color Alpha(this Color color, float alpha) {
        return new(color.r, color.g, color.b, alpha);
    }

    public static bool ContainsCount<T>(this List<T> list, T item, out int count) {
        count = 0;
        
        if (list == null) {
            return false;
        }
        
        foreach (T listItem in list) {
            if (listItem.Equals(item)) {
                count++;
            }
        }
        return count > 0;
    }

    public static int GetCount<T>(this List<T> list, T item) {
        int count = 0; 
        foreach (T listItem in list) {
            if (listItem.Equals(item)) {
                count++;
            }
        }
        return count;
    }

    public static T PopLast<T>(this List<T> list) {
        if (list.Count <= 0) throw new Exception("Cannot pop last item from empty list");
        T lastItem = list[^1];
        list.RemoveAt(list.Count - 1);
        return lastItem;
    }
    
    public static void Shuffle<T>(this List<T> list) {
        for (int i = 0; i < list.Count; i++) {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
    
    public static void CopyTo<T>(this List<T> list, List<T> dest) {
        if (list == null) return;
        dest = new(list.Count);
        dest.AddRange(list);
    }
    
    public static List<T> Clone<T>(this List<T> list) {
        if (list == null) {
            return null;
        }
        List<T> clone = new(list.Count);
        clone.AddRange(list);
        return clone;
    }

    public static void InitalizeWithDefault<T>(this T[] array) where T : new() {
        for (int i = 0; i < array.Length; i++) {
            array[i] = new();
        }
    }
    
    public static bool IndexInRange<T>(this T[] array, int index) {
        return index >= 0 && index < array.Length;
    }
    
    public static bool Contains<T>(this T[] array, T element) {
        for (int i = 0; i < array.Length; i++) {
            if (array[i].Equals(element)) {
                return true;
            }
        }
        return false;
    }

    public static bool IndexInRange<T>(this T[,] array, Vector2 index) {
        int x = (int)index.x;
        int y = (int)index.y;
        return x >= 0 && y >= 0 && y < array.GetLength(0) && x < array.GetLength(1);
    }
    
    public static bool IndexInRange<T>(this List<T> list, int index) {
        return index >= 0 && index < list.Count;
    }
    
    public static V RandomValue<K, V>(this Dictionary<K, V> dictionary) where V : class {
        int target = Random.Range(0, dictionary.Count);

        int i = 0;
        foreach (var kvp in dictionary) {
            if (i++ == target) {
                return kvp.Value;
            }
        }

        return null;
    }

    public static bool TryGetValue<T>(this T? nullableStruct, out T value) where T : struct {
        value = nullableStruct.GetValueOrDefault();
        return nullableStruct.HasValue;
    }
    
    public static void PlayIfNotAlready(this Animator animator, int animHash) {
        if (!animator.gameObject.activeInHierarchy || animator.Playing(animHash)) return;
        animator.Play(animHash);
    }
    
    public static bool Playing(this Animator animator, int animHash) {
        if (!animator.gameObject.activeInHierarchy) return false;
        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == animHash;
    }
    
    public static float TimeLeftInCurrentAnimation(this Animator animator) {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTimeLeft = 1f - Mathf.Clamp01(stateInfo.normalizedTime);
        return normalizedTimeLeft * stateInfo.length; 
    }
    
    public static void CreateObjects<T>(this ObjectPool<T> pool, int count) where T : class {
        if (count <= 0) return;
        var poolObject = pool.Get();
        pool.CreateObjects(--count);
        pool.Release(poolObject); 
    }
    
    /// Returns true if 1 or more flags are found in the enum 
    /// Ex. enumType.HasAnyFlag(flag1 | flag2) is like saying enumType.HasFlag(flag1) || enumType.HasFlag(flag2)
    public static bool HasAnyFlag<T>(this T enumFlags, T flags) where T : struct, Enum {
        return (Convert.ToInt64(enumFlags) & Convert.ToInt64(flags)) != 0;
    }
    
#if UNITY_EDITOR
    
    public static GUID AssetGUID(this Object obj) {
        return AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(obj));
    }

    public static T LoadAsset<T>(this GUID guid) where T : Object { 
        return AssetDatabase.LoadAssetByGUID<T>(guid);
    }

#endif

}
