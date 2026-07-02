using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class WaterFeature : ScriptableRendererFeature {
    
    public RenderPassEvent renderPassEvent;
    public ComputeShader computeShader;
    
    public static MapWaterSettings waterSettings;
    private static WaterPass waterPass;
    
    private static readonly int worldXInPixelsId = Shader.PropertyToID("_camWorldXInPixels");
    private static readonly int pixelsPerUnitId = Shader.PropertyToID("_pixelsPerUnit");
    private static readonly int waveStrideId = Shader.PropertyToID("_waveStride");
    private static readonly int startReflectionFadeId = Shader.PropertyToID("_startReflectionFade");
    private static readonly int endReflectionFadeId = Shader.PropertyToID("_endReflectionFade");
    private static readonly int waterFillColorId = Shader.PropertyToID("_waterFillColor");
    private static readonly int offsetInRadiansId = Shader.PropertyToID("_sinOffsetInRadians");
    private static readonly int heightInPixelsId = Shader.PropertyToID("_waveHeightInPixels");
    private static readonly int offsetInPixelsId = Shader.PropertyToID("_reflectionOffsetInPixels");
    private static readonly int lengthInPixelsId = Shader.PropertyToID("_reflectionLengthInPixels");
    private static readonly int lineLengthInPixelsId = Shader.PropertyToID("_waterLineLengthInPixels");
    private static readonly int heightId = Shader.PropertyToID("_height");
    private static readonly int widthId = Shader.PropertyToID("_width");
    
    private static readonly int tilemapTextureId = Shader.PropertyToID("_TilemapTexture");
    private static readonly int sceneTextureId = Shader.PropertyToID("_SceneTexture");
    private static readonly int outputTextureId = Shader.PropertyToID("_OutputTexture");

    public override void Create() {
        waterSettings = new();
        waterPass = new(computeShader) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(waterPass);
    }
    
    public class WaterPass : ScriptableRenderPass {
        private ComputeShader computeShader;
        private int mainKernal;
        
        public class PassData {
            public ComputeShader computeShader;
            public int kernal;
            public TextureHandle tilemapRT;
            public TextureHandle sceneRT;
            public TextureHandle waterRT;
        }
        
        public WaterPass(ComputeShader computeShader) {
            this.computeShader = computeShader;
            mainKernal = this.computeShader.FindKernel("CSMain");
            requiresIntermediateTexture = true;  // Required for RenderGraph passes that do texture reads
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            using var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData);
            
            passData.computeShader = computeShader;
            passData.kernal = mainKernal;
            
            passData.tilemapRT = renderGraph.ImportTexture(RenderManager.GetRenderTexture(RenderManager.Texture.Tilemap));
            passData.sceneRT = renderGraph.ImportTexture(RenderManager.GetRenderTexture(RenderManager.Texture.Scene));
            passData.waterRT = renderGraph.ImportTexture(RenderManager.GetRenderTexture(RenderManager.Texture.Water));
            
            builder.UseTexture(passData.tilemapRT, AccessFlags.Read);
            builder.UseTexture(passData.sceneRT, AccessFlags.Read);
            builder.UseTexture(passData.waterRT, AccessFlags.Write);
            
            builder.AllowPassCulling(false);
            
            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, tilemapTextureId, data.tilemapRT);
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, sceneTextureId, data.sceneRT);
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, outputTextureId, data.waterRT);
                
                int width = RenderManager.offScreenRenderingSize.x;
                int height = RenderManager.offScreenRenderingSize.y;
                
                int threadGroupsX = Mathf.CeilToInt(width / 8f);
                int threadGroupsY = Mathf.CeilToInt(height / 8f);
                
                cmdBuffer.SetComputeIntParam(data.computeShader, widthId, width);
                cmdBuffer.SetComputeIntParam(data.computeShader, heightId, height);
                
                int waterLineLengthInPixels = waterSettings.waterLineLength * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeIntParam(data.computeShader, lineLengthInPixelsId, waterLineLengthInPixels);
                
                int reflectionLengthInPixels = waterSettings.reflectionLength * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeIntParam(data.computeShader, lengthInPixelsId, reflectionLengthInPixels);
                
                int reflectionOffsetInPixels = waterSettings.reflectionOffset * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeFloatParam(data.computeShader, offsetInPixelsId, reflectionOffsetInPixels);
                
                float waveHeightInPixels = waterSettings.waveHeight * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeFloatParam(data.computeShader, heightInPixelsId, waveHeightInPixels);
                
                float sinOffsetInRadians = Time.time * waterSettings.waveSpeed * Mathf.Deg2Rad;
                cmdBuffer.SetComputeFloatParam(data.computeShader, offsetInRadiansId, sinOffsetInRadians);
                
                float camWorldXInPixels = RenderManager.cameraPosition.x * RenderManager.screenPixelsPerUnit;
                cmdBuffer.SetComputeFloatParam(data.computeShader, worldXInPixelsId, camWorldXInPixels);
                cmdBuffer.SetComputeFloatParam(data.computeShader, pixelsPerUnitId, RenderManager.screenPixelsPerUnit);
                
                cmdBuffer.SetComputeFloatParam(data.computeShader, waveStrideId, waterSettings.waveStride);
                cmdBuffer.SetComputeFloatParam(data.computeShader, startReflectionFadeId, waterSettings.startReflectionFade);
                cmdBuffer.SetComputeFloatParam(data.computeShader, endReflectionFadeId, waterSettings.endReflectionFade);
                cmdBuffer.SetComputeVectorParam(data.computeShader, waterFillColorId, waterSettings.waterFillColor.linear);
                
                cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
            });
        }
        
    }
    
}
