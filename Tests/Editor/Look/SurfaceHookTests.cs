using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>An entry shader's surface hook (ANIME_CUSTOM_SURFACE), through the test probe: what it does to the
     * colour and to the normal both reach the lit frame.</summary>
     */
    public class SurfaceHookTests
    {
        #region Private Fields

        private static readonly int Tint = Shader.PropertyToID("_ProbeTint");
        private static readonly int Tilt = Shader.PropertyToID("_ProbeTilt");

        private LitStage _stage;
        private Material _material;
        private Mesh _quad;

        #endregion

        #region Helpers

        private Color Centre()
        {
            return FrameReads.At(_stage.Render(), _stage.Size / 2, _stage.Size / 2);
        }

        /** <summary>The lit value at the centre with the light from <paramref name="towardLight" />, a flat face sitting mid-ramp.</summary> */
        private float LitFrom(Vector3 towardLight)
        {
            _stage.LightFrom(towardLight);
            _material.SetFloat(Ids.ShadowThreshold, Vector3.Dot(Vector3.back, towardLight.normalized) * 0.5f + 0.5f);
            _material.SetFloat(Ids.ShadowSoftness, 0.4f);
            return Centre().g;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage();
            _material = new Material(Shader.Find("Hidden/AnimeShaders/Tests/SurfaceProbe"));
            TestMaterials.SetLinear(_material, Ids.BaseColor, Color.white);
            TestMaterials.SetLinear(_material, Ids.ShadowTint, new Color(0.5f, 0.5f, 0.5f));
            _material.SetFloat(Ids.LightInfluence, 0f);
            _material.SetFloat(Ids.AmbientStrength, 0f);
            _material.SetFloat(Ids.CastShadowStrength, 0f);
            _material.SetFloat(Ids.ShadowThreshold, 0.5f);
            _material.SetFloat(Ids.ShadowSoftness, 0.004f);
            _quad = TestMeshes.Quad();
            _stage.Add(_quad, _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_quad);
        }

        [Test]
        public void TheHooksColour_ReachesTheFrame()
        {
            Assert.IsTrue(FrameReads.Near(Centre(), Color.white, 0.02f), "untinted");
            var tint = new Color(0.2f, 0.9f, 0.3f);
            TestMaterials.SetLinear(_material, Tint, tint);
            Assert.IsTrue(FrameReads.Near(Centre(), tint, 0.02f), $"tinted {Centre()}");
        }

        [Test]
        public void TheHooksNormal_ReachesTheLighting()
        {
            Assert.AreEqual(LitFrom(new Vector3(1f, 0f, -1f)), LitFrom(new Vector3(-1f, 0f, -1f)), 0.01f, "flat: lit the same from either side");
            _material.SetFloat(Tilt, 0.8f);
            Assert.Greater(LitFrom(new Vector3(1f, 0f, -1f)), LitFrom(new Vector3(-1f, 0f, -1f)) + 0.05f, "tilted toward +X: lit from +X");
        }
    }
}
