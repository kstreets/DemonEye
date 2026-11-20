#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

public static partial class Game {

    static bool showWaveDebugOverlay;
    static GUIStyle waveDebugStyle;

    static void UpdateDebugOverlayToggle() {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) {
            showWaveDebugOverlay = !showWaveDebugOverlay;
        }
    }

    public static void OnGUI() {
        if (!showWaveDebugOverlay || states.gameStateMachine.CurState != states.raid || curRaid.mapInstance.isBossMap) return;

        waveDebugStyle ??= new GUIStyle(GUI.skin.box) {
            fontSize = 18,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(10, 10, 8, 8),
        };

        EnemySpawnManager sm = spawnManager;
        string text;
        if (sm.curPhaseIndex < 0) {
            text = $"Before first wave\n" +
                   $"Time: {sm.timeInCurPhase:0.0}s / {sm.spawnPattern.timeBeforeFirstPhase:0.0}s";
        }
        else if (sm.OnBossWave) {
            text = $"Wave {sm.CurWaveNumber}/{sm.TotalWaveCount} (Boss)\n" +
                   $"Time: {sm.timeInCurPhase:0.0}s";
        }
        else {
            bool counting = sm.timeInCurPhase >= sm.CurMaxDuration || (sm.FinishedSpawningThisWave && entities.enemies.Count <= 0);
            text = $"Wave {sm.CurWaveNumber}/{sm.TotalWaveCount}\n" +
                   $"Time: {sm.timeInCurPhase:0.0}s\n" +
                   $"Max duration: {sm.CurMaxDuration:0.0}s\n" +
                   $"Accrued delay: {sm.timeAccruedDuringWave:0.0}s\n" +
                   $"Action time: {sm.timeAddedThroughActions:0.0}s {(sm.FinishedSpawningThisWave ? "" : "(spawning, ignored)")}\n" +
                   $"Next wave in: {Mathf.Max(0f, sm.startNextWaveDelay):0.0}s {(counting ? "(counting)" : "(waiting)")}";
        }
        // text += $"\nStruggled last wave: {(sm.struggledLastWave ? "Yes" : "No")}";

        GUIContent content = new(text);
        Vector2 size = waveDebugStyle.CalcSize(content);
        GUI.Box(new Rect(10f, Screen.height / 2f, size.x, size.y), content, waveDebugStyle);
    }
}
#endif
