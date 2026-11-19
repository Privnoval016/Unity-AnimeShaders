using System.Collections.Generic;
using System.Reflection;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /**
     * <summary>
     * Every shader in the pack's folder: it keeps the SRP Batcher, and its variant count (Unity's own, before stripping) is
     * held to a recorded snapshot. A change that adds a compiled keyword multiplies a count and fails here; a deliberate
     * one records the new count in the same commit. A cheap feature stays a dynamic branch or a uniform (the quality
     * tiers), adding none.
     * </summary>
     */
    public class PackShaderTests
    {
        #region Private Fields

        /** <summary>The recorded counts: a code budget, not a tuned value; a deliberate change records the new count.</summary> */
        private static readonly Dictionary<string, ulong> Recorded = new()
        {
            [AnimeShaderIds.CharacterShader] = 41943120,
            [AnimeShaderIds.EyesShader] = 10485776,
            [AnimeShaderIds.FaceShader] = 1310800,
            ["Anime/Ground"] = 327694,
            [AnimeShaderIds.HairShader] = 41943122,
            ["Anime/Sky"] = 8,
            ["Hidden/AnimeShaders/SunShafts"] = 4,
            ["Anime/World"] = 2621454,
        };

        #endregion

        /** <summary>Every shader in the pack's shader folder.</summary> */
        private static IEnumerable<Shader> PackShaders()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { ShaderDefaults.PackShadersFolder() }))
                yield return AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
        }

        /** <summary>Unity's count of <paramref name="shader" />'s variants before stripping (an internal call).</summary> */
        private static ulong VariantCount(Shader shader)
        {
            MethodInfo count = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(count, "ShaderUtil.GetVariantCount moved: update this test");
            object[] args = { shader, false, 0UL };
            count.Invoke(null, args);
            return (ulong)args[2];
        }

        [Test]
        public void EveryPackShader_KeepsItsRecordedVariantCount()
        {
            var seen = new List<string>();
            var failures = new List<string>();
            foreach (Shader shader in PackShaders())
            {
                ulong actual = VariantCount(shader);
                seen.Add(shader.name);
                if (!Recorded.TryGetValue(shader.name, out ulong budget))
                    failures.Add($"{shader.name}: {actual} variants, not recorded (add it)");
                else if (actual != budget)
                    failures.Add($"{shader.name}: {actual} variants, recorded {budget}");
            }
            foreach (string name in Recorded.Keys)
            {
                if (!seen.Contains(name))
                    failures.Add($"{name}: recorded but no longer in the pack (remove it)");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void EveryPackShader_KeepsTheSrpBatcher()
        {
            var failures = new List<string>();
            foreach (Shader shader in PackShaders())
            {
                string problem = ShaderChecks.SrpBatcherProblem(shader);
                if (problem != null)
                    failures.Add($"{shader.name}: {problem}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
