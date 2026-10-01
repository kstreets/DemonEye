using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Game {

    // Icon strings for the current input device, cleared when the device changes
    private readonly Dictionary<InputAction, string> inputIconCache = new();
    private readonly List<InputPrompt> inputPrompts = new();

    private struct InputPrompt {
        public TextMeshProUGUI text;
        public InputAction action;
        public string label;
    }

    private void InitInputIcons() {
        // Lets TMP find the sprite asset by name in <sprite="..."> tags without it having to be in a Resources folder
        if (ui.inputIconSpriteAsset) {
            MaterialReferenceManager.AddSpriteAsset(ui.inputIconSpriteAsset);
        }

        AddInputPrompt(ui.menuBackButton.text, input.escape);
    }

    private void InputIconsOnInputDeviceChanged() {
        inputIconCache.Clear();
        foreach (InputPrompt prompt in inputPrompts) {
            RefreshInputPrompt(prompt);
        }
    }

    private void AddInputPrompt(TextMeshProUGUI text, InputAction action) {
        InputPrompt prompt = new() {
            text = text,
            action = action, 
            label = text.text,
        };
        inputPrompts.Add(prompt);
        RefreshInputPrompt(prompt);
    }

    private void RefreshInputPrompt(InputPrompt prompt) {
        prompt.text.text = $"{InputIcon(prompt.action)} {prompt.label}";
    }

    // Rich text for the button bound to this action on the current input device.
    // Icons are looked up by name in the input icon sprite asset, e.g. "gamepad_buttonsouth", "keyboard_e", "mouse_leftbutton".
    // Buttons without an icon yet show their name instead, e.g. [E] or [A].
    public string InputIcon(InputAction action) {
        if (inputIconCache.TryGetValue(action, out string icon)) return icon;
        icon = BuildInputIcon(action);
        inputIconCache[action] = icon;
        return icon;
    }

    private string BuildInputIcon(InputAction action) {
        var bindings = action.bindings;
        for (int i = 0; i < bindings.Count; i++) {
            InputBinding binding = bindings[i];
            if (binding.isPartOfComposite) continue;

            if (!binding.isComposite) {
                if (!IsBindingForCurrentDevice(binding)) continue;
                return BindingIcon(action, i);
            }

            // Composites like shift + left click show each of their parts
            int firstPart = i + 1;
            if (firstPart >= bindings.Count || !IsBindingForCurrentDevice(bindings[firstPart])) continue;

            string separator = binding.GetNameOfComposite().Contains("Modifier") ? " + " : " / ";
            string icon = string.Empty;
            for (int part = firstPart; part < bindings.Count && bindings[part].isPartOfComposite; part++) {
                if (part > firstPart) {
                    icon += separator;
                }
                icon += BindingIcon(action, part);
            }
            return icon;
        }
        // Not bound on this device
        return string.Empty;
    }

    private bool IsBindingForCurrentDevice(InputBinding binding) {
        string layout = InputControlPath.TryGetDeviceLayout(binding.effectivePath);
        if (string.IsNullOrEmpty(layout)) return false;

        if (usingController) {
            return InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad");
        }
        return InputSystem.IsFirstLayoutBasedOnSecond(layout, "Keyboard") || InputSystem.IsFirstLayoutBasedOnSecond(layout, "Mouse");
    }

    private string BindingIcon(InputAction action, int bindingIndex) {
        string displayName = action.GetBindingDisplayString(bindingIndex);
        string tint = ColorUtility.ToHtmlStringRGBA(config.styles.inputIconTint);

        // Taken from the binding's path, e.g. "<Gamepad>/buttonSouth", instead of the display string's out parameters.
        // Those give the connected device's layout (e.g. "XInputControllerWindows") which would change the icon name per controller.
        string bindingPath = action.bindings[bindingIndex].effectivePath;
        string deviceName = GenericDeviceName(InputControlPath.TryGetDeviceLayout(bindingPath));
        string controlPath = bindingPath.Substring(bindingPath.IndexOf('/') + 1);

        string spriteName = $"{deviceName}_{controlPath}".ToLowerInvariant().Replace('/', '_');
        TMP_SpriteAsset spriteAsset = ui.inputIconSpriteAsset;
        if (spriteAsset && spriteAsset.GetSpriteIndexFromName(spriteName) != -1) {
            return $"<sprite=\"{spriteAsset.name}\" name=\"{spriteName}\" color=#{tint}>";
        }

        // We only show Xbox buttons for controllers
        string label = usingController ? XboxButtonName(controlPath) ?? displayName : displayName;
        return $"<color=#{tint}>[{label}]</color>";
    }

    private static string GenericDeviceName(string layout) {
        if (string.IsNullOrEmpty(layout)) return string.Empty;
        if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad")) return "gamepad";
        if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Keyboard")) return "keyboard";
        if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Mouse")) return "mouse";
        return layout;
    }

    private static string XboxButtonName(string controlPath) {
        return controlPath switch {
            "buttonSouth" => "A",
            "buttonEast" => "B",
            "buttonWest" => "X",
            "buttonNorth" => "Y",
            "leftShoulder" => "LB",
            "rightShoulder" => "RB",
            "leftTrigger" => "LT",
            "rightTrigger" => "RT",
            "leftStickPress" => "LS",
            "rightStickPress" => "RS",
            "leftStick" => "Left Stick",
            "rightStick" => "Right Stick",
            "dpad" => "D-Pad",
            "start" => "Menu",
            "select" => "View",
            _ => null,
        };
    }

}
