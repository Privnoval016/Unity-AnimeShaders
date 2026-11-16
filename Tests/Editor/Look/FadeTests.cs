using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /** <summary>The dither fade by the renderer's user value: a rising level only ever removes pixels, line included.</summary> */
    public class FadeTests
    {
        #region Private Fields

        private const string LitPass = "UniversalForward";

        private static readonly uint[] Levels = { 0u, 16u, 64u, 128u, 192u, 240u, 254u, 255u };

        private LitStage _stage;
        private Material _material;
        private MeshRenderer _sphere;

        #endregion

        #region Helpers

        private bool[] CoverageAt(uint level)
        {
            _sphere.SetShaderUserValue(level);
            Color[] frame = _stage.Render();
            var covered = new bool[frame.Length];
            for (int i = 0; i < frame.Length; i++)
                covered[i] = FrameReads.Covered(frame[i]);
            return covered;
        }

        private static int Count(bool[] covered)
        {
            int count = 0;
            foreach (bool pixel in covered)
            {
                if (pixel)
                    count++;
            }
            return count;
        }

        private void AssertNestedFalling()
        {
            bool[] previous = CoverageAt(Levels[0]);
            for (int step = 1; step < Levels.Length; step++)
            {
                bool[] current = CoverageAt(Levels[step]);
                for (int i = 0; i < current.Length; i++)
                    Assert.IsFalse(current[i] && !previous[i], $"pixel {i} came back at level {Levels[step]}");
                Assert.LessOrEqual(Count(current), Count(previous), $"level {Levels[step]}");
                previous = current;
            }
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage();
            _material = TestMaterials.Character(Color.white, Color.gray);
            AnimeMaterials.SetLine(_material, true);
            _material.SetFloat(Ids.OutlineWidth, 40f);
            _sphere = _stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void EachLevel_KeepsOnlyPixelsTheLevelBeforeKept()
        {
            AssertNestedFalling();
        }

        [Test]
        public void TheLineAlone_FadesByTheSameNestedSteps()
        {
            _material.SetShaderPassEnabled(LitPass, false);
            Assert.Greater(Count(CoverageAt(AnimeShaderIds.Fade.Solid)), 0, "the line draws on its own");
            AssertNestedFalling();
        }

        [Test]
        public void CoverageFalls_FromSolidToNothing()
        {
            int solid = Count(CoverageAt(AnimeShaderIds.Fade.Solid));
            int half = Count(CoverageAt(128u));
            int none = Count(CoverageAt(AnimeShaderIds.Fade.Full));
            Assert.Greater(solid, 0);
            Assert.That(half, Is.InRange(solid * 3 / 10, solid * 7 / 10), "about half the pixels at half fade");
            Assert.AreEqual(0, none, "a fully faded body draws nothing, line included");
        }

        [Test]
        public void ReservedBits_DoNotFade()
        {
            int solid = Count(CoverageAt(AnimeShaderIds.Fade.Solid));
            Assert.AreEqual(solid, Count(CoverageAt(~AnimeShaderIds.Fade.LevelMask)));
        }
    }
}
