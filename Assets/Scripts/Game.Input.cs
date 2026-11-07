using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using static GameData;

public partial class Game {

    // The only action set in the Steam In-Game Actions file, it holds every action since the game reads all of them at once
    private const string steamActionSet = "Gameplay";
    
    public bool UsingController => inputs.device is Inputs.Device.Controller or Inputs.Device.ControllerWithMouse;
    public bool UsingControllerAsPointer => inputs.device is Inputs.Device.Controller;
    
    public static bool EventCameFromCurrentPointerDevice(PointerEventData eventData) {
        if (eventData == null) {
            return true;
        }
        const int controllerId = ControllerNavigation.pointerEventId;
        if (gameInstance.UsingControllerAsPointer) {
            return eventData.pointerId == controllerId;
        }
        return eventData.pointerId != controllerId;
    }

    private void InitInput() {
        inputs.move = AddAction("Move");
        inputs.aim = AddAction("Aim");
        inputs.interact = AddAction("Interact");
        inputs.advanceDialogue = AddAction("AdvanceDialogue");
        inputs.inventory = AddAction("Inventory");
        inputs.selectItem = AddAction("SelectItem");
        inputs.placeSingleItem = AddAction("PlaceSingleItem");
        inputs.splitStack = AddAction("SplitStack");
        inputs.moveStack = AddAction("MoveStack");
        inputs.useItem = AddAction("UseItem");
        inputs.escape = AddAction("Escape");
        inputs.pause = AddAction("Pause");
        inputs.quickUse1 = AddAction("QuickUse1");
        inputs.quickUse2 = AddAction("QuickUse2");
        inputs.quickUse3 = AddAction("QuickUse3");
        inputs.quickUse4 = AddAction("QuickUse4");
        inputs.quickUseSelected = AddAction("QuickUseSelected");
        inputs.quickUsePrevious = AddAction("QuickUsePrevious");
        inputs.quickUseNext = AddAction("QuickUseNext");
        inputs.teleport = AddAction("Teleport");
        inputs.menuMove = AddAction("MenuMove");
        inputs.menuSubmit = AddAction("MenuSubmit");
        inputs.menuTabLeft = AddAction("MenuTabLeft");
        inputs.menuTabRight = AddAction("MenuTabRight");
        inputs.menuSecondaryTabLeft = AddAction("MenuSecondaryTabLeft");
        inputs.menuSecondaryTabRight = AddAction("MenuSecondaryTabRight");
        
        // The Steam Deck starts on the controller, its touchpad switches to the cursor once it's used
        inputs.device = OnSteamDeck ? Inputs.Device.ControllerWithMouse : Inputs.Device.KeyboardMouse;

        if (OnSteam) {
            inputs.steamInputInitalized = SteamInput.Init(true);
            if (!inputs.steamInputInitalized) {
                Debug.Log("Steam Input failed to initialize");
            }
        }
        
        UpdateUnityInputGlyphs();

        GameAction AddAction(string actionName) {
            GameAction action = new(actionName);
            inputs.all.Add(action);
            return action;
        }
    }
    
    private void ShutdownInput() {
        if (inputs.steamInputInitalized) {
            SteamInput.Shutdown();
            inputs.steamInputInitalized = false;
        }
    }

    private void UpdateInput() {
        if (inputs.steamInputInitalized) {
            inputs.activelyUsingSteamInput = UpdateSteamInput();
        }
        
        // When using steam input we want to hide all gamepads from Unity
        ToggleGamepadsForUnity(!inputs.activelyUsingSteamInput);
        
        if (CheckForInputDeviceChange()) {
            inputs.lastDeviceSwitchTime = Time.unscaledTime;
            MenuNavigationOnInputDeviceChanged();
        }
        
        // Done here instead of with performed callbacks so Steam Input presses work too
        if (inputs.pause.WasPressedThisFrame()) {
            OnPausePressed();
        }
        if (inputs.escape.WasPressedThisFrame()) {
            OnEscapePressed(fromKeyboard: inputs.escape.WasPressedThisFrameOnKeyboard());
        }
        
    }
    
    public void UpdateUnityInputGlyphs() {
        Gamepad gamepad = Gamepad.current;
        inputs.gamepadUsedForCurrentGlyphs = gamepad;

        InputGlyphSheet gamepadSheet = gamepad switch {
            _ => inputPrompts.xboxGlyphs,
        };

        foreach (GameAction action in inputs.all) {
            action.UpdateUnityInputGlyphs(inputPrompts.keyboardMouseGlyphs, gamepadSheet);
        }
    }
    
    private bool UpdateSteamInput() {
        SteamInput.RunFrame();

        foreach (GameAction action in inputs.all) {
            action.ResetSteamState();
        }

        // Keep polling to load Steam Actions, because it can take a few frames after init
        if (!SteamActionSetLoaded() && !TryLoadSteamActionSet()) {
            return false;
        }
        
        // Steam Input takes over controllers when it has them. When it has none (e.g. the player turned Steam Input off for the game,
        // or Steam doesn't handle that controller type), Unity reads them instead so the controller still works.
        int controllerCount = SteamInput.GetConnectedControllers(inputs.steamControllers);
        
        if (controllerCount == 0) {
            return false;
        }

        // This is just setting all controllers to use the only action set the game has,
        // apparently you can have multiple action sets and assign different controllers to different sets
        SteamInput.ActivateActionSet(new(Constants.STEAM_INPUT_HANDLE_ALL_CONTROLLERS), inputs.steamActionSet);

        for (int i = 0; i < controllerCount; i++) {
            InputHandle_t controller = inputs.steamControllers[i];
            bool used = false;
            foreach (GameAction action in inputs.all) {
                used |= action.ReadSteamState(controller);
            }
            if (used) {
                inputs.activeSteamController = controller;
                break;
            }
        }
        
        // Assign the first controller we see to be the active steam controller until any other one is used
        if (inputs.activeSteamController.m_InputHandle == 0 && controllerCount > 0) {
            inputs.activeSteamController = inputs.steamControllers[0];
        }

        InputHandle_t activeController = inputs.activeSteamController;
        if (activeController.m_InputHandle != 0) {
            foreach (GameAction action in inputs.all) {
                action.UpdateSteamInputGlyph(activeController, inputs.steamActionSet);
            }
        }
        
        return true;

        bool SteamActionSetLoaded() {
            return inputs.steamActionSet.m_InputActionSetHandle != 0;
        }
        
        bool TryLoadSteamActionSet() {
            inputs.steamActionSet = SteamInput.GetActionSetHandle(steamActionSet);
            bool loadedOnThisFrame = SteamActionSetLoaded();
            
            if (loadedOnThisFrame) {
                foreach (GameAction action in inputs.all) {
                    action.BindSteamActionHandle();
                }
            }
            
            return loadedOnThisFrame;
        }
    }
    
    private bool CheckForInputDeviceChange() {
        if (inputs.device == Inputs.Device.Controller && MouseUsedThisFrame()) {
            inputs.device = Inputs.Device.ControllerWithMouse;
            return true;
        }
        
        if (inputs.device != Inputs.Device.KeyboardMouse && KeyboardUsedThisFrame()) {
            inputs.device = Inputs.Device.KeyboardMouse;
            return true;
        }
        
        if (inputs.device == Inputs.Device.ControllerWithMouse) {
            bool analogUsed = inputs.activelyUsingSteamInput ? SteamInputAnalogUsedThisFrame() : GamepadAnalogUsedThisFrame(); 
            if (analogUsed) {
                inputs.device = Inputs.Device.Controller;
                return true;
            }
        }
        
        if (inputs.device == Inputs.Device.KeyboardMouse) {
            bool gamepadUsed = inputs.activelyUsingSteamInput ? AnySteamInputUsedThisFrame() : GamepadUsedThisFrame();
            if (gamepadUsed) {
                inputs.device = Inputs.Device.Controller;
                return true;
            }
        }
        
        return false;
    }

    private bool GamepadAnalogUsedThisFrame() {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return false;

        const float deadzone = ControllerNavigation.stickDeadzone;
        return gamepad.leftStick.ReadValue().magnitude > deadzone
            || gamepad.rightStick.ReadValue().magnitude > deadzone
            || gamepad.dpad.ReadValue() != Vector2.zero;
    }

    private bool GamepadUsedThisFrame() {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) {
            return false;
        }
        
        if (GamepadAnalogUsedThisFrame()) {
            return true;
        }

        foreach (InputControl control in gamepad.allControls) {
            if (control is ButtonControl { synthetic: false } button && button.wasPressedThisFrame) {
                return true;
            }
        }
        return false;
    }

    private bool MouseUsedThisFrame() {
        Mouse mouse = Mouse.current;
        return mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
    }

    private bool KeyboardUsedThisFrame() {
        return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
    }

    private void ToggleGamepadsForUnity(bool enable) {
        foreach (InputDevice device in InputSystem.devices) {
            if (device is not Gamepad || device.enabled == enable) continue;
            if (enable) {
                InputSystem.EnableDevice(device);
            }
            else {
                InputSystem.DisableDevice(device);
            }
        }
    }

    private bool AnySteamInputUsedThisFrame() {
        if (SteamInputAnalogUsedThisFrame()) {
            return true;
        }
        foreach (GameAction action in inputs.all) {
            if (!action.isAnalog && action.steamInputIsDown && !action.steamInputWasDownLastFrame) {
                return true;
            }
        }
        return false;
    }
    
    private bool SteamInputAnalogUsedThisFrame() {
        foreach (GameAction action in inputs.all) {
            if (action.isAnalog && action.steamValue.magnitude > ControllerNavigation.stickDeadzone) {
                return true;
            }
        }
        return false;
    }

}

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
    
    private static EInputActionOrigin[] actionOrigins = new EInputActionOrigin[Constants.STEAM_INPUT_MAX_ORIGINS];
    private static readonly Dictionary<EInputActionOrigin, Sprite> steamGlyphSprites = new();
    
    public GameAction(string name) {
        this.name = name;
        unityInputAction = InputSystem.actions.FindAction(name, throwIfNotFound: true);
        isAnalog = unityInputAction.expectedControlType == "Vector2";
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
        int originCount = isAnalog ? SteamInput.GetAnalogActionOrigins(controller, inputActionSet, steamAnalogHandle, actionOrigins) :
                                     SteamInput.GetDigitalActionOrigins(controller, inputActionSet, steamDigitalHandle, actionOrigins);
        
        // None when the action isn't bound on this controller, which clears the glyph
        EInputActionOrigin origin = originCount > 0 ? actionOrigins[0] : EInputActionOrigin.k_EInputActionOrigin_None;
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
        return steamInputIsDown || unityInputAction.IsPressed();
    }

    public bool WasPressedThisFrame() {
        return (steamInputIsDown && !steamInputWasDownLastFrame) || unityInputAction.WasPressedThisFrame();
    }

    public bool WasReleasedThisFrame() {
        return (!steamInputIsDown && steamInputWasDownLastFrame) || unityInputAction.WasReleasedThisFrame();
    }
    
    // Checks the bound controls instead of activeControl, which is already cleared if the key was tapped and released within the frame
    public bool WasPressedThisFrameOnKeyboard() {
        foreach (InputControl control in unityInputAction.controls) {
            if (control.device is Keyboard && control is ButtonControl { wasPressedThisFrame: true }) {
                return true;
            }
        }
        return false;
    }

    public Vector2 ReadVector2() {
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
