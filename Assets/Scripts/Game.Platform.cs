using System;
using Steamworks;
using UnityEngine;

public partial class Game {

    public enum Platform {
        PC,        
        Steam,
        SteamDeck,
    }
    private Platform platform;

    private const uint steamAppId = 4101300;
    private bool steamInitialized;
    
    public bool OnSteam => steamInitialized;
    public bool OnSteamDeck => platform == Platform.SteamDeck;

    public bool ShowCursorInMenus() {
        return !UsingController;
    }

    private void InitPlatform() {
        try {
            Environment.SetEnvironmentVariable("SteamAppId", steamAppId.ToString());
            Environment.SetEnvironmentVariable("SteamGameId", steamAppId.ToString());

            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            steamInitialized = result == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
            if (!steamInitialized) {
                Debug.Log($"Steam failed to initialize, running without it: {result} {error}");
            }
        }
        catch (Exception e) {
            Debug.Log($"Steam failed to initialize, running without it: {e.Message}");
        }

        if (OnSteam) {
            platform = SteamUtils.IsRunningOnSteamHardware() == ESteamHardwareType.k_ESteamHardwareTypeSteamDeck ? Platform.SteamDeck : Platform.Steam;
        }
        else {
            bool SteamDeck = Environment.GetEnvironmentVariable("SteamDeck") == "1";
            platform = SteamDeck ? Platform.SteamDeck : Platform.PC;
        }
    }

    private void ShutdownPlatform() {
        if (steamInitialized) {
            SteamAPI.Shutdown();
            steamInitialized = false;
        }
    }

    private void UpdatePlatform() {
        if (steamInitialized) {
            SteamAPI.RunCallbacks();
        }
    }

}
