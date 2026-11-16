using System;
using System.Collections.Generic;
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
     * it; whether a set's map is out of date; the neutral map and the ground's defaults.</summary>
     */
    public static class AnimeDetailPacker
    {
        #region Private Fields

        private static readonly string[] DetailTextures = { Ids.FieldDetailName, Ids.CircleDetailName };
        private static readonly Dictionary<string, (long Stamp, string Hash)> FileHashes = new();

        #endregion

        #region Properties

        /** <summary>The neutral map's file name, beside the pack's noise.</summary> */
        public const string NeutralFileName = "AnimeDetailNeutral.png";

        /** <summary>The neutral map's width and height.</summary> */
        public const int NeutralSize = 16;

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
            set.Record(AssetDatabase.LoadAssetAtPath<Texture2D>(path), HashOf(set));
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssetIfDirty(set);
        }

        /** <summary>A hash of the set's source files' bytes and its settings: when it differs from the recorded one, the packed map is out of date.</summary> */
        public static string HashOf(AnimeDetailSet set)
        {
            var hash = new Hash128();
            foreach (Texture2D source in new[] { set.Colour, set.Occlusion, set.Normal, set.Mask })
                hash.Append(source == null ? string.Empty : FileHash(AssetDatabase.GetAssetPath(source)));
            DetailSettings settings = set.Settings;
            hash.Append(settings.Soften);
            hash.Append(settings.OcclusionWeight);
            hash.Append(settings.Size);
            hash.Append((int)settings.Normals);
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

        /** <summary>A source file's content hash, kept until the file changes on disk.</summary> */
        private static string FileHash(string path)
        {
            long stamp = File.GetLastWriteTimeUtc(path).Ticks ^ new FileInfo(path).Length;
            if (FileHashes.TryGetValue(path, out (long Stamp, string Hash) known) && known.Stamp == stamp)
                return known.Hash;
            string hash = Hash128.Compute(File.ReadAllBytes(path)).ToString();
            FileHashes[path] = (stamp, hash);
            return hash;
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
