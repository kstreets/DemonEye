using UnityEngine;
using VInspector;

public class GridObstacle : MonoBehaviour {
    
    public enum Mode { Collider, Manual }
    public Mode mode;
    
    [ShowIf("mode", Mode.Collider)]
    public CircleCollider2D carvingCollider;
    [EndIf]
    
    [ShowIf("mode", Mode.Manual)]
    public int cellRadius;
    public Vector2 localOffset;
    [EndIf]
    
    public Vector2 Center(Vector2 pos) {
        if (mode == Mode.Manual) {
            return pos + localOffset;
        }
        return pos + carvingCollider.offset;
    }

    public int Radius(float cellSize) {
        if (mode == Mode.Manual) {
            return cellRadius;
        }
        return Mathf.RoundToInt(carvingCollider.radius / cellSize);
    }
    
}