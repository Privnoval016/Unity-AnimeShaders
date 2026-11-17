using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The name tables in <see cref="AnimeShaderIds" />, the HLSL and each pack shader's own properties name the
     * same things: every lit shader declares exactly the variables of the feature groups it opts into.</summary>
     */
    public class PropertyNameTests
    {
        #region Private Fields

        private static readonly string[] LitShaders = { "Anime/Character", "Anime/World", "Anime/Ground", AnimeShaderIds.FaceShader, AnimeShaderIds.HairShader };

        #endregion

        #region Helpers

        private static Shader Character()
        {
            return Shader.Find(TestMaterials.CharacterShader);
        }

        private static Dictionary<string, HashSet<string>> Groups()
        {
            return PackSources.BufferGroups(PackSources.IncludeText(Character(), "AnimeProperties.hlsl"), "UnityPerMaterial");
        }

        private static HashSet<string> ShaderProperties(Shader shader)
        {
            var names = new HashSet<string>();
            for (int i = 0; i < shader.GetPropertyCount(); i++)
                names.Add(shader.GetPropertyName(i));
            return names;
        }

        /** <summary>The block's variables for <paramref name="shader" />: the core and the groups its file defines.</summary> */
        private static HashSet<string> BlockFor(Shader shader)
        {
            Dictionary<string, HashSet<string>> groups = Groups();
            var names = new HashSet<string>(groups[string.Empty]);
            foreach (string group in PackSources.ShaderGroups(PackSources.ShaderText(shader)))
                names.UnionWith(groups[group]);
            return names;
        }

        #endregion

        [Test]
        public void MaterialTable_MatchesTheBlockAndItsTextures()
        {
            string text = PackSources.IncludeText(Character(), "AnimeProperties.hlsl");
            var block = new HashSet<string>(Groups().SelectMany(group => group.Value));
            block.UnionWith(PackSources.TextureNames(text));
            CollectionAssert.AreEquivalent(block, PackSources.NameTable(typeof(AnimeShaderIds.MaterialProperties)));
        }

        [TestCase("Anime/Character")]
        [TestCase("Anime/World")]
        [TestCase("Anime/Ground")]
        [TestCase(AnimeShaderIds.FaceShader)]
        [TestCase(AnimeShaderIds.HairShader)]
        public void EveryVariableOfTheShadersGroups_IsOneOfItsProperties(string name)
        {
            Shader shader = Shader.Find(name);
            HashSet<string> properties = ShaderProperties(shader);
            var missing = BlockFor(shader).Where(variable => !variable.EndsWith("_ST") && !properties.Contains(variable)).ToList();
            Assert.IsEmpty(missing, "the SRP Batcher needs every block variable declared");
        }

        [TestCase("Anime/Character")]
        [TestCase("Anime/World")]
        [TestCase("Anime/Ground")]
        [TestCase(AnimeShaderIds.FaceShader)]
        [TestCase(AnimeShaderIds.HairShader)]
        public void EveryPropertyOfTheShader_BelongsToItsGroups_IsATexture_OrASwitch(string name)
        {
            Shader shader = Shader.Find(name);
            HashSet<string> allowed = BlockFor(shader);
            allowed.UnionWith(PackSources.AllTextureNames(shader));
            allowed.UnionWith(PackSources.NameTable(typeof(AnimeShaderIds.Switches)));
            var stray = ShaderProperties(shader).Where(property => !allowed.Contains(property)).ToList();
            Assert.IsEmpty(stray, "a property no group of this shader reads");
        }

        [Test]
        public void SkyTable_MatchesTheSkyShader()
        {
            var table = PackSources.NameTable(typeof(AnimeShaderIds.Sky));
            table.Remove(AnimeShaderIds.Sky.StarsKeyword);
            table.Remove(AnimeShaderIds.Sky.ShaderName);
            CollectionAssert.AreEquivalent(table, ShaderProperties(Shader.Find(AnimeShaderIds.Sky.ShaderName)));
        }

        [Test]
        public void DirectorTable_MatchesTheDirectorBuffer()
        {
            string text = PackSources.IncludeText(Character(), "AnimeDirector.hlsl");
            CollectionAssert.AreEquivalent(PackSources.BufferNames(text, "AnimeDirector"),
                PackSources.NameTable(typeof(AnimeShaderIds.Director)));
        }

        [Test]
        public void WorldTable_MatchesTheWorldBufferAndItsTexture()
        {
            string text = PackSources.IncludeText(Character(), "AnimeWorld.hlsl");
            var expected = PackSources.BufferNames(text, "AnimeWorld");
            expected.UnionWith(PackSources.TextureNames(text));
            var table = PackSources.NameTable(typeof(AnimeShaderIds.World));
            table.Remove(AnimeShaderIds.World.AtmosphereKeyword);
            table.Remove(AnimeShaderIds.World.CloudShadowsKeyword);
            CollectionAssert.AreEquivalent(expected, table);
        }

        [Test]
        public void KeywordTable_MatchesThePackShadersMaterialKeywords()
        {
            var local = LitShaders.SelectMany(name => Shader.Find(name).keywordSpace.keywords)
                .Where(keyword => !keyword.isOverridable).Select(keyword => keyword.name).Distinct();
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
