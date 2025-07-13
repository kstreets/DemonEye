using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

    public static void InitalizeWithDefault<T>(this T[] array) where T : new() {
        for (int i = 0; i < array.Length; i++) {
            array[i] = new();
        }
    }

}
