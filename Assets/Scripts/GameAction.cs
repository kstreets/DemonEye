using System;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using static GameData;
using Object = UnityEngine.Object;

public class GameAction {
    
    public readonly string name;
    public readonly InputAction unityInputAction;
    public readonly bool isAnalog;

    public bool steamInputIsDown;
    public bool steamInputWasDownLastFrame;
    public Vector2 steamValue;

    public EInputActionOrigin curSteamGlyphId;
    public InputDigitalActionHandle_t steamDigitalHandle;
    public InputAnalogActionHandle_t steamAnalogHandle;
    
    // Two slots so a modifier composite can show both parts: [modifier, binding], or [binding, null] when there's no modifier
    private readonly Sprite[] gamepadGlyphs = new Sprite[2];
    private readonly Sprite[] keyboardMouseGlyphs = new Sprite[2];
    private readonly Sprite[] steamGlyphs = new Sprite[2];
    
    private static readonly Dictionary<EInputActionOrigin, Sprite> steamGlyphSprites = new();

    public readonly string label;
    public bool isActive = true;
    public int layerIndex = -1;

    public enum BindingSource { KeyboardMouse, Gamepad, Steam }

    // One key per binding, composites joined into a single key so Shift+LMB doesn't count as the same binding as LMB
    private readonly List<string> keyboardMouseBindingKeys = new();
    private readonly List<string> gamepadBindingKeys = new();

    private readonly EInputActionOrigin[] steamOrigins = new EInputActionOrigin[Constants.STEAM_INPUT_MAX_ORIGINS];
    private int steamOriginCount;

    public GameAction(string name) {
        this.name = name;
        label = MakeLabel(name);
        unityInputAction = InputSystem.actions.FindAction(name, throwIfNotFound: true);
        isAnalog = unityInputAction.expectedControlType == "Vector2";
        UpdateUnityBindingKeys();
    }

    public static string MakeLabel(string actionName) {
        System.Text.StringBuilder builder = new(actionName.Length + 4);
        for (int i = 0; i < actionName.Length; i++) {
            char c = actionName[i];
            if (i > 0) {
                char previous = actionName[i - 1];
                bool wordStart = char.IsUpper(c) && !char.IsUpper(previous);
                bool numberStart = char.IsDigit(c) && !char.IsDigit(previous);
                if (wordStart || numberStart) {
                    builder.Append(' ');
                }
            }
            builder.Append(c);
        }
        return builder.ToString();
    }

    // Call again after rebinding, keys come from effective paths so overrides are included
    public void UpdateUnityBindingKeys() {
        CollectBindingKeys("Keyboard&Mouse", keyboardMouseBindingKeys);
        CollectBindingKeys("Gamepad", gamepadBindingKeys);
    }

    private void CollectBindingKeys(string bindingGroup, List<string> keys) {
        keys.Clear();
        InputBinding groupMask = InputBinding.MaskByGroup(bindingGroup);
        ReadOnlyArray<InputBinding> bindings = unityInputAction.bindings;

        // Bindings with no group count for every device, otherwise they would never be shadowed
        bool InGroup(InputBinding binding) => string.IsNullOrEmpty(binding.groups) || groupMask.Matches(binding);

        for (int i = 0; i < bindings.Count; i++) {
            InputBinding binding = bindings[i];

            if (binding.isComposite) {
                List<string> parts = new();
                int part = i + 1;
                for (; part < bindings.Count && bindings[part].isPartOfComposite; part++) {
                    if (InGroup(bindings[part]) && !string.IsNullOrEmpty(bindings[part].effectivePath)) {
                        parts.Add(bindings[part].effectivePath);
                    }
                }
                i = part - 1;

                if (parts.Count > 0) {
                    parts.Sort(StringComparer.Ordinal);
                    keys.Add(string.Join("+", parts));
                }
                continue;
            }

            if (InGroup(binding) && !string.IsNullOrEmpty(binding.effectivePath)) {
                keys.Add(binding.effectivePath);
            }
        }
    }

    public bool HasBindings(BindingSource source) {
        return source switch {
            BindingSource.Steam => steamOriginCount > 0,
            BindingSource.KeyboardMouse => keyboardMouseBindingKeys.Count > 0,
            _ => gamepadBindingKeys.Count > 0,
        };
    }

    public bool SharesClaimedBinding(BindingSource source, HashSet<string> claimedUnityBindings, HashSet<EInputActionOrigin> claimedSteamOrigins) {
        if (source == BindingSource.Steam) {
            for (int i = 0; i < steamOriginCount; i++) {
                if (claimedSteamOrigins.Contains(steamOrigins[i])) return true;
            }
            return false;
        }

        foreach (string key in source == BindingSource.KeyboardMouse ? keyboardMouseBindingKeys : gamepadBindingKeys) {
            if (claimedUnityBindings.Contains(key)) return true;
        }
        return false;
    }

    public void ClaimBindings(BindingSource source, HashSet<string> claimedUnityBindings, HashSet<EInputActionOrigin> claimedSteamOrigins) {
        if (source == BindingSource.Steam) {
            for (int i = 0; i < steamOriginCount; i++) {
                claimedSteamOrigins.Add(steamOrigins[i]);
            }
            return;
        }

        foreach (string key in source == BindingSource.KeyboardMouse ? keyboardMouseBindingKeys : gamepadBindingKeys) {
            claimedUnityBindings.Add(key);
        }
    }
    
    public void BindSteamActionHandle() {
        if (isAnalog) {
            steamAnalogHandle = SteamInput.GetAnalogActionHandle(name); 
            return;
        }
        steamDigitalHandle = SteamInput.GetDigitalActionHandle(name);
    }
    
    public void UpdateUnityInputGlyphs(InputGlyphSheet keyboardMouseSheet, InputGlyphSheet curGamepadSheet) {
        ResolveUnityGlyphs(keyboardMouseSheet, "Keyboard&Mouse", keyboardMouseGlyphs);
        ResolveUnityGlyphs(curGamepadSheet, "Gamepad", gamepadGlyphs);
    }

    private void ResolveUnityGlyphs(InputGlyphSheet sheet, string bindingGroup, Sprite[] glyphs) {
        glyphs[0] = null;
        glyphs[1] = null;
        if (sheet == null) return;

        InputBinding groupMask = InputBinding.MaskByGroup(bindingGroup);
        int index = unityInputAction.GetBindingIndex(groupMask);
        if (index < 0) return;
        
        ReadOnlyArray<InputBinding> bindings = unityInputAction.bindings;
        
        if (bindings[index].isPartOfComposite) {
            int compositeIndex = index;
            while (compositeIndex > 0 && !bindings[compositeIndex].isComposite) {
                compositeIndex--;
            }
        
            string compositeName = bindings[compositeIndex].GetNameOfComposite();
            if (compositeName == "OneModifier" || compositeName == "ButtonWithOneModifier") {
                Sprite modifier = null;
                Sprite binding = null;
                for (int i = compositeIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++) {
                    if (!groupMask.Matches(bindings[i])) continue;
        
                    if (string.Equals(bindings[i].name, "modifier", System.StringComparison.OrdinalIgnoreCase)) {
                        modifier = LookUp(i);
                    }
                    else {
                        binding = LookUp(i);
                    }
                }
        
                if (modifier != null) {
                    glyphs[0] = modifier;
                    glyphs[1] = binding;
                }
                else {
                    glyphs[0] = binding;
                }
                return;
            }
            // Other composites (like WASD) just show the first part for now
        }
        
        glyphs[0] = LookUp(index);
        
        Sprite LookUp(int bindingIndex) {
            unityInputAction.GetBindingDisplayString(bindingIndex, out _, out string controlPath);
            return sheet.Get(controlPath);
        }
    }
    
    public void UpdateSteamInputGlyph(InputHandle_t controller, InputActionSetHandle_t inputActionSet) {
        steamOriginCount = isAnalog ? SteamInput.GetAnalogActionOrigins(controller, inputActionSet, steamAnalogHandle, steamOrigins) :
                                      SteamInput.GetDigitalActionOrigins(controller, inputActionSet, steamDigitalHandle, steamOrigins);

        // None when the action isn't bound on this controller, which clears the glyph
        EInputActionOrigin origin = steamOriginCount > 0 ? steamOrigins[0] : EInputActionOrigin.k_EInputActionOrigin_None;
        if (curSteamGlyphId == origin) return;
        
        curSteamGlyphId = origin;
        steamGlyphs[0] = LoadSteamGlyph(origin);
    }
    
    public Sprite[] GetCurrentDisplayGlyphs() {
        Inputs inputs = Game.gameInstance.inputs;
        if (inputs.device == Inputs.Device.KeyboardMouse) {
            return keyboardMouseGlyphs;
        }
        return inputs.activelyUsingSteamInput ? steamGlyphs : gamepadGlyphs;
    }

    // Controller glyphs even while using keyboard and mouse
    public Sprite[] GetControllerDisplayGlyphs() {
        return Game.gameInstance.inputs.activelyUsingSteamInput ? steamGlyphs : gamepadGlyphs;
    }

    public void ResetSteamState() {
        steamInputWasDownLastFrame = steamInputIsDown;
        steamInputIsDown = false;
        steamValue = Vector2.zero;
    }

    public bool ReadSteamState(InputHandle_t controller) {
        if (isAnalog) {
            InputAnalogActionData_t data = SteamInput.GetAnalogActionData(controller, steamAnalogHandle);
            Vector2 value = new(data.x, data.y);
            if (value.sqrMagnitude > steamValue.sqrMagnitude) {
                steamValue = value;
            }
            return value.magnitude > ControllerNavigation.stickDeadzone;
        }
        
        bool down = SteamInput.GetDigitalActionData(controller, steamDigitalHandle).bState != 0;
        steamInputIsDown |= down;
        return down;
    }

    public bool IsPressed() {
        if (!isActive) return false;
        return steamInputIsDown || unityInputAction.IsPressed();
    }

    public bool WasPressedThisFrame() {
        if (!isActive) return false;
        return (steamInputIsDown && !steamInputWasDownLastFrame) || unityInputAction.WasPressedThisFrame();
    }

    public bool WasReleasedThisFrame() {
        if (!isActive) return false;
        return (!steamInputIsDown && steamInputWasDownLastFrame) || unityInputAction.WasReleasedThisFrame();
    }

    // Checks the bound controls instead of activeControl, which is already cleared if the key was tapped and released within the frame
    public bool WasPressedThisFrameOnKeyboard() {
        if (!isActive) return false;
        foreach (InputControl control in unityInputAction.controls) {
            if (control.device is Keyboard && control is ButtonControl { wasPressedThisFrame: true }) {
                return true;
            }
        }
        return false;
    }

    public Vector2 ReadVector2() {
        if (!isActive) return Vector2.zero;
        Vector2 unityValue = unityInputAction.ReadValue<Vector2>();
        return steamValue.sqrMagnitude > unityValue.sqrMagnitude ? steamValue : unityValue;
    }
    
    private static Sprite LoadSteamGlyph(EInputActionOrigin origin) {
        if (origin == EInputActionOrigin.k_EInputActionOrigin_None) {
            return null;
        }
        
        if (steamGlyphSprites.TryGetValue(origin, out Sprite sprite)) {
            return sprite;
        }

        string path = SteamInput.GetGlyphPNGForActionOrigin(origin, ESteamInputGlyphSize.k_ESteamInputGlyphSize_Medium, 0);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
            Debug.LogWarning($"No Steam Input glyph for {origin} ({path})");
            steamGlyphSprites[origin] = null; // So a missing glyph isn't looked up again every time
            return null;
        }

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, mipChain: false) {
            name = origin.ToString(),
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        
        // Resizes the texture to the PNG's size, and frees the CPU copy since it's only drawn
        if (!texture.LoadImage(File.ReadAllBytes(path), markNonReadable: true)) {
            Debug.LogWarning($"Failed to load Steam Input glyph {path}");
            Object.Destroy(texture);
            steamGlyphSprites[origin] = null;
            return null;
        }

        sprite = Sprite.Create(texture, new(0f, 0f, texture.width, texture.height), new(0.5f, 0.5f));
        sprite.name = origin.ToString();
        steamGlyphSprites[origin] = sprite;
        return sprite;
    }
    
}