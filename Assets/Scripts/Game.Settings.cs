using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static GameData;

public partial class Game {
    
    // -1, or a resolution the screen doesn't have, means not saved, so the setting starts on its default
    [Serializable]
    public class SettingsState {
        public Vector2Int resolution = new(-1, -1);
        public int fullScreenIndex = -1;
        public int fpsLimitIndex = -1;
        public int targetMonitor = -1;
        public int vsyncEnabled = -1;

        public int masterVolumeIndex = -1;
        public int musicVolumeIndex = -1;
        public int gameVolumeIndex = -1;
    }
    
    private string[] fullScreenNames = { "Exclusive Full Screen", "Full Screen Window", "Maximized", "Windowed" };
    private int[] fpsLimits = { 60, 90, 120, 144, 165, -1 };
    
    private List<Vector2Int> allScreenResolutions = new();
    private readonly List<DisplayInfo> displayLayout = new();
    
    public static void OnAnySettingsChanged() {
        gameInstance.UpdateApplySettingsButtonState();
    }
    
    private void InitSettings(SettingsState loadedSettings) {
        // Includes inactive ones because only the shown settings tab is active, and the others still need applying and tracking for changes
        settings.all = settings.settingsParent.GetComponentsInChildren<SingleSetting>(includeInactive: true);
        
        foreach (Resolution resolution in Screen.resolutions) {
            Vector2Int resDim = new(resolution.width, resolution.height);
            if (!allScreenResolutions.Contains(resDim)) {
                allScreenResolutions.Add(resDim);
            }
        }
        
        // Filled in by the settings below when nothing was saved
        settings.curSettingsState = loadedSettings ?? new SettingsState();

        // Video Settings
        {
            const int numberOfFullscreenModes = 2;
            settings.fullscreenMode.Init(numberOfFullscreenModes, loadedSettings?.fullScreenIndex)
            .OnDefault(() => IndexFromScreenMode(FullScreenMode.FullScreenWindow))
            .OnChange(i => {
                int nameIndex = (int)GetScreenModeFromIndex(i);
                settings.fullscreenMode.Display(fullScreenNames[nameIndex]);
            })
            .OnApply(i => {
                settings.curSettingsState.fullScreenIndex = i;
                ApplyScreenModeAndResolution();
            });
            
            // A saved resolution the screen no longer has falls back to the default
            int savedResolutionIndex = loadedSettings != null ? FindSettingResolutionIndex(loadedSettings.resolution) : -1;
            settings.resolution.Init(allScreenResolutions.Count, savedResolutionIndex >= 0 ? savedResolutionIndex : null)
            .OnDefault(() => allScreenResolutions.Count - 1)
            .OnChange(i => {
                Vector2Int screenSize = allScreenResolutions[i];
                settings.resolution.Display($"{screenSize.x} x {screenSize.y}");
            })
            .OnApply(i => {
                settings.curSettingsState.resolution = allScreenResolutions[i];
                ApplyScreenModeAndResolution();
            });
            
            settings.fpsLimit.Init(fpsLimits.Length, loadedSettings?.fpsLimitIndex)
            .OnDefault(() => fpsLimits.Length - 1)
            .OnChange((i) => {
                int chosenFpsLimit = fpsLimits[i];
                string frameRateString = chosenFpsLimit == -1 ? "Unlimited" : chosenFpsLimit.ToString();
                settings.fpsLimit.Display(frameRateString);
                Application.targetFrameRate = chosenFpsLimit;
                settings.curSettingsState.fpsLimitIndex = i;
            });
            
            // The displays the window can be moved to. A saved display that's no longer plugged in falls back to the default.
            Screen.GetDisplayLayout(displayLayout);
            int savedMonitor = loadedSettings?.targetMonitor ?? -1;
            settings.targetMonitor.Init(Mathf.Max(1, displayLayout.Count), savedMonitor >= 0 && savedMonitor < displayLayout.Count ? savedMonitor : null)
            .OnDefault(() => Mathf.Max(0, displayLayout.IndexOf(Screen.mainWindowDisplayInfo))) // Wherever the window already is
            .OnChange(i => settings.targetMonitor.Display($"Display {i + 1}"))
            .OnApply(i => {
                settings.curSettingsState.targetMonitor = i;
                if (i < displayLayout.Count) {
                    MoveWindowToDisplay(displayLayout[i]);
                }
            });

            // On means the fps limit is ignored, since frames wait for the monitor
            const int numVsyncSettings = 2;
            settings.vsync.Init(numVsyncSettings, loadedSettings?.vsyncEnabled)
            .OnDefault(() => 0)
            .OnChange((i) => {
                settings.vsync.Display(i == 0 ? "Off" : "On");
                QualitySettings.vSyncCount = i;
                settings.curSettingsState.vsyncEnabled = i;
            });
        }

        // Audio Settings
        {
            const int numVolumeOptions = 11; // 0 - 10
            const float maxVolumeIndex = numVolumeOptions - 1f;
        
            settings.masterVolume.Init(numVolumeOptions, loadedSettings?.masterVolumeIndex)
            .OnDefault(() => 6)
            .OnChange(i => {
                float linearVolume = i / maxVolumeIndex;
                AudioListener.volume = linearVolume;
                settings.masterVolume.Display(linearVolume.ToString("0%"));
                settings.curSettingsState.masterVolumeIndex = i;
            });
        
            settings.musicVolume.Init(numVolumeOptions, loadedSettings?.musicVolumeIndex)
            .OnDefault(() => 5)
            .OnChange(i => {
                float linearVolume = i / maxVolumeIndex;
                settings.musicAudioMixer.SetFloat("Volume", linearVolume.LinearToDecibel());
                settings.musicVolume.Display(linearVolume.ToString("0%"));
                settings.curSettingsState.musicVolumeIndex = i;
            });
        
            settings.gameVolume.Init(numVolumeOptions, loadedSettings?.gameVolumeIndex)
            .OnDefault(() => 10)
            .OnChange(i => {
                float linearVolume = i / maxVolumeIndex;
                settings.gameAudioMixer.SetFloat("Volume", linearVolume.LinearToDecibel());
                settings.gameVolume.Display(linearVolume.ToString("0%"));
                settings.curSettingsState.gameVolumeIndex = i;
            });
        }

        // Game Settings
        {
            // settings.cameraShake.Init(2).OnDefault(() => 1).OnChange(i => settings.cameraShake.Display(i == 0 ? "Off" : "On"));
            // settings.cameraDistance.Init(3).OnDefault(() => 0).OnChange(i => settings.cameraDistance.Display("Normal"));
            // settings.showHoverHints.Init(2).OnDefault(() => 1).OnChange(i => settings.showHoverHints.Display(i == 0 ? "Off" : "On"));
            // settings.itemPopupDelay.Init(3).OnDefault(() => 0).OnChange(i => settings.itemPopupDelay.Display("Normal"));
            // settings.uncMode.Init(2).OnDefault(() => 0).OnChange(i => settings.uncMode.Display(i == 0 ? "Off" : "On"));
        }
        
        ApplySettings();
    }
    
    private void UpdateSettings() {
        UpdateUIHints();
        
        bool controlPageActive = settings.controlsParent.gameObject.activeInHierarchy;
        if (!controlPageActive) {
            controlBindings.activeRebindOperation?.Cancel();
            return;
        }
        
        bool steam = inputs.activelyUsingSteamInput;
        if (settings.steamInputBanner.gameObject.activeSelf != steam) {
            settings.steamInputBanner.gameObject.SetActive(steam);
        }
        foreach (BindingSlot slot in controlBindings.bindingSlots) {
            RefreshCell(slot);
        }
    }
    
    private void SettingsOnScreenSizeChanged() {
        Vector2Int settingResolution = allScreenResolutions[settings.resolution.curIndex];
        if (settingResolution != ScreenSize) {
            int index = FindSettingResolutionIndex(ScreenSize);
            if (index != -1) {
                settings.resolution.ForceChangeWithoutApplying(index);
            }
        }
        
        FullScreenMode settingScreenMode = GetScreenModeFromIndex(settings.curSettingsState.fullScreenIndex);
        if (settingScreenMode != Screen.fullScreenMode) {
            settings.fullscreenMode.ForceChangeWithoutApplying(IndexFromScreenMode(Screen.fullScreenMode));
        }
    }
    
    private void ApplySettings() {
        foreach (SingleSetting setting in settings.all) {
            setting.Apply();
        }
        UpdateApplySettingsButtonState();
    }
    
    private void UpdateApplySettingsButtonState() {
        settings.applyChangesButton.SetClickableState(SettingsHaveChangesToApply());
    }

    private bool SettingsHaveChangesToApply() {
        foreach (SingleSetting setting in settings.all) {
            if (setting.HasChangesToApply()) return true;
        }
        return false;
    }

    // Asks first when changes are waiting for apply, so they aren't left half changed
    private void LeaveSettings(Action close) {
        if (!SettingsHaveChangesToApply()) {
            close();
            return;
        }

        ui.messagePopup.Show("You have changes that haven't been applied.", "Apply", "Close", onYes: () => {
            ApplySettings();
            close();
        }, onNo: () => {
            DiscardUnappliedSettings();
            close();
        });
    }

    private void DiscardUnappliedSettings() {
        foreach (SingleSetting setting in settings.all) {
            if (setting.HasChangesToApply()) {
                setting.ForceChangeWithoutApplying(setting.appliedIndex);
            }
        }
        UpdateApplySettingsButtonState();
    }
    
    // The screen mode and resolution are always set together in one call. Setting Screen.fullScreenMode and then calling
    // Screen.SetResolution in the same frame can drop the resolution, e.g. switching to windowed and a new size at once did nothing.
    private void ApplyScreenModeAndResolution() {
        Vector2Int screenSize = allScreenResolutions[settings.resolution.curIndex];
        FullScreenMode screenMode = GetScreenModeFromIndex(settings.fullscreenMode.curIndex);
        Screen.SetResolution(screenSize.x, screenSize.y, screenMode);
    }

    private void MoveWindowToDisplay(DisplayInfo display) {
        if (display.Equals(Screen.mainWindowDisplayInfo)) return;

        // Centered, which only matters when windowed
        Vector2Int position = new((display.width - Screen.width) / 2, (display.height - Screen.height) / 2);
        Screen.MoveMainWindowTo(display, position);
    }

    private FullScreenMode GetScreenModeFromIndex(int index) {
        return index == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }
    
    private int IndexFromScreenMode(FullScreenMode mode) {
        return mode == FullScreenMode.FullScreenWindow ? 0 : 1;
    }
    
    private int FindSettingResolutionIndex(Vector2Int resolution) {
        for (int i = 0; i < allScreenResolutions.Count; i++) {
            if (resolution == allScreenResolutions[i]) {
                return i;
            }
        }
        return -1;
    }

    private void ShowSettingsPage(RectTransform page) {
        settings.curPage = page;
        settings.audioParent.gameObject.SetActive(page == settings.audioParent);
        settings.displayParent.gameObject.SetActive(page == settings.displayParent);
        // settings.gameParent.gameObject.SetActive(page == settings.gameParent);
        settings.controlsParent.gameObject.SetActive(page == settings.controlsParent);
        
        bool openedControlsPage = page == settings.controlsContent; 
        if (openedControlsPage) {
            settings.controlsScrollRect.velocity = Vector2.zero;
            settings.controlsScrollRect.verticalNormalizedPosition = 1f;

            if (!UsingControllerAsPointer) return;
            
            RectTransform first = null;
            foreach (BindingSlot slot in controlBindings.bindingSlots) {
                if (IsValidNavTarget(slot.cellUI.rectTransform)) {
                    first = slot.cellUI.rectTransform;
                }
            }
            if (first == null) {
                first = inputs.activelyUsingSteamInput ? settings.steamInputBindingsButton.rectTransform : null;
            }
                
            if (first) {
                SetControllerSelection(first);
            }
            else {
                ClearControllerSelection();
            }
        }
    }

    private void OnResetPagePressed() {
        RectTransform page = settings.curPage;
        if (!page) return;

        ui.messagePopup.Show("Reset this page to its defaults?", "Reset", "Cancel", onYes: () => {
            if (page == settings.controlsParent) {
                ResetControlsToDefaults();
                return;
            }
            foreach (SingleSetting setting in page.GetComponentsInChildren<SingleSetting>()) {
                setting.ResetToDefault();
            }
        });
    }
    
    // ----------------------------------------------
    // Input Rebinding
    // ----------------------------------------------

    public enum BindingDevice { KeyboardMouse, Controller }

    // One cell on the controls page, an action's keyboard and mouse binding or its controller binding
    public class BindingSlot {
        public GameAction action;
        public int bindingIndex = -1;
        public BindingDevice device;
        public InputBindingCellUI cellUI;
    }

    public class ControlBindingsState {
        public const string emptyBindingText = "Unbound";
        public readonly List<BindingSlot> bindingSlots = new();
        public readonly Dictionary<BindingSlot, List<GameAction>> conflicts = new();
        public InputActionRebindingExtensions.RebindingOperation activeRebindOperation;
        public BindingSlot rebindingSlot;
        public bool IsRebindingControls => activeRebindOperation != null;
    }

    private readonly ControlBindingsState controlBindings = new();

    private void InitSettingsControlsPage() {
        settings.steamInputBindingsButton.AddListener(OpenSteamBindingPanel);

        InputActionMap gameplay = InputSystem.actions.FindActionMap(Inputs.GameplayLayer, throwIfNotFound: true);
        foreach (InputAction unityAction in gameplay.actions) {
            GameAction action = FindGameAction(unityAction);
            if (action == null) continue;
            if (IsRebindable(action)) {
                settings.rebindableActions.Add(action);
            }
            else {
                settings.conflictOnlyActions.Add(action);
            }
        }
        
        // Global, but rebound like a gameplay action so keyboard action can close inventory,
        // while controller overrides it with inventory specifc action
        settings.rebindableActions.Insert(Mathf.Min(3, settings.rebindableActions.Count), inputs.inventory);

        foreach (GameAction action in settings.rebindableActions) {
            InputBindingRow row = Instantiate(settings.controlsRowPrefab, settings.controlsContent);
            row.label.text = action.label;
            settings.controlsRows.Add(row);
            AddSlot(row.keyboardMouseCellUI, action, BindingDevice.KeyboardMouse);
            AddSlot(row.controllerCellUI, action, BindingDevice.Controller);
        }
        RecalculateBindingConflicts();
    }

    private static bool IsRebindable(GameAction action) {
        if (action.isAnalog) return false;
        foreach (InputBinding binding in action.unityInputAction.bindings) {
            if (binding.isComposite) return false;
        }
        return true;
    }

    private static string GetBindingGroup(BindingDevice device) {
        return device == BindingDevice.KeyboardMouse ? "Keyboard&Mouse" : "Gamepad";
    }

    private void AddSlot(InputBindingCellUI cellUI, GameAction action, BindingDevice device) {
        BindingSlot slot = new() {
            action = action,
            bindingIndex = action.unityInputAction.GetBindingIndex(InputBinding.MaskByGroup(GetBindingGroup(device))),
            device = device,
            cellUI = cellUI,
        };
        cellUI.slot = slot;
        cellUI.button.AddListener(() => StartRebind(slot));
        AddHintWithCallback(cellUI.rectTransform, () => GetSlotHint(slot));
        controlBindings.bindingSlots.Add(slot);
    }

    private bool IsSteamSlot(BindingSlot slot) {
        return slot.device == BindingDevice.Controller && inputs.activelyUsingSteamInput;
    }

    private void RefreshCell(BindingSlot slot) {
        InputBindingCellUI cellUI = slot.cellUI;
        bool steam = IsSteamSlot(slot);
        bool hasBinding = slot.bindingIndex >= 0;

        // Still not interactable so the controller skips it
        bool canRebind = hasBinding && !steam;
        cellUI.button.button.interactable = canRebind;
        cellUI.button.SetClickableState(canRebind);
        bool conflicted = !steam && controlBindings.conflicts.ContainsKey(slot);
        if (cellUI.warningIcon.activeSelf != conflicted) {
            cellUI.warningIcon.SetActive(conflicted);
        }

        if (controlBindings.rebindingSlot == slot) {
            cellUI.Show(null, slot.device == BindingDevice.KeyboardMouse ? "Press a key" : "Press a button");
            return;
        }
        if (steam) {
            Sprite steamGlyph = slot.action.GetControllerDisplayGlyphs()[0];
            cellUI.Show(steamGlyph, steamGlyph ? null : ControlBindingsState.emptyBindingText);
            return;
        }

        // The action only exists on the other device
        if (!hasBinding) {
            cellUI.Show(null, slot.device == BindingDevice.KeyboardMouse ? "Controller Only" : "Keyboard Only");
            return;
        }

        InputAction action = slot.action.unityInputAction;
        if (string.IsNullOrEmpty(action.bindings[slot.bindingIndex].effectivePath)) {
            cellUI.Show(null, ControlBindingsState.emptyBindingText);
            return;
        }

        // Falls back to the binding's name when the sheet has no glyph for it
        InputGlyphSheet sheet = slot.device == BindingDevice.KeyboardMouse ? inputPrompts.keyboardMouseGlyphs : inputPrompts.xboxGlyphs;
        string displayString = action.GetBindingDisplayString(slot.bindingIndex, out _, out string controlPath);
        Sprite glyph = sheet ? sheet.Get(controlPath) : null;
        cellUI.Show(glyph, glyph ? null : displayString);
    }

    private void StartRebind(BindingSlot slot) {
        if (controlBindings.activeRebindOperation != null || slot.bindingIndex < 0 || IsSteamSlot(slot)) return;

        InputAction action = slot.action.unityInputAction;
        bool wasEnabled = action.enabled;
        
        // Kept so the binding can go back if the player cancels the conflict warning
        string previousOverride = action.bindings[slot.bindingIndex].overridePath;
        string previousPath = action.bindings[slot.bindingIndex].effectivePath;

        // Rebinding needs the action off
        action.Disable();

        bool clear = false;
        var operation = action.PerformInteractiveRebinding(slot.bindingIndex)
            .WithMatchingEventsBeingSuppressed() // So the press doesn't also click buttons or trigger other actions
            .WithExpectedControlType("Axis") // The actions expect buttons, which would rule out the scroll wheel
            .WithControlsExcluding("<Keyboard>/anyKey")
            .OnPotentialMatch(op => {
                // Buttons only, plus the wheel directions which are axes
                for (int i = op.candidates.Count - 1; i >= 0; i--) {
                    if (!IsRebindableControl(op.candidates[i])) {
                        op.RemoveCandidate(op.candidates[i]);
                    }
                }
                if (op.candidates.Count == 0) return;

                InputControl control = op.candidates[0];
                if (MatchesAnyBinding(inputs.unbind, control)) {
                    clear = true;
                    op.Cancel();
                }
                else if (MatchesAnyBinding(inputs.cancelRebind, control)) {
                    op.Cancel();
                }
                else {
                    op.Complete();
                }
            })
            .OnComplete(_ => {
                EndRebind(action, wasEnabled);
                OnSlotRebound(slot, previousPath, previousOverride);
            })
            .OnCancel(_ => {
                EndRebind(action, wasEnabled);
                if (clear) {
                    action.ApplyBindingOverride(slot.bindingIndex, string.Empty);
                    OnBindingsChanged();
                    SaveBindingOverrides();
                }
            });

        // Cancel and unbind work from either device, whichever slot is being rebound
        AllowBindingPaths(operation, inputs.cancelRebind);
        AllowBindingPaths(operation, inputs.unbind);

        if (slot.device == BindingDevice.KeyboardMouse) {
            operation.WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsHavingToMatchPath("<Mouse>")
                .WithControlsExcluding("<Mouse>/press")
                .WithControlsExcluding("<Pointer>/press")
                .WithControlsExcluding("<Mouse>/scroll/left")
                .WithControlsExcluding("<Mouse>/scroll/right");
        }
        else {
            // Stick directions would be bound instead of the stick press while navigating
            operation.WithControlsHavingToMatchPath("<Gamepad>")
                .WithControlsExcluding("<Gamepad>/leftStick/up")
                .WithControlsExcluding("<Gamepad>/leftStick/down")
                .WithControlsExcluding("<Gamepad>/leftStick/left")
                .WithControlsExcluding("<Gamepad>/leftStick/right")
                .WithControlsExcluding("<Gamepad>/rightStick/up")
                .WithControlsExcluding("<Gamepad>/rightStick/down")
                .WithControlsExcluding("<Gamepad>/rightStick/left")
                .WithControlsExcluding("<Gamepad>/rightStick/right");
        }

        controlBindings.activeRebindOperation = operation;
        controlBindings.rebindingSlot = slot;
        HideHint();
        RefreshCell(slot);
        operation.Start();
    }

    private static void AllowBindingPaths(InputActionRebindingExtensions.RebindingOperation operation, GameAction action) {
        foreach (InputBinding binding in action.unityInputAction.bindings) {
            if (!string.IsNullOrEmpty(binding.effectivePath)) {
                operation.WithControlsHavingToMatchPath(binding.effectivePath);
            }
        }
    }

    private static bool MatchesAnyBinding(GameAction action, InputControl control) {
        foreach (InputBinding binding in action.unityInputAction.bindings) {
            if (!string.IsNullOrEmpty(binding.effectivePath) && InputControlPath.Matches(binding.effectivePath, control)) return true;
        }
        return false;
    }

    private static bool IsRebindableControl(InputControl control) {
        if (control is ButtonControl) return true;
        return control.parent is Vector2Control { name: "scroll" } && control.name is "up" or "down";
    }

    private void EndRebind(InputAction action, bool wasEnabled) {
        controlBindings.activeRebindOperation?.Dispose();
        controlBindings.activeRebindOperation = null;
        controlBindings.rebindingSlot = null;
        if (wasEnabled) {
            action.Enable();
        }
    }

    private void OnSlotRebound(BindingSlot slot, string previousPath, string previousOverride) {
        InputAction action = slot.action.unityInputAction;
        if (action.bindings[slot.bindingIndex].effectivePath == previousPath) return;

        OnBindingsChanged();

        if (!controlBindings.conflicts.TryGetValue(slot, out List<GameAction> conflicts)) {
            SaveBindingOverrides();
            return;
        }

        string message = $"{DescribeConflicts(slot, conflicts)}\nKeep this binding?";
        ui.messagePopup.Show(message, "OK", "Cancel", onYes: SaveBindingOverrides, onNo: () => {
            if (previousOverride == null) {
                action.RemoveBindingOverride(slot.bindingIndex);
            }
            else {
                action.ApplyBindingOverride(slot.bindingIndex, previousOverride);
            }
            OnBindingsChanged();
        });
    }

    private void OnBindingsChanged() {
        UpdateUnityInputGlyphs();
        RecalculateBindingConflicts();
    }

    private void ResetControlsToDefaults() {
        foreach (BindingSlot slot in controlBindings.bindingSlots) {
            if (slot.bindingIndex >= 0) {
                slot.action.unityInputAction.RemoveBindingOverride(slot.bindingIndex);
            }
        }
        OnBindingsChanged();
        SaveBindingOverrides();
    }

    private void OpenSteamBindingPanel() {
        SteamInput.ShowBindingPanel(inputs.activeSteamController);
    }

    private string GetSlotHint(BindingSlot slot) {
        if (controlBindings.rebindingSlot == slot) {
            return null;
        }
        if (IsSteamSlot(slot) || !controlBindings.conflicts.TryGetValue(slot, out List<GameAction> conflicts)) {
            return string.Empty;
        }
        return DescribeConflicts(slot, conflicts);
    }

    private static string DescribeConflicts(BindingSlot slot, List<GameAction> conflicts) {
        string binding = slot.action.unityInputAction.GetBindingDisplayString(slot.bindingIndex);
        System.Text.StringBuilder builder = new();
        foreach (GameAction other in conflicts) {
            if (builder.Length > 0) {
                builder.Append('\n');
            }
            builder.Append($"{other.label} also uses {binding}.");
        }
        return builder.ToString();
    }

    // Only the rebindable and conflict-only actions count as conflicts
    private void RecalculateBindingConflicts() {
        controlBindings.conflicts.Clear();
        foreach (BindingSlot slot in controlBindings.bindingSlots) {
            if (slot.bindingIndex < 0) continue;
            string path = slot.action.unityInputAction.bindings[slot.bindingIndex].effectivePath;
            if (string.IsNullOrEmpty(path)) continue;

            InputBinding groupMask = InputBinding.MaskByGroup(GetBindingGroup(slot.device));
            AddBindingConflicts(slot, path, groupMask, settings.rebindableActions);
            AddBindingConflicts(slot, path, groupMask, settings.conflictOnlyActions);
        }
    }

    private void AddBindingConflicts(BindingSlot slot, string path, InputBinding groupMask, List<GameAction> others) {
        foreach (GameAction other in others) {
            if (other == slot.action) continue;

            foreach (InputBinding binding in other.unityInputAction.bindings) {
                // Bindings with no group count for every device
                if (binding.isComposite || (!string.IsNullOrEmpty(binding.groups) && !groupMask.Matches(binding))) continue;
                if (!BindingPathsOverlap(path, binding.effectivePath)) continue;

                if (!controlBindings.conflicts.TryGetValue(slot, out List<GameAction> list)) {
                    list = new();
                    controlBindings.conflicts.Add(slot, list);
                }
                list.Add(other);
                break;
            }
        }
    }

    // Overlapping counts too, like the dpad and dpad up
    private static bool BindingPathsOverlap(string a, string b) {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return a == b || a.StartsWith(b + "/", StringComparison.Ordinal) || b.StartsWith(a + "/", StringComparison.Ordinal);
    }
    
}
