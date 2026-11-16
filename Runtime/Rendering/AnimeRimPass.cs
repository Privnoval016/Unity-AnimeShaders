using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders
{
    /** <summary>The rim's render-graph pass: the opaque renderers' rim pass drawn additively over the camera colour.</summary> */
    internal sealed class AnimeRimPass : ScriptableRenderPass
    {
        #region Private Fields

        private ShaderTagId _tag;
        private ProfilingSampler _sampler;
        private bool _ready;

        #endregion

        #region Public API

        /** <summary>Resolves the tag and asks for the camera depth texture; on the main thread, before recording.</summary> */
        public void Setup()
        {
            if (!_ready)
            {
                _tag = new ShaderTagId(AnimeShaderIds.RimLightMode);
                _sampler = new ProfilingSampler("Anime Rim");
                _ready = true;
            }
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        /** <inheritdoc /> */
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            if (!_ready || !resources.cameraDepthTexture.IsValid() || !resources.activeColorTexture.IsValid())
                return;

            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(_sampler.name, out PassData passData, _sampler);
            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(_tag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
            FilteringSettings filtering = new(RenderQueueRange.opaque, cameraData.camera.cullingMask);
            passData.Renderers = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));

            builder.UseRendererList(passData.Renderers);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
            builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
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
