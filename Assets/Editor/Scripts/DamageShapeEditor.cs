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

    private static EnemyData FindEnemyData(GameObject go) {
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
        if (stateName == null || !animator) return null;

        RuntimeAnimatorController controller = data && data.animatorOverride ? data.animatorOverride : animator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = controller as AnimatorOverrideController;
        AnimatorController baseController = (overrideController ? overrideController.runtimeAnimatorController : controller) as AnimatorController;
        if (!baseController) return null;

        foreach (ChildAnimatorState child in baseController.layers[0].stateMachine.states) {
            if (child.state.name != stateName || child.state.motion is not AnimationClip original) continue;
            return overrideController ? overrideController[original] : original;
        }
        return null;
    }

    [MenuItem("GameObject/DemonEye/Create Attack Damage Shapes", false, 0)]
    private static void CreateAttackShapesForSelection() {
        foreach (GameObject go in Selection.gameObjects) {
            CreateAttackShapes(go.transform, FindEnemyData(go));
        }
    }

    // Seeds Side/Up/Down children from the EnemyData values the melee check uses today
    public static void CreateAttackShapes(Transform root, EnemyData data) {
        if (!data) {
            Debug.LogWarning($"No EnemyData uses {root.name} as its prefab", root);
            return;
        }
        Animator animator = root.GetComponent<Animator>();
        CreateAttackShape(root, animator, data, DamageShape.AttackDir.Side, data.sideAttackOffset);
        CreateAttackShape(root, animator, data, DamageShape.AttackDir.Up, data.upAttackOffset);
        CreateAttackShape(root, animator, data, DamageShape.AttackDir.Down, data.donwAttackOffset);
    }

    private static void CreateAttackShape(Transform root, Animator animator, EnemyData data, DamageShape.AttackDir dir, Vector2 offset) {
        string name = $"Attack{dir}";
        if (root.Find(name)) return;

        GameObject go = new(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Attack Damage Shapes");
        go.transform.SetParent(root, false);

        // The melee check uses world offsets and radius, so undo the enemy's scale
        Vector3 scale = root.lossyScale;
        go.transform.localPosition = new Vector3(offset.x / scale.x, offset.y / scale.y, 0f);

        DamageShape shape = go.AddComponent<DamageShape>();
        shape.shape = DamageShape.ShapeType.Circle;
        shape.radius = data.attackRadius / Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        shape.attackDir = dir;
        shape.mirrorForLeft = dir == DamageShape.AttackDir.Side;

        AnimationClip clip = FindAttackClip(animator, dir, data);
        shape.damageFrame = clip ? Mathf.RoundToInt(data.attackDamageDelay * clip.frameRate) : 0;
    }

}
