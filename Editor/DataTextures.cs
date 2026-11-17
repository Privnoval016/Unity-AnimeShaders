using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Textures that hold data, not colour (the noise, packed detail maps, stone maps, face maps): written as
     * PNGs, or half-float EXRs for values a byte can't hold, carrying an asset label, and held by their import rule to
     * linear, alpha-as-data settings, sampled as each needs (<see cref="DataSampling" />); and a cached hash of a source
     * file's bytes, so a baker can tell when its map is out of date.</summary>
     */
    public static class DataTextures
    {
        #region Private Fields

        private const int Anisotropy = 8;

        private static readonly Dictionary<string, (long Stamp, string Hash)> FileHashes = new();

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
            AddLabel(path, label);
        }

        /** <summary>As <see cref="Write" />, as a half-float EXR: for values a byte can't hold finely enough.</summary> */
        public static void WriteHalf(Color[] pixels, int width, int height, string path, string label)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
            try
            {
                texture.SetPixels(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            AddLabel(path, label);
        }

        /** <summary>Holds <paramref name="importer" /> to a data texture's settings, with the given compression, largest size and sampling.</summary> */
        public static void HoldTo(TextureImporter importer, TextureImporterCompression compression, int maxSize, DataSampling sampling = DataSampling.Blended)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = compression;
            importer.mipmapEnabled = sampling == DataSampling.Blended;
            importer.wrapMode = sampling == DataSampling.Clamped ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.filterMode = sampling switch
            {
                DataSampling.Blended => FilterMode.Trilinear,
                DataSampling.Exact => FilterMode.Point,
                _ => FilterMode.Bilinear,
            };
            importer.anisoLevel = sampling == DataSampling.Blended ? Anisotropy : 0;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxSize;
        }

        /** <summary>Imports the file at <paramref name="path" /> and adds <paramref name="label" /> if missing, importing again so its rule applies.</summary> */
        public static void AddLabel(string path, string label)
        {
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

        /** <summary>A file's content hash (a project path), kept until the file changes on disk.</summary> */
        public static string FileHash(string path)
        {
            long stamp = File.GetLastWriteTimeUtc(path).Ticks ^ new FileInfo(path).Length;
            if (FileHashes.TryGetValue(path, out (long Stamp, string Hash) known) && known.Stamp == stamp)
                return known.Hash;
            string hash = Hash128.Compute(File.ReadAllBytes(path)).ToString();
            FileHashes[path] = (stamp, hash);
            return hash;
        }

        #endregion
    }
}
