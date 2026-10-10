using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class WaterFeature : ScriptableRendererFeature {
    
    public RenderPassEvent renderPassEvent;
    public ComputeShader computeShader;
    
    public static MapWaterSettings waterSettings;
    private static WaterPass waterPass;
    
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
                
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, ShaderIds.tilemapTexture, data.tilemapRT);
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, ShaderIds.sceneTexture, data.sceneRT);
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, ShaderIds.outputTexture, data.waterRT);
                
                int width = RenderManager.offScreenRenderingSize.x;
                int height = RenderManager.offScreenRenderingSize.y;
                
                int threadGroupsX = Mathf.Max(1, Mathf.CeilToInt(width / 8f));
                int threadGroupsY = Mathf.Max(1, Mathf.CeilToInt(height / 8f));
                
                cmdBuffer.SetComputeIntParam(data.computeShader, ShaderIds.width, width);
                cmdBuffer.SetComputeIntParam(data.computeShader, ShaderIds.height, height);
                
                int waterLineLengthInPixels = waterSettings.waterLineLength * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeIntParam(data.computeShader, ShaderIds.waterLineLengthInPixels, waterLineLengthInPixels);
                
                int reflectionLengthInPixels = waterSettings.reflectionLength * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeIntParam(data.computeShader, ShaderIds.reflectionLengthInPixels, reflectionLengthInPixels);
                
                int reflectionOffsetInPixels = waterSettings.reflectionOffset * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.reflectionOffsetInPixels, reflectionOffsetInPixels);
                
                float waveHeightInPixels = waterSettings.waveHeight * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.waveHeightInPixels, waveHeightInPixels);
                
                float sinOffsetInRadians = Time.time * waterSettings.waveSpeed * Mathf.Deg2Rad;
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.sinOffsetInRadians, sinOffsetInRadians);
                
                float camWorldXInPixels = RenderManager.cameraPosition.x * RenderManager.screenPixelsPerUnit;
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.camWorldXInPixels, camWorldXInPixels);
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.pixelsPerUnit, RenderManager.screenPixelsPerUnit);
                
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.waveStride, waterSettings.waveStride);
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.startReflectionFade, waterSettings.startReflectionFade);
                cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.endReflectionFade, waterSettings.endReflectionFade);
                cmdBuffer.SetComputeVectorParam(data.computeShader, ShaderIds.waterFillColor, waterSettings.waterFillColor.linear);
                
                cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
            });
        }
        
    }
    
}
