using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// Edits the TrailerSettings asset, which gets included in builds
public class TrailerWindow : EditorWindow {

    private const string settingsAssetPath = "Assets/Resources/" + TrailerSettings.resourcesPath + ".asset";

    // Only used to show the portal in the field when its map scene is open, so it doesn't need to be in the build
    private const string portalGlobalIdEditorPrefKey = "DemonEye_Trailer_PortalGlobalId";

    private TrailerSettings settings;
    private ObjectField portalField;
    private Label portalInfoLabel;

    [MenuItem("Window/UI Toolkit/Trailer")]
    public static void ShowWindow() {
        TrailerWindow wnd = GetWindow<TrailerWindow>();
        wnd.titleContent = new("Trailer");
    }

    public void CreateGUI() {
        settings = LoadOrCreateSettings();

        VisualElement panel = new() { style = { marginTop = 5, marginLeft = 5, marginRight = 5 } };
        rootVisualElement.Add(panel);

        Toggle enabledToggle = new("Enabled") {
            value = settings.enabled,
            tooltip = "Turn off to play raids normally without losing the settings below. Make sure it's off for release builds!",
        };
        enabledToggle.RegisterValueChangedCallback(changeEvent => ModifySettings(() => settings.enabled = changeEvent.newValue));
        panel.Add(enabledToggle);

        panel.Add(CreateDivider());

        portalField = new("Exit Portal") {
            objectType = typeof(Portal),
            allowSceneObjects = true,
            tooltip = "Open the map scene and drag in one of its exit portals. It will be the only active exit portal when raiding that map",
        };
        portalField.value = LoadSavedPortal();
        portalField.RegisterValueChangedCallback(OnPortalChanged);
        panel.Add(portalField);

        portalInfoLabel = new() { style = { marginLeft = 3, marginTop = 2, unityFontStyleAndWeight = FontStyle.Italic, whiteSpace = WhiteSpace.Normal } };
        panel.Add(portalInfoLabel);
        RefreshPortalInfo();

        FloatField summonDelayField = new("Auto Summon Delay") {
            value = settings.autoSummonDelay,
            tooltip = "Seconds after the raid starts to automatically summon the exit portal. Negative to not auto summon",
        };
        summonDelayField.RegisterValueChangedCallback(changeEvent => ModifySettings(() => settings.autoSummonDelay = changeEvent.newValue));
        panel.Add(summonDelayField);
    }

    private static TrailerSettings LoadOrCreateSettings() {
        TrailerSettings settings = AssetDatabase.LoadAssetAtPath<TrailerSettings>(settingsAssetPath);
        if (settings != null) return settings;

        settings = CreateInstance<TrailerSettings>();
        AssetDatabase.CreateAsset(settings, settingsAssetPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private void ModifySettings(System.Action modify) {
        Undo.RecordObject(settings, "Modify Trailer Settings");
        modify();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);
    }

    private void OnPortalChanged(ChangeEvent<Object> changeEvent) {
        Portal portal = changeEvent.newValue as Portal;

        if (portal == null) {
            ModifySettings(() => {
                settings.portalScene = string.Empty;
                settings.portalIndex = -1;
                settings.portalName = string.Empty;
            });
            EditorPrefs.DeleteKey(portalGlobalIdEditorPrefKey);
            RefreshPortalInfo();
            return;
        }

        MapInstance mapInstance = portal.GetComponentInParent<MapInstance>(true);
        if (mapInstance == null || portal.transform.parent != mapInstance.exitPortalsParent) {
            Debug.LogWarning($"{portal.name} isn't a child of a map's exit portals parent", portal);
            portalField.SetValueWithoutNotify(changeEvent.previousValue);
            return;
        }

        ModifySettings(() => {
            settings.portalScene = portal.gameObject.scene.name;
            settings.portalIndex = portal.transform.GetSiblingIndex();
            settings.portalName = portal.name;
        });

        // Runtime objects don't have a stable id, the scene and index are still enough for the game to find it
        if (!Application.isPlaying) {
            EditorPrefs.SetString(portalGlobalIdEditorPrefKey, GlobalObjectId.GetGlobalObjectIdSlow(portal).ToString());
        }

        RefreshPortalInfo();
    }

    // Only resolves while the portal's map scene is open in the editor
    private static Portal LoadSavedPortal() {
        if (!GlobalObjectId.TryParse(EditorPrefs.GetString(portalGlobalIdEditorPrefKey), out GlobalObjectId globalId)) return null;
        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as Portal;
    }

    // The field shows empty when the map scene isn't open, so show what's saved
    private void RefreshPortalInfo() {
        if (!settings.HasPortal) {
            portalInfoLabel.text = "No exit portal picked, raids use random exit portals";
            return;
        }
        portalInfoLabel.text = $"Saved: {settings.portalName} (#{settings.portalIndex}) in {settings.portalScene}";
    }

    private static VisualElement CreateDivider() {
        return new() { style = { backgroundColor = new Color(42f / 255f, 42f / 255f, 42f / 255f), height = 1, marginTop = 5, marginBottom = 5 } };
    }

}
