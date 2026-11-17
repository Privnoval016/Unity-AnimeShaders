using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders
{
    /**
     * <summary>The eyes over their fringe: after the opaques, every <see cref="AnimeShaderIds.EyesOverHairLightMode" />
     * pass in view drawn over the camera colour, where the eyes marked the stencil (their pass tests it and blends).</summary>
     */
    internal sealed class AnimeEyesOverHairPass : ScriptableRenderPass
    {
        #region Private Fields

        private ShaderTagId _tag;
        private ProfilingSampler _sampler;
        private bool _ready;

        #endregion

        #region Public API

        /** <summary>Resolves the tag; on the main thread, before recording.</summary> */
        public void Setup()
        {
            if (_ready)
                return;
            _tag = new ShaderTagId(AnimeShaderIds.EyesOverHairLightMode);
            _sampler = new ProfilingSampler("Anime Eyes Over Hair");
            _ready = true;
        }

        /** <inheritdoc /> */
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            if (!_ready || !resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid())
                return;

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(_sampler.name, out PassData passData, _sampler);
            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(_tag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
            FilteringSettings filtering = new(RenderQueueRange.opaque, cameraData.camera.cullingMask);
            passData.Renderers = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));

            builder.UseRendererList(passData.Renderers);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => context.cmd.DrawRendererList(data.Renderers));
        }

        #endregion

        #region Helpers

        private sealed class PassData
        {
            public RendererListHandle Renderers;
        }

        #endregion
    }
}
