using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders
{
    /**
     * <summary>Draws the hair (every material with the <see cref="AnimeShaderIds.HairDepthLightMode" /> pass, as
     * <c>Anime/Hair</c>) into the global <see cref="AnimeShaderIds.HairDepth.Texture" /> before the opaques, so faces on
     * <c>Anime/Face</c> take the front hair's shadow, and after the opaques draws eyes and brows on <c>Anime/Eyes</c> over
     * their own fringe where they marked it (<see cref="AnimeEyesOverHairPass" />). Add it to each renderer that should
     * show them. Every camera starts with
     * the texture black (no hair), so a camera it skips (previews, reflections, a renderer without it) never reads another
     * camera's hair.</summary>
     * <remarks>Costs one depth-only draw of each head of hair in view at half resolution, and one more draw of each eye
     * and brow in view; nothing with none in view.
     * Game and Scene view cameras only; not XR single-pass.</remarks>
     */
    public sealed class AnimeHairDepthFeature : ScriptableRendererFeature
    {
        #region Inspector Fields

        [Tooltip("Draw the hair's depth at the screen's full resolution: a crisper shadow edge for four times the pixels. Half is the default.")]
        [SerializeField] private bool fullResolution;

        #endregion

        #region Private Fields

        private AnimeHairDepthPass _pass;
        private AnimeEyesOverHairPass _eyes;

        #endregion

        #region Public API

        /** <inheritdoc /> */
        public override void Create()
        {
            _pass = new AnimeHairDepthPass { renderPassEvent = RenderPassEvent.BeforeRenderingOpaques };
            _eyes = new AnimeEyesOverHairPass { renderPassEvent = RenderPassEvent.AfterRenderingOpaques };
        }

        /** <inheritdoc /> */
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            RenderPipelineManager.beginCameraRendering -= ClearForCamera;
            RenderPipelineManager.beginCameraRendering += ClearForCamera;
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (_pass == null || (cameraType != CameraType.Game && cameraType != CameraType.SceneView) || renderingData.cameraData.xr.enabled)
                return;
            _pass.Setup(fullResolution);
            renderer.EnqueuePass(_pass);
            _eyes.Setup();
            renderer.EnqueuePass(_eyes);
        }

        #endregion

        #region Helpers

        /** <inheritdoc /> */
        protected override void Dispose(bool disposing)
        {
            RenderPipelineManager.beginCameraRendering -= ClearForCamera;
        }

        private static void ClearForCamera(ScriptableRenderContext context, Camera camera)
        {
            Shader.SetGlobalTexture(AnimeShaderIds.HairDepth.Texture, Texture2D.blackTexture);
            Shader.SetGlobalFloat(AnimeShaderIds.HairDepth.On, 0f);
        }

        #endregion
    }
}
