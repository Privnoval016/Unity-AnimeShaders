using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Sets the pack shaders' default textures (what a new material takes) without disturbing their other
     * defaults. Every shader in the pack's Shaders folder that declares one of the properties takes it, so a new shader
     * needs no list.</summary>
     */
    public static class ShaderDefaults
    {
        #region Public API

        /** <summary>Makes <paramref name="texture" /> the default of each of <paramref name="properties" /> in every pack shader that declares it, keeping each shader's other defaults; answers how many shaders took it.</summary> */
        public static int Set(string[] properties, Texture texture)
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { PackShadersFolder() }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null || !DeclaresAny(shader, properties))
                    continue;
                var importer = (ShaderImporter)AssetImporter.GetAtPath(path);
                var names = new List<string>();
                var textures = new List<Texture>();
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    if (shader.GetPropertyType(i) != ShaderPropertyType.Texture)
                        continue;
                    string property = shader.GetPropertyName(i);
                    names.Add(property);
                    textures.Add(Array.IndexOf(properties, property) >= 0 ? texture : importer.GetDefaultTexture(property));
                }
                importer.SetDefaultTextures(names.ToArray(), textures.ToArray());
                importer.SaveAndReimport();
                changed++;
            }
            if (changed == 0)
                Debug.LogWarning($"No pack shader declares {string.Join(", ", properties)}.");
            return changed;
        }

        /** <summary>The pack's Shaders folder (where `Anime/Character` lives).</summary> */
        public static string PackShadersFolder()
        {
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(Shader.Find("Anime/Character")))?.Replace('\\', '/') ?? "Assets";
        }

        #endregion

        #region Helpers

        private static bool DeclaresAny(Shader shader, string[] properties)
        {
            foreach (string property in properties)
            {
                if (shader.FindPropertyIndex(property) >= 0)
                    return true;
            }
            return false;
        }

        #endregion
    }
}
