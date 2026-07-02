using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class RenderManager {
    
    public static Vector2Int curScreenSize;
    public static Vector2Int offScreenRenderingSize;
    
    public static int pixelsPerUnit;
    public static int pixelsPerTexel;
    public static int fixedPixelsPerUnit;
    public static int referenceResolution;
    public static int verticalSamplingOffset;
    public static int screenPixelsPerUnit;
    public static Vector3 cameraPosition;
    
    // This will sometimes be null so we don't want render features accessing the pixel perfect camera.
    // Instead we expose value type members that reflect the pixel perfect camera's data or just the sensible defaults.
    private static PixelPerfectCamera pixelPerfectCamera;
    private static readonly int waterMapShaderProp = Shader.PropertyToID("_WaterMap");
    private static readonly int waterUVScalerShaderProp = Shader.PropertyToID("_WaterUVScaler");
    
    private static RTHandle tilemapRT;
    private static RTHandle sceneRT;
    private static RTHandle waterRT;
    private static RTHandle finalOutputRT;
    
#if UNITY_EDITOR
    static RenderManager() {
        Initialize();
        EditorApplication.playModeStateChanged += _ => Initialize();
    }
#endif
    
    public enum Texture { Tilemap, Scene, Water, Final }
    
    public static RTHandle GetRenderTexture(Texture type) {
        return type switch {
            Texture.Tilemap => EnsureRenderTexture(ref tilemapRT),
            Texture.Scene   => EnsureRenderTexture(ref sceneRT),
            Texture.Water   => EnsureRenderTexture(ref waterRT),
            Texture.Final   => EnsureRenderTexture(ref finalOutputRT),
            _               => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        offScreenRenderingSize = Vector2Int.zero;
        pixelsPerUnit = 48;
        fixedPixelsPerUnit = 48;
        referenceResolution = 180;
        RenderPipelineManager.beginCameraRendering -= OnBeginRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginRendering;
    }
    
    private static void OnBeginRendering(ScriptableRenderContext context, Camera camera) {
        if (camera.cameraType != CameraType.SceneView && camera.cameraType != CameraType.Game) return;
        
        if (pixelPerfectCamera == null) {
            if (camera.TryGetComponent(out pixelPerfectCamera)) {
                fixedPixelsPerUnit = pixelPerfectCamera.assetsPPU;
            }
        }
        if (pixelPerfectCamera != null) {
            pixelsPerUnit = pixelPerfectCamera.assetsPPU;
            referenceResolution = pixelPerfectCamera.refResolutionX;
            cameraPosition = pixelPerfectCamera.RoundToPixel(camera.transform.position);
        }
        
        curScreenSize = camera.cameraType == CameraType.SceneView ? GetGameViewSize() : new(Screen.width, Screen.height);
        // This is how the PixelPerfectCamera calculates its 'zoom'
        pixelsPerTexel = Mathf.Max(1, curScreenSize.y / referenceResolution);
        screenPixelsPerUnit = curScreenSize.y / (referenceResolution / pixelsPerUnit);
        
        int offScreenHeight = curScreenSize.y + (fixedPixelsPerUnit * pixelsPerTexel);
        verticalSamplingOffset = (offScreenHeight - curScreenSize.y) / 2;
        
        // We need to manually define the tilemap chunk culling to match the size of the offscreen render textures
        if (Game.gameInstance != null && Game.gameInstance.InRaid) {
            TilemapRenderer tileMapRenderer = Game.gameInstance.curRaid.mapInstance.mainTilemapRenderer;
            if (tileMapRenderer.detectChunkCullingBounds != TilemapRenderer.DetectChunkCullingBounds.Manual) {
                tileMapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            }
            float offscreenWidthInWorldSpace = Mathf.CeilToInt(curScreenSize.x / (float)screenPixelsPerUnit);
            float offscreenHeightInWorldSpace = Mathf.CeilToInt(offScreenHeight / (float)screenPixelsPerUnit);
            Vector3 cullingBounds = new(offscreenWidthInWorldSpace, offscreenHeightInWorldSpace, 100f);
            if (tileMapRenderer.chunkCullingBounds != cullingBounds) {
                tileMapRenderer.chunkCullingBounds = cullingBounds;
            }
        }
        
        bool resizeRenderTextures = offScreenRenderingSize.x != curScreenSize.x || offScreenRenderingSize.y != offScreenHeight;
        if (resizeRenderTextures) {
            AllocRenderTexture(ref tilemapRT, curScreenSize.x, offScreenHeight);
            AllocRenderTexture(ref sceneRT, curScreenSize.x, offScreenHeight);
            AllocRenderTexture(ref waterRT, curScreenSize.x, offScreenHeight);
            AllocRenderTexture(ref finalOutputRT, curScreenSize.x, curScreenSize.y);
            
            Shader.SetGlobalTexture(waterMapShaderProp, waterRT);
            
            Vector4 uvShaderScaler = Vector4.one;
            uvShaderScaler.y = 1f - ((offScreenHeight - curScreenSize.y) / (float)offScreenHeight);
            Shader.SetGlobalVector(waterUVScalerShaderProp, uvShaderScaler);
        }
        
        offScreenRenderingSize = new(curScreenSize.x, offScreenHeight);
    }
    
    private static RTHandle EnsureRenderTexture(ref RTHandle rtHandle) {
        if (RenderTextureExists(ref rtHandle)) {
            return rtHandle;
        }
        AllocRenderTexture(ref rtHandle, Screen.width, Screen.height);
        return rtHandle;
    }
    
    private static bool RenderTextureExists(ref RTHandle rtHandle) {
        return rtHandle != null && rtHandle.rt != null && rtHandle.rt.IsCreated();
    }
    
    private static void AllocRenderTexture(ref RTHandle rtHandle, int width, int height) {
        rtHandle?.Release();
        rtHandle = RTHandles.Alloc(width, height, GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true, autoGenerateMips: false, filterMode: FilterMode.Point);
    }
    
    private static Vector2Int GetGameViewSize() {
        #if UNITY_EDITOR
            Vector2 size = Handles.GetMainGameViewSize();
            int w = (int)size.x;
            int h = (int)size.y;
            return new(w, h);
        #else
            return new(Screen.width, Screen.height);
        #endif
    }
    
}
