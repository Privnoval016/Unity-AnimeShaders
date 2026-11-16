using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders
{
    /**
     * <summary>Draws the sun shafts set by the <see cref="AnimeSunShafts" /> volume override, before the post (so bloom and
     * tonemapping see them). Add it to each renderer that should show them; a camera on a renderer without it draws none.</summary>
     * <remarks>Three half-resolution passes and a composite while the volume's intensity is above 0 and the sun is in
     * front; nothing otherwise. Game and Scene view cameras with post-processing on only. Forward and Forward+ renderers; MSAA unsupported; not XR
     * single-pass.</remarks>
     */
    public sealed class AnimeSunShaftsFeature : ScriptableRendererFeature
    {
        #region Inspector Fields

        [Tooltip("The shafts' shader (Hidden/AnimeShaders/SunShafts); kept here so a build includes it.")]
        [SerializeField] private Shader shader;

        [Tooltip("When the shafts draw. Before the post, so bloom and tonemapping take them.")]
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        #endregion

        #region Private Fields

        private AnimeSunShaftsPass _pass;
        private Material _material;

        #endregion

        #region Public API

        /** <inheritdoc /> */
        public override void Create()
        {
            _pass = new AnimeSunShaftsPass();
        }

        /** <inheritdoc /> */
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (_pass == null || (cameraType != CameraType.Game && cameraType != CameraType.SceneView))
                return;
            if (shader == null)
                shader = Shader.Find(AnimeShaderIds.SunShafts.ShaderName);
            if (_material == null && shader != null)
                _material = CoreUtils.CreateEngineMaterial(shader);
            if (_material == null)
                return;
            _pass.renderPassEvent = passEvent;
            _pass.Setup(_material);
            renderer.EnqueuePass(_pass);
        }

        #endregion

        #region Helpers

        /** <inheritdoc /> */
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        #endregion
    }
}
