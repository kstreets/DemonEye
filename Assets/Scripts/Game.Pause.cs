using UnityEngine;
using UnityEngine.InputSystem;

public partial class Game {

    private void InitPauseMenu() {
        input.pause.performed += OnPausePressed;
        pauseMenu.resumeButton.AddListener(ResumeRaid);
        pauseMenu.settingsButton.AddListener(ShowPauseSettings);
        pauseMenu.suicideButton.AddListener(() => {
            ui.messagePopup.Show("Are you sure you want to suicide? All items will be lost.", yesText: "Yes", noText: "No", onYes: () => {
                player.health = 0;
                ResumeRaid();
            });
        });
        pauseMenu.panel.gameObject.SetActive(false);
    }

    private void OnPausePressed(InputAction.CallbackContext context) {
        if (!InRaid) return;

        if (pauseMenu.paused) {
            ResumeRaid();
        }
        else {
            PauseRaid();
        }
    }

    // Stops the raid from ticking and freezes time so physics, tweens, animations and Time.time based timers all hold still
    private void PauseRaid() {
        if (pauseMenu.paused) return;
        pauseMenu.paused = true;

        states.gameStateMachine.Pause();
        Time.timeScale = 0f;

        CancelItemDrag();
        ClosePlayerInventory();
        CloseLootInventory();
        HideInventoryItemPopup();
        HideInteractionPopup();
        HideHint();

        foreach (AudioSource source in audio.generationLookup.Keys) {
            if (!source.isPlaying) continue;
            source.Pause();
            pauseMenu.pausedAudioSources.Add(source);
        }
        if (music.source.isPlaying) {
            music.source.Pause();
            pauseMenu.pausedAudioSources.Add(music.source);
        }

        pauseMenu.panel.gameObject.SetActive(true);
        IgnoreHeldNavigationInput();
        ClearControllerSelection();
        Cursor.visible = !usingController;
    }

    private void ResumeRaid() {
        if (!pauseMenu.paused) return;

        if (pauseMenu.showingSettings) {
            // Goes through the pause settings close so the raid HUD it hid is shown again
            ClosePauseSettings();
        }
        pauseMenu.panel.gameObject.SetActive(false);

        foreach (AudioSource source in pauseMenu.pausedAudioSources) {
            source.UnPause();
        }
        pauseMenu.pausedAudioSources.Clear();

        pauseMenu.paused = false;
        Time.timeScale = 1f;
        states.gameStateMachine.UnPause();

        ClearControllerSelection();
        Cursor.visible = false;
    }

    // Settings is normally its own game state, but switching states would end the raid, so it's shown over the raid instead
    private void ShowPauseSettings() {
        pauseMenu.showingSettings = true;
        pauseMenu.panel.gameObject.SetActive(false);
        ShowSettingsMenuUI();
        IgnoreHeldNavigationInput();
        ClearControllerSelection();
        playerInfo.parent.gameObject.SetActive(false);
        ui.minimap.gameObject.SetActive(false);
        ui.hotBarParent.gameObject.SetActive(false);
        raidInfo.waveText.gameObject.SetActive(false);
    }

    private void ClosePauseSettings() {
        pauseMenu.showingSettings = false;
        CloseSettingsMenuUI();
        pauseMenu.panel.gameObject.SetActive(true);
        IgnoreHeldNavigationInput();
        ClearControllerSelection();
        playerInfo.parent.gameObject.SetActive(true);
        ui.minimap.gameObject.SetActive(true);
        ui.hotBarParent.gameObject.SetActive(true);
        raidInfo.waveText.gameObject.SetActive(true);
    }

}
