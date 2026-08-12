using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public static class MinimapCapture {

    // Pixels-per-world-unit for the captured texture. Higher = sharper minimap, larger file.
    private const int PixelsPerUnit = 20;

    [MenuItem("Tools/Minimap/Capture Tileset Layer")]
    private static void CaptureTilesetLayer() {
        MapInstance mapInstance = Object.FindObjectOfType<MapInstance>();
        if (mapInstance == null) {
            Debug.LogError("No MapInstance found in the currently open scene.");
            return;
        }

        // CoolerGrid grid = mapInstance.grid;
        Tilemap tilemap = mapInstance.mainTilemapRenderer.GetComponent<Tilemap>();
        if (tilemap == null) {
            Debug.LogError("MapInstance has no tilemap assigned.");
            return;
        }
        
        tilemap.CompressBounds();
        BoundsInt cellBounds = tilemap.cellBounds;
        float cellSize = tilemap.cellSize.x;
        Vector2 worldCenter = tilemap.LocalToWorld(tilemap.localBounds.center);
        
        float worldWidth = cellBounds.size.x * cellSize;
        float worldHeight = cellBounds.size.y * cellSize;
        
        int texWidth = Mathf.CeilToInt(worldWidth * PixelsPerUnit);
        int texHeight = Mathf.CeilToInt(worldHeight * PixelsPerUnit);

        if (texWidth <= 0 || texHeight <= 0) {
            Debug.LogError("Map is too small to capture.");
            return;
        }

        int tilesetLayer = LayerMask.NameToLayer("Tilemap");
        if (tilesetLayer < 0) {
            Debug.LogError("No layer named 'Tileset' exists. Create it in Tags & Layers.");
            return;
        }

        // Temporary camera that renders ONLY the Tileset layer.
        GameObject camGo = new GameObject("__MinimapCaptureCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.aspect = worldWidth / worldHeight;
        cam.orthographicSize = worldHeight * 0.5f;
        cam.cullingMask = 1 << tilesetLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent background
        cam.transform.position = new Vector3(worldCenter.x, worldCenter.y, -10f);

        var cameraData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.SetRenderer(1);
        
        RenderTexture rt = new RenderTexture(texWidth, texHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(texWidth + 2, texHeight + 2, TextureFormat.ARGB32, false);
        
        result.ReadPixels(new Rect(0, 0, texWidth, texHeight), 1, 1);
        for (int x = 0; x < texWidth + 2; x++) {
            result.SetPixel(x, 0, Color.clear);
            result.SetPixel(x, texHeight + 1, Color.clear);
        }
        for (int y = 0; y < texHeight + 2; y++) {
            result.SetPixel(0, y, Color.clear);
            result.SetPixel(texWidth + 1, y, Color.clear);
        }
        result.Apply();
        
        RenderTexture.active = prevActive;

        // Save PNG under Assets/Art/Minimaps/<sceneName>_Minimap.png
        string dir = "Assets/Art/Minimaps";
        Directory.CreateDirectory(dir);
        string sceneName = mapInstance.gameObject.scene.name;
        string path = $"{dir}/{sceneName}_Minimap.png";
        File.WriteAllBytes(path, result.EncodeToPNG());

        // Cleanup
        cam.targetTexture = null;
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(result);

        AssetDatabase.Refresh();
        Debug.Log($"Minimap captured: {path} ({texWidth}x{texHeight})");
    }

}
