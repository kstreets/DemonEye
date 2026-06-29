using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class FinalBlitFeature : ScriptableRendererFeature  {
    
    public RenderPassEvent renderPassEvent;
    public RenderTexture inputTexture;
    public RenderTexture outputTexture;
    public ComputeShader pixelPerfectBlitShader;
    private BlitPass blitPass;
    
    public override void Create() {
        blitPass = new() { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        blitPass.Setup(inputTexture, outputTexture, pixelPerfectBlitShader);
        renderer.EnqueuePass(blitPass);
    }
    
    private class BlitPass : ScriptableRenderPass {
        
        private ComputeShader pixelPerfectBlitShader;
        private int kernal;
        private RTHandle inputTextureHandle;
        private RTHandle outputTextureHandle;

        private class PassData {
            public ComputeShader computeShader;
            public int kernal;
            public int width;
            public int height;
            public int verticalSampleOffset;
        }
        
        public void Setup(RenderTexture inputTexture, RenderTexture outputTexture, ComputeShader pixelPerfectBlitShader) {
            this.pixelPerfectBlitShader = pixelPerfectBlitShader;
            kernal = pixelPerfectBlitShader.FindKernel("CSMain");
            inputTextureHandle = RTHandles.Alloc(inputTexture);
            outputTextureHandle = RTHandles.Alloc(outputTexture);
            pixelPerfectBlitShader.SetTexture(kernal, Shader.PropertyToID("_Input"), inputTexture);
            pixelPerfectBlitShader.SetTexture(kernal, Shader.PropertyToID("_Result"), outputTexture);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            
            if (cameraData.isSceneViewCamera) {
                TextureHandle inputTexture = renderGraph.ImportTexture(inputTextureHandle);
                renderGraph.AddBlitPass(inputTexture, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
                return;
            }
            
            using (var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData)) {
                passData.computeShader = pixelPerfectBlitShader;
                passData.kernal = kernal;
                
                passData.width = 1920;
                passData.height = 1080;
                passData.verticalSampleOffset = (inputTextureHandle.rt.height - passData.height) / 2;
            
                builder.AllowPassCulling(false);
            
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                    CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    int threadGroupsX = Mathf.CeilToInt(data.width / 8f);
                    int threadGroupsY = Mathf.CeilToInt(data.height / 8f);
                
                    cmdBuffer.SetComputeIntParam(data.computeShader, "_Width", data.width);
                    cmdBuffer.SetComputeIntParam(data.computeShader, "_Height", data.height);
                    cmdBuffer.SetComputeIntParam(data.computeShader, "_VerticalSampleOffset", data.verticalSampleOffset);
                    cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
                });
            }
            
            TextureHandle outputHandle = renderGraph.ImportTexture(outputTextureHandle); 
            renderGraph.AddBlitPass(outputHandle, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
        }
        
    }
    
}
