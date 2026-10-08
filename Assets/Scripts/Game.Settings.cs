using System.Collections.Generic;
using UnityEngine;

public partial class Game {
    
    public class SettingsState {
        public Vector2Int resolution;
        public int fullScreenIndex;
        public int fpsLimitIndex;
        public int targetMonitor;
        public int vsyncEnabled;
        
        public int masterVolumeIndex;
        public int musicVolumeIndex;
        public int gameVolumeIndex;
    }
    
    private string[] fullScreenNames = { "Exclusive Full Screen", "Full Screen Window", "Maximized", "Windowed" };
    private int[] fpsLimits = { 60, 90, 120, 144, 165, -1 };
    
    private List<Vector2Int> allScreenResolutions = new();
    
    public static void OnAnySettingsChanged() {
        gameInstance.UpdateApplySettingsButtonState();
    }
    
    private void InitSettings(SettingsState loadedSettings) {
        settings.all = settings.settingsParent.GetComponentsInChildren<SingleSetting>();
        
        foreach (Resolution resolution in Screen.resolutions) {
            Vector2Int resDim = new(resolution.width, resolution.height);
            if (!allScreenResolutions.Contains(resDim)) {
                allScreenResolutions.Add(resDim);
            }
        }
        
        loadedSettings ??= GetSensibleDefaultSettings();
        settings.curSettingsState = loadedSettings;

        // Video Settings
        {
            const int numberOfFullscreenModes = 2;
            settings.fullscreenMode.Init(loadedSettings.fullScreenIndex, numberOfFullscreenModes)
            .OnChange(i => {
                int nameIndex = (int)GetScreenModeFromIndex(i);
                settings.fullscreenMode.Display(fullScreenNames[nameIndex]);
            })
            .OnApply(i => {
                settings.curSettingsState.fullScreenIndex = i;
                ApplyScreenModeAndResolution();
            });
            
            int startingResolutionIndex = FindSettingResolutionIndex(loadedSettings.resolution);
            if (startingResolutionIndex == -1) {
                startingResolutionIndex = allScreenResolutions.Count - 1;
            }
            
            settings.resolution.Init(startingResolutionIndex, allScreenResolutions.Count)
            .OnChange(i => {
                Vector2Int screenSize = allScreenResolutions[i];
                settings.resolution.Display($"{screenSize.x} x {screenSize.y}");
            })
            .OnApply(i => {
                settings.curSettingsState.resolution = allScreenResolutions[i];
                ApplyScreenModeAndResolution();
            });
            
            settings.fpsLimit.Init(loadedSettings.fpsLimitIndex, fpsLimits.Length)
            .OnChange((i) => {
                int chosenFpsLimit = fpsLimits[i];
                string frameRateString = chosenFpsLimit == -1 ? "Unlimited" : chosenFpsLimit.ToString();
                settings.fpsLimit.Display(frameRateString);
                Application.targetFrameRate = chosenFpsLimit;
                settings.curSettingsState.fpsLimitIndex = i;
            });
            
            settings.targetMonitor.Init(loadedSettings.targetMonitor, Display.displays.Length)
            .OnChange((i) => {
                settings.targetMonitor.Display($"Display {i + 1}");
                settings.curSettingsState.targetMonitor = i;
            });
            
            const int numVsyncSettings = 2;
            settings.vsync.Init(loadedSettings.vsyncEnabled, numVsyncSettings)
            .OnChange((i) => {
                settings.vsync.Display(i == 0 ? "Off" : "On");
                settings.curSettingsState.vsyncEnabled = i;
            });
        }

        // Audio Settings
        {
            const int numVolumeOptions = 11; // 0 - 10
            const float maxVolumeIndex = numVolumeOptions - 1f;
        
            settings.masterVolume.Init(loadedSettings.masterVolumeIndex, numVolumeOptions)
            .OnChange(i => {
                float linearVolume = i / maxVolumeIndex;
                AudioListener.volume = linearVolume;
                settings.masterVolume.Display(linearVolume.ToString("0%"));
                settings.curSettingsState.masterVolumeIndex = i;
            });
        
            settings.musicVolume.Init(loadedSettings.musicVolumeIndex, numVolumeOptions)
            .OnChange(i => {
                float linearVolume = i / maxVolumeIndex;
                settings.musicAudioMixer.SetFloat("Volume", linearVolume.LinearToDecibel());
                settings.musicVolume.Display(linearVolume.ToString("0%"));
                settings.curSettingsState.musicVolumeIndex = i;
            });
        
            settings.gameVolume.Init(loadedSettings.gameVolumeIndex, numVolumeOptions)
            .OnChange(i => {
                float linearVolume = i / maxVolumeIndex;
                settings.gameAudioMixer.SetFloat("Volume", linearVolume.LinearToDecibel());
                settings.gameVolume.Display(linearVolume.ToString("0%"));
                settings.curSettingsState.gameVolumeIndex = i;
            });
        }
        
        ApplySettings();
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
    
    private SettingsState GetSensibleDefaultSettings() {
        return new() {
            resolution = allScreenResolutions[^1],
            fullScreenIndex = IndexFromScreenMode(FullScreenMode.FullScreenWindow),
            fpsLimitIndex = fpsLimits.Length - 1,
            targetMonitor = 0,
            vsyncEnabled = 0,
            masterVolumeIndex = 6,
            musicVolumeIndex = 5,
            gameVolumeIndex = 10,
        };
    }
    
    private void ApplySettings() {
        foreach (SingleSetting setting in settings.all) {
            setting.Apply();
        }
        UpdateApplySettingsButtonState();
    }
    
    private void UpdateApplySettingsButtonState() {
        int changesToApplyCount = 0;
        foreach (SingleSetting setting in settings.all) {
            if (setting.HasChangesToApply()) {
                changesToApplyCount++;
            }
        }
        settings.applyChangesButton.SetClickableState(changesToApplyCount > 0);
    }
    
    // The screen mode and resolution are always set together in one call. Setting Screen.fullScreenMode and then calling
    // Screen.SetResolution in the same frame can drop the resolution, e.g. switching to windowed and a new size at once did nothing.
    private void ApplyScreenModeAndResolution() {
        Vector2Int screenSize = allScreenResolutions[settings.resolution.curIndex];
        FullScreenMode screenMode = GetScreenModeFromIndex(settings.fullscreenMode.curIndex);
        Screen.SetResolution(screenSize.x, screenSize.y, screenMode);
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
    
}
