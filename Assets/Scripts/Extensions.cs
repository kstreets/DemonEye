using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class Extensions {

    public static Vector2 PositionV2(this Transform transform) {
        return new Vector2(transform.position.x, transform.position.y);
    }
    
    public static Vector2 ToVector2(this Vector3 vector) {
        return new Vector2(vector.x, vector.y);
    }
    
    public static Vector3 ToVector3(this Vector2 vector) {
        return new Vector3(vector.x, vector.y, 0f);
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
    
    public static void ResizeWidth(this RectTransform rectTransform, float width) {
        rectTransform.sizeDelta = new(width, rectTransform.sizeDelta.y);
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

    public static void InitalizeWithDefault<T>(this T[] array) where T : new() {
        for (int i = 0; i < array.Length; i++) {
            array[i] = new();
        }
    }
    
    public static bool IndexInRange<T>(this T[] array, int index) {
        return index >= 0 && index < array.Length;
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
        int target = UnityEngine.Random.Range(0, dictionary.Count);

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
    
#if UNITY_EDITOR
    
    public static GUID AssetGUID(this Object obj) {
        return AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(obj));
    }

    public static T LoadAsset<T>(this GUID guid) where T : Object { 
        return AssetDatabase.LoadAssetByGUID<T>(guid);
    }

#endif

}
