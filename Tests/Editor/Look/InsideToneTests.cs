using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A two-sided quad lit from the camera's side: its front shows the lit tone, its back the inside tone
     * when on and the lit tone when off; a mirrored instance still shows its front.</summary>
     */
    public class InsideToneTests
    {
        #region Private Fields

        private const float Tolerance = 0.01f;

        private static readonly Color Base = new(0.8f, 0.6f, 0.4f);

        private LitStage _stage;
        private Material _material;
        private Mesh _quad;

        #endregion

        #region Helpers

        private Color CentreOf(Quaternion rotation, Vector3 scale)
        {
            MeshRenderer renderer = _stage.Add(_quad, _material, Vector3.zero, rotation, scale);
            Color pixel = FrameReads.At(_stage.Render(), _stage.Size / 2, _stage.Size / 2);
            Object.DestroyImmediate(renderer.gameObject);
            return pixel;
        }

        private void InsideTone(bool on)
        {
            AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.InsideToneName, AnimeShaderIds.Switches.InsideTone, on);
        }

        /** <summary>The inside tone as specified: tinted, saturation scaled about its luminance, darkened.</summary> */
        private static Color Inside(Color albedo, Color tint, float saturation, float darken)
        {
            Color tinted = new(albedo.r * tint.r, albedo.g * tint.g, albedo.b * tint.b);
            float luma = 0.2126f * tinted.r + 0.7152f * tinted.g + 0.0722f * tinted.b;
            Color grey = new(luma, luma, luma);
            return Color.LerpUnclamped(grey, tinted, saturation) * (1f - darken);
        }

        /** <summary>The back of the quad, twice its size, with the inside fade along <paramref name="axis" /> from <paramref name="start" /> to <paramref name="end" />; returns the frame.</summary> */
        private Color[] FadedBack(float axis, float start, float end)
        {
            InsideTone(true);
            AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.InsideFadeName, AnimeShaderIds.Switches.InsideFade, true);
            _material.SetFloat(Ids.InsideFadeAxis, axis);
            _material.SetFloat(Ids.InsideFadeStart, start);
            _material.SetFloat(Ids.InsideFadeEnd, end);
            MeshRenderer renderer = _stage.Add(_quad, _material, Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2f);
            Color[] frame = _stage.Render();
            Object.DestroyImmediate(renderer.gameObject);
            return frame;
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage();
            _stage.LightFrom(Vector3.back);
            _quad = TestMeshes.Quad();
            _material = TestMaterials.Character(Base, new Color(0.5f, 0.5f, 0.5f));
            _material.SetFloat(AnimeShaderIds.Switches.Cull, (float)CullMode.Off);
            TestMaterials.SetLinear(_material, Ids.InsideTint, new Color(1f, 0.5f, 0.5f));
            _material.SetFloat(Ids.InsideDarken, 0.3f);
            _material.SetFloat(Ids.InsideSaturation, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_quad);
        }

        [Test]
        public void Off_BothSidesShowTheLitTone()
        {
            InsideTone(false);
            Assert.IsTrue(FrameReads.Near(CentreOf(Quaternion.identity, Vector3.one), Base, Tolerance));
            Assert.IsTrue(FrameReads.Near(CentreOf(Quaternion.Euler(0f, 180f, 0f), Vector3.one), Base, Tolerance));
        }

        [Test]
        public void On_OnlyTheBackChanges()
        {
            InsideTone(true);
            Color front = CentreOf(Quaternion.identity, Vector3.one);
            Color back = CentreOf(Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            Color expected = Inside(Base, new Color(1f, 0.5f, 0.5f), 1f, 0.3f);
            Assert.IsTrue(FrameReads.Near(front, Base, Tolerance), $"front {front}");
            Assert.IsTrue(FrameReads.Near(back, expected, Tolerance), $"back {back}, expected {expected}");
        }

        [Test]
        public void On_AMirroredInstance_KeepsItsFrontOutside()
        {
            InsideTone(true);
            Color mirrored = CentreOf(Quaternion.identity, new Vector3(-1f, 1f, 1f));
            Assert.IsTrue(FrameReads.Near(mirrored, Base, Tolerance), $"mirrored front {mirrored}");
        }

        [Test]
        public void On_AMirroredInstanceSeenFromBehind_ShowsItsInside()
        {
            InsideTone(true);
            Color mirroredBack = CentreOf(Quaternion.Euler(0f, 180f, 0f), new Vector3(-1f, 1f, 1f));
            Assert.IsFalse(FrameReads.Near(mirroredBack, Base, 0.05f), $"mirrored back {mirroredBack}");
        }

        [Test]
        public void Saturation_And_Darken_ShapeTheInside()
        {
            InsideTone(true);
            _material.SetFloat(Ids.InsideSaturation, 0f);
            _material.SetFloat(Ids.InsideDarken, 0.6f);
            Color back = CentreOf(Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            Color expected = Inside(Base, new Color(1f, 0.5f, 0.5f), 0f, 0.6f);
            Assert.IsTrue(FrameReads.Near(back, expected, Tolerance), $"back {back}, expected {expected}");
        }

        [Test]
        public void Fade_RampsAlongV()
        {
            Color[] frame = FadedBack(1f, 0f, 1f);
            int column = _stage.Size / 2;
            Color low = FrameReads.At(frame, column, _stage.Column(-0.8f));
            Color high = FrameReads.At(frame, column, _stage.Column(0.8f));
            Assert.Less(Distance(low, Base), Distance(high, Base), $"low {low}, high {high}");
        }

        [Test]
        public void Fade_RampsAlongU_AndReversesWithItsEnds()
        {
            int row = _stage.Size / 2;
            Color[] forward = FadedBack(0f, 0f, 1f);
            Color uLow = FrameReads.At(forward, _stage.Column(0.8f), row);
            Color uHigh = FrameReads.At(forward, _stage.Column(-0.8f), row);
            Assert.Less(Distance(uLow, Base), Distance(uHigh, Base), $"u low {uLow}, u high {uHigh}");

            Color[] reversed = FadedBack(0f, 1f, 0f);
            Color reversedLow = FrameReads.At(reversed, _stage.Column(0.8f), row);
            Color reversedHigh = FrameReads.At(reversed, _stage.Column(-0.8f), row);
            Assert.Greater(Distance(reversedLow, Base), Distance(reversedHigh, Base), "start and end swapped");
        }
    }
}
