using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class RenderTargetFeature : ScriptableRendererFeature {
    
    public RenderPassEvent renderPassEvent;
    public RenderTexture destination;
    public ComputeShader computeShader;
    private CapturePass capturePass;

    public override void Create() {
        capturePass = new() {
            renderPassEvent = renderPassEvent,
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (destination == null) return;
        capturePass.Setup(destination, computeShader);
        renderer.EnqueuePass(capturePass);
    }

    public class CapturePass : ScriptableRenderPass {
        
        private RenderTexture target;
        private ComputeShader computeShader;
        private int mainKernal;
        
        public class PassData {
            public ComputeShader computeShader;
            public RenderTexture target;
            public TextureHandle colorTexture;
            public int kernal;
        }
        
        public void Setup(RenderTexture dest, ComputeShader edge) {
            target = dest;
            computeShader = edge;
            requiresIntermediateTexture = true;  // Required for RenderGraph passes that do texture reads
            mainKernal = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(mainKernal, Shader.PropertyToID("_OutputTexture"), target);
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            using var builder = renderGraph.AddUnsafePass<PassData>("Capture Camera Output", out var passData);
            
            passData.computeShader = computeShader;
            passData.target = target;
            passData.colorTexture = resourceData.activeColorTexture;
            passData.kernal = mainKernal;
            
            builder.UseTexture(passData.colorTexture);
            builder.AllowPassCulling(false);
            
            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                CommandBuffer cmdBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                int threadGroupsX = Mathf.CeilToInt(data.target.width / 8f);
                int threadGroupsY = Mathf.CeilToInt(data.target.height / 8f);
                
                cmdBuffer.SetComputeIntParam(data.computeShader, "_Width", data.target.width);
                cmdBuffer.SetComputeIntParam(data.computeShader, "_Height", data.target.height);
                cmdBuffer.SetComputeTextureParam(data.computeShader, data.kernal, Shader.PropertyToID("_InputTexture"), data.colorTexture);
                cmdBuffer.DispatchCompute(data.computeShader, data.kernal, threadGroupsX, threadGroupsY, 1);
            });
        }
        
    }
    
}
