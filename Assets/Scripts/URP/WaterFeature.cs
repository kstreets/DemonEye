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
    
    public static void BindRenderTextures() {
        waterPass.BindRenderTextures();
    }
    
    public class WaterPass : ScriptableRenderPass {
        private ComputeShader computeShader;
        private int mainKernal;
        
        public class PassData {
            public ComputeShader computeShader;
            public float camWorldXInPixels;
            public float pixelsPerUnit;
            public int kernal;
        }
        
        public WaterPass(ComputeShader computeShader) {
            this.computeShader = computeShader;
            requiresIntermediateTexture = true;  // Required for RenderGraph passes that do texture reads
            mainKernal = this.computeShader.FindKernel("CSMain");
        }
        
        public void BindRenderTextures() {
            this.computeShader.SetTexture(mainKernal, Shader.PropertyToID("_TilemapTexture"), RenderManager.tilemapRT);
            this.computeShader.SetTexture(mainKernal, Shader.PropertyToID("_SceneTexture"), RenderManager.sceneRT);
            this.computeShader.SetTexture(mainKernal, Shader.PropertyToID("_OutputTexture"), RenderManager.waterRT);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            using var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData);
            
            passData.computeShader = computeShader;
            passData.kernal = mainKernal;
            
            passData.pixelsPerUnit = RenderManager.screenPixelsPerUnit;
            passData.camWorldXInPixels = RenderManager.cameraPosition.x * RenderManager.screenPixelsPerUnit;
            
            builder.AllowPassCulling(false);
            
            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                
                int width = RenderManager.offScreenRenderingSize.x;
                int height = RenderManager.offScreenRenderingSize.y;
                
                int threadGroupsX = Mathf.CeilToInt(width / 8f);
                int threadGroupsY = Mathf.CeilToInt(height / 8f);
                
                cmdBuffer.SetComputeIntParam(data.computeShader, "_width", width);
                cmdBuffer.SetComputeIntParam(data.computeShader, "_height", height);
                
                int waterLineLengthInPixels = waterSettings.waterLineLength * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeIntParam(data.computeShader, "_waterLineLengthInPixels", waterLineLengthInPixels);
                
                int reflectionLengthInPixels = waterSettings.reflectionLength * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeIntParam(data.computeShader, "_reflectionLengthInPixels", reflectionLengthInPixels);
                
                float waveHeightInPixels = waterSettings.waveHeight * RenderManager.pixelsPerTexel;
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_waveHeightInPixels", waveHeightInPixels);
                
                float sinOffsetInRadians = Time.time * waterSettings.waveSpeed * Mathf.Deg2Rad;
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_sinOffsetInRadians", sinOffsetInRadians);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_waveStride", waterSettings.waveStride);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_startReflectionFade", waterSettings.startReflectionFade);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_endReflectionFade", waterSettings.endReflectionFade);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_camWorldXInPixels", data.camWorldXInPixels);
                cmdBuffer.SetComputeFloatParam(data.computeShader, "_pixelsPerUnit", data.pixelsPerUnit);
                
                cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
            });
        }
        
    }
    
}
