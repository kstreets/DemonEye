using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageShape : MonoBehaviour {

    public enum ShapeType { Circle, Box, Capsule }
    
    [Flags]
    public enum DamageTargets {
        None       = 0,
        Player     = 1 << 0,
        Enemies    = 1 << 1,
        Damageable = 1 << 2, // Enemies and mineables
    }

    [Header("Shape")]
    public ShapeType shape = ShapeType.Circle;
    public Vector2 offset;
    public float radius = 0.1f;
    public Vector2 size = new(0.2f, 0.2f);
    public CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Vertical;

    [Header("Damage")]
    public DamageTargets targets = DamageTargets.Enemies;
    public int damageFrame;

    public Vector2 Center => transform.TransformPoint(offset);
    
    public LayerMask TargetMask {
        get {
            int mask = 0;
            if (targets.HasFlag(DamageTargets.Player))     mask |= Masks.PlayerHurtMask;
            if (targets.HasFlag(DamageTargets.Enemies))    mask |= Masks.EnemyMask;
            if (targets.HasFlag(DamageTargets.Damageable)) mask |= Masks.DamagableMask;
            return mask;
        }
    }

    public List<Collider2D> Overlap(LayerMask mask) {
        Vector2 scale = LossyScale2D();
        float angle = transform.eulerAngles.z;
        return shape switch {
            ShapeType.Box     => Physics.OverlapBox(Center, Vector2.Scale(size, scale), angle, mask),
            ShapeType.Capsule => Physics.OverlapCapsule(Center, Vector2.Scale(size, scale), capsuleDirection, angle, mask),
            _                 => Physics.OverlapCircle(Center, radius * Mathf.Max(scale.x, scale.y), mask),
        };
    }

    public Collider2D OverlapFirst(LayerMask mask) {
        Vector2 scale = LossyScale2D();
        float angle = transform.eulerAngles.z;
        return shape switch {
            ShapeType.Box     => Physics2D.OverlapBox(Center, Vector2.Scale(size, scale), angle, mask),
            ShapeType.Capsule => Physics2D.OverlapCapsule(Center, Vector2.Scale(size, scale), capsuleDirection, angle, mask),
            _                 => Physics2D.OverlapCircle(Center, radius * Mathf.Max(scale.x, scale.y), mask),
        };
    }

    private Vector2 LossyScale2D() {
        Vector3 scale = transform.lossyScale;
        return new(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    private void OnDrawGizmosSelected() {
        Vector2 center = offset;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
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
