using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class FinalBlitFeature : ScriptableRendererFeature  {
    
    public RenderPassEvent renderPassEvent;
    public ComputeShader pixelPerfectBlitShader;
    
    private static BlitPass blitPass;
    private static readonly int inputId = Shader.PropertyToID("_Input");
    private static readonly int resultId = Shader.PropertyToID("_Result");
    private static readonly int widthId = Shader.PropertyToID("_width");
    private static readonly int heightId = Shader.PropertyToID("_height");
    private static readonly int verticalSampleOffsetId = Shader.PropertyToID("_verticalSampleOffset");

    public override void Create() {
        blitPass = new(pixelPerfectBlitShader) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(blitPass);
    }
    
    private class BlitPass : ScriptableRenderPass {
        
        private ComputeShader blitShader;
        private int kernal;

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
                    
                    cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, inputId, data.inputTextureHandle);
                    cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, resultId, data.outputTextureHandle);
                
                    cmdBuffer.SetComputeIntParam(data.computeShader, widthId, Screen.width);
                    cmdBuffer.SetComputeIntParam(data.computeShader, heightId, Screen.height);
                    cmdBuffer.SetComputeIntParam(data.computeShader, verticalSampleOffsetId, RenderManager.verticalSamplingOffset);
                    cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
                });
            }
            
            renderGraph.AddBlitPass(outputTexture, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
        }
        
    }
    
}