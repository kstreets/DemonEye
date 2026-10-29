using UnityEngine;

public class SpawnOffset : MonoBehaviour {

    public Vector2 offset;
    public bool rotateWithEntity;

    public Vector3 Apply(Vector3 position, Quaternion rotation) {
        Vector3 worldOffset = rotateWithEntity ? rotation * offset : offset;
        return position + worldOffset;
    }

    // The dot marks the requested spawn position relative to where the entity ends up
    private void OnDrawGizmosSelected() {
        Vector3 worldOffset = rotateWithEntity ? transform.rotation * offset : offset;
        Vector3 spawnPos = transform.position - worldOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(spawnPos, transform.position);
        Gizmos.DrawWireSphere(spawnPos, 0.01f);
    }

}
