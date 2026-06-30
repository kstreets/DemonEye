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
    
    public static void BindRenderTextures() {
        blitPass.BindRenderTextures();
    }
    
    private class BlitPass : ScriptableRenderPass {
        
        private ComputeShader pixelPerfectBlitShader;
        private int kernal;
        private RTHandle inputTextureHandle;
        private RTHandle outputTextureHandle;

        private class PassData {
            public ComputeShader computeShader;
            public int kernal;
        }
        
        public BlitPass(ComputeShader pixelPerfectBlitShader) {
            this.pixelPerfectBlitShader = pixelPerfectBlitShader;
            kernal = pixelPerfectBlitShader.FindKernel("CSMain");
        }
        
        public void BindRenderTextures() {
            pixelPerfectBlitShader.SetTexture(kernal, Shader.PropertyToID("_Input"), RenderManager.sceneRT);
            pixelPerfectBlitShader.SetTexture(kernal, Shader.PropertyToID("_Result"), RenderManager.finalOutputRT);
            inputTextureHandle = RenderManager.sceneRT;
            outputTextureHandle = RenderManager.finalOutputRT;
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            
            if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera) {
                // For a single frame when coming out of playmode this can be null
                if (inputTextureHandle != null && inputTextureHandle.rt != null) {
                    TextureHandle inputTexture = renderGraph.ImportTexture(inputTextureHandle);
                    renderGraph.AddBlitPass(inputTexture, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
                }
                return;
            }
            
            using (var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData)) {
                passData.computeShader = pixelPerfectBlitShader;
                passData.kernal = kernal;
            
                builder.AllowPassCulling(false);
            
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                    CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    int threadGroupsX = Mathf.CeilToInt(Screen.width / 8f);
                    int threadGroupsY = Mathf.CeilToInt(Screen.height / 8f);
                
                    cmdBuffer.SetComputeIntParam(data.computeShader, "_Width", Screen.width);
                    cmdBuffer.SetComputeIntParam(data.computeShader, "_Height", Screen.height);
                    cmdBuffer.SetComputeIntParam(data.computeShader, "_VerticalSampleOffset", RenderManager.verticalSamplingOffset);
                    cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
                });
            }
            
            TextureHandle outputHandle = renderGraph.ImportTexture(outputTextureHandle); 
            renderGraph.AddBlitPass(outputHandle, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
        }
        
    }
    
}