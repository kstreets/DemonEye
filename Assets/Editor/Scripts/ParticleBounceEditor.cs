using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParticleBounce))]
public class ParticleBounceEditor : Editor {

    private static int previewCount = 15;
    // Used when the game's bounce curve isn't loaded (Game only lives in the Main scene)
    private static AnimationCurve fallbackCurve = new(new Keyframe(0f, 0f, 4f, 4f), new Keyframe(0.5f, 1f, 0f, 0f), new Keyframe(1f, 0f, -4f, -4f));

    private ParticleBounce bounce;
    private ParticleSystem ps;
    private double lastTime;
    private bool previewing;

    private void OnEnable() {
        bounce = (ParticleBounce)target;
        ps = bounce.GetComponent<ParticleSystem>();
    }

    private void OnDisable() {
        StopPreview();
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (Application.isPlaying) return;

        EditorGUILayout.Space();
        if (bounce.GetHeightCurve() == null) {
            EditorGUILayout.HelpBox("The game's bounce curve isn't loaded (open the Main scene). Previewing with this curve instead.", MessageType.Info);
            fallbackCurve = EditorGUILayout.CurveField("Preview Curve", fallbackCurve);
        }

        previewCount = EditorGUILayout.IntSlider("Preview Count", previewCount, 1, ps.main.maxParticles);
        if (GUILayout.Button(previewing ? "Restart Preview" : "Preview Bounce")) {
            StartPreview();
        }
    }

    private void StartPreview() {
        StopPreview();
        ps.Emit(previewCount);
        lastTime = EditorApplication.timeSinceStartup;
        previewing = true;
        EditorApplication.update += UpdatePreview;
    }

    private void UpdatePreview() {
        if (!ps) {
            StopPreview();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        ps.Simulate((float)(now - lastTime), true, false, false);
        lastTime = now;
        bounce.Apply(fallbackCurve);
        SceneView.RepaintAll();

        if (ps.particleCount == 0) StopPreview();
    }

    private void StopPreview() {
        EditorApplication.update -= UpdatePreview;
        if (previewing && ps) ps.Clear();
        previewing = false;
        Repaint();
    }

}
