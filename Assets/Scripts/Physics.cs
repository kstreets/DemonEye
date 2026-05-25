using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public static class Physics {
    
    private static List<Collider2D> _overlapCircleResults = new(1000);
    
    public static List<Collider2D> OverlapCircle(Vector2 center, float radius, LayerMask mask) {
        _overlapCircleResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCircle(center, radius, contactFilter, _overlapCircleResults);
        Assert.IsFalse(count > _overlapCircleResults.Capacity);
        
        return _overlapCircleResults;
    }
    
    private static List<Collider2D> _overlapCapsuleResults = new(100);
    
    public static List<Collider2D> OverlapCapsule(Vector2 center, CapsuleCollider2D capsule, LayerMask mask) {
        _overlapCapsuleResults.Clear();
        
        ContactFilter2D contactFilter = new() {
        layerMask = mask, 
        useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCapsule(center, capsule.size, capsule.direction, 0f, contactFilter, _overlapCapsuleResults);
        Assert.IsFalse(count > _overlapCircleResults.Capacity);
        
        return _overlapCapsuleResults;
    }
    
}
