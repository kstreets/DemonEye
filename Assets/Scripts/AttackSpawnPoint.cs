using System;
using PrimeTween;
using UnityEngine;

public class AttackSpawnPoint : MonoBehaviour {

    public string animationState;
    public int spawnFrame;
    public bool mirrorForLeft;

    // Set by OnAttackSpawnFrame so the delay callback can be static
    [NonSerialized] public Action<Game.Enemy, Vector2> pendingCallback;
    [NonSerialized] public Tween pendingTween;

    public int StateHash { get; private set; }

    private void Awake() {
        StateHash = Animator.StringToHash(animationState);
    }

    public Vector2 GetPosition(bool mirrored) {
        if (!mirrored || !transform.parent) {
            return transform.position;
        }
        Vector3 local = transform.parent.InverseTransformPoint(transform.position);
        local.x = -local.x;
        return transform.parent.TransformPoint(local);
    }

    private void OnDrawGizmos() {
        DrawGizmo(transform.position, new Color(0.2f, 0.9f, 1f));
        if (mirrorForLeft && transform.parent) {
            DrawGizmo(GetPosition(true), new Color(1f, 0.6f, 0.1f));
        }
    }

    private static void DrawGizmo(Vector2 pos, Color color) {
        const float size = 0.04f;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(pos, size * 0.5f);
        Gizmos.DrawLine(pos + Vector2.left * size, pos + Vector2.right * size);
        Gizmos.DrawLine(pos + Vector2.down * size, pos + Vector2.up * size);
    }

}
