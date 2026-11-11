using PrimeTween;
using UnityEngine;
using static GameData;

public partial class Game {

    public class CameraLookahead {
        public const float maxDistance = 0.17f; // World units, the view is only ~3 units tall
        public const float mouseRange = 0.2f; // Cursor distance from the player that gives the full offset
        public const float smoothingTime = 1.3f;
        public const float minSpeed = 0.12f; // World units per second
        public Vector2 velocity;
        public Tween fadeOutTween;
    }

    private void UpdateAimLookAhead() {
        bool overridingSequenceOccuring = raidEnterSequence.isAlive || camera.lookAhead.fadeOutTween.isAlive || cutscene.playing;
        if (overridingSequenceOccuring && !MenuNavigationNeeded) return;
        
        bool inventoryOpen = PlayerInventoryIsOpen || LootInventoryIsOpen;
        Vector2 target = Vector2.zero;
        if (!inventoryOpen && TryGetAimDirection(out Vector2 aim)) {
            float strength = UsingController ? Mathf.InverseLerp(ControllerNavigation.stickDeadzone, 1f, aim.magnitude) : Mathf.Clamp01(aim.magnitude / CameraLookahead.mouseRange);
            target = aim.normalized * (strength * CameraLookahead.maxDistance);
        }
        
        Vector2 current = camera.aimOffset.offset;
        Vector2 next = Vector2.SmoothDamp(current, target, ref camera.lookAhead.velocity, CameraLookahead.smoothingTime);

        // SmoothDamp's tail crawls across single pixels, which jitters on the pixel perfect camera
        Vector2 minStep = Vector2.MoveTowards(current, target, CameraLookahead.minSpeed * Time.deltaTime);
        if (Vector2.Distance(minStep, target) < Vector2.Distance(next, target)) {
            next = minStep;
        }
        if (next == target) {
            camera.lookAhead.velocity = Vector2.zero;
        }
        camera.aimOffset.offset = next;
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
