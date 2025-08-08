#if UNITY_EDITOR

using UnityEngine;

[ExecuteAlways]
public class ResourceSpawnVisualizer : MonoBehaviour {

    public bool drawGizmos = true;
    
    private void OnDrawGizmos() {
        if (!drawGizmos) return;
        
        foreach (Transform child in transform) {
            Gizmos.DrawSphere(child.position, 0.05f);
        }
    }
    
}

#endif