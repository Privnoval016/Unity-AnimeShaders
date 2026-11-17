using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Object = UnityEngine.Object;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A detail set's packing on disk: reads its sources from their files' bytes (so their import settings
     * never matter), packs them (<see cref="DetailMath" />), writes the map as a labelled PNG beside the set and records
     * it; its stone map (<see cref="StoneMath" />) beside it when the set makes one, removed when it doesn't; whether a
     * set's maps are out of date; the neutral map and the ground's defaults.</summary>
     */
    public static class AnimeDetailPacker
    {
        #region Private Fields

        private static readonly string[] DetailTextures = { Ids.FieldDetailName, Ids.CircleDetailName };

        #endregion

        #region Properties

        /** <summary>The neutral map's file name, beside the pack's noise.</summary> */
        public const string NeutralFileName = "AnimeDetailNeutral.png";

        /** <summary>The neutral map's width and height.</summary> */
        public const int NeutralSize = 16;

        /** <summary>What a set's stone map file adds to the set's name.</summary> */
        public const string StoneMapSuffix = "_Stones";

        #endregion

        #region Public API

        /** <summary>Packs <paramref name="set" /> into a PNG beside it (named after it), imports it and records it on the set.</summary> */
        public static void Pack(AnimeDetailSet set)
        {
            string setPath = AssetDatabase.GetAssetPath(set);
            if (string.IsNullOrEmpty(setPath))
                throw new InvalidOperationException($"Detail set {set.name} is not saved as an asset.");
            var sources = new DetailSources(Read(set.Colour, "colour", set, true), Read(set.Occlusion, "occlusion", set, false),
                Read(set.Normal, "normal", set, true), Read(set.Mask, "mask", set, false));
            DetailImage packed = DetailMath.Pack(sources, set.Settings);
            string path = Path.ChangeExtension(setPath, "png");
            DataTextures.Write(packed.Pixels, packed.Width, packed.Height, path, AnimeDetailImport.Label);
            PackStones(set, sources.Occlusion, setPath);
            set.Record(AssetDatabase.LoadAssetAtPath<Texture2D>(path), HashOf(set));
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssetIfDirty(set);
        }

        /** <summary>A hash of the set's source files' bytes and its settings: when it differs from the recorded one, the packed map is out of date.</summary> */
        public static string HashOf(AnimeDetailSet set)
        {
            var hash = new Hash128();
            foreach (Texture2D source in new[] { set.Colour, set.Occlusion, set.Normal, set.Mask })
                hash.Append(source == null ? string.Empty : DataTextures.FileHash(AssetDatabase.GetAssetPath(source)));
            DetailSettings settings = set.Settings;
            hash.Append(settings.Soften);
            hash.Append(settings.OcclusionWeight);
            hash.Append(settings.Size);
            hash.Append((int)settings.Normals);
            StoneSettings stones = set.StoneSettings;
            hash.Append(set.MakesStones ? 1 : 0);
            hash.Append(stones.Blur);
            hash.Append(stones.Threshold);
            hash.Append(stones.MinSize);
            return hash.ToString();
        }

        /** <summary>Writes the neutral map into the pack, beside its noise, and makes it the default detail map of every pack shader that reads one.</summary> */
        [MenuItem("Tools/Anime Shaders/Make Neutral Detail Map")]
        public static void MakeNeutralIntoPack()
        {
            string path = $"{ShaderDefaults.PackShadersFolder()}/Textures/{NeutralFileName}";
            DetailImage neutral = DetailMath.Neutral(NeutralSize);
            DataTextures.Write(neutral.Pixels, neutral.Width, neutral.Height, path, AnimeDetailImport.Label);
            ShaderDefaults.Set(DetailTextures, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }

        #endregion

        #region Helpers

        /** <summary>Writes the set's stone map beside it, or removes the one it had when it makes none or finds no stones.</summary> */
        private static void PackStones(AnimeDetailSet set, DetailImage occlusion, string setPath)
        {
            string path = Path.Combine(Path.GetDirectoryName(setPath) ?? string.Empty, Path.GetFileNameWithoutExtension(setPath) + StoneMapSuffix + ".png").Replace('\\', '/');
            StoneMap map = set.MakesStones && occlusion != null ? StoneMath.Find(occlusion, set.StoneSettings) : null;
            if (map == null || map.Count == 0)
            {
                if (File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
                set.RecordStones(null, 0);
                if (set.MakesStones)
                    Debug.LogWarning(occlusion == null ? $"{set.name}: stones need an occlusion map." : $"{set.name}: no stones found.", set);
                return;
            }
            DataTextures.Write(map.Image.Pixels, map.Image.Width, map.Image.Height, path, AnimeDetailImport.StonesLabel);
            set.RecordStones(AssetDatabase.LoadAssetAtPath<Texture2D>(path), map.Count);
            Debug.Log(map.Dropped > 0 ? $"{set.name}: {map.Count} stones, {map.Dropped} dropped." : $"{set.name}: {map.Count} stones.", set);
        }

        private static DetailImage Read(Texture2D source, string role, AnimeDetailSet set, bool required)
        {
            if (source == null)
            {
                if (required)
                    throw new InvalidOperationException($"Detail set {set.name} needs its {role} map.");
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)), false))
                    throw new InvalidOperationException($"Detail set {set.name}: its {role} map is not a PNG or JPG.");
                return new DetailImage(texture.width, texture.height, texture.GetPixels32());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        #endregion
    }
}
