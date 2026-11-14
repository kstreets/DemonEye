using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BarrelSmoke : MonoBehaviour {

    public ParticleSystem particles; // Simulation Space: World
    public Vector2Int countRange = new(3, 5);
    public Vector2 speedRange = new(0.15f, 0.4f);
    public float spreadAngle = 20f;
    public float forwardOffset = 0.04f;

    public void Emit(Vector2 position, Vector2 aimDir, Vector2 playerVelocity) {
        Vector2 origin = position + aimDir * forwardOffset;
        int count = Random.Range(countRange.x, countRange.y + 1);
        for (int i = 0; i < count; i++) {
            Vector2 dir = Quaternion.Euler(0f, 0f, Random.Range(-spreadAngle, spreadAngle)) * aimDir;
            ParticleSystem.EmitParams emit = new() {
                position = origin,
                velocity = playerVelocity + dir * Random.Range(speedRange.x, speedRange.y),
            };
            particles.Emit(emit, 1);
        }
    }

}
