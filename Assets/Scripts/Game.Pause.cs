using UnityEngine;

public partial class Game {

    private void InitPauseMenu() {
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

    private void OnPausePressed() {
        if (!InRaid) return;
        if (cutscene.playing) return; // Unpausing would resume the raid in the middle of the cutscene

        if (pauseMenu.paused) {
            // Start skips back to the raid, so it asks about unapplied settings too
            if (pauseMenu.showingSettings) {
                LeaveSettings(ResumeRaid);
            }
            else {
                ResumeRaid();
            }
        }
        else {
            PauseRaid();
        }
    }

    private void PauseRaid() {
        if (pauseMenu.paused) return;
        
        pauseMenu.paused = true;
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
        Cursor.visible = ShowCursorInMenus;
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
        tutorial.stateMachine?.UnPause();

        ClearControllerSelection();
        Cursor.visible = false;
    }

    private void ShowPauseSettings() {
        pauseMenu.showingSettings = true;
        pauseMenu.panel.gameObject.SetActive(false);
        SetRaidHUDVisible(false);
        ShowSettingsMenuUI();
        IgnoreHeldNavigationInput();
        ClearControllerSelection();
    }

    private void ClosePauseSettings() {
        pauseMenu.showingSettings = false;
        CloseSettingsMenuUI();
        SetRaidHUDVisible(true);
        pauseMenu.panel.gameObject.SetActive(true);
        IgnoreHeldNavigationInput();
        ClearControllerSelection();
    }

    // The settings menu shows the raid behind it, so the HUD would poke through
    private void SetRaidHUDVisible(bool visible) {
        playerInfo.parent.gameObject.SetActive(visible);
        ui.hotBarParent.gameObject.SetActive(visible);
        ui.minimap.gameObject.SetActive(visible);
    }

}
