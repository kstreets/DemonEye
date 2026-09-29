using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class MapScreenshotCapture {

    private const int imageWidth = 960;
    private const int imageHeight = 400;
    private const int cameraPixelPerfectUnits = 80;

    [MenuItem("Tools/Map Screenshot Capture")]
    private static void CaptureTilesetLayer() {
        MapInstance mapInstance = Object.FindObjectOfType<MapInstance>();
        if (mapInstance == null) {
            Debug.LogError("No MapInstance found in the currently open scene.");
            return;
        }
        
        if (Screen.width != imageWidth || Screen.height != imageHeight) {
            Debug.LogError($"Game screen must be rendering as {imageWidth} x {imageHeight}");
            return;
        }
        
        Camera cam = Camera.main;
        if (cam == null) {
            Debug.LogError("No main camera in scene.");
            return;
        }
        
        if (cam.GetComponent<PixelPerfectCamera>().assetsPPU != cameraPixelPerfectUnits) {
            cam.GetComponent<PixelPerfectCamera>().assetsPPU = cameraPixelPerfectUnits;
            RenderManager.fixedPixelsPerUnit = cameraPixelPerfectUnits;
            EditorApplication.delayCall += CaptureTilesetLayer;
            return;
        }
        
        Game.gameInstance.entities.player.gameObject.SetActive(false);
        
        RenderTexture rt = new(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new(imageWidth + 2, imageHeight + 2, TextureFormat.ARGB32, false);
        
        result.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 1, 1);
        for (int x = 0; x < imageWidth + 2; x++) {
            result.SetPixel(x, 0, Color.clear);
            result.SetPixel(x, imageHeight + 1, Color.clear);
        }
        for (int y = 0; y < imageHeight + 2; y++) {
            result.SetPixel(0, y, Color.clear);
            result.SetPixel(imageWidth + 1, y, Color.clear);
        }
        result.Apply();
        
        RenderTexture.active = prevActive;

        // Save PNG under Assets/Art/Minimaps/<sceneName>_Minimap.png
        string dir = "Assets/Art/MapScreenshots";
        Directory.CreateDirectory(dir);
        string sceneName = mapInstance.gameObject.scene.name;
        string path = $"{dir}/{sceneName}_Screenshot.png";
        File.WriteAllBytes(path, result.EncodeToPNG());

        // Cleanup
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(result);
        Game.gameInstance.entities.player.gameObject.SetActive(true);

        AssetDatabase.Refresh();
        Debug.Log($"Screenshot captured: {path} ({imageWidth}x{imageHeight})");
    }

}
