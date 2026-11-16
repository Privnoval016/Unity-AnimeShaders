using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.Tests
{
    /** <summary>The name tables in <see cref="AnimeShaderIds" />, the HLSL and the shader's own properties name the same things.</summary> */
    public class PropertyNameTests
    {
        #region Helpers

        private static Shader Character()
        {
            return Shader.Find(TestMaterials.CharacterShader);
        }

        private static HashSet<string> BlockAndTextures(Shader shader)
        {
            string text = PackSources.IncludeText(shader, "AnimeProperties.hlsl");
            var names = PackSources.BufferNames(text, "UnityPerMaterial");
            names.UnionWith(PackSources.TextureNames(text));
            return names;
        }

        private static HashSet<string> ShaderProperties(Shader shader)
        {
            var names = new HashSet<string>();
            for (int i = 0; i < shader.GetPropertyCount(); i++)
                names.Add(shader.GetPropertyName(i));
            return names;
        }

        #endregion

        [Test]
        public void MaterialTable_MatchesTheBlockAndTextures()
        {
            CollectionAssert.AreEquivalent(BlockAndTextures(Character()),
                PackSources.NameTable(typeof(AnimeShaderIds.MaterialProperties)));
        }

        [Test]
        public void ShaderProperties_AreTheBlockTexturesAndSwitches()
        {
            Shader shader = Character();
            var expected = BlockAndTextures(shader);
            expected.RemoveWhere(name => name.EndsWith("_ST"));
            expected.UnionWith(PackSources.NameTable(typeof(AnimeShaderIds.Switches)));
            CollectionAssert.AreEquivalent(expected, ShaderProperties(shader));
        }

        [Test]
        public void DirectorTable_MatchesTheDirectorBuffer()
        {
            string text = PackSources.IncludeText(Character(), "AnimeDirector.hlsl");
            CollectionAssert.AreEquivalent(PackSources.BufferNames(text, "AnimeDirector"),
                PackSources.NameTable(typeof(AnimeShaderIds.Director)));
        }

        [Test]
        public void KeywordTable_MatchesTheShadersMaterialKeywords()
        {
            var local = Character().keywordSpace.keywords.Where(keyword => !keyword.isOverridable)
                .Select(keyword => keyword.name);
            CollectionAssert.AreEquivalent(PackSources.NameTable(typeof(AnimeShaderIds.Keywords)), local);
        }

        [Test]
        public void TheMaterialBlock_HoldsOnlyDeclarationsTheReaderKnows()
        {
            string text = PackSources.IncludeText(Character(), "AnimeProperties.hlsl");
            CollectionAssert.IsEmpty(PackSources.UnreadStatements(text, "UnityPerMaterial"));
        }

        [Test]
        public void ConstantsInTheHlsl_MatchTheirCSharpNames()
        {
            Shader shader = Character();
            string fade = PackSources.IncludeText(shader, "AnimeFade.hlsl");
            string outline = PackSources.IncludeText(shader, "AnimeOutline.hlsl");
            Assert.AreEqual(AnimeShaderIds.Fade.LevelMask,
                Convert.ToUInt32(PackSources.Capture(fade, @"#define ANIME_FADE_LEVEL_MASK (0x[0-9A-Fa-f]+)u"), 16));
            Assert.AreEqual(AnimeShaderIds.Fade.Full.ToString(), PackSources.Capture(fade, @"#define ANIME_FADE_LEVEL_FULL (\d+)u"));
            Assert.AreEqual(AnimeShaderIds.OutlineNormalChannel.ToString(), PackSources.Capture(outline, @"outlineNormal : TEXCOORD(\d)"));
        }

        [Test]
        public void TheLinePass_UsesTheNamedLightMode()
        {
            Shader shader = Character();
            var modes = new List<string>();
            for (int pass = 0; pass < shader.GetPassCountInSubshader(0); pass++)
                modes.Add(shader.FindPassTagValue(pass, new ShaderTagId("LightMode")).name.ToUpperInvariant());
            CollectionAssert.Contains(modes, AnimeShaderIds.OutlineLightMode.ToUpperInvariant());
        }
    }
}
