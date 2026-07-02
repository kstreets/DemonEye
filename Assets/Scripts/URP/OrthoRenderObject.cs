using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Scripting.APIUpdating;

public class OrthoRenderFeature : ScriptableRendererFeature {
    
    public RenderPassEvent renderPassEvent;
    public string[] shaderTags;
    public RenderQueueType renderQueueType; 
    public LayerMask layerMask;
    public bool clearRenderTarget;
    public RenderManager.Texture texture;
    private OrthoRenderObject orthoRender;

    public override void Create() {
        orthoRender = new(renderPassEvent, shaderTags, renderQueueType, layerMask.value, clearRenderTarget, texture);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(orthoRender);
    }
    
    /// <summary>
    /// The scriptable render pass used with the render objects renderer feature.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal")]
    public class OrthoRenderObject : ScriptableRenderPass {
        public readonly Material overrideMaterial;
        public readonly int overrideMaterialPassIndex;
        public readonly Shader overrideShader;
        public readonly int overrideShaderPassIndex;
        
        private RenderQueueType renderQueueType;
        private FilteringSettings m_FilteringSettings;
        private RenderManager.Texture texture;
        private bool clearRenderTarget;
        private List<ShaderTagId> m_ShaderTagIdList = new();
        private RenderStateBlock m_RenderStateBlock;

        public OrthoRenderObject(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, bool clearRenderTarget, RenderManager.Texture texture) {
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            this.overrideShader = null;
            this.overrideShaderPassIndex = 0;
            this.texture = texture;
            this.clearRenderTarget = clearRenderTarget;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent) ? RenderQueueRange.transparent : RenderQueueRange.opaque;
            m_FilteringSettings = new(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0) {
                foreach (var tag in shaderTags)
                    m_ShaderTagIdList.Add(new(tag));
            }
            else {
                m_ShaderTagIdList.Add(new("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new("UniversalForward"));
                m_ShaderTagIdList.Add(new("UniversalForwardOnly"));
            }

            m_RenderStateBlock = new (RenderStateMask.Nothing);
        }

        private class PassData {
            public bool clearRenderTarget;
            public TextureHandle color;
            public int viewportHeight;
            public RendererListHandle rendererListHdl;
            public UniversalCameraData cameraData;
            // Required for code sharing purpose between RG and non-RG.
            public RendererList rendererList;
        }

        private void InitRendererLists(UniversalRenderingData renderingData, UniversalLightData lightData,
            ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent) ? SortingCriteria.CommonTransparent : passData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, passData.cameraData, lightData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;
            drawingSettings.overrideShader = overrideShader;
            drawingSettings.overrideShaderPassIndex = overrideShaderPassIndex;

            if (useRenderGraph) {
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
            }
            else {
                CreateRendererListWithRenderStateBlock(context, ref renderingData.cullResults, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererList);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);
            
            RTHandle renderTextureHandle = RenderManager.GetRenderTexture(texture);
            TextureHandle targetColorTexture = renderGraph.ImportTexture(renderTextureHandle);
            builder.SetRenderAttachment(targetColorTexture, 0, AccessFlags.Write);

            passData.color = targetColorTexture;
            passData.viewportHeight = renderTextureHandle.referenceSize.y;
            passData.clearRenderTarget = clearRenderTarget;
            passData.cameraData = cameraData;
                
            InitRendererLists(renderingData, lightData, ref passData, default, renderGraph, true);
            builder.UseRendererList(passData.rendererListHdl);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext rgContext) => {
                bool isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                ExecutePass(data, rgContext.cmd, data.rendererListHdl, isYFlipped);
            });
        }
        
        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped) {
            bool isGameCamera = passData.cameraData.isGameCamera;
            Camera camera = passData.cameraData.camera;
            
            if (isGameCamera) {
                Rect viewPortRect = new(0, 0, Screen.width, passData.viewportHeight);
                float extraScreenPixels = viewPortRect.height - Screen.height;
                float extraWorldUnits = extraScreenPixels / RenderManager.pixelsPerTexel / RenderManager.pixelsPerUnit;
                float halfExtraWorldUnits = extraWorldUnits * 0.5f;
            
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(
                    -camera.orthographicSize * camera.aspect,
                    camera.orthographicSize * camera.aspect,
                    -camera.orthographicSize - halfExtraWorldUnits,
                    camera.orthographicSize + halfExtraWorldUnits,
                    camera.nearClipPlane,
                    camera.farClipPlane
                );
                projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, isYFlipped);
            
                cmd.SetViewport(viewPortRect);
                
                Matrix4x4 viewMatrix = passData.cameraData.GetViewMatrix();
                RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
            }

            if (passData.clearRenderTarget) {
                cmd.ClearRenderTarget(RTClearFlags.All, camera.backgroundColor, 0, 0);
            }
            cmd.DrawRendererList(rendererList);

            if (isGameCamera) {
                cmd.SetViewport(new(0, 0, Screen.width, Screen.height));
            }
            RenderingUtils.SetViewAndProjectionMatrices(cmd, passData.cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(passData.cameraData.GetProjectionMatrix(), isYFlipped), false);
        }
        
        private static void CreateRendererListWithRenderStateBlock(ScriptableRenderContext context, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererList rl)
        {
            RendererListParams param = new RendererListParams();
            unsafe
            {
                // Taking references to stack variables in the current function does not require any pinning (as long as you stay within the scope)
                // so we can safely alias it as a native array
                RenderStateBlock* rsbPtr = &rsb;
                var stateBlocks = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<RenderStateBlock>(rsbPtr, 1, Allocator.None);

                var shaderTag = ShaderTagId.none;
                var tagValues = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ShaderTagId>(&shaderTag, 1, Allocator.None);

                // Inside CreateRendererList (below), we pass the NativeArrays to C++ by calling GetUnsafeReadOnlyPtr
                // This will check read access but NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray does not set up the SafetyHandle (by design) so create/add it here
                // NOTE: we explicitly share the handle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safetyHandle = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref stateBlocks, safetyHandle);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref tagValues, safetyHandle);
#endif

                // Create & schedule the RL
                param = new RendererListParams(cullResults, ds, fs)
                {
                    tagValues = tagValues,
                    stateBlocks = stateBlocks

                };

                rl = context.CreateRendererList(ref param);

                // we need to explicitly release the SafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(safetyHandle);
#endif
            }
        }
        
        static ShaderTagId[]      s_ShaderTagValues   = new ShaderTagId[1];
        static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];
        // Create a RendererList using a RenderStateBlock override is quite common so we have this optimized utility function for it
        private static void CreateRendererListWithRenderStateBlock(RenderGraph renderGraph, ref CullingResults cullResults, DrawingSettings ds, FilteringSettings fs, RenderStateBlock rsb, ref RendererListHandle rl) {
            s_ShaderTagValues[0] = ShaderTagId.none;
            s_RenderStateBlocks[0] = rsb;
            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(cullResults, ds, fs)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };
            rl = renderGraph.CreateRendererList(param);
        }
    }
}
