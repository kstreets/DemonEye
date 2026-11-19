#if UNITY_EDITOR
using UnityEngine;

[ExecuteAlways]
public class ResourceSpawnVisualizer : MonoBehaviour {

    public bool drawGizmos = true;

    void OnDrawGizmos() {
        if (!drawGizmos) return;
    }
    
}

#endif