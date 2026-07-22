using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public static class Physics {
    
    private static List<Collider2D> overlapCircleResults = new(1000);
    
    public static List<Collider2D> OverlapCircle(Vector2 center, float radius, LayerMask mask) {
        overlapCircleResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCircle(center, radius, contactFilter, overlapCircleResults);
        Assert.IsFalse(count > overlapCircleResults.Capacity);
        
        return overlapCircleResults;
    }
    
    private static List<Collider2D> overlapCapsuleResults = new(100);
    
    public static List<Collider2D> OverlapCapsule(Vector2 center, CapsuleCollider2D capsule, LayerMask mask) {
        overlapCapsuleResults.Clear();
        
        ContactFilter2D contactFilter = new() {
        layerMask = mask, 
        useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCapsule(center, capsule.size, capsule.direction, 0f, contactFilter, overlapCapsuleResults);
        Assert.IsFalse(count > overlapCircleResults.Capacity);
        
        return overlapCapsuleResults;
    }
    
}
