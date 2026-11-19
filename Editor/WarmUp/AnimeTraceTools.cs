using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * Shader traces in the Editor: merging a new recording into a trace, so something new needs only itself played, and a
     * trace's shape (each traced shader's pass count and declared keywords), kept beside the trace so a test can tell
     * when a traced shader has changed in a way its recorded states no longer cover.
     * </summary>
     */
    public static class AnimeTraceTools
    {
        #region Public API

        /**
         * <summary>Adds every state of the trace file <paramref name="fromPath" /> to the trace file <paramref name="intoPath" />
         * (states already there kept once). Both must be the same graphics API's.</summary>
         * <returns>Null when merged; else what was wrong.</returns>
         */
        public static string Merge(string intoPath, string fromPath)
        {
            var into = new GraphicsStateCollection();
            var from = new GraphicsStateCollection();
            try
            {
                if (!into.LoadFromFile(intoPath))
                    return $"can't read {intoPath}";
                if (!from.LoadFromFile(fromPath))
                    return $"can't read {fromPath}";
                if (into.graphicsDeviceType != from.graphicsDeviceType)
                    return $"{fromPath} is {from.graphicsDeviceType}, {intoPath} is {into.graphicsDeviceType}";
                if (!into.Append(from))
                    return "the traces didn't merge";
                return into.SaveToFile(intoPath) ? null : $"can't write {intoPath}";
            }
            finally
            {
                Object.DestroyImmediate(into);
                Object.DestroyImmediate(from);
            }
        }

        /** <summary>The trace's shape: a line per traced shader, by name, with its pass count and declared keywords as they are now.</summary> */
        public static string Shape(GraphicsStateCollection trace)
        {
            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            trace.GetVariants(variants);
            var text = new StringBuilder();
            foreach (Shader shader in variants.Select(variant => variant.shader).Where(shader => shader != null).Distinct().OrderBy(shader => shader.name, StringComparer.Ordinal))
                text.Append(ShapeOf(shader)).Append('\n');
            return text.ToString();
        }

        /** <summary>The shaders whose shape differs between <paramref name="recorded" /> and <paramref name="now" /> (both from <see cref="Shape" />), by name.</summary> */
        public static List<string> Changed(string recorded, string now)
        {
            Dictionary<string, string> before = ByShader(recorded);
            Dictionary<string, string> after = ByShader(now);
            return after.Where(pair => !before.TryGetValue(pair.Key, out string line) || line != pair.Value).Select(pair => pair.Key).OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        /** <summary>Where a trace's shape is kept: beside it, as <c>&lt;name&gt;.shape.txt</c>.</summary> */
        public static string ShapePath(string tracePath) => Path.ChangeExtension(tracePath, null) + ".shape.txt";

        #endregion

        #region Helpers

        private static string ShapeOf(Shader shader)
        {
            string[] keywords = shader.keywordSpace.keywordNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            return $"{shader.name} | passes {shader.passCount} | {string.Join(" ", keywords)}";
        }

        private static Dictionary<string, string> ByShader(string shape)
        {
            var lines = new Dictionary<string, string>();
            foreach (string line in (shape ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
            {
                int bar = line.IndexOf(" | ", StringComparison.Ordinal);
                if (bar > 0)
                    lines[line.Substring(0, bar)] = line;
            }
            return lines;
        }

        #endregion
    }
}
