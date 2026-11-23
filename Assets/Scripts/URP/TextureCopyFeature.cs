using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Copies one of the RenderManager textures into another so a later pass can sample what was rendered before it
public class TextureCopyFeature : ScriptableRendererFeature {

    public RenderPassEvent renderPassEvent;
    public RenderManager.Texture source;
    public RenderManager.Texture destination;
    CopyPass copyPass;

    public override void Create() {
        copyPass = new(source, destination) { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(copyPass);
    }

    class CopyPass : ScriptableRenderPass {
        RenderManager.Texture source;
        RenderManager.Texture destination;

        class PassData {
            public TextureHandle source;
            public TextureHandle destination;
        }

        public CopyPass(RenderManager.Texture source, RenderManager.Texture destination) {
            this.source = source;
            this.destination = destination;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            using var builder = renderGraph.AddUnsafePass<PassData>("Copy Texture", out var passData);

            passData.source = renderGraph.ImportTexture(RenderManager.GetRenderTexture(source));
            passData.destination = renderGraph.ImportTexture(RenderManager.GetRenderTexture(destination));
            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.destination, AccessFlags.Write);
            // Nothing in the graph reads the destination, shaders sample it as a global texture
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) => {
                CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd).CopyTexture(data.source, data.destination);
            });
        }
    }

}
