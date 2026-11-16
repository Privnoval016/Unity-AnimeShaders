using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A URP asset and renderer made for a test (holding the given features), set as the quality level's
     * pipeline until disposed. The project's own pipeline is recorded in <see cref="SessionState" /> first, so a domain
     * reload in the middle of a test puts it back (<see cref="RestoreAfterReload" />).</summary>
     */
    public sealed class MadePipeline : IDisposable
    {
        #region Private Fields

        private const string Recorded = "AnimeShaders.Tests.MadePipeline.Original";

        private readonly RenderPipelineAsset _before;
        private readonly UniversalRendererData _renderer;
        private readonly UniversalRenderPipelineAsset _asset;

        #endregion

        #region Properties

        /** <summary>Whether the quality level's pipeline is a made one (a test left it, or one is running).</summary> */
        public static bool Active => QualitySettings.renderPipeline != null
            && (QualitySettings.renderPipeline.hideFlags & HideFlags.DontSave) != 0;

        #endregion

        #region Public API

        /** <summary>Swaps in a made pipeline whose renderer holds <paramref name="features" />, with the camera depth texture on.</summary> */
        public MadePipeline(params ScriptableRendererFeature[] features)
        {
            _before = QualitySettings.renderPipeline;
            SessionState.SetString(Recorded, _before == null ? string.Empty : AssetDatabase.GetAssetPath(_before));

            _renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            _renderer.hideFlags = HideFlags.DontSave;
            foreach (ScriptableRendererFeature feature in features)
            {
                feature.hideFlags = HideFlags.DontSave;
                _renderer.rendererFeatures.Add(feature);
            }
            _asset = UniversalRenderPipelineAsset.Create(_renderer);
            _asset.hideFlags = HideFlags.DontSave;
            _asset.supportsCameraDepthTexture = true;
            QualitySettings.renderPipeline = _asset;
        }

        /** <summary>Puts the project's pipeline back and destroys what was made.</summary> */
        public void Dispose()
        {
            QualitySettings.renderPipeline = _before;
            SessionState.EraseString(Recorded);
            foreach (ScriptableRendererFeature feature in _renderer.rendererFeatures)
            {
                if (feature != null)
                    Object.DestroyImmediate(feature);
            }
            Object.DestroyImmediate(_asset);
            Object.DestroyImmediate(_renderer);
        }

        #endregion

        #region Helpers

        [InitializeOnLoadMethod]
        private static void RestoreAfterReload()
        {
            string path = SessionState.GetString(Recorded, null);
            if (path == null)
                return;
            QualitySettings.renderPipeline = path.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            SessionState.EraseString(Recorded);
        }

        #endregion
    }
}
