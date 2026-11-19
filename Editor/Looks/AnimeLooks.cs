using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * Looks in the Editor: taking one from a material (Extract, Update), applying one under Undo, what differs from it,
     * and which look a material follows, kept in its importer's user data (<c>anime.look=GUID</c>) so the material
     * itself carries nothing and the link is shared with the project.
     * </summary>
     */
    public static class AnimeLooks
    {
        #region Private Fields

        private const string LinkKey = "anime.look=";
        private const float Tolerance = 1e-4f;
        private const string LookSuffix = " Look.asset";

        private static List<AnimeLookPreset> _all;
        private static readonly Dictionary<Material, string> Links = new();

        #endregion

        #region Public API

        /** <summary>Every look in the project that holds at least one setting <paramref name="shader" /> has.</summary> */
        public static List<AnimeLookPreset> For(Shader shader)
        {
            var fitting = new List<AnimeLookPreset>();
            foreach (AnimeLookPreset look in All())
            {
                foreach (AnimeLookPreset.Setting setting in look.Settings)
                {
                    if (!setting.Fits(shader, out _))
                        continue;
                    fitting.Add(look);
                    break;
                }
            }
            return fitting;
        }

        /** <summary>A new look taken from <paramref name="material" />, saved beside it under a unique name, which it then follows.</summary> */
        public static AnimeLookPreset Extract(Material material)
        {
            string materialPath = AssetDatabase.GetAssetPath(material);
            string folder = string.IsNullOrEmpty(materialPath) ? "Assets" : Path.GetDirectoryName(materialPath)?.Replace('\\', '/');
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{material.name}{LookSuffix}");
            var look = ScriptableObject.CreateInstance<AnimeLookPreset>();
            Take(material, out List<AnimeLookPreset.Setting> settings, out List<AnimeLookPreset.PassSwitch> passes);
            look.Hold(material.shader, settings, passes);
            AssetDatabase.CreateAsset(look, path);
            AssetDatabase.SaveAssetIfDirty(look);
            _all = null;
            Follow(material, look);
            return look;
        }

        /**
         * <summary>
         * Writes <paramref name="material" />'s values into <paramref name="look" />: each setting and switch its shader has
         * is overwritten or added, every other one kept (a look is shared across shaders). Sections it covered stay covered,
         * sections it left stay left, and a section new to it is covered. Undo takes it back.
         * </summary>
         */
        public static void Update(AnimeLookPreset look, Material material)
        {
            Undo.RecordObject(look, $"Update {look.name}");
            var had = new HashSet<string>();
            foreach (AnimeLookPreset.Setting setting in look.Settings)
                had.Add(setting.section);
            foreach (AnimeLookPreset.PassSwitch pass in look.Passes)
                had.Add(pass.section);
            var covered = new HashSet<string>(look.Covers);

            Take(material, out List<AnimeLookPreset.Setting> taken, out List<AnimeLookPreset.PassSwitch> passes);
            var settings = new List<AnimeLookPreset.Setting>(look.Settings);
            foreach (AnimeLookPreset.Setting setting in taken)
            {
                int at = settings.FindIndex(held => held.property == setting.property);
                if (at >= 0)
                    settings[at] = setting;
                else
                    settings.Add(setting);
            }
            var switches = new List<AnimeLookPreset.PassSwitch>(look.Passes);
            foreach (AnimeLookPreset.PassSwitch pass in passes)
            {
                int at = switches.FindIndex(held => held.lightMode == pass.lightMode);
                if (at >= 0)
                    switches[at] = pass;
                else
                    switches.Add(pass);
            }

            look.Hold(look.MadeFrom != null ? look.MadeFrom : material.shader, settings, switches);
            foreach (string section in new List<string>(look.Covers))
                look.Cover(section, covered.Contains(section) || !had.Contains(section));
            EditorUtility.SetDirty(look);
            AssetDatabase.SaveAssetIfDirty(look);
        }

        /** <summary>Applies <paramref name="look" /> to every one of <paramref name="materials" /> under one Undo.</summary> */
        public static void Apply(Material[] materials, AnimeLookPreset look)
        {
            Undo.RecordObjects(materials, $"Apply {look.name}");
            foreach (Material material in materials)
            {
                AnimeMaterials.ApplyLook(material, look);
                EditorUtility.SetDirty(material);
            }
        }

        /** <summary>The shown settings of <paramref name="material" /> that <paramref name="look" /> covers and holds a different value for (as Apply would set it), and its covered switches that differ.</summary> */
        public static List<string> Differences(Material material, AnimeLookPreset look)
        {
            var differ = new List<string>();
            Shader shader = material.shader;
            InspectorLayout layout = InspectorLayout.For(shader);
            foreach (AnimeLookPreset.Setting setting in look.Settings)
            {
                if (!look.Sets(setting.section) || !setting.Fits(shader, out int index)
                    || (shader.GetPropertyFlags(index) & ShaderPropertyFlags.HideInInspector) != 0 || !layout.Shown(material, setting.property))
                    continue;
                if (setting.Differs(material, index, Tolerance))
                    differ.Add(setting.property);
            }
            foreach (AnimeLookPreset.PassSwitch pass in look.Passes)
            {
                if (look.Sets(pass.section) && AnimeMaterials.HasPass(shader, pass.lightMode) && material.GetShaderPassEnabled(pass.lightMode) != pass.on)
                    differ.Add(pass.lightMode);
            }
            return differ;
        }

        /** <summary>Sets <paramref name="property" /> on every one of <paramref name="materials" /> back to <paramref name="look" />'s value, as Apply would, under one Undo.</summary> */
        public static void Revert(Material[] materials, AnimeLookPreset look, string property)
        {
            Undo.RecordObjects(materials, $"Revert {property}");
            foreach (AnimeLookPreset.Setting setting in look.Settings)
            {
                if (setting.property != property)
                    continue;
                foreach (Material material in materials)
                {
                    if (!setting.Fits(material.shader, out int index))
                        continue;
                    setting.ApplyTo(material, index);
                    AnimeMaterials.SyncDerived(material);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        /** <summary>The look <paramref name="material" /> follows; null when none, or when its look was deleted (<paramref name="gone" /> then true).</summary> */
        public static AnimeLookPreset Followed(Material material, out bool gone)
        {
            gone = false;
            string guid = LinkOf(material);
            if (string.IsNullOrEmpty(guid))
                return null;
            var look = AssetDatabase.LoadAssetAtPath<AnimeLookPreset>(AssetDatabase.GUIDToAssetPath(guid));
            gone = look == null;
            return look;
        }

        /** <summary>Whether <paramref name="material" /> can follow a look (keep a link); <paramref name="reason" /> says why not, and what to do.</summary> */
        public static bool CanFollow(Material material, out string reason)
        {
            reason = null;
            string path = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(path))
                reason = "This material isn't saved as an asset, so it can't follow a look. Save it first.";
            else if (material.isVariant)
                reason = "A material variant takes its look from its parent: apply a look on the parent.";
            else if (!path.EndsWith(".mat") || !AssetDatabase.IsMainAsset(material))
                reason = "This material lives inside another asset (a model), so it can't follow a look. Extract it to a material first.";
            else if (path.StartsWith("Packages/"))
                reason = "This material is in a package and can't be changed.";
            return reason == null;
        }

        /** <summary>Makes <paramref name="material" /> follow <paramref name="look" /> (null: none), kept in its importer's user data among anything else there. Answers false when it can't follow one.</summary> */
        public static bool Follow(Material material, AnimeLookPreset look)
        {
            if (!CanFollow(material, out _))
                return false;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material));
            string guid = look != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(look)) : null;
            Links[material] = guid;
            string data = WithLink(importer.userData, guid);
            if (data == importer.userData)
                return true;
            AssetDatabase.SaveAssetIfDirty(material);
            importer.userData = data;
            importer.SaveAndReimport();
            return true;
        }

        #endregion

        #region Public API — Internal

        /** <summary>The settings and pass switches <paramref name="material" />'s look sections hold, as a look stores them: never the material's own settings or a texture.</summary> */
        internal static void Take(Material material, out List<AnimeLookPreset.Setting> settings, out List<AnimeLookPreset.PassSwitch> passes)
        {
            Shader shader = material.shader;
            InspectorLayout layout = InspectorLayout.For(shader);
            settings = new List<AnimeLookPreset.Setting>();
            passes = new List<AnimeLookPreset.PassSwitch>();
            foreach (InspectorLayout.Section section in layout.Sections)
            {
                if (section.SwitchPass != null)
                    passes.Add(new AnimeLookPreset.PassSwitch { lightMode = section.SwitchPass, section = section.Name, on = material.GetShaderPassEnabled(section.SwitchPass) });
                foreach (int index in section.Properties)
                {
                    if (!layout.IsOwn(shader.GetPropertyName(index)) && AnimeLookPreset.Setting.Take(material, index, section.Name, out AnimeLookPreset.Setting setting))
                        settings.Add(setting);
                }
            }
        }

        /** <summary>Forgets the looks found and the links read, so the next call reads them afresh.</summary> */
        internal static void Forget()
        {
            _all = null;
            Links.Clear();
        }

        /** <summary>Forgets the looks found (a look was made, moved or deleted).</summary> */
        internal static void ForgetLooks() => _all = null;

        /** <summary><paramref name="userData" /> with its look link set to <paramref name="guid" /> (null removes it), everything else kept as it was.</summary> */
        internal static string WithLink(string userData, string guid)
        {
            var lines = new List<string>((userData ?? string.Empty).Split('\n'));
            lines.RemoveAll(line => line.StartsWith(LinkKey));
            if (lines.Count == 1 && lines[0].Length == 0)
                lines.Clear();
            if (!string.IsNullOrEmpty(guid))
                lines.Add(LinkKey + guid);
            return string.Join("\n", lines);
        }

        /** <summary>The look GUID in <paramref name="userData" />; null when none.</summary> */
        internal static string LinkIn(string userData)
        {
            foreach (string line in (userData ?? string.Empty).Split('\n'))
            {
                if (line.StartsWith(LinkKey))
                    return line.Substring(LinkKey.Length).Trim();
            }
            return null;
        }

        #endregion

        #region Helpers

        private static List<AnimeLookPreset> All()
        {
            if (_all != null)
                return _all;
            _all = new List<AnimeLookPreset>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AnimeLookPreset)}"))
            {
                var look = AssetDatabase.LoadAssetAtPath<AnimeLookPreset>(AssetDatabase.GUIDToAssetPath(guid));
                if (look != null)
                    _all.Add(look);
            }
            _all.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return _all;
        }

        private static string LinkOf(Material material)
        {
            if (Links.TryGetValue(material, out string cached))
                return cached;
            string path = AssetDatabase.GetAssetPath(material);
            string guid = string.IsNullOrEmpty(path) || !CanFollow(material, out _) ? null : LinkIn(AssetImporter.GetAtPath(path)?.userData);
            Links[material] = guid;
            return guid;
        }

        #endregion
    }

    /** <summary>Forgets the links read when a material is imported, and the looks found when a look asset is made, moved or deleted.</summary> */
    internal sealed class LookImports : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string[] paths in new[] { imported, deleted, moved, movedFrom })
            {
                foreach (string path in paths)
                {
                    if (path.EndsWith(".mat"))
                        AnimeLooks.Forget();
                    else if (path.EndsWith(".asset") && AssetDatabase.GetMainAssetTypeAtPath(path) is var type && (type == null || type == typeof(AnimeLookPreset)))
                        AnimeLooks.ForgetLooks();
                }
            }
        }
    }
}
