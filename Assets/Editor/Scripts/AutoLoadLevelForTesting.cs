using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutoLoadLevelForTesting {
    
    [InitializeOnEnterPlayMode]
    public static void OnEnterPlaymode(EnterPlayModeOptions options) {
        string startingSceneName = EditorSceneManager.GetActiveScene().name;
        if (startingSceneName == "Main") return;
        
        // Store the target scene name, then let Unity load normally
        SessionState.SetString("StartingScene", startingSceneName);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {
        if (state != PlayModeStateChange.EnteredPlayMode) return;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        string startingSceneName = SessionState.GetString("StartingScene", null);
        if (string.IsNullOrEmpty(startingSceneName)) return;
        SessionState.EraseString("StartingScene");
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots) {
                if (root.TryGetComponent(out Game game)) {
                    MapData map = game.config.maps.First(m => m.sceneReference == startingSceneName);
                    if (map != null) {
                        game.LoadMapAsync(map);
                        break;
                    }
                }
            }
        }
    }
    
}
