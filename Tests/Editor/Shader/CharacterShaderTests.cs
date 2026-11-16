using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.Tests
{
    /** <summary>Anime/Character compiles cleanly, has its passes, keeps the SRP Batcher, and every pass shares the one block.</summary> */
    public class CharacterShaderTests
    {
        #region Private Fields

        private static readonly string[] ExpectedLightModes =
            { "UniversalForward", "SRPDefaultUnlit", "AnimeRim", "ShadowCaster", "DepthOnly", "DepthNormals" };

        #endregion

        #region Helpers

        private static Shader Character()
        {
            var shader = Shader.Find(TestMaterials.CharacterShader);
            Assert.IsNotNull(shader, TestMaterials.CharacterShader + " not found");
            return shader;
        }

        #endregion

        [Test]
        public void Compiles_WithNoMessages()
        {
            ShaderMessage[] messages = ShaderUtil.GetShaderMessages(Character());
            Assert.IsEmpty(messages, string.Join("\n", messages.Select(message => message.message)));
        }

        [Test]
        public void Passes_AreEachLightModeOnce()
        {
            Shader shader = Character();
            var modes = new List<string>();
            for (int pass = 0; pass < shader.GetPassCountInSubshader(0); pass++)
                modes.Add(shader.FindPassTagValue(pass, new ShaderTagId("LightMode")).name.ToUpperInvariant());
            CollectionAssert.AreEquivalent(ExpectedLightModes.Select(mode => mode.ToUpperInvariant()), modes);
        }

        [Test]
        public void SrpBatcher_IsCompatible()
        {
            Assert.IsNull(ShaderChecks.SrpBatcherProblem(Character()));
        }

        [Test]
        public void AnEntryShaderAddingItsOwnProperty_CompilesAndKeepsTheBatcher()
        {
            var probe = Shader.Find("Hidden/AnimeShaders/Tests/EntryProbe");
            Assert.IsNotNull(probe, "the probe entry shader is missing");
            ShaderMessage[] messages = ShaderUtil.GetShaderMessages(probe);
            Assert.IsEmpty(messages, string.Join("\n", messages.Select(message => message.message)));
            Assert.IsNull(ShaderChecks.SrpBatcherProblem(probe));
        }

        [Test]
        public void EveryPass_ReachesTheOneMaterialBlock()
        {
            Shader shader = Character();
            List<string> passIncludes = PackSources.PassIncludes(PackSources.ShaderText(shader));
            Assert.AreEqual(ExpectedLightModes.Length, passIncludes.Count, "one core include per pass");
            foreach (string include in passIncludes)
                Assert.IsTrue(PackSources.Reached(shader, include).Contains("AnimeProperties.hlsl"), include);
        }

        [Test]
        public void OnlyThePropertiesInclude_DeclaresUnityPerMaterial()
        {
            Shader shader = Character();
            var declaring = PackSources.AllIncludes(shader)
                .Where(path => File.ReadAllText(path).Contains("CBUFFER_START(UnityPerMaterial)"))
                .Select(Path.GetFileName).ToList();
            CollectionAssert.AreEqual(new[] { "AnimeProperties.hlsl" }, declaring);
            StringAssert.DoesNotContain("CBUFFER_START", PackSources.ShaderText(shader));
        }
    }
}
