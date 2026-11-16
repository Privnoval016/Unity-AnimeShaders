using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A URP asset and renderer made for a test (holding the given features), set as the current quality level's
     * pipeline until disposed. Every quality level's own pipeline is recorded first and every one is put back on dispose
     * (a test must never leave the project's settings changed); if any differs afterwards, dispose throws. The paths are
     * also kept in <see cref="SessionState" />, so a domain reload in the middle of a test puts them back
     * (<see cref="RestoreAfterReload" />).</summary>
     */
    public sealed class MadePipeline : IDisposable
    {
        #region Private Fields

        private const string Recorded = "AnimeShaders.Tests.MadePipeline.Levels";

        private readonly RenderPipelineAsset[] _levels;
        private readonly int _level;
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
            _level = QualitySettings.GetQualityLevel();
            _levels = new RenderPipelineAsset[QualitySettings.names.Length];
            var paths = new string[_levels.Length];
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] = QualitySettings.GetRenderPipelineAssetAt(i);
                paths[i] = _levels[i] == null ? string.Empty : AssetDatabase.GetAssetPath(_levels[i]);
            }
            SessionState.SetString(Recorded, string.Join("|", paths));

            try
            {
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
            catch
            {
                PutBack(_levels, _level);
                SessionState.EraseString(Recorded);
                throw;
            }
        }

        /** <summary>Puts every quality level's pipeline back, returns to the level the test began on, and destroys what was made.</summary> */
        public void Dispose()
        {
            PutBack(_levels, _level);
            SessionState.EraseString(Recorded);
            foreach (ScriptableRendererFeature feature in _renderer.rendererFeatures)
            {
                if (feature != null)
                    Object.DestroyImmediate(feature);
            }
            Object.DestroyImmediate(_asset);
            Object.DestroyImmediate(_renderer);
            for (int i = 0; i < _levels.Length; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) != _levels[i])
                    throw new InvalidOperationException($"Quality level {QualitySettings.names[i]}'s pipeline was not put back.");
            }
        }

        #endregion

        #region Helpers

        /** <summary>Whether a recorded value means nothing to put back: SessionState answers an empty string, not null, for a key never set.</summary> */
        private static bool NothingRecorded(string recorded)
        {
            return string.IsNullOrEmpty(recorded);
        }

        private static void PutBack(RenderPipelineAsset[] levels, int current)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) == levels[i])
                    continue;
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = levels[i];
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        [InitializeOnLoadMethod]
        private static void RestoreAfterReload()
        {
            string recorded = SessionState.GetString(Recorded, string.Empty);
            if (NothingRecorded(recorded))
                return;
            string[] paths = recorded.Split('|');
            var levels = new RenderPipelineAsset[Mathf.Min(paths.Length, QualitySettings.names.Length)];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = paths[i].Length == 0 ? null : AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(paths[i]);
                if (paths[i].Length > 0 && levels[i] == null)
                    return;
            }
            PutBack(levels, QualitySettings.GetQualityLevel());
            SessionState.EraseString(Recorded);
        }

        #endregion
    }
}
