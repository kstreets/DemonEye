using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public static class Physics {
    
    private static List<Collider2D> _overlapResults = new(100);
    
    public static List<Collider2D> OverlapCircle(Vector2 center, float radius, LayerMask mask) {
        _overlapResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCircle(center, radius, contactFilter, _overlapResults);
        Assert.IsFalse(count > _overlapResults.Capacity);
        
        return _overlapResults;
    }
    
    public static List<Collider2D> OverlapCapsule(Vector2 center, CapsuleCollider2D capsule, LayerMask mask) {
        _overlapResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
            
        };
        
        int count = Physics2D.OverlapCapsule(center, capsule.size, capsule.direction, 0f, contactFilter, _overlapResults);
        Assert.IsFalse(count > _overlapResults.Capacity);
        
        return _overlapResults;
    }
    
    public static List<Collider2D> OverlapCapsule(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angle, LayerMask mask) {
        _overlapResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        int count = Physics2D.OverlapCapsule(center, size, direction, angle, contactFilter, _overlapResults);
        Assert.IsFalse(count > _overlapResults.Capacity);
        
        return _overlapResults;
    }
    
    public static List<Collider2D> OverlapBox(Vector2 center, Vector2 size, float angle, LayerMask mask) {
        _overlapResults.Clear();
        
        ContactFilter2D contactFilter = new() {
            layerMask = mask, 
            useLayerMask = true,
        };
        
        int count = Physics2D.OverlapBox(center, size, angle, contactFilter, _overlapResults);
        Assert.IsFalse(count > _overlapResults.Capacity);
        
        return _overlapResults;
    }
    
}
