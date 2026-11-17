using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders
{
    /**
     * <summary>The hair depth's render-graph pass: the opaque renderers' hair depth pass drawn into a cleared target of
     * one channel (one over the eye depth; 0 is no hair) with its own depth buffer, then set as the global texture with
     * its switch on. The depth buffer is 16-bit with no stencil: it only picks the nearest hair, whose depth the colour
     * target holds in full.</summary>
     */
    internal sealed class AnimeHairDepthPass : ScriptableRenderPass
    {
        #region Private Fields

        private const int HalfResolution = 2;

        private ShaderTagId _tag;
        private ProfilingSampler _sampler;
        private int _texture;
        private int _on;
        private int _size;
        private bool _full;
        private bool _ready;

        #endregion

        #region Public API

        /** <summary>Resolves the tag and the texture's id; on the main thread, before recording.</summary> */
        public void Setup(bool fullResolution)
        {
            if (!_ready)
            {
                _tag = new ShaderTagId(AnimeShaderIds.HairDepthLightMode);
                _sampler = new ProfilingSampler("Anime Hair Depth");
                _texture = AnimeShaderIds.HairDepth.Texture;
                _on = AnimeShaderIds.HairDepth.On;
                _size = AnimeShaderIds.HairDepth.Size;
                _ready = true;
            }
            _full = fullResolution;
        }

        /** <inheritdoc /> */
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!_ready)
                return;
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            RenderTextureDescriptor target = cameraData.cameraTargetDescriptor;
            int divisor = _full ? 1 : HalfResolution;
            target.width = Mathf.Max(1, target.width / divisor);
            target.height = Mathf.Max(1, target.height / divisor);
            target.msaaSamples = 1;
            target.depthStencilFormat = GraphicsFormat.None;
            target.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Render)
                ? GraphicsFormat.R16_SFloat
                : GraphicsFormat.R32_SFloat;
            RenderTextureDescriptor depth = target;
            depth.graphicsFormat = GraphicsFormat.None;
            depth.depthStencilFormat = GraphicsFormat.D16_UNorm;
            TextureHandle colour = UniversalRenderer.CreateRenderGraphTexture(renderGraph, target, AnimeShaderIds.HairDepth.TextureName, true, FilterMode.Bilinear);
            TextureHandle depthBuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depth, "Anime Hair Depth Buffer", true, FilterMode.Point);

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(_sampler.name, out PassData passData, _sampler);
            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(_tag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
            FilteringSettings filtering = new(RenderQueueRange.opaque, cameraData.camera.cullingMask);
            passData.Renderers = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
            passData.On = _on;
            passData.SizeId = _size;
            passData.Size = new Vector4(target.width, target.height, 1f / target.width, 1f / target.height);

            builder.UseRendererList(passData.Renderers);
            builder.SetRenderAttachment(colour, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(depthBuffer, AccessFlags.Write);
            builder.SetGlobalTextureAfterPass(colour, _texture);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.Renderers);
                context.cmd.SetGlobalFloat(data.On, 1f);
                context.cmd.SetGlobalVector(data.SizeId, data.Size);
            });
        }

        #endregion

        #region Helpers

        private sealed class PassData
        {
            public RendererListHandle Renderers;
            public int On;
            public int SizeId;
            public Vector4 Size;
        }

        #endregion
    }
}
