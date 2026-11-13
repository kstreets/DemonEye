using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class FinalBlitFeature : ScriptableRendererFeature  {
    
    public RenderPassEvent renderPassEvent;
    public ComputeShader pixelPerfectBlitShader;
    
    private static BlitPass blitPass;

    public override void Create() {
        blitPass = new(pixelPerfectBlitShader) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(blitPass);
    }
    
    private class BlitPass : ScriptableRenderPass {
        
        private ComputeShader blitShader;
        private int kernal;
        private static readonly int[] sampleOffset = new int[2];

        private class PassData {
            public ComputeShader computeShader;
            public int kernal;
            public TextureHandle inputTextureHandle;
            public TextureHandle outputTextureHandle;
        }
        
        public BlitPass(ComputeShader blitShader) {
            this.blitShader = blitShader;
            kernal = blitShader.FindKernel("CSMain");
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            
            TextureHandle inputTexture = renderGraph.ImportTexture(RenderManager.GetRenderTexture(RenderManager.Texture.Scene));
            TextureHandle outputTexture = renderGraph.ImportTexture(RenderManager.GetRenderTexture(RenderManager.Texture.Final));
            
            if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera) {
                renderGraph.AddBlitPass(inputTexture, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
                return;
            }
            
            using (var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData)) {
                passData.computeShader = blitShader;
                passData.kernal = kernal;
                passData.inputTextureHandle = inputTexture;
                passData.outputTextureHandle = outputTexture;
            
                builder.UseTexture(inputTexture, AccessFlags.Read);
                builder.UseTexture(outputTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
            
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                    CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    int threadGroupsX = Mathf.CeilToInt(Screen.width / 8f);
                    int threadGroupsY = Mathf.CeilToInt(Screen.height / 8f);
                    
                    cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, ShaderIds.input, data.inputTextureHandle);
                    cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, ShaderIds.result, data.outputTextureHandle);
                
                    cmdBuffer.SetComputeIntParam(data.computeShader, ShaderIds.width, Screen.width);
                    cmdBuffer.SetComputeIntParam(data.computeShader, ShaderIds.height, Screen.height);
                    // Camera shake moves the view by +offset, so the image samples further along by the same amount
                    int margin = RenderManager.horizontalSamplingOffset;
                    float screenPixelsPerWorldUnit = RenderManager.pixelsPerTexel * RenderManager.pixelsPerUnit;
                    int shakeX = Mathf.Clamp(Mathf.RoundToInt(RenderManager.shakeOffset.x * screenPixelsPerWorldUnit), -margin, margin);
                    int shakeY = Mathf.Clamp(Mathf.RoundToInt(RenderManager.shakeOffset.y * screenPixelsPerWorldUnit), -RenderManager.verticalSamplingOffset, RenderManager.verticalSamplingOffset);
                    sampleOffset[0] = margin + shakeX;
                    sampleOffset[1] = RenderManager.verticalSamplingOffset + shakeY;
                    cmdBuffer.SetComputeIntParams(data.computeShader, ShaderIds.sampleOffset, sampleOffset);

                    // Zooming in samples closer to the center. Never zooms out, there's nothing rendered past the margins.
                    float punchZoom = Mathf.Max(1f, RenderManager.punchZoom);
                    cmdBuffer.SetComputeFloatParam(data.computeShader, ShaderIds.inverseZoom, 1f / punchZoom);
                    cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
                });
            }
            
            renderGraph.AddBlitPass(outputTexture, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
        }
        
    }
    
}