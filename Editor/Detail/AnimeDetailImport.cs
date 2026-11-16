using UnityEditor;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Holds a packed detail map (<see cref="AnimeDetailPacker" />) to a data texture's settings
     * (<see cref="DataTextures.HoldTo" />), compressed at high quality, whatever an import would choose. Applies to
     * textures carrying the <see cref="Label" /> asset label, on every import.</summary>
     * <remarks>Raising <see cref="GetVersion" /> reimports every texture in the project (a postprocessor cannot be
     * limited to labelled assets).</remarks>
     */
    public class AnimeDetailImport : AssetPostprocessor
    {
        #region Properties

        /** <summary>The asset label that marks a texture as a packed detail map.</summary> */
        public const string Label = "AnimeDetail";

        /** <summary>The largest size a packed map keeps.</summary> */
        public const int MaxSize = 8192;

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
                DataTextures.HoldTo((TextureImporter)assetImporter, TextureImporterCompression.CompressedHQ, MaxSize);
        }

        #endregion
    }
}
