using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class BurnEdgeEmberSpawner : MonoBehaviour {
    
    [Header("Target")]
    [Tooltip("The UI rect being burned. emberParticles' transform should coincide with this.")]
    public RectTransform targetRect;
    public ParticleSystem emberParticles; // Simulation Space: Local
 
    [Header("Match the shader's burn line")]
    [SerializeField] private Vector2 burnDirection = new Vector2(-1f, 1f); // bottom-right -> top-left
 
    [Header("Emission")]
    [SerializeField] private float embersPerUnitLengthPerSecond = 30f;
    [SerializeField] private float positionJitter = 0.01f;
 
    [Tooltip("Drive this from the same value you feed the shader's _BurnAmount.")]
    [Range(0f, 1f)] public float BurnProgress;
 
    private float _emitAccumulator;
 
    private void Update()
    {
        if (targetRect == null || emberParticles == null) return;
 
        Vector2 dir = burnDirection.normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        Vector2 center = Vector2.one * 0.5f;
 
        // How far the unit square actually reaches along dir depends on its angle --
        // a diagonal direction's corners sit sqrt(2)/2 out, not 0.5. This is the
        // standard support function of an axis-aligned box: half-extent (0.5, 0.5)
        // projected onto dir. Skipping it (assuming a flat +-0.5 range) is why the
        // line fell short of the true corner whenever dir wasn't horizontal/vertical.
        float halfExtent = 0.5f * (Mathf.Abs(dir.x) + Mathf.Abs(dir.y));
        Vector2 linePoint = center + Mathf.Lerp(-halfExtent, halfExtent, BurnProgress) * dir;
 
        // Clip the infinite line { linePoint + t * perp } against the unit square on both axes.
        float tMin = float.NegativeInfinity, tMax = float.PositiveInfinity;
        ClipAxis(linePoint.x, perp.x, 0f, 1f, ref tMin, ref tMax);
        ClipAxis(linePoint.y, perp.y, 0f, 1f, ref tMin, ref tMax);
        if (tMin >= tMax) return; // sweep hasn't reached the rect yet, or has fully passed it
 
        float visibleLength = tMax - tMin;
        _emitAccumulator += embersPerUnitLengthPerSecond * visibleLength * Time.deltaTime;
        int emitCount = Mathf.FloorToInt(_emitAccumulator);
        _emitAccumulator -= emitCount;
 
        for (int i = 0; i < emitCount; i++)
        {
            float t = Random.Range(tMin, tMax);
            Vector2 uv = linePoint + t * perp;
            var emit = new ParticleSystem.EmitParams { position = UVToLocalOffset(uv) };
            emberParticles.Emit(emit, 1);
        }
    }
 
    // Standard slab-clip: intersects [tMin, tMax] with the range of t for which
    // p0 + t * d stays inside [lo, hi] on one axis.
    private static void ClipAxis(float p0, float d, float lo, float hi, ref float tMin, ref float tMax)
    {
        if (Mathf.Approximately(d, 0f))
        {
            if (p0 < lo || p0 > hi) tMax = tMin - 1f; // line is parallel and outside this axis: empty
            return;
        }
        float t1 = (lo - p0) / d;
        float t2 = (hi - p0) / d;
        if (t1 > t2) (t1, t2) = (t2, t1);
        tMin = Mathf.Max(tMin, t1);
        tMax = Mathf.Min(tMax, t2);
    }
 
    // Local-space offset (relative to emberParticles' own transform), NOT a world position.
    // Requires Simulation Space = Local and emberParticles' transform to coincide with targetRect.
    private Vector3 UVToLocalOffset(Vector2 uv)
    {
        Vector2 jitteredUV = uv + Random.insideUnitCircle * positionJitter;
        Rect r = targetRect.rect;
        return new Vector3(
            Mathf.Lerp(r.xMin, r.xMax, jitteredUV.x),
            Mathf.Lerp(r.yMin, r.yMax, jitteredUV.y),
            0f);
    }
}
 