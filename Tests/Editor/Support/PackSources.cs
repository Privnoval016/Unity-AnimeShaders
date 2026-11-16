using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /** <summary>Reads the pack's shader sources and name tables, wherever the pack is placed.</summary> */
    public static class PackSources
    {
        #region Private Fields

        private static readonly Regex Include = new("#include\\s+\"([^\"]+\\.hlsl)\"");
        private static readonly Regex Declaration = new("^\\s*(?:half|float|int|uint)\\d?\\s+(_\\w+)\\s*;", RegexOptions.Multiline);
        private static readonly Regex Texture = new("TEXTURE2D\\((_\\w+)\\)");

        #endregion

        #region Public API

        /** <summary>The text of a shader's source file.</summary> */
        public static string ShaderText(Shader shader)
        {
            return File.ReadAllText(AssetDatabase.GetAssetPath(shader));
        }

        /** <summary>The folder holding the core includes, beside the shader.</summary> */
        public static string IncludesFolder(Shader shader)
        {
            return Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(shader)), "Includes");
        }

        /** <summary>The text of a core include by file name.</summary> */
        public static string IncludeText(Shader shader, string file)
        {
            return File.ReadAllText(Path.Combine(IncludesFolder(shader), file));
        }

        /** <summary>Every core include file (by name) <paramref name="file" /> reaches through its includes, itself included.</summary> */
        public static HashSet<string> Reached(Shader shader, string file)
        {
            var reached = new HashSet<string>();
            var waiting = new Stack<string>();
            waiting.Push(file);
            while (waiting.Count > 0)
            {
                string next = waiting.Pop();
                string path = Path.Combine(IncludesFolder(shader), next);
                if (!reached.Add(next) || !File.Exists(path))
                    continue;
                foreach (Match match in Include.Matches(File.ReadAllText(path)))
                {
                    string name = Path.GetFileName(match.Groups[1].Value);
                    if (!match.Groups[1].Value.StartsWith("Packages/"))
                        waiting.Push(name);
                }
            }
            return reached;
        }

        /** <summary>The includes each pass of a shader file names (the core's, relative), in pass order.</summary> */
        public static List<string> PassIncludes(string shaderText)
        {
            return Include.Matches(shaderText).Cast<Match>().Select(match => match.Groups[1].Value)
                .Where(value => !value.StartsWith("Packages/")).Select(Path.GetFileName).ToList();
        }

        /** <summary>The variables declared in the constant buffer <paramref name="buffer" /> of <paramref name="text" />.</summary> */
        public static HashSet<string> BufferNames(string text, string buffer)
        {
            int start = text.IndexOf("CBUFFER_START(" + buffer + ")", StringComparison.Ordinal);
            int end = text.IndexOf("CBUFFER_END", start, StringComparison.Ordinal);
            string block = text.Substring(start, end - start);
            return new HashSet<string>(Declaration.Matches(block).Cast<Match>().Select(match => match.Groups[1].Value));
        }

        /** <summary>The statements in the constant buffer <paramref name="buffer" /> of <paramref name="text" /> that <see cref="BufferNames" /> does not read (a type it doesn't know): none, or a name could drift unseen.</summary> */
        public static List<string> UnreadStatements(string text, string buffer)
        {
            int start = text.IndexOf("CBUFFER_START(" + buffer + ")", StringComparison.Ordinal);
            int end = text.IndexOf("CBUFFER_END", start, StringComparison.Ordinal);
            string block = text.Substring(start, end - start);
            return block.Split('\n').Select(line => line.Trim())
                .Where(line => line.EndsWith(";") && !line.StartsWith("//") && !Declaration.IsMatch(line)).ToList();
        }

        /** <summary>The first capture of <paramref name="pattern" /> in <paramref name="text" />, or null.</summary> */
        public static string Capture(string text, string pattern)
        {
            Match match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        /** <summary>The textures declared in <paramref name="text" />.</summary> */
        public static HashSet<string> TextureNames(string text)
        {
            return new HashSet<string>(Texture.Matches(text).Cast<Match>().Select(match => match.Groups[1].Value));
        }

        /** <summary>The values of the public string constants of a nested name table.</summary> */
        public static HashSet<string> NameTable(Type table)
        {
            return new HashSet<string>(table.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()));
        }

        #endregion
    }
}
