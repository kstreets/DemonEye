using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(AttackSpawnPoint))]
public class AttackSpawnPointEditor : Editor {

    private static bool previewAnimation = true;

    private AttackSpawnPoint spawnPoint;
    private Animator animator;
    private EnemyData enemyData;
    private AnimationClip clip;
    private string[] stateNames;

    private void OnEnable() {
        spawnPoint = (AttackSpawnPoint)target;
        animator = spawnPoint.GetComponentInParent<Animator>();
        enemyData = DamageShapeEditor.FindEnemyData(spawnPoint.gameObject);
        stateNames = StateNames(animator, enemyData);
        clip = DamageShapeEditor.FindStateClip(animator, spawnPoint.animationState, enemyData);
        SamplePreview();
    }

    private void OnDisable() {
        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        SerializedProperty stateProp = serializedObject.FindProperty(nameof(AttackSpawnPoint.animationState));
        if (stateNames.Length > 0) {
            int index = EditorGUILayout.Popup("Animation State", System.Array.IndexOf(stateNames, stateProp.stringValue), stateNames);
            if (index >= 0) stateProp.stringValue = stateNames[index];
        }
        else {
            EditorGUILayout.PropertyField(stateProp);
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AttackSpawnPoint.mirrorForLeft)));

        SerializedProperty frameProp = serializedObject.FindProperty(nameof(AttackSpawnPoint.spawnFrame));
        if (clip) {
            int frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
            EditorGUILayout.IntSlider(frameProp, 0, frameCount, "Spawn Frame");
            EditorGUILayout.HelpBox($"{clip.name}: spawns at {frameProp.intValue / clip.frameRate:F2}s of {clip.length:F2}s ({clip.frameRate}fps)", MessageType.None);
        }
        else {
            EditorGUILayout.PropertyField(frameProp);
            EditorGUILayout.HelpBox(animator ? "Pick an animation state to preview" : "No Animator on this object or its parents", MessageType.Info);
        }

        previewAnimation = EditorGUILayout.Toggle("Preview In Scene", previewAnimation);

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck()) {
            clip = DamageShapeEditor.FindStateClip(animator, spawnPoint.animationState, enemyData);
            SamplePreview();
        }
    }

    private void OnSceneGUI() {
        Handles.Label(spawnPoint.transform.position + Vector3.up * 0.06f, $"{spawnPoint.animationState} @ {spawnPoint.spawnFrame}", EditorStyles.whiteMiniLabel);
    }

    private void SamplePreview() {
        bool canPreview = previewAnimation && animator && clip && animator.gameObject.scene.IsValid() && !EditorApplication.isPlayingOrWillChangePlaymode;
        if (!canPreview) {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            return;
        }
        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(animator.gameObject, clip, spawnPoint.spawnFrame / clip.frameRate);
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }

    private static string[] StateNames(Animator animator, EnemyData data) {
        AnimatorController controller = DamageShapeEditor.BaseController(animator, data, out _);
        if (!controller) return new string[0];
        List<string> names = new();
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states) {
            names.Add(child.state.name);
        }
        return names.ToArray();
    }

}
