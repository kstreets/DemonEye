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
    public static int horizontalSamplingOffset; // Extra columns on each side so the shake offset never samples past the edge
    public static int screenPixelsPerUnit;
    public static Vector3 cameraPosition;
    
    public static Vector2 shakeOffset; // World units, applied to the whole finished image in the final blit
    public static float punchZoom = 1f; // Scales the whole finished image about the screen center in the final blit, for small quick zooms
    
    // This will sometimes be null so we don't want render features accessing the pixel perfect camera.
    // Instead we expose value type members that reflect the pixel perfect camera's data or just the sensible defaults.
    private static PixelPerfectCamera pixelPerfectCamera;
    private const int shakeMarginTexels = 16; // Furthest the final blit can shift for camera shake, in pixel perfect camera texels
    
    private static RTHandle tilemapRT;
    private static RTHandle waterOcclusionRT;
    private static RTHandle sceneRT;
    private static RTHandle waterRT;
    private static RTHandle finalOutputRT;

    private static ScriptableRendererData renderer;
    private static FullScreenPassRendererFeature environmentFullscreenPass;
    private static FullScreenPassRendererFeature damageVignettePass;
    private static FullScreenPassRendererFeature deathWipePass;

#if UNITY_EDITOR
    static RenderManager() {
        Initialize();
        EditorApplication.playModeStateChanged += _ => Initialize();
    }
#endif

    public enum Texture { Tilemap, WaterOcclusion, Scene, Water, Final, UrpColorTexture, }
    
    public static RTHandle GetRenderTexture(Texture type) {
        return type switch {
            Texture.Tilemap        => EnsureRenderTexture(ref tilemapRT),
            Texture.WaterOcclusion => EnsureRenderTexture(ref waterOcclusionRT),
            Texture.Scene          => EnsureRenderTexture(ref sceneRT),
            Texture.Water          => EnsureRenderTexture(ref waterRT),
            Texture.Final          => EnsureRenderTexture(ref finalOutputRT),
            _                      => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
    
    public static void SetFullscreenEnvironmentMaterial(Material mat) {
        environmentFullscreenPass ??= GetRenderFeature<FullScreenPassRendererFeature>("Environment");
        if (environmentFullscreenPass == null) return;
        
        environmentFullscreenPass.SetActive(mat != null);
        environmentFullscreenPass.passMaterial = mat;
        renderer.SetDirty();
    }

    public static void SetDamageVignettePassActive(bool active) {
        damageVignettePass ??= GetRenderFeature<FullScreenPassRendererFeature>("DamageVignette");
        if (damageVignettePass == null || damageVignettePass.isActive == active) return;

        damageVignettePass.SetActive(active);
        renderer.SetDirty();
    }

    public static void SetDeathWipePassActive(bool active) {
        deathWipePass ??= GetRenderFeature<FullScreenPassRendererFeature>("Death Wipe");
        if (deathWipePass == null || deathWipePass.isActive == active) return;

        deathWipePass.SetActive(active);
        renderer.SetDirty();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        offScreenRenderingSize = Vector2Int.zero;
        shakeOffset = Vector2.zero;
        punchZoom = 1f;
        pixelsPerUnit = 58;
        fixedPixelsPerUnit = 58;
        referenceResolution = 180;
        renderer = null;
        environmentFullscreenPass = null;
        damageVignettePass = null;
        deathWipePass = null;
        SetFullscreenEnvironmentMaterial(null);
        SetDamageVignettePassActive(false);
        SetDeathWipePassActive(false);
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
        horizontalSamplingOffset = shakeMarginTexels * pixelsPerTexel;
        int offScreenWidth = curScreenSize.x + horizontalSamplingOffset * 2;
        
        // We need to manually define the tilemap chunk culling to match the size of the offscreen render textures
        if (Game.gameInstance != null && Game.gameInstance.InRaid) {
            TilemapRenderer tileMapRenderer = Game.gameInstance.curRaid.mapInstance.mainTilemapRenderer;
            if (tileMapRenderer.detectChunkCullingBounds != TilemapRenderer.DetectChunkCullingBounds.Manual) {
                tileMapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            }
            float offscreenWidthInWorldSpace = Mathf.CeilToInt(offScreenWidth / (float)screenPixelsPerUnit);
            float offscreenHeightInWorldSpace = Mathf.CeilToInt(offScreenHeight / (float)screenPixelsPerUnit);
            Vector3 cullingBounds = new(offscreenWidthInWorldSpace, offscreenHeightInWorldSpace, 100f);
            if (tileMapRenderer.chunkCullingBounds != cullingBounds) {
                tileMapRenderer.chunkCullingBounds = cullingBounds;
            }
        }
        
        bool resizeRenderTextures = offScreenRenderingSize.x != offScreenWidth || offScreenRenderingSize.y != offScreenHeight
                                    || finalOutputRT == null || finalOutputRT.referenceSize != curScreenSize;
        if (resizeRenderTextures) {
            AllocRenderTexture(ref tilemapRT, offScreenWidth, offScreenHeight);
            AllocRenderTexture(ref sceneRT, offScreenWidth, offScreenHeight);
            AllocRenderTexture(ref waterRT, offScreenWidth, offScreenHeight);
            AllocRenderTexture(ref waterOcclusionRT, offScreenWidth, offScreenHeight);
            AllocRenderTexture(ref finalOutputRT, curScreenSize.x, curScreenSize.y);

            Shader.SetGlobalTexture(ShaderIds.waterMap, waterRT);
            Shader.SetGlobalTexture(ShaderIds.waterOcclusionMap, waterOcclusionRT);

            // The water shader's screen position is render target pixels / screen size, this remaps it to the render texture's uv
            Vector4 uvShaderScaler = new(curScreenSize.x / (float)offScreenWidth, curScreenSize.y / (float)offScreenHeight, 1f, 1f);
            Shader.SetGlobalVector(ShaderIds.waterUVScaler, uvShaderScaler);
        }

        offScreenRenderingSize = new(offScreenWidth, offScreenHeight);
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
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
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
    
    private static ScriptableRendererData GetRenderer(int index = 0) {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urpAsset) return null;
        ReadOnlySpan<ScriptableRendererData> list = urpAsset.rendererDataList;
        return index < list.Length ? list[index] : null;
    }

    private static T GetRenderFeature<T>(string name) where T : ScriptableRendererFeature {
        renderer ??= GetRenderer();
        if (renderer == null) {
            return null;
        }
        foreach (var feature in renderer.rendererFeatures) {
            if (feature is T typed && feature.name == name) {
                return typed;
            }
        }
        return null;
    }
    
}
