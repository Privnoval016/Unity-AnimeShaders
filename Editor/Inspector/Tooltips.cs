using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * Each pack shader setting's tooltip, from <c>Tooltips.txt</c> beside this script: one line per setting,
     * <c>_Property: text</c>, or <c>Shader Name/_Property: text</c> for one shader only, which wins. A setting with no
     * line (a project's own shader) has none.
     * </summary>
     */
    public static class Tooltips
    {
        #region Private Fields

        private const string FileName = "Tooltips";
        private const string FileEnding = "/Editor/Inspector/Tooltips.txt";

        private static Dictionary<string, string> _lines;

        #endregion

        #region Public API

        /** <summary>The tooltip for <paramref name="property" /> on <paramref name="shader" />; null when it has none.</summary> */
        public static string For(Shader shader, string property) => Lookup(Lines(), shader != null ? shader.name : null, property);

        /** <summary>The tooltip in <paramref name="lines" /> for <paramref name="property" />: the shader's own line first, then the shared one; null for none.</summary> */
        internal static string Lookup(IReadOnlyDictionary<string, string> lines, string shaderName, string property)
        {
            if (shaderName != null && lines.TryGetValue(shaderName + "/" + property, out string own))
                return own;
            return lines.TryGetValue(property, out string shared) ? shared : null;
        }

        /** <summary>The lines of a tooltips file's <paramref name="text" />.</summary> */
        internal static Dictionary<string, string> Parse(string text)
        {
            var lines = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
                Read(lines, line.Trim());
            return lines;
        }

        /** <summary>Forgets the lines read, so the next call reads the file afresh (it was imported).</summary> */
        public static void Forget() => _lines = null;

        #endregion

        #region Helpers

        private static Dictionary<string, string> Lines()
        {
            if (_lines != null)
                return _lines;

            var lines = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets($"{FileName} t:TextAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var file = path.EndsWith(FileEnding) ? AssetDatabase.LoadAssetAtPath<TextAsset>(path) : null;
                if (file == null)
                    continue;
                lines = Parse(file.text);
                _lines = lines;
                break;
            }
            return lines;
        }

        /** <summary>One line into <paramref name="lines" />: <c>key: text</c>; blank lines and lines starting with # are skipped. (Not kept until the file is found, so a read before the asset database is ready tries again.)</summary> */
        private static void Read(Dictionary<string, string> lines, string line)
        {
            if (line.Length == 0 || line[0] == '#')
                return;
            int colon = line.IndexOf(": ");
            if (colon > 0)
                lines[line.Substring(0, colon)] = line.Substring(colon + 2);
        }

        #endregion
    }

    /** <summary>Forgets read layouts and tooltips when a shader or the tooltips file is imported again.</summary> */
    internal sealed class InspectorImports : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (path.EndsWith(".shader") || path.EndsWith(".hlsl"))
                    InspectorLayout.Forget();
                if (path.EndsWith("Tooltips.txt"))
                    Tooltips.Forget();
            }
        }
    }
}
