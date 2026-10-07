using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using static GameData;

public partial class Game {

    private const int exitPortalCutsceneWave = 4;

    // Shows players who have never extracted where the exit portals are, partway through their first map
    private void CheckForExitPortalCutscene() {
        if (curRaid.data.exitPortalCutscenePlayed) return;
        if (persistentFlags.HasFlag(PersistentFlags.HasExtracted)) return;
        if (curRaid.map != config.maps[0]) return;
        // Once the final wave starts the express exit portal shows up on its own
        if (curRaid.state != RaidState.InitialWaves) return;

        bool clearedWave = spawnManager.CurWaveNumber == exitPortalCutsceneWave && spawnManager.FinishedSpawningThisWave && entities.enemies.Count <= 0;
        bool pastWave = spawnManager.CurWaveNumber > exitPortalCutsceneWave; // The next wave can start before the player clears this one
        if (!clearedWave && !pastWave) return;
        
        curRaid.data.exitPortalCutscenePlayed = true;
        Tween.Delay(1.5f, static () => {
            // The raid can end or the player can find a portal on their own during the delay
            if (!gameInstance.InRaid || player.health <= 0 || gameInstance.pauseMenu.paused || gameInstance.cutscene.playing) return;
            if (gameInstance.AnyExitPortalSummoned()) return; // They already know how to extract, and the portal would keep closing during the cutscene

            Portal closestPortal = ClosestInactiveExitPortal();
            if (closestPortal == null) return; // Every portal was already used up
            
            gameInstance.spawnManager.startNextWaveDelay += 8;
            gameInstance.PlayExitPortalCutscene(closestPortal);
        });
    }

    private bool AnyExitPortalSummoned() {
        foreach (Portal portal in curRaid.activeExitPortals) {
            if (portal.state is Portal.State.BeingSummoned or Portal.State.Open) return true;
        }
        return false;
    }

    private static Portal ClosestInactiveExitPortal() {
        Portal closest = null;
        float closestSqrDist = float.MaxValue;
        foreach (Portal portal in gameInstance.curRaid.activeExitPortals) {
            if (portal.state != Portal.State.Inactive) continue; // Closed portals can't be summoned again
            float sqrDist = (portal.transform.position - player.position).sqrMagnitude;
            if (sqrDist >= closestSqrDist) continue;
            closestSqrDist = sqrDist;
            closest = portal;
        }
        return closest;
    }

    private void PlayExitPortalCutscene(Portal portal) {
        const float panTime = 1.6f;
        const float settleTime = 0.25f; // Lets the camera's damping catch up before the trader talks

        StartCutscene();

        cutscene.cameraTarget.position = player.position;
        camera.cinemachine.Follow = cutscene.cameraTarget;

        tutorial.dialogueTypewriter = ui.traderTutorialTypewriter;
        ui.traderTutorialDialogueBox.SetActive(true);
        FadeIn(ui.traderTutorialDialogueCanvasGroup, 0.4f);

        StartDialogue(
            onFinished: () => {
                ui.traderTutorialDialogueBox.SetActive(false);
                Vector3 portalPos = portal.transform.position;
                portalPos.z = player.position.z;

                Sequence.Create()
                    .Chain(Tween.Position(cutscene.cameraTarget, portalPos, panTime, Ease.InOutCubic))
                    .ChainDelay(settleTime)
                    .ChainCallback(() => {
                        ui.traderTutorialDialogueBox.SetActive(true);
                        StartDialogue(
                            onFinished: () => {
                                ui.traderTutorialDialogueBox.SetActive(false);
                                Sequence.Create()
                                    .Chain(Tween.Position(cutscene.cameraTarget, player.position, panTime, Ease.InOutCubic))
                                    .ChainDelay(settleTime)
                                    .ChainCallback(EndCutscene);
                            },
                            Line("Summon this extraction portal and use it to get back to the hideout.")
                        );
                    });
            },
            Line("It's time to skedaddle with the loot. You're not powerful enough yet to survive the remaining waves.")
        );
    }

    // Freezes the raid in place so the camera can be moved around
    private void StartCutscene() {
        cutscene.playing = true;
        states.gameStateMachine.Pause();

        CancelItemDrag();
        ClosePlayerInventory();
        CloseLootInventory();
        HideInventoryItemPopup();
        HideInteractionPopup();
        HideHint();

        player.velocity = Vector3.zero;
        player.animator.Play(player.nextIdleAnimHash);

        // Physics still runs while the raid is paused, so stop anything that's mid-movement from sliding
        foreach (Enemy enemy in entities.enemies) {
            if (enemy.rigidbody) {
                enemy.rigidbody.linearVelocity = Vector2.zero;
            }
        }

        if (cutscene.cameraTarget == null) {
            cutscene.cameraTarget = new GameObject("CutsceneCameraTarget").transform;
        }

        // Lookahead aims ahead of a moving target, which makes the camera overshoot wherever a pan stops
        if (camera.cinemachine.TryGetComponent(out CinemachinePositionComposer composer)) {
            cutscene.restoreLookahead = composer.Lookahead.Enabled;
            composer.Lookahead.Enabled = false;
        }
    }

    private void EndCutscene() {
        if (cutscene.restoreLookahead && camera.cinemachine.TryGetComponent(out CinemachinePositionComposer composer)) {
            composer.Lookahead.Enabled = true;
        }
        camera.cinemachine.Follow = player.trans;
        cutscene.playing = false;
        states.gameStateMachine.UnPause();
    }

}
