using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>
     * Writes the shaders' tier global (<c>_AnimeTierDrops</c>: the sum of the dropped features' <see cref="AnimeTierDrop" />
     * bits) for the quality level in use, from the <see cref="AnimeQualityTiers" /> the project hands to
     * <see cref="Use" />: at once, and again whenever the quality level changes. Never set, nothing is dropped. Its own
     * owner, apart from the light director, whose neutral writes never touch it.
     * </summary>
     */
    public static class AnimeTiers
    {
        #region Private Fields

        private static AnimeQualityTiers _tiers;
        private static bool _listening;

        #endregion

        #region Properties

        /** <summary>The tiers in use; null when none were handed over.</summary> */
        public static AnimeQualityTiers Tiers => _tiers;

        /** <summary>What the quality level in use drops now.</summary> */
        public static AnimeTierDrop Current => _tiers != null ? _tiers.DropsFor(QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline) : AnimeTierDrop.None;

        #endregion

        #region Public API

        /** <summary>Takes <paramref name="tiers" /> (null: nothing dropped), writes the global for the level in use, and writes it again on each change of level.</summary> */
        public static void Use(AnimeQualityTiers tiers)
        {
            _tiers = tiers;
            if (!_listening)
            {
                QualitySettings.activeQualityLevelChanged += OnLevelChanged;
                _listening = true;
            }
            Write();
        }

        /** <summary>Writes the global for the quality level in use.</summary> */
        public static void Write() => Shader.SetGlobalFloat(AnimeShaderIds.Director.TierDrops, AsValue(Current));

        /** <summary>The global's value for <paramref name="drops" />: the sum of the dropped features' bits.</summary> */
        public static float AsValue(AnimeTierDrop drops) => (float)drops;

        #endregion

        #region Event Handlers

        private static void OnLevelChanged(int previous, int current) => Write();

        #endregion
    }
}
