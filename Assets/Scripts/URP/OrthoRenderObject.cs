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
    public Vector3 cameraViewOffset;
    public RenderTexture targetTexture;
    private OrthoRenderObject orthoRender;

    public override void Create() {
        orthoRender = new(renderPassEvent, shaderTags, renderQueueType, layerMask.value, clearRenderTarget, cameraViewOffset, targetTexture);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(orthoRender);
    }
    
    /// <summary>
    /// The scriptable render pass used with the render objects renderer feature.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal")]
    public class OrthoRenderObject : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        bool clearRenderTarget;
        Vector3 camViewOffset;
        RenderTexture targetTexture;
        RTHandle targetTextureHandle;
        PixelPerfectCamera pixelPerfectCam;

        /// <summary>
        /// The override material to use.
        /// </summary>
        public Material overrideMaterial { get; set; }

        /// <summary>
        /// The pass index to use with the override material.
        /// </summary>
        public int overrideMaterialPassIndex { get; set; }

        /// <summary>
        /// The override shader to use.
        /// </summary>
        public Shader overrideShader { get; set; }

        /// <summary>
        /// The pass index to use with the override shader.
        /// </summary>
        public int overrideShaderPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private PassData m_PassData;

        /// <summary>
        /// Sets the write and comparison function for depth.
        /// </summary>
        /// <param name="writeEnabled">Sets whether it should write to depth or not.</param>
        /// <param name="function">The depth comparison function to use.</param>
        public void SetDepthState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Sets up the stencil settings for the pass.
        /// </summary>
        /// <param name="reference">The stencil reference value.</param>
        /// <param name="compareFunction">The comparison function to use.</param>
        /// <param name="passOp">The stencil operation to use when the stencil test passes.</param>
        /// <param name="failOp">The stencil operation to use when the stencil test fails.</param>
        /// <param name="zFailOp">The stencil operation to use when the stencil test fails because of depth.</param>
        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }

        RenderStateBlock m_RenderStateBlock;

        /// <summary>
        /// The constructor for render objects pass.
        /// </summary>
        /// <param name="renderPassEvent">Controls when the render pass executes.</param>
        /// <param name="shaderTags">List of shader tags to render with.</param>
        /// <param name="renderQueueType">The queue type for the objects to render.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        public OrthoRenderObject(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, 
            int layerMask, bool clearRenderTarget, Vector3 camViewOffset, RenderTexture targetTexture)            
        {
            Init(renderPassEvent, shaderTags, renderQueueType, layerMask, clearRenderTarget, camViewOffset, targetTexture);
        }

        public void Init(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, 
            int layerMask, bool clearRenderTarget, Vector3 camViewOffset, RenderTexture targetTexture)
        {
            m_PassData = new PassData();

            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            this.overrideShader = null;
            this.overrideShaderPassIndex = 0;
            this.clearRenderTarget = clearRenderTarget;
            this.camViewOffset = camViewOffset;
            this.targetTexture = targetTexture;
            this.targetTextureHandle = RTHandles.Alloc(targetTexture);
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                                                ? RenderQueueRange.transparent
                                                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var tag in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(tag));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd, RendererList rendererList, bool isYFlipped)
        {
            Camera camera = passData.cameraData.camera;
            
            if (!passData.cameraData.isSceneViewCamera) {
                Rect viewPortRect = new(0, 0, 1920, 1368);
                float pixelScale = 1080f / passData.referenceResolution;
                float extraScreenPixels = viewPortRect.height - 1080f;
                float extraWorldUnits = extraScreenPixels / pixelScale / passData.assetsPerPixel;
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
                Vector4 camTranslation = viewMatrix.GetColumn(3);
                Vector4 customTranslation = new(passData.camViewOffset.x, passData.camViewOffset.y, passData.camViewOffset.z, 0f);
                viewMatrix.SetColumn(3, camTranslation + customTranslation);
                RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
            }

            if (passData.clearRenderTarget) {
                cmd.ClearRenderTarget(RTClearFlags.All, camera.backgroundColor, 0, 0);
            }
            cmd.DrawRendererList(rendererList);

            if (!passData.cameraData.isSceneViewCamera) {
                cmd.SetViewport(new(0, 0, 1920, 1080));
            }
            RenderingUtils.SetViewAndProjectionMatrices(cmd, passData.cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(passData.cameraData.GetProjectionMatrix(0), isYFlipped), false);
        }

        private class PassData
        {
            public bool clearRenderTarget;
            public Vector3 camViewOffset;

            public TextureHandle color;
            public RendererListHandle rendererListHdl;

            public UniversalCameraData cameraData;
            public int assetsPerPixel;
            public int referenceResolution;

            // Required for code sharing purpose between RG and non-RG.
            public RendererList rendererList;
        }

        private void InitPassData(UniversalCameraData cameraData, PixelPerfectCamera pixelPerfectCam, ref PassData passData)
        {
            if (cameraData.isSceneViewCamera || pixelPerfectCam == null) {
                passData.assetsPerPixel = 48;
                passData.referenceResolution = 180;
            }
            else {
                passData.assetsPerPixel = pixelPerfectCam.assetsPPU;
                passData.referenceResolution = pixelPerfectCam.refResolutionX;
            }
            passData.clearRenderTarget = clearRenderTarget;
            passData.camViewOffset = camViewOffset;
            passData.cameraData = cameraData;
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

            if (useRenderGraph)
            {
                CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererListHdl);
            }
            else
            {
                CreateRendererListWithRenderStateBlock(context, ref renderingData.cullResults, drawingSettings, m_FilteringSettings, m_RenderStateBlock, ref passData.rendererList);
            }
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            
            TextureHandle targetColorTexture = renderGraph.ImportTexture(targetTextureHandle);
            
            if (pixelPerfectCam == null) {
                cameraData.camera.TryGetComponent(out pixelPerfectCam);
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                InitPassData(cameraData, pixelPerfectCam, ref passData);
                passData.color = targetColorTexture;
                builder.SetRenderAttachment(targetColorTexture, 0, AccessFlags.Write);

                InitRendererLists(renderingData, lightData, ref passData, default, renderGraph, true);
                builder.UseRendererList(passData.rendererListHdl);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var isYFlipped = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.color);
                    ExecutePass(data, rgContext.cmd, data.rendererListHdl, isYFlipped);
                });
            }
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
