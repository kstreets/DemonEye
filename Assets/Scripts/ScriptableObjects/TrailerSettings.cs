using UnityEngine;

// Edited from the Trailer window. Lives in a Resources folder so it gets included in builds without being referenced
public class TrailerSettings : ScriptableObject {

    public const string resourcesPath = "TrailerSettings";

    public bool enabled;

    [Header("Exit Portal")]
    // The portal is saved as its sibling index under the map's exit portals parent, since it lives in a map scene that isn't loaded yet
    public string portalScene;
    public int portalIndex = -1;
    public string portalName;
    [Tooltip("Seconds after the raid starts to automatically summon the exit portal. Negative to not auto summon")]
    public float autoSummonDelay = -1f;

    public bool HasPortal => !string.IsNullOrEmpty(portalScene) && portalIndex >= 0;

}
