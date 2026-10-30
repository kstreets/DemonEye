using PrimeTween;
using UnityEngine;
using static GameData;

public partial class Game {

    public class CameraLookahead {
        public const float maxDistance = 0.17f; // World units, the view is only ~3 units tall
        public const float mouseRange = 0.2f; // Cursor distance from the player that gives the full offset
        public const float smoothingTime = 1.3f;
        public Vector2 velocity;
        public Tween fadeOutTween;
    }

    private void UpdateAimLookAhead() {
        bool overridingSequenceOccuring = raidEnterSequence.isAlive || camera.lookAhead.fadeOutTween.isAlive || cutscene.playing;
        if (overridingSequenceOccuring && !MenuNavigationNeeded) return;
        
        Vector2 target = Vector2.zero;
        if (TryGetAimDirection(out Vector2 aim)) {
            float strength = usingController ? Mathf.InverseLerp(ControllerNavigation.stickDeadzone, 1f, aim.magnitude) : Mathf.Clamp01(aim.magnitude / CameraLookahead.mouseRange);
            target = aim.normalized * (strength * CameraLookahead.maxDistance);
        }
        camera.aimOffset.offset = Vector2.SmoothDamp(camera.aimOffset.offset, target, ref camera.lookAhead.velocity, CameraLookahead.smoothingTime);
    }
    
    private void FadeoutAimLookAhead() {
        const float fadeOutTime = 0.25f;
        camera.lookAhead.velocity = Vector2.zero;
        camera.lookAhead.fadeOutTween = Tween.Custom(camera.aimOffset.offset, Vector2.zero, fadeOutTime, val => camera.aimOffset.offset = val, Ease.OutQuad);
    }

    private void ResetAimLookAhead() {
        camera.aimOffset.offset = Vector2.zero;
        camera.lookAhead.velocity = Vector2.zero;
    }

}
