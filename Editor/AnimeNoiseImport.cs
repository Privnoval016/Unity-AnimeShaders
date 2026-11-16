using System;
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
            string[] labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(assetPath));
            if (Array.IndexOf(labels, Label) < 0)
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = AnimeNoiseBaker.Size;
        }

        #endregion
    }
}
