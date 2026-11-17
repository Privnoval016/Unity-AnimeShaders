using UnityEditor;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Holds a face map (<see cref="AnimeFaceMapBaker" />) to a data texture's settings: linear, uncompressed
     * (a compressed or sRGB map bends the face's shadow line), no mips, bilinear and clamped. Applies to textures
     * carrying the <see cref="Label" /> asset label, on every import.</summary>
     */
    public sealed class AnimeFaceMapImport : AssetPostprocessor
    {
        #region Properties

        /** <summary>The asset label that marks a texture as a face map.</summary> */
        public const string Label = "AnimeFaceMap";

        /** <summary>The largest size a face map keeps.</summary> */
        public const int MaxSize = 4096;

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
                DataTextures.HoldTo((TextureImporter)assetImporter, TextureImporterCompression.Uncompressed, MaxSize, DataSampling.Clamped);
        }

        #endregion
    }
}
