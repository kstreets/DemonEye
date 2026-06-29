using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class WaterFeature : ScriptableRendererFeature {
    
    public RenderPassEvent renderPassEvent;
    public RenderTexture tilemapTexture;
    public RenderTexture sceneTexture;
    public RenderTexture outputTexture;
    public ComputeShader computeShader;
    
    private static WaterPass waterPass;

    public override void Create() {
        waterPass = new() { renderPassEvent = renderPassEvent };
        waterPass.waterSettings = new();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (outputTexture == null) return;
        waterPass.Setup(tilemapTexture, sceneTexture, outputTexture, computeShader);
        renderer.EnqueuePass(waterPass);
    }
    
    public static void SetWaterSettings(MapWaterSettings settings) {
        if (waterPass == null) return;
        waterPass.waterSettings = settings;
    }

    public class WaterPass : ScriptableRenderPass {
        
        public MapWaterSettings waterSettings;
        private RenderTexture outputTex;
        private ComputeShader computeShader;
        private int mainKernal;
        
        public class PassData {
            public ComputeShader computeShader;
            public RenderTexture outputTexture;
            public MapWaterSettings settings;
            public float textureWorldX;
            public float pixelsPerUnit;
            public int kernal;
        }
        
        public void Setup(RenderTexture tilemapTex, RenderTexture sceneTex, RenderTexture outputTex, ComputeShader edge) {
            this.outputTex = outputTex;
            computeShader = edge;
            requiresIntermediateTexture = true;  // Required for RenderGraph passes that do texture reads
            mainKernal = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(mainKernal, Shader.PropertyToID("_TilemapTexture"), tilemapTex);
            computeShader.SetTexture(mainKernal, Shader.PropertyToID("_SceneTexture"), sceneTex);
            computeShader.SetTexture(mainKernal, Shader.PropertyToID("_OutputTexture"), outputTex);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            using var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData);
            
            passData.computeShader = computeShader;
            passData.outputTexture = outputTex;
            passData.kernal = mainKernal;
            passData.settings = waterSettings;
            
            float actualPixelsPerUnit = 48;
            float cameraX = 0;
            if (cameraData.camera.TryGetComponent(out PixelPerfectCamera pixelPerfectCamera)) {
                actualPixelsPerUnit = Screen.height / (pixelPerfectCamera.refResolutionY / (float)pixelPerfectCamera.assetsPPU);
                cameraX = pixelPerfectCamera.RoundToPixel(cameraData.worldSpaceCameraPos).x;
            }
            passData.pixelsPerUnit = actualPixelsPerUnit;
            passData.textureWorldX = cameraX * actualPixelsPerUnit;
            
            builder.AllowPassCulling(false);
            
            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                int threadGroupsX = Mathf.CeilToInt(data.outputTexture.width / 8f);
                int threadGroupsY = Mathf.CeilToInt(data.outputTexture.height / 8f);
                
                cmdBuffer.SetComputeIntParam(data.computeShader, "_Width", data.outputTexture.width);
                cmdBuffer.SetComputeIntParam(data.computeShader, "_Height", data.outputTexture.height);
                
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_sinInputInRadians", Time.time * data.settings.waveSpeed * Mathf.Deg2Rad);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_waveStride", data.settings.waveStride);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_waveHeight", data.settings.waveHeight);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_textureWorldX", data.textureWorldX);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_pixelsPerUnit", data.pixelsPerUnit);
                
                cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
            });
        }
        
    }
    
}
