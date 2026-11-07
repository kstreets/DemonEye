using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InputGlyphSheet", menuName = "Scriptable Objects/InputGlyphSheet")]
public class InputGlyphSheet : ScriptableObject {

    public Sprite[] sprites;

    [NonSerialized] private Dictionary<string, Sprite> lookup;

    public Sprite Get(string controlPath) {
        if (string.IsNullOrEmpty(controlPath)) {
            return null;
        }

        if (lookup == null) {
            BuildLookup();
        }

        string key = controlPath.Replace('/', '_');
        if (lookup.TryGetValue(key, out Sprite sprite)) {
            return sprite;
        }

        // Cached under the original key so the fallback only runs once
        string fallback = Fallback(key);
        if (fallback != null && lookup.TryGetValue(fallback, out sprite)) {
            lookup[key] = sprite;
            return sprite;
        }

        Debug.LogWarning($"{name} has no glyph for {controlPath}");
        lookup[key] = null; // So the warning only shows once
        return null;
    }

    // Controls that share one sprite
    private static readonly Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase) {
        { "position", "delta" },
        { "scroll_y", "scroll" },
        { "numpadPeriod", "period" },
        { "numpadMinus", "minus" },
        { "numpadDivide", "slash" },
        { "numpadEquals", "equals" },
    };

    private static readonly string[] sidedModifiers = { "Shift", "Ctrl", "Alt", "Meta" };

    private static string Fallback(string key) {
        if (aliases.TryGetValue(key, out string alias)) {
            return alias;
        }

        // numpad5 -> 5
        if (key.Length == 7 && key.StartsWith("numpad", StringComparison.OrdinalIgnoreCase) && char.IsDigit(key[6])) {
            return key.Substring(6);
        }

        // leftShift / rightShift -> shift
        foreach (string modifier in sidedModifiers) {
            if (key.Equals("left" + modifier, StringComparison.OrdinalIgnoreCase) || key.Equals("right" + modifier, StringComparison.OrdinalIgnoreCase)) {
                return modifier;
            }
        }

        return null;
    }

    private void BuildLookup() {
        lookup = new(StringComparer.OrdinalIgnoreCase);
        foreach (Sprite sprite in sprites) {
            if (sprite != null) {
                lookup[sprite.name] = sprite;
            }
        }
    }

#if UNITY_EDITOR
    // Rebuild after the sprites are edited in the inspector
    private void OnValidate() {
        lookup = null;
    }
#endif

}
