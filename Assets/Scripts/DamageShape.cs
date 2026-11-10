using System.Collections.Generic;
using UnityEngine;

public class DamageShape : MonoBehaviour {

    public enum ShapeType { Circle, Box, Capsule }
    public enum AttackDir { None, Side, Up, Down }
    
    [Header("Shape")]
    public ShapeType shape = ShapeType.Circle;
    public Vector2 offset;
    public float radius = 0.1f;
    public Vector2 size = new(0.2f, 0.2f);
    public CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Vertical;

    [Header("Damage")]
    public int damageFrame;
    public AttackDir attackDir;
    [Tooltip("Also used flipped across the parent for left facing attacks")]
    public bool mirrorForLeft;

    public Vector2 Center => transform.TransformPoint(offset);

    public Vector2 GetCenter(bool mirrored) {
        if (!mirrored || !transform.parent) return Center;
        Vector3 local = transform.parent.InverseTransformPoint(Center);
        local.x = -local.x;
        return transform.parent.TransformPoint(local);
    }
    
    public List<Collider2D> Overlap(LayerMask mask, bool mirrored = false) {
        Vector2 scale = LossyScale2D();
        Vector2 center = GetCenter(mirrored);
        float angle = mirrored ? -transform.eulerAngles.z : transform.eulerAngles.z;
        return shape switch {
            ShapeType.Box     => Physics.OverlapBox(center, Vector2.Scale(size, scale), angle, mask),
            ShapeType.Capsule => Physics.OverlapCapsule(center, Vector2.Scale(size, scale), capsuleDirection, angle, mask),
            _                 => Physics.OverlapCircle(center, radius * Mathf.Max(scale.x, scale.y), mask),
        };
    }

    public Collider2D OverlapFirst(LayerMask mask, bool mirrored = false) {
        Vector2 scale = LossyScale2D();
        Vector2 center = GetCenter(mirrored);
        float angle = mirrored ? -transform.eulerAngles.z : transform.eulerAngles.z;
        return shape switch {
            ShapeType.Box     => Physics2D.OverlapBox(center, Vector2.Scale(size, scale), angle, mask),
            ShapeType.Capsule => Physics2D.OverlapCapsule(center, Vector2.Scale(size, scale), capsuleDirection, angle, mask),
            _                 => Physics2D.OverlapCircle(center, radius * Mathf.Max(scale.x, scale.y), mask),
        };
    }

    private Vector2 LossyScale2D() {
        Vector3 scale = transform.lossyScale;
        return new(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    private void OnDrawGizmosSelected() {
        DrawGizmo(transform.localToWorldMatrix, Color.red);
        if (mirrorForLeft && transform.parent) {
            Transform parent = transform.parent;
            Matrix4x4 mirror = parent.localToWorldMatrix * Matrix4x4.Scale(new(-1f, 1f, 1f)) * parent.worldToLocalMatrix;
            DrawGizmo(mirror * transform.localToWorldMatrix, new Color(1f, 0.6f, 0.1f));
        }
    }

    private void DrawGizmo(Matrix4x4 matrix, Color color) {
        Vector2 center = offset;
        Gizmos.matrix = matrix;
        Gizmos.color = color;
        switch (shape) {
            case ShapeType.Circle:
                Gizmos.DrawWireSphere(center, radius);
                break;
            case ShapeType.Box:
                Gizmos.DrawWireCube(center, size);
                break;
            case ShapeType.Capsule:
                bool vertical = capsuleDirection == CapsuleDirection2D.Vertical;
                float capRadius = (vertical ? size.x : size.y) * 0.5f;
                float straightLength = Mathf.Max(0f, (vertical ? size.y : size.x) - capRadius * 2f);
                Vector2 axis = vertical ? Vector2.up : Vector2.right;
                Gizmos.DrawWireSphere(center + axis * straightLength * 0.5f, capRadius);
                Gizmos.DrawWireSphere(center - axis * straightLength * 0.5f, capRadius);
                Gizmos.DrawWireCube(center, vertical ? new Vector2(capRadius * 2f, straightLength) : new Vector2(straightLength, capRadius * 2f));
                break;
        }
    }

}
