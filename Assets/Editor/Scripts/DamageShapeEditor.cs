using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(DamageShape))]
public class DamageShapeEditor : Editor {

    private static bool previewAnimation = true;

    private DamageShape damageShape;
    private Animator animator;
    private EnemyData enemyData;
    private AnimationClip clip;
    private DamageShape.AttackDir clipDir;

    private void OnEnable() {
        damageShape = (DamageShape)target;
        animator = damageShape.GetComponentInParent<Animator>();
        enemyData = FindEnemyData(damageShape.gameObject);
        clipDir = damageShape.attackDir;
        clip = FindAttackClip(animator, clipDir, enemyData);
        SamplePreview();
    }

    private void OnDisable() {
        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        DrawPropertiesExcluding(serializedObject, "m_Script", nameof(DamageShape.damageFrame));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Damage Frame Preview", EditorStyles.boldLabel);
        clip = (AnimationClip)EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false);

        SerializedProperty frameProp = serializedObject.FindProperty(nameof(DamageShape.damageFrame));
        if (clip) {
            int frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
            EditorGUILayout.IntSlider(frameProp, 0, frameCount, "Damage Frame");
            string info = $"Hits at {frameProp.intValue / clip.frameRate:F2}s of {clip.length:F2}s ({clip.frameRate}fps)";
            if (enemyData) {
                info += $"\nEnemyData.attackDamageDelay is {enemyData.attackDamageDelay:F2}s (frame {enemyData.attackDamageDelay * clip.frameRate:F1})";
            }
            EditorGUILayout.HelpBox(info, MessageType.None);
        }
        else {
            EditorGUILayout.PropertyField(frameProp);
            EditorGUILayout.HelpBox(animator ? "Set Attack Dir or pick a clip to preview" : "No Animator on this object or its parents", MessageType.Info);
        }

        previewAnimation = EditorGUILayout.Toggle("Preview In Scene", previewAnimation);

        serializedObject.ApplyModifiedProperties();

        if (damageShape.attackDir != clipDir) {
            clipDir = damageShape.attackDir;
            clip = FindAttackClip(animator, clipDir, enemyData);
        }

        if (EditorGUI.EndChangeCheck()) {
            SamplePreview();
        }
    }

    private void SamplePreview() {
        bool canPreview = previewAnimation && animator && clip && animator.gameObject.scene.IsValid() && !EditorApplication.isPlayingOrWillChangePlaymode;
        if (!canPreview) {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            return;
        }
        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(animator.gameObject, clip, damageShape.damageFrame / clip.frameRate);
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }

    public static EnemyData FindEnemyData(GameObject go) {
        string path = PrefabStageUtility.GetPrefabStage(go)?.assetPath;
        if (string.IsNullOrEmpty(path)) path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        if (string.IsNullOrEmpty(path)) path = AssetDatabase.GetAssetPath(go);
        if (string.IsNullOrEmpty(path)) return null;

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(EnemyData)}")) {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data.enemyPrefab && AssetDatabase.GetAssetPath(data.enemyPrefab) == path) return data;
        }
        return null;
    }

    // Enemies get their real clips from EnemyData.animatorOverride at spawn, so resolve the state through that
    private static AnimationClip FindAttackClip(Animator animator, DamageShape.AttackDir dir, EnemyData data) {
        string stateName = dir switch {
            DamageShape.AttackDir.Side => "AttackSide",
            DamageShape.AttackDir.Up   => "AttackUp",
            DamageShape.AttackDir.Down => "AttackDown",
            _                          => null,
        };
        return FindStateClip(animator, stateName, data);
    }

    public static AnimationClip FindStateClip(Animator animator, string stateName, EnemyData data) {
        if (string.IsNullOrEmpty(stateName)) return null;
        AnimatorController baseController = BaseController(animator, data, out AnimatorOverrideController overrideController);
        if (!baseController) return null;

        foreach (ChildAnimatorState child in baseController.layers[0].stateMachine.states) {
            if (child.state.name != stateName || child.state.motion is not AnimationClip original) continue;
            return overrideController ? overrideController[original] : original;
        }
        return null;
    }

    public static AnimatorController BaseController(Animator animator, EnemyData data, out AnimatorOverrideController overrideController) {
        overrideController = null;
        if (!animator) return null;
        RuntimeAnimatorController controller = data && data.animatorOverride ? data.animatorOverride : animator.runtimeAnimatorController;
        overrideController = controller as AnimatorOverrideController;
        return (overrideController ? overrideController.runtimeAnimatorController : controller) as AnimatorController;
    }

}
