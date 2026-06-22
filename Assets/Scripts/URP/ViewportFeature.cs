using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class ViewportFeature : ScriptableRendererFeature {
    
    public RenderTexture rt;
    public RenderPassEvent renderPassEvent;
    private ViewportPass viewportPass;
    
    public override void Create() {
        viewportPass = new() {
            renderPassEvent = renderPassEvent,
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        viewportPass.Setup(rt);
        renderer.EnqueuePass(viewportPass);
    }
    
    private class ViewportPass :  ScriptableRenderPass {
        
        private RenderTexture rt;
        
        public void Setup(RenderTexture rt) {
            this.rt = rt;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            using var builder = renderGraph.AddUnsafePass<PassData>("SetViewport", out var passData);
        
            passData.rt = rt;
            builder.AllowPassCulling(false);
        
            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) => {
                ctx.cmd.SetViewport(new Rect(0, 0, data.rt.width, data.rt.height));
            });
        }
    
        class PassData {
            public RenderTexture rt;
        }
        
    }
    
    
}
