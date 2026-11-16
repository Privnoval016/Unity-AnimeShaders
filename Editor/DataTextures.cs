using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Textures that hold data, not colour (the noise, packed detail maps, stone maps): written as PNGs carrying
     * an asset label, and held by their import rule to linear, alpha-as-data, repeating settings: mipmapped and trilinear,
     * or, for values that must never blend (ids, codes), point-sampled without mips.</summary>
     */
    public static class DataTextures
    {
        #region Private Fields

        private const int Anisotropy = 8;

        #endregion

        #region Public API

        /** <summary>A 0..1 value as a byte, rounded and clamped.</summary> */
        public static byte Byte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        /** <summary><paramref name="value" /> wrapped into 0..<paramref name="period" />.</summary> */
        public static int Wrap(int value, int period)
        {
            int wrapped = value % period;
            return wrapped < 0 ? wrapped + period : wrapped;
        }

        /** <summary>Whether the asset at <paramref name="assetPath" /> carries <paramref name="label" />.</summary> */
        public static bool HasLabel(string assetPath, string label)
        {
            return Array.IndexOf(AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(assetPath)), label) >= 0;
        }

        /**
         * <summary>Writes <paramref name="pixels" /> (bottom row first) as a PNG at <paramref name="path" /> (a project
         * path), imports it, and adds <paramref name="label" /> to its labels if missing, importing again so its rule
         * applies.</summary>
         */
        public static void Write(Color32[] pixels, int width, int height, string path, string label)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (HasLabel(path, label))
                return;
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            string[] labels = AssetDatabase.GetLabels(asset);
            Array.Resize(ref labels, labels.Length + 1);
            labels[^1] = label;
            AssetDatabase.SetLabels(asset, labels);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /** <summary>Holds <paramref name="importer" /> to a data texture's settings, with the given compression and largest size; <paramref name="point" /> for values that must never blend.</summary> */
        public static void HoldTo(TextureImporter importer, TextureImporterCompression compression, int maxSize, bool point = false)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = compression;
            importer.mipmapEnabled = !point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = point ? FilterMode.Point : FilterMode.Trilinear;
            importer.anisoLevel = point ? 0 : Anisotropy;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxSize;
        }

        #endregion
    }
}
