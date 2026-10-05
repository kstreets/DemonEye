using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Game {

    public class InputPrompt {
        public TextMeshProUGUI text;
        public Func<(InputAction action, string label)> getPrompt;
        public InputAction shownAction;
        public string shownLabel;
        public bool dirty = true;
    }

    private void InitInputIcons() {
        // Lets TMP find the sprite asset by name in <sprite="..."> tags without it having to be in a Resources folder
        if (ui.inputIconSpriteAsset) {
            MaterialReferenceManager.AddSpriteAsset(ui.inputIconSpriteAsset);
        }

        AddInputPrompt(ui.menuBackButton.text, input.escape);
        AddInputPrompt(ui.dialogueBoxPrompt, input.advanceDialogue);
        
        AddInputPrompt(inputPrompts.hideoutSelect_place, input.selectItem);
        AddInputPrompt(inputPrompts.hideoutQuickMove, input.moveStack);
        AddInputPrompt(inputPrompts.hideoutSplit_placeSingle, static () => {
            GameData.Input input = gameInstance.input;
            if (gameInstance.IsDraggingItem) {
                return (input.placeSingleItem, "Place Single");
            }
            return (input.placeSingleItem, "Split Stack");
        });
        
        AddInputPrompt(inputPrompts.raidInventory, input.inventory);

        foreach (ToggleButtonGroup group in ui.navToggleGroups) {
            AddToggleGroupInputPrompts(group);
        }
    }

    // Shows the bumpers or triggers on either side of the group depending on its navigation mode, only when using a controller
    private void AddToggleGroupInputPrompts(ToggleButtonGroup group) {
        (InputAction left, InputAction right) = group.navigationMode switch {
            ToggleButtonGroup.NavigationMode.Primary => (input.menuTabLeft, input.menuTabRight),
            ToggleButtonGroup.NavigationMode.Secondary => (input.menuSecondaryTabLeft, input.menuSecondaryTabRight),
            _ => (null, null),
        };
        if (left == null) return;

        if (group.leftInputPrompt) {
            AddInputPrompt(group.leftInputPrompt, () => UsingControllerControls && group.TogglesUsable ? (left, string.Empty) : (null, null));
        }
        if (group.rightInputPrompt) {
            AddInputPrompt(group.rightInputPrompt, () => UsingControllerControls && group.TogglesUsable ? (right, string.Empty) : (null, null));
        }
    }

    private void InputIconsOnInputDeviceChanged() {
        inputPrompts.inputIconCache.Clear();
        foreach (InputPrompt prompt in inputPrompts.inputPrompts) {
            prompt.dirty = true;
        }
    }

    // A prompt that always shows the same action, using the text's current content as the label
    private void AddInputPrompt(TextMeshProUGUI text, InputAction action) {
        string label = text.text;
        AddInputPrompt(text, () => (action, label));
    }

    // A prompt whose action and label depend on the game's state, e.g. () => IsDraggingItem ? (input.placeSingleItem, "Place One") : (input.splitStack, "Split")
    private void AddInputPrompt(TextMeshProUGUI text, Func<(InputAction action, string label)> getPrompt) {
        InputPrompt prompt = new() { text = text, getPrompt = getPrompt };
        inputPrompts.inputPrompts.Add(prompt);
        RefreshInputPrompt(prompt);
    }

    private void UpdateInputPrompts() {
        if (InRaid && thisFrame.flags.HasFlag(GameData.FrameFlags.InventoryOpened)) {
            if (UsingControllerControls) { // Hide it on controller because Y splits the stack not closes inventory
                inputPrompts.raidInventory.gameObject.SetActive(false);
            }
            foreach (Transform invPrompt in inputPrompts.allInventoryPrompts) {
                invPrompt.gameObject.SetActive(true);
            }
        }
        else if (InRaid && !PlayerInventoryIsOpen) {
            inputPrompts.raidInventory.gameObject.SetActive(true);
            foreach (Transform invPrompt in inputPrompts.allInventoryPrompts) {
                invPrompt.gameObject.SetActive(false);
            }
        }
        
        foreach (InputPrompt prompt in inputPrompts.inputPrompts) {
            if (!prompt.text.isActiveAndEnabled) continue;
            RefreshInputPrompt(prompt);
        }
    }

    // Only rebuilds the text when what it shows changed, since setting TMP text every frame is expensive
    private void RefreshInputPrompt(InputPrompt prompt) {
        (InputAction action, string label) = prompt.getPrompt();
        if (!prompt.dirty && action == prompt.shownAction && label == prompt.shownLabel) return;

        prompt.dirty = false;
        prompt.shownAction = action;
        prompt.shownLabel = label;

        // Also hidden when the action has no button on the current device
        string icon = action != null ? InputIcon(action) : string.Empty;
        if (icon == string.Empty) {
            prompt.text.text = string.Empty;
        }
        else {
            // Prompts without a label just show the button
            prompt.text.text = string.IsNullOrEmpty(label) ? icon : $"{icon} {label}";
        }
    }

    // Rich text for the button bound to this action on the current input device.
    // Icons are looked up by name in the input icon sprite asset, e.g. "gamepad_buttonsouth", "keyboard_e", "mouse_leftbutton".
    // Buttons without an icon yet show their name instead, e.g. [E] or [A].
    public string InputIcon(InputAction action) {
        if (inputPrompts.inputIconCache.TryGetValue(action, out string icon)) return icon;
        icon = BuildInputIcon(action);
        inputPrompts.inputIconCache[action] = icon;
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

            string separator = binding.GetNameOfComposite().Contains("Modifier") ? "+" : "/";
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

        if (UsingControllerControls) {
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
        string label = deviceName == "gamepad" ? XboxButtonName(controlPath) ?? displayName : displayName;
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
