using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Ids = AnimeShaders.AnimeShaderIds.SunShafts;

namespace AnimeShaders
{
    /**
     * <summary>The sun shafts' render-graph passes, at half resolution: the mask (the sky near the sun), a long and a short
     * radial blur toward the sun, and the composite added over the camera colour. Records nothing when the volume's
     * intensity is 0, the camera has post-processing off, is an overlay, renders with MSAA or in XR, there is no main
     * light, or the sun is behind the camera; fades in by the sun's angle from the view.</summary>
     */
    internal sealed class AnimeSunShaftsPass : ScriptableRenderPass
    {
        #region Private Fields

        private const int MaskPass = 0;
        private const int BlurLongPass = 1;
        private const int BlurShortPass = 2;
        private const int CompositePass = 3;
        private const float ShortBlurShare = 0.35f;
        private const float FadeInFacing = 0.25f;
        private const float SunDistanceShare = 0.5f;

        private static readonly Vector4 WholeTexture = new(1f, 1f, 0f, 0f);

        private Material _material;
        private ProfilingSampler _sampler;

        #endregion

        #region Public API

        /** <summary>Takes the material, asks for the depth texture and an intermediate colour target; on the main thread, before recording.</summary> */
        public void Setup(Material material)
        {
            _material = material;
            _sampler ??= new ProfilingSampler("Anime Sun Shafts");
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        /** <inheritdoc /> */
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            AnimeSunShafts shafts = VolumeManager.instance.stack.GetComponent<AnimeSunShafts>();
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            if (_material == null || shafts == null || !shafts.IsActive() || !cameraData.postProcessEnabled || lightData.mainLightIndex < 0
                || resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid() || cameraData.renderType != CameraRenderType.Base
                || cameraData.cameraTargetDescriptor.msaaSamples > 1 || cameraData.xr.enabled)
                return;

            VisibleLight sun = lightData.visibleLights[lightData.mainLightIndex];
            Vector3 towardSun = -((Vector3)sun.localToWorldMatrix.GetColumn(2)).normalized;
            Camera camera = cameraData.camera;
            float facing = Vector3.Dot(camera.transform.forward, towardSun);
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(facing / FadeInFacing));
            if (fade <= 0f)
                return;

            RenderTextureDescriptor full = cameraData.cameraTargetDescriptor;
            Vector3 onScreen = camera.WorldToViewportPoint(camera.transform.position + towardSun * camera.farClipPlane * SunDistanceShare);
            float length = shafts.length.value;
            _material.SetVector(Ids.Sun, new Vector4(onScreen.x, onScreen.y, shafts.reach.value, full.width / (float)Mathf.Max(full.height, 1)));
            _material.SetVector(Ids.Blur, new Vector4(length, shafts.decay.value, shafts.samples.value, length * ShortBlurShare));
            Color colour = sun.finalColor * shafts.tint.value.linear * (shafts.intensity.value * fade);
            _material.SetVector(Ids.Color, new Vector4(colour.r, colour.g, colour.b, shafts.threshold.value));

            RenderTextureDescriptor half = full;
            half.width = Mathf.Max(1, full.width / 2);
            half.height = Mathf.Max(1, full.height / 2);
            half.depthStencilFormat = GraphicsFormat.None;
            half.msaaSamples = 1;
            half.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.Render)
                ? GraphicsFormat.R16G16B16A16_SFloat
                : GraphicsFormat.R8G8B8A8_UNorm;
            TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, half, "Sun Shafts Mask", false, FilterMode.Bilinear);
            TextureHandle blurred = UniversalRenderer.CreateRenderGraphTexture(renderGraph, half, "Sun Shafts Blur", false, FilterMode.Bilinear);

            Blit(renderGraph, "Sun Shafts Mask", resources.activeColorTexture, mask, MaskPass, resources.cameraDepthTexture, false);
            Blit(renderGraph, "Sun Shafts Blur Long", mask, blurred, BlurLongPass, TextureHandle.nullHandle, false);
            Blit(renderGraph, "Sun Shafts Blur Short", blurred, mask, BlurShortPass, TextureHandle.nullHandle, false);
            Blit(renderGraph, "Sun Shafts Composite", mask, resources.activeColorTexture, CompositePass, TextureHandle.nullHandle, true);
        }

        #endregion

        #region Helpers

        private void Blit(RenderGraph renderGraph, string name, TextureHandle source, TextureHandle target, int pass, TextureHandle depth, bool blend)
        {
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(name, out PassData data, _sampler);
            data.Source = source;
            data.Material = _material;
            data.Pass = pass;
            builder.UseTexture(source, AccessFlags.Read);
            if (depth.IsValid())
                builder.UseTexture(depth, AccessFlags.Read);
            builder.SetRenderAttachment(target, 0, blend ? AccessFlags.ReadWrite : AccessFlags.Write);
            builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                Blitter.BlitTexture(context.cmd, passData.Source, WholeTexture, passData.Material, passData.Pass));
        }

        private sealed class PassData
        {
            public TextureHandle Source;
            public Material Material;
            public int Pass;
        }

        #endregion
    }
}
