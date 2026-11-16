using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders
{
    /**
     * <summary>Draws the depth-edge rim of every material with the <see cref="AnimeShaderIds.RimLightMode" /> pass on
     * over the camera colour after the skybox, reading URP's camera depth texture (scenery and bodies). Add it to each
     * renderer that should show the rim; a camera on a renderer without it draws no depth rim.</summary>
     * <remarks>Costs one more draw for each body with the rim on and in view, with a cheap fragment; nothing when none
     * is. Forward and Forward+ renderers; not XR single-pass.</remarks>
     */
    public sealed class AnimeRimFeature : ScriptableRendererFeature
    {
        #region Inspector Fields

        [Tooltip("When the rim draws. After the skybox keeps it over the bodies and under transparent effects.")]
        [SerializeField] private RenderPassEvent _event = RenderPassEvent.AfterRenderingSkybox;

        #endregion

        #region Private Fields

        private AnimeRimPass _pass;

        #endregion

        #region Public API

        /** <inheritdoc /> */
        public override void Create()
        {
            _pass = new AnimeRimPass { renderPassEvent = _event };
        }

        /** <inheritdoc /> */
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (_pass == null || (cameraType != CameraType.Game && cameraType != CameraType.SceneView))
                return;
            _pass.renderPassEvent = _event;
            _pass.Setup();
            renderer.EnqueuePass(_pass);
        }

        #endregion
    }
}
