using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Keywords = AnimeShaders.AnimeShaderIds.Keywords;
using Switches = AnimeShaders.AnimeShaderIds.Switches;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A sphere (radius 1) lit from +X splits into the material's two tones at its threshold: on the middle
     * row, world x maps to N.L, so a threshold t puts the terminator at x = 2t - 1. Every value a check expects is set
     * on the made material here.</summary>
     */
    public class ToneTests
    {
        #region Private Fields

        private const float Tolerance = 0.01f;

        private static readonly Color Base = new(0.8f, 0.6f, 0.4f);
        private static readonly Color Tint = new(0.5f, 0.4f, 0.7f);

        private LitStage _stage;
        private Material _material;
        private DirectorGlobals _director;

        #endregion

        #region Properties

        private int Middle => _stage.Size / 2;

        private static Color Shadow => Times(Base, Tint);

        #endregion

        #region Helpers

        private static Color Times(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b);
        }

        private Color At(Color[] frame, float worldX)
        {
            return FrameReads.At(frame, _stage.Column(worldX), Middle);
        }

        /** <summary>The first column of the middle row, from the left, holding the lit tone.</summary> */
        private int FirstLitColumn(Color[] frame)
        {
            for (int x = 0; x < _stage.Size; x++)
            {
                if (FrameReads.Near(FrameReads.At(frame, x, Middle), Base, Tolerance))
                    return x;
            }
            return -1;
        }

        /** <summary>How many middle-row pixels across the sphere hold neither tone.</summary> */
        private int BetweenTones(Color[] frame)
        {
            int between = 0;
            for (int x = _stage.Column(-0.9f); x <= _stage.Column(0.9f); x++)
            {
                Color pixel = FrameReads.At(frame, x, Middle);
                if (!FrameReads.Near(pixel, Base, Tolerance) && !FrameReads.Near(pixel, Shadow, Tolerance))
                    between++;
            }
            return between;
        }

        private static Color[] Differences(Color[] a, Color[] b)
        {
            var result = new Color[a.Length];
            for (int i = 0; i < a.Length; i++)
                result[i] = FrameReads.Near(a[i], b[i], 1e-3f) ? Color.clear : Color.white;
            return result;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _director = new DirectorGlobals();
            _stage = new LitStage();
            _stage.LightFrom(Vector3.right);
            _material = TestMaterials.Character(Base, Tint);
            _stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            _director.Dispose();
        }

        [Test]
        public void LitSide_IsTheBaseColour_ShadowSide_IsBaseTimesTint()
        {
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, 0.6f), Base, Tolerance), $"lit {At(frame, 0.6f)}, expected {Base}");
            Assert.IsTrue(FrameReads.Near(At(frame, -0.6f), Shadow, Tolerance), $"shadow {At(frame, -0.6f)}");
        }

        [TestCase(0.5f)]
        [TestCase(0.75f)]
        [TestCase(0.3f)]
        public void Terminator_SitsWhereTheThresholdPutsIt(float threshold)
        {
            _material.SetFloat(Ids.ShadowThreshold, threshold);
            int expected = _stage.Column(2f * threshold - 1f);
            Assert.That(FirstLitColumn(_stage.Render()), Is.InRange(expected - 2, expected + 2));
        }

        [Test]
        public void Softness_SetsTheBoundarysWidth()
        {
            _material.SetFloat(Ids.ShadowSoftness, 0.1f);
            int narrow = BetweenTones(_stage.Render());
            _material.SetFloat(Ids.ShadowSoftness, 0.2f);
            int wide = BetweenTones(_stage.Render());
            int widthInPixels = _stage.Column(0.2f) - _stage.Column(-0.2f);
            Assert.That(wide, Is.InRange(widthInPixels / 2, widthInPixels + 2), $"wide {wide} of {widthInPixels}");
            Assert.That(wide, Is.InRange(2 * narrow - 3, 2 * narrow + 3), $"narrow {narrow}, wide {wide}");
        }

        [Test]
        public void Emission_AddsTheSameOnBothTones()
        {
            Color emission = new(0.2f, 0.1f, 0f);
            Color[] plain = _stage.Render();
            AnimeMaterials.SetFeature(_material, Keywords.EmissionName, Switches.Emission, true);
            TestMaterials.SetLinear(_material, Ids.EmissionColor, emission);
            Color[] glowing = _stage.Render();
            foreach (float x in new[] { -0.6f, 0.6f })
            {
                Color added = At(glowing, x) - At(plain, x);
                Assert.IsTrue(FrameReads.Near(added, emission, Tolerance), $"at x {x}: added {added}");
            }
        }

        [Test]
        public void RimAndSecondBand_Off_RenderAsIfAbsent()
        {
            Color[] before = _stage.Render();
            _material.SetFloat(Ids.RimStrength, 2f);
            TestMaterials.SetLinear(_material, Ids.RimColor, Color.red);
            TestMaterials.SetLinear(_material, Ids.Band2Tint, Color.blue);
            _material.SetFloat(Ids.Band2Offset, 0.4f);
            Color[] after = _stage.Render();
            Assert.AreEqual(0, FrameReads.Count(Differences(before, after), changed => changed.a > 0f));
        }

        [Test]
        public void Rim_On_AddsItsColourPastItsWidth_AndNothingInside()
        {
            Color rim = new(0.3f, 0.2f, 0.1f);
            AnimeMaterials.SetFeature(_material, Keywords.RimName, Switches.Rim, true);
            TestMaterials.SetLinear(_material, Ids.RimColor, rim);
            _material.SetFloat(Ids.RimStrength, 1f);
            _material.SetFloat(Ids.RimWidth, 0.5f);
            _material.SetFloat(Ids.RimSoftness, 0.004f);
            _material.SetFloat(Ids.RimLightMask, 0f);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, 0.7f), Base, Tolerance), $"inside {At(frame, 0.7f)}");
            Assert.IsTrue(FrameReads.Near(At(frame, 0.95f), Base + rim, Tolerance), $"edge {At(frame, 0.95f)}");
        }

        [Test]
        public void SecondBand_On_TintsOnlyTheDeeperShadow()
        {
            Color band = new(0.5f, 0.5f, 1f);
            AnimeMaterials.SetFeature(_material, Keywords.Band2Name, Switches.Band2, true);
            TestMaterials.SetLinear(_material, Ids.Band2Tint, band);
            _material.SetFloat(Ids.Band2Offset, 0.2f);
            _material.SetFloat(Ids.Band2Softness, 0.004f);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, -0.2f), Shadow, Tolerance), $"near the terminator {At(frame, -0.2f)}");
            Assert.IsTrue(FrameReads.Near(At(frame, -0.7f), Times(Shadow, band), Tolerance), $"deep {At(frame, -0.7f)}");
            Assert.IsTrue(FrameReads.Near(At(frame, 0.6f), Base, Tolerance), "the lit tone is untouched");
        }

        [Test]
        public void DirectorLightColour_ScalesTheLitTone_WithFullLightInfluence()
        {
            _material.SetFloat(Ids.LightInfluence, 1f);
            Color plain = At(_stage.Render(), 0.6f);
            Shader.SetGlobalVector(AnimeShaderIds.Director.LightColor, new Vector4(-0.5f, -0.5f, -0.5f, 0f));
            Color halved = At(_stage.Render(), 0.6f);
            Assert.IsTrue(FrameReads.Near(halved, plain * 0.5f, Tolerance), $"plain {plain}, halved {halved}");
        }

        [Test]
        public void DirectorShadowColour_ScalesOnlyTheShadowTone()
        {
            Shader.SetGlobalVector(AnimeShaderIds.Director.ShadowColor, new Vector4(-0.5f, -0.5f, -0.5f, 0f));
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, -0.6f), Shadow * 0.5f, Tolerance), $"shadow {At(frame, -0.6f)}");
            Assert.IsTrue(FrameReads.Near(At(frame, 0.6f), Base, Tolerance), $"lit {At(frame, 0.6f)}");
        }

        [Test]
        public void DirectorAmbient_AddsToTheShadowTone_ByAmbientStrength()
        {
            Color ambient = new(0.2f, 0.3f, 0.4f);
            const float strength = 0.5f;
            _material.SetFloat(Ids.AmbientStrength, strength);
            Shader.SetGlobalVector(AnimeShaderIds.Director.Ambient, new Vector4(ambient.r, ambient.g, ambient.b, 1f));
            Color expected = Times(Base, Tint + ambient * strength);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, -0.6f), expected, Tolerance), $"shadow {At(frame, -0.6f)}, expected {expected}");
            Assert.IsTrue(FrameReads.Near(At(frame, 0.6f), Base, Tolerance), "the lit tone takes no ambient");
        }

        [Test]
        public void DirectorLightDirection_AtFullWeight_MovesTheLitSide()
        {
            Shader.SetGlobalVector(AnimeShaderIds.Director.LightDirection, new Vector4(-1f, 0f, 0f, 1f));
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, -0.6f), Base, Tolerance), "lit where the director's light points");
            Assert.IsTrue(FrameReads.Near(At(frame, 0.6f), Shadow, Tolerance), "shadow on the scene light's side");
        }
    }
}
