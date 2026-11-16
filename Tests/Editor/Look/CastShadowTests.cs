using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A sphere lit from above with a board over its left half, out of the camera's view: the board's shadow
     * puts the sphere's upper left in the shadow tone, and cast shadow strength 0 takes it away.</summary>
     */
    public class CastShadowTests
    {
        #region Private Fields

        private const float Tolerance = 0.02f;

        private static readonly Color Base = new(0.8f, 0.6f, 0.4f);
        private static readonly Color Tint = new(0.5f, 0.4f, 0.7f);

        private LitStage _stage;
        private Material _material;
        private Material _boardMaterial;
        private Mesh _board;
        private MeshRenderer _boardRenderer;
        private DirectorGlobals _director;

        #endregion

        #region Properties

        private static Color Shadow => new(Base.r * Tint.r, Base.g * Tint.g, Base.b * Tint.b);

        #endregion

        #region Helpers

        private Color[] Render(float castShadowStrength)
        {
            _material.SetFloat(Ids.CastShadowStrength, castShadowStrength);
            return _stage.Render();
        }

        private Color UpperLeft(Color[] frame)
        {
            return FrameReads.At(frame, _stage.Column(-0.5f), _stage.Column(0.7f));
        }

        private Color UpperRight(Color[] frame)
        {
            return FrameReads.At(frame, _stage.Column(0.5f), _stage.Column(0.7f));
        }

        /** <summary>The mean colour of a block of pixels around the covered sample point.</summary> */
        private Color UpperLeftMean(Color[] frame)
        {
            int cx = _stage.Column(-0.5f), cy = _stage.Column(0.7f);
            Color sum = Color.clear;
            int count = 0;
            for (int y = cy - 4; y <= cy + 4; y++)
            {
                for (int x = cx - 4; x <= cx + 4; x++)
                {
                    sum += FrameReads.At(frame, x, y);
                    count++;
                }
            }
            return sum / count;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assume.That(pipeline != null && pipeline.supportsMainLightShadows, "the active URP asset casts no main light shadows");

            _director = new DirectorGlobals();
            _stage = new LitStage();
            _stage.LightFrom(Vector3.up);
            _stage.Light.shadows = LightShadows.Hard;
            _stage.Light.shadowBias = 0.02f;
            _material = TestMaterials.Character(Base, Tint);
            _stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);

            _board = TestMeshes.Quad();
            _boardMaterial = TestMaterials.Character(Color.white, Color.white);
            _boardMaterial.SetFloat(AnimeShaderIds.Switches.Cull, (float)CullMode.Off);
            _boardRenderer = _stage.Add(_board, _boardMaterial, new Vector3(-2f, 2f, 0f), Quaternion.Euler(90f, 0f, 0f),
                Vector3.one * 4f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage?.Dispose();
            _director?.Dispose();
            foreach (Object made in new Object[] { _material, _boardMaterial, _board })
            {
                if (made != null)
                    Object.DestroyImmediate(made);
            }
        }

        [Test]
        public void Board_PutsTheCoveredSideInTheShadowTone()
        {
            Color[] frame = Render(1f);
            Assert.IsTrue(FrameReads.Near(UpperLeft(frame), Shadow, Tolerance), $"covered {UpperLeft(frame)}");
            Assert.IsTrue(FrameReads.Near(UpperRight(frame), Base, Tolerance), $"open {UpperRight(frame)}");
        }

        [Test]
        public void StrengthZero_TakesTheCastShadowAway()
        {
            Color[] frame = Render(0f);
            Assert.IsTrue(FrameReads.Near(UpperLeft(frame), Base, Tolerance), $"covered {UpperLeft(frame)}");
        }

        [Test]
        public void HalfStrength_LandsHalfwayBetweenTheTones()
        {
            Color expected = (Base + Shadow) * 0.5f;
            Color covered = UpperLeft(Render(0.5f));
            Assert.IsTrue(FrameReads.Near(covered, expected, Tolerance), $"covered {covered}, expected {expected}");
        }

        [Test]
        public void AHalfFadedBoard_CastsAPartialShadow()
        {
            Color full = UpperLeftMean(Render(1f));
            _boardRenderer.SetShaderUserValue(128u);
            Color partial = UpperLeftMean(Render(1f));
            Assert.Greater(partial.grayscale, full.grayscale + Tolerance, $"faded {partial}, solid {full}");
            Assert.Less(partial.grayscale, Base.grayscale - Tolerance, $"faded {partial}, lit {Base}");
        }
    }
}
