using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Pool;
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
        inputs.attack = AddAction("Attack");
        inputs.interact = AddAction("Interact");
        inputs.advanceDialogue = AddAction("AdvanceDialogue");
        inputs.inventory = AddAction("Inventory");
        inputs.select = AddAction("Select");
        inputs.sellSingleItem = AddAction("SellSingle");
        inputs.sellStack = AddAction("SellStack");
        inputs.quickSell = AddAction("QuickSell");
        inputs.tradeItems = AddAction("TradeItems");
        inputs.purchase = AddAction("Purchase");
        inputs.place = AddAction("Place");
        inputs.placeSingle = AddAction("PlaceSingle");
        inputs.cancel = AddAction("Cancel");
        inputs.splitStack = AddAction("SplitStack");
        inputs.quickMove = AddAction("QuickMove");
        inputs.quickDrop = AddAction("QuickDrop");
        inputs.useItem = AddAction("UseItem");
        inputs.back = AddAction("Back");
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
        
        LoadBindingOverrides();
        UpdateUnityInputGlyphs();

        GameAction AddAction(string actionName) {
            GameAction action = new(actionName);
            inputs.all.Add(action);
            inputs.gameActionLookup.Add(action.unityInputAction.id, action);
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

        BuildInputLayerStack();
        UpdateInputLayers();

        // The press that sets or cancels a binding shouldn't also go back out of the menu
        if (controlBindings.IsRebindingControls) return;

        CheckButtonControllerShortcuts();

        // Done here instead of with performed callbacks so Steam Input presses work too
        if (inputs.pause.WasPressedThisFrame()) {
            OnPausePressed();
        }
        if (inputs.back.WasPressedThisFrame()) {
            OnBackPressed(fromKeyboard: inputs.back.WasPressedThisFrameOnKeyboard());
        }
        
    }
    
    // Layers are added bottom to top. A layer that isn't added is fully off, which is how menus block gameplay.
    private void BuildInputLayerStack() {
        inputs.layerStack.Clear();
        AddInputLayer(Inputs.GlobalLayer);

        bool popupOpen = pauseMenu.paused || ui.messagePopup.IsShowing;
        if (popupOpen) {
            AddInputLayer(Inputs.SelectionLayer);
            AddInputLayer(Inputs.MenuLayer);
        }
        else if (InRaid) {
            if (PlayerInventoryIsOpen || LootInventoryIsOpen) {
                AddInputLayer(Inputs.SelectionLayer);
                AddInputLayer(Inputs.MenuLayer);
                if (!IsDraggingItem) {
                    AddInputLayer(Inputs.InventoryLayer);
                }
                if (!IsDraggingItem && !LootInventoryIsOpen) {
                    AddInputLayer(Inputs.PlayerInventoryLayer);
                }
            }
            else {
                AddInputLayer(Inputs.GameplayLayer);
            }
        }
        else if (InHideout) {
            AddInputLayer(Inputs.SelectionLayer);
            AddInputLayer(Inputs.MenuLayer);
            if (!IsDraggingItem) {
                AddInputLayer(Inputs.InventoryLayer);
            }
            if (!IsDraggingItem && OnTradingTab && transactionState == TransactionState.Selling) {
                AddInputLayer(Inputs.SellingLayer);
            }
            if (OnTradingTab && transactionState == TransactionState.Buying) {
                AddInputLayer(Inputs.BuyingLayer);
            }
        }
        else {
            AddInputLayer(Inputs.SelectionLayer);
            AddInputLayer(Inputs.MenuLayer);
        }

        if (IsDraggingItem && !popupOpen) {
            AddInputLayer(Inputs.SelectedItemLayer);
        }

        if (!tutorial.dialogue.Finished) {
            AddInputLayer(Inputs.DialogueLayer);
        }
    }

    private void AddInputLayer(string actionMapName) {
        inputs.layerStack.Add(InputSystem.actions.FindActionMap(actionMapName, throwIfNotFound: true));
    }

    public GameAction FindGameAction(InputAction unityAction) {
        if (unityAction == null) return null;
        return inputs.gameActionLookup.GetValueOrDefault(unityAction.id);
    }

    private void CheckButtonControllerShortcuts() {
        // Clicking a button can enable or disable others, so this goes over a copy
        using var _ = ListPool<ButtonFeel>.Get(out var buttonsToCheck);
        buttonsToCheck.AddRange(ButtonFeel.activeWithControllerShortcuts);
        
        foreach (ButtonFeel button in buttonsToCheck) {
            if (!button.isActiveAndEnabled) continue; // Disabled by an earlier click this frame
            GameAction action = FindGameAction(button.controllerShortcut.action);
            UpdateButtonShortcutGlyph(button, action);
            if (action == null || !action.WasPressedThisFrame()) continue;
            button.PlayShortcutPress();
            button.TriggerClick();
        }
    }

    private void UpdateButtonShortcutGlyph(ButtonFeel button, GameAction action) {
        // Single image, so a modifier composite only shows its first part
        Sprite glyph = action != null && action.isActive ? action.GetCurrentDisplayGlyphs()[0] : null;
        button.SetInputGlyph(glyph);
    }

    // Decides which actions are active this frame. An action is active when its map is on the stack and none of its bindings
    // for the current device are also used by an active action on a higher layer. Actions on the same layer never shadow each other.
    private void UpdateInputLayers() {
        if (inputs.layerStack.Count == 0) {
            foreach (GameAction action in inputs.all) {
                action.isActive = true;
                action.layerIndex = -1;
            }
            return;
        }

        GameAction.BindingSource source;
        if (inputs.device == Inputs.Device.KeyboardMouse) {
            source = GameAction.BindingSource.KeyboardMouse;
        }
        else if (inputs.activelyUsingSteamInput) {
            source = GameAction.BindingSource.Steam;
        }
        else {
            source = GameAction.BindingSource.Gamepad;
        }

        foreach (GameAction action in inputs.all) {
            action.isActive = false;
            action.layerIndex = -1;
        }

        inputs.claimedUnityBindings.Clear();
        inputs.claimedSteamOrigins.Clear();

        for (int i = inputs.layerStack.Count - 1; i >= 0; i--) {
            InputActionMap map = inputs.layerStack[i];

            // Checked against higher layers only, before this layer claims anything, so actions on the same layer don't shadow each other
            foreach (GameAction action in inputs.all) {
                if (action.unityInputAction.actionMap != map) continue;
                action.layerIndex = i;
                
                bool hasBindings = action.HasBindings(source);
                bool bindingsNotClaimed = !action.SharesClaimedBinding(source, inputs.claimedUnityBindings, inputs.claimedSteamOrigins);
                action.isActive = hasBindings && bindingsNotClaimed;
            }

            // Shadowed actions don't claim, so their other bindings still reach lower layers
            foreach (GameAction action in inputs.all) {
                if (action.unityInputAction.actionMap != map || !action.isActive) continue;
                action.ClaimBindings(source, inputs.claimedUnityBindings, inputs.claimedSteamOrigins);
            }
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
            action.UpdateUnityBindingKeys();
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