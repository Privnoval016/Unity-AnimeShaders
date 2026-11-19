using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Switches = AnimeShaders.AnimeShaderIds.Switches;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Quality tiers: a dropped feature renders exactly as if switched off, nothing dropped renders exactly as
     * before, each dropped feature's branch skips its work in the source, and the global follows the quality level.</summary>
     */
    public class TierTests
    {
        #region Private Fields

        private static readonly Color Base = new(0.8f, 0.6f, 0.4f);
        private static readonly Color Tint = new(0.5f, 0.4f, 0.7f);
        private static readonly int DropsId = AnimeShaderIds.Director.TierDrops;

        private LitStage _stage;
        private Material _material;
        private DirectorGlobals _director;
        private AnimeQualityTiers _tiers;
        private AnimeQualityTiers _before;

        #endregion

        [SetUp]
        public void SetUp()
        {
            _before = AnimeTiers.Tiers;
            _director = new DirectorGlobals();
            _stage = new LitStage();
            _stage.LightFrom(Vector3.right);
            _material = TestMaterials.Character(Base, Tint);
            _stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            _tiers = ScriptableObject.CreateInstance<AnimeQualityTiers>();
        }

        [TearDown]
        public void TearDown()
        {
            AnimeTiers.Use(_before);
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_tiers);
            _director.Dispose();
        }

        private static RenderPipelineAsset InUse => QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;

        private void Drop(AnimeTierDrop drops)
        {
            _tiers.Set(InUse, drops);
            AnimeTiers.Use(_tiers);
        }

        private static int Different(Color[] a, Color[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++)
                count += FrameReads.Near(a[i], b[i], 1e-3f) ? 0 : 1;
            return count;
        }

        /** <summary>The material with its fresnel rim and second band on, both strong enough to show.</summary> */
        private void RimAndBandOn(bool on)
        {
            AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.RimName, Switches.Rim, on);
            AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.Band2Name, Switches.Band2, on);
            _material.SetFloat(Ids.RimStrength, 2f);
            TestMaterials.SetLinear(_material, Ids.RimColor, Color.red);
            TestMaterials.SetLinear(_material, Ids.Band2Tint, Color.blue);
            _material.SetFloat(Ids.Band2Offset, 0.4f);
        }

        [Test]
        public void ADroppedRimAndBand_RenderExactlyAsSwitchedOff()
        {
            RimAndBandOn(false);
            AnimeTiers.Use(null);
            Color[] off = _stage.Render();
            RimAndBandOn(true);
            Color[] on = _stage.Render();
            Assume.That(Different(off, on), Is.GreaterThan(0), "rim and band show when on");

            Drop(AnimeTierDrop.FresnelRim | AnimeTierDrop.SecondBand);
            Assert.AreEqual(0, Different(off, _stage.Render()), "dropped: as if off");

            Drop(AnimeTierDrop.FresnelRim);
            Assert.Greater(Different(off, _stage.Render()), 0, "only the rim dropped: the band still shows");
        }

        [Test]
        public void NothingDropped_RendersExactlyAsWithNoTiers()
        {
            RimAndBandOn(true);
            Shader.SetGlobalVector(DropsId, Vector4.zero);
            Color[] none = _stage.Render();

            Drop(AnimeTierDrop.None);
            Assert.AreEqual(0, Different(none, _stage.Render()));
        }

        [Test]
        public void TheGlobal_IsWhatTheLevelInUseDrops_AndZeroWithNoTiers()
        {
            Drop(AnimeTierDrop.FresnelRim);
            Assert.AreEqual(1f, Shader.GetGlobalFloat(DropsId), "the rim alone");
            Drop(AnimeTierDrop.AngelRing);
            Assert.AreEqual(4f, Shader.GetGlobalFloat(DropsId), "the ring alone");
            Drop(AnimeTierDrop.SecondBand | AnimeTierDrop.ExtraLights);
            Assert.AreEqual(10f, Shader.GetGlobalFloat(DropsId), "the band and the extra lights");

            AnimeTiers.Use(null);
            Assert.AreEqual(0f, Shader.GetGlobalFloat(DropsId), "no tiers: nothing dropped");
        }

        [Test]
        public void AQualityChange_WritesTheNewLevelsDrops()
        {
            int start = QualitySettings.GetQualityLevel();
            int other = start == 0 ? 1 : 0;
            Assume.That(QualitySettings.count, Is.GreaterThan(1));
            RenderPipelineAsset otherPipeline = QualitySettings.GetRenderPipelineAssetAt(other);
            Assume.That(otherPipeline, Is.Not.Null.And.Not.EqualTo(InUse), "the two levels have their own pipelines");
            _tiers.Set(otherPipeline, AnimeTierDrop.SecondBand);
            _tiers.Set(InUse, AnimeTierDrop.None);
            AnimeTiers.Use(_tiers);
            try
            {
                QualitySettings.SetQualityLevel(other, false);
                Assert.AreEqual((float)AnimeTierDrop.SecondBand, Shader.GetGlobalFloat(DropsId));
            }
            finally
            {
                QualitySettings.SetQualityLevel(start, false);
            }
            Assert.AreEqual(0f, Shader.GetGlobalFloat(DropsId), "back on the first level");
        }

        [Test]
        public void TheRingAndExtraLights_SkipTheirWork_WhenDropped()
        {
            string hair = PackSources.IncludeText(Shader.Find(AnimeShaderIds.HairShader), "AnimeHair.hlsl");
            string lighting = PackSources.IncludeText(Shader.Find(AnimeShaderIds.CharacterShader), "AnimeLighting.hlsl");

            StringAssert.IsMatch(@"ANIME_TIER_DROPS_RING[^\n]*\)\s*return;", hair, "the ring returns before any of its work");
            StringAssert.IsMatch(@"if\s*\(\s*ANIME_TIER_DROPS_EXTRA_LIGHTS\s*\)\s*return sum;", lighting, "the extra lights return before their loop");
        }

        [Test]
        public void EveryDrop_HasItsShaderTest_WithItsOwnBit()
        {
            string director = PackSources.IncludeText(Shader.Find(AnimeShaderIds.CharacterShader), "AnimeDirector.hlsl");
            var tested = Regex.Matches(director, @"#define ANIME_TIER_DROPS_\w+ ANIME_TIER_DROPS\(([\d.]+)\)")
                .Select(match => (int)float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
                .OrderBy(bit => bit)
                .ToArray();
            int[] drops = Enum.GetValues(typeof(AnimeTierDrop)).Cast<int>().Where(bit => bit != 0).OrderBy(bit => bit).ToArray();
            CollectionAssert.AreEqual(drops, tested, "each AnimeTierDrop value has one ANIME_TIER_DROPS_ line in AnimeDirector.hlsl testing its bit");
        }

        [Test]
        public void TheTierGlobal_IsNeverAKeywordOrInAPragma()
        {
            Shader character = Shader.Find(AnimeShaderIds.CharacterShader);
            string[] shaders = AssetDatabase.FindAssets("t:Shader", new[] { ShaderDefaults.PackShadersFolder() }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string file in PackSources.AllIncludes(character).Concat(shaders))
            {
                foreach (string line in File.ReadAllText(file).Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#if") || trimmed.StartsWith("#elif") || trimmed.StartsWith("#pragma"))
                        StringAssert.DoesNotContain("TIER", trimmed, $"{file}: a tier must stay a uniform, never a variant");
                }
            }
        }
    }
}
