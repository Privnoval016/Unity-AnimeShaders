using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Holds the pack's noise texture to the settings its shaders need, whatever an import would choose: linear,
     * uncompressed (compression ruins the distance channel), mipmapped, trilinear, repeating. Applies to textures
     * carrying the <see cref="Label" /> asset label.</summary>
     */
    public class AnimeNoiseImport : AssetPostprocessor
    {
        #region Properties

        /** <summary>The asset label that marks a texture as the pack's noise.</summary> */
        public const string Label = "AnimeNoise";

        #endregion

        #region Public API

        /** <summary>Bumps the postprocessor version when the settings change.</summary> */
        public override uint GetVersion()
        {
            return 1u;
        }

        #endregion

        #region Event Handlers

        private void OnPreprocessTexture()
        {
            if (DataTextures.HasLabel(assetPath, Label))
                DataTextures.HoldTo((TextureImporter)assetImporter, TextureImporterCompression.Uncompressed, AnimeNoiseBaker.Size);
        }

        #endregion
    }
}
