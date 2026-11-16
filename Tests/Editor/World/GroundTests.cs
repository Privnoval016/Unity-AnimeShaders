using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Anime/Ground seen from above, lit straight down: stone inside its circle and grass outside, the circle
     * where its centre and radius put it, grout only where asked and only near, at most three noise reads a pixel.
     * Every colour expected is set on the made material.</summary>
     */
    public class GroundTests
    {
        #region Private Fields

        private static readonly Color Grass = new(0.1f, 0.6f, 0.1f);
        private static readonly Color Stone = new(0.8f, 0.8f, 0.8f);
        private static readonly Color Grout = new(0.2f, 0.2f, 0.2f);

        private LitStage _stage;
        private Material _material;
        private Mesh _floor;

        #endregion

        #region Helpers

        private Color At(Color[] frame, float x, float z)
        {
            return FrameReads.At(frame, _stage.Column(x), _stage.Column(z));
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private static bool IsStone(Color pixel)
        {
            return Distance(pixel, Stone) < Distance(pixel, Grass);
        }

        private void Circle(float x, float z, float radius)
        {
            _material.SetVector(Ids.GroundCircle, new Vector4(x, z, radius, 0f));
        }

        private int GroutPixels()
        {
            return FrameReads.Count(_stage.Render(), pixel => FrameReads.Covered(pixel) && FrameReads.Near(pixel, Grout, 0.1f));
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage();
            _stage.Camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            _stage.Camera.orthographicSize = 3f;
            _stage.LightFrom(Vector3.up);
            _material = TestMaterials.WorldLit("Anime/Ground");
            foreach (int colour in new[] { Ids.GrassColorA, Ids.GrassColorB })
                TestMaterials.SetLinear(_material, colour, Grass);
            foreach (int colour in new[] { Ids.StoneColorA, Ids.StoneColorB })
                TestMaterials.SetLinear(_material, colour, Stone);
            TestMaterials.SetLinear(_material, Ids.GroutColor, Grout);
            _material.SetFloat(Ids.EarthAmount, 0f);
            _material.SetFloat(Ids.GrainStrength, 0f);
            _material.SetFloat(Ids.GroutWidth, 0f);
            _material.SetFloat(Ids.EdgeSoftness, 0.05f);
            _material.SetFloat(Ids.DetailFadeStart, 100f);
            Circle(0f, 0f, 1.5f);
            _floor = TestMeshes.Quad();
            _stage.Add(_floor, _material, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 20f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_floor);
        }

        [Test]
        public void InsideTheCircle_IsStone_OutsideIsGrass()
        {
            Color[] frame = _stage.Render();
            Assert.IsTrue(IsStone(At(frame, 0.5f, 0f)), $"inside {At(frame, 0.5f, 0f)}");
            Assert.IsFalse(IsStone(At(frame, 2.5f, 0f)), $"outside {At(frame, 2.5f, 0f)}");
        }

        [Test]
        public void TheCircle_FollowsItsCentre_AndItsRadius()
        {
            Circle(2.5f, 0f, 1.5f);
            Color[] moved = _stage.Render();
            Assert.IsTrue(IsStone(At(moved, 2.5f, 0f)), "the new centre");
            Assert.IsFalse(IsStone(At(moved, -0.5f, 0f)), "two metres from the new centre");

            Circle(0f, 0f, 3f);
            Assert.IsTrue(IsStone(At(_stage.Render(), 2.5f, 0f)), "inside the doubled radius");
        }

        [Test]
        public void NoRadius_IsAllGrass()
        {
            Circle(0f, 0f, 0f);
            Assert.AreEqual(0, FrameReads.Count(_stage.Render(), pixel => FrameReads.Covered(pixel) && IsStone(pixel)));
        }

        [Test]
        public void Grout_ShowsOnlyWithAWidth_AndOnlyNear()
        {
            Assert.AreEqual(0, GroutPixels(), "no width, no grout");
            _material.SetFloat(Ids.GroutWidth, 0.3f);
            Assert.Greater(GroutPixels(), 0, "grout between the flagstones");
            _material.SetFloat(Ids.DetailFadeStart, 0f);
            _material.SetFloat(Ids.DetailFadeRange, 1f);
            Assert.AreEqual(0, GroutPixels(), "faded out far from the camera");
        }

        [Test]
        public void TheSurface_ReadsTheNoiseAtMostThreeTimes()
        {
            string text = PackSources.IncludeText(Shader.Find("Anime/Ground"), "AnimeGround.hlsl");
            int body = text.IndexOf("half3 AnimeCustomAlbedo", System.StringComparison.Ordinal);
            Assert.LessOrEqual(Regex.Matches(text.Substring(body), @"AnimeGroundNoise\(").Count, 3);
        }
    }
}
