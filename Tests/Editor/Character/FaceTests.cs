using System;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Anime/Face on a quad facing the camera, its head facing the camera too (so the head's right is the
     * world's -X): its form comes from the face map, not its normal, picking the side the map points; across a map whose
     * direction turns a full circle it lights where plain Lambert round the horizon does, at eight light angles; the bias
     * lights more of it; a light's height never moves the line, dims it by Light Height and never breaks it; with no face weight, or no head
     * frame, it shades as Anime/Character.</summary>
     */
    public class FaceTests
    {
        #region Private Fields

        private const int MapWidth = 256;
        private const float Shade = 0.5f;
        private const float Close = 0.02f;
        private const float TerminatorDegrees = 6f;

        private static readonly Vector3 HeadForward = Vector3.back;
        private static readonly Vector3 HeadUp = Vector3.up;
        private static readonly Vector3 HeadRight = Vector3.Cross(HeadUp, HeadForward);

        private LitStage _stage;
        private Material _material;
        private Mesh _quad;
        private MeshRenderer _renderer;
        private Texture2D _map;

        #endregion

        #region Helpers

        private static Material Lit(string shader)
        {
            var material = new Material(Shader.Find(shader));
            TestMaterials.SetLinear(material, Ids.BaseColor, Color.white);
            TestMaterials.SetLinear(material, Ids.ShadowTint, new Color(Shade, Shade, Shade));
            material.SetFloat(Ids.LightInfluence, 0f);
            material.SetFloat(Ids.AmbientStrength, 0f);
            material.SetFloat(Ids.CastShadowStrength, 0f);
            material.SetFloat(Ids.ContactShading, 0f);
            material.SetFloat(Ids.ShadowThreshold, 0.5f);
            material.SetFloat(Ids.ShadowSoftness, 0.004f);
            return material;
        }

        /** <summary>A face map whose direction at u (x right, y front of the head) is <paramref name="direction" />, with the given face weight.</summary> */
        private void Map(Func<float, Vector2> direction, float weight = 1f)
        {
            if (_map != null)
                Object.DestroyImmediate(_map);
            _map = new Texture2D(MapWidth, 1, TextureFormat.RGBAHalf, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[MapWidth];
            for (int i = 0; i < MapWidth; i++)
            {
                Vector2 d = direction((i + 0.5f) / MapWidth).normalized;
                pixels[i] = new Color(d.x * 0.5f + 0.5f, d.y * 0.5f + 0.5f, weight, 1f);
            }
            _map.SetPixels(pixels);
            _map.Apply(false);
            _material.SetTexture(Ids.FaceMap, _map);
        }

        private void HeadFrame()
        {
            var block = new MaterialPropertyBlock();
            block.SetVector(AnimeShaderIds.Head.Forward, HeadForward);
            block.SetVector(AnimeShaderIds.Head.Right, HeadRight);
            block.SetVector(AnimeShaderIds.Head.Up, HeadUp);
            _renderer.SetPropertyBlock(block);
        }

        /** <summary>The direction toward a light at <paramref name="degrees" /> round the head (0 its right, 90 its front), raised by <paramref name="height" />.</summary> */
        private static Vector3 RoundTheHead(float degrees, float height = 0f)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return (HeadRight * Mathf.Cos(radians) + HeadForward * Mathf.Sin(radians) + HeadUp * height).normalized;
        }

        /** <summary>How lit the pixel is, 0 (the shade tone) to 1 (lit).</summary> */
        private static float LitOf(Color pixel)
        {
            return (pixel.g - Shade) / (1f - Shade);
        }

        private float CentreLit(Vector3 towardLight)
        {
            _stage.LightFrom(towardLight);
            return LitOf(FrameReads.Centre(_stage.Render()));
        }

        private Color[] RenderFrom(Vector3 towardLight)
        {
            _stage.LightFrom(towardLight);
            return _stage.Render();
        }

        private Color PlainCentre(Vector3 towardLight)
        {
            Material plain = Lit("Anime/Character");
            try
            {
                _renderer.sharedMaterial = plain;
                _stage.LightFrom(towardLight);
                return FrameReads.Centre(_stage.Render());
            }
            finally
            {
                _renderer.sharedMaterial = _material;
                Object.DestroyImmediate(plain);
            }
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage();
            _material = Lit(AnimeShaderIds.FaceShader);
            _material.SetFloat(Ids.FaceShadowSoftness, 1f);
            _material.SetFloat(Ids.FaceLightHeight, 0f);
            _quad = TestMeshes.Quad();
            _renderer = _stage.Add(_quad, _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            HeadFrame();
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_quad);
            if (_map != null)
                Object.DestroyImmediate(_map);
        }

        [Test]
        public void TheMap_PicksTheLitSide_NotTheNormal()
        {
            Map(_ => Vector2.right);
            Assert.Greater(CentreLit(RoundTheHead(0f)), 0.95f, "light from the head's right: the side the map points");
            Assert.Less(CentreLit(RoundTheHead(180f)), 0.05f, "light from its left: shade");
        }

        [Test]
        public void AcrossAMapTurningAFullCircle_ItLightsAsLambert_AtEightAngles()
        {
            Map(u => new Vector2(Mathf.Cos(u * 2f * Mathf.PI), Mathf.Sin(u * 2f * Mathf.PI)));
            for (int step = 0; step < 8; step++)
            {
                float light = step * 45f + 10f;
                _stage.LightFrom(RoundTheHead(light));
                Color[] frame = _stage.Render();
                int checkedColumns = 0;
                for (float u = 0.02f; u < 0.98f; u += 0.01f)
                {
                    float angle = u * 360f;
                    float apart = Mathf.Abs(Mathf.DeltaAngle(angle, light));
                    if (Mathf.Abs(apart - 90f) < TerminatorDegrees)
                        continue;
                    float lit = LitOf(FrameReads.At(frame, _stage.Column(u * 2f - 1f), _stage.Size / 2));
                    Assert.AreEqual(apart < 90f ? 1f : 0f, lit, 0.1f, $"light at {light}, texel facing {angle}");
                    checkedColumns++;
                }
                Assert.Greater(checkedColumns, 60, "columns across the quad");
            }
        }

        [Test]
        public void TheBias_LightsMoreOfTheFace()
        {
            Map(_ => Vector2.up);
            float even = CentreLit(RoundTheHead(0f));
            Assert.AreEqual(0.5f, even, 0.1f, "light square to the stored direction: on the line");
            _material.SetFloat(Ids.FaceShadowBias, 0.2f);
            Assert.Greater(CentreLit(RoundTheHead(0f)), 0.95f, "biased up: lit");
            _material.SetFloat(Ids.FaceShadowBias, -0.2f);
            Assert.Less(CentreLit(RoundTheHead(0f)), 0.05f, "biased down: shade");
        }

        [Test]
        public void ALightAbove_DimsTheFaceByLightHeight()
        {
            Map(_ => Vector2.up);
            Vector3 raised = RoundTheHead(90f, 1f);
            float full = CentreLit(raised);
            Assert.Greater(full, 0.95f, "Light Height 0: lit as from the front");
            _material.SetFloat(Ids.FaceLightHeight, 1f);
            float upness = Vector3.Dot(raised, HeadUp);
            Assert.AreEqual(1f - upness, CentreLit(raised) / full, Close, "Light Height 1: dimmed by how far up the light is");
            _material.SetFloat(Ids.FaceLightHeight, 0.5f);
            Assert.AreEqual(1f - 0.5f * upness, CentreLit(raised) / full, Close, "halfway: half as much");
        }

        [Test]
        public void ALightsHeight_DoesNotMoveTheLine()
        {
            Map(_ => Vector2.up);
            _material.SetFloat(Ids.FaceShadowSoftness, 60f);
            float level = CentreLit(RoundTheHead(10f));
            Assert.That(level, Is.InRange(0.2f, 0.95f), "inside the soft edge");
            Assert.AreEqual(level, CentreLit(RoundTheHead(10f, 1f)), Close, "raised: the same place on the line");
        }

        [Test]
        public void ALightStraightAbove_IsTakenAsFromTheFront()
        {
            Map(_ => Vector2.up);
            _stage.LightFrom(HeadUp);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.AllFinite(frame), "no NaN with no horizontal part");
            Assert.Greater(LitOf(FrameReads.Centre(frame)), 0.95f, "a front-facing texel lit");
        }

        [Test]
        public void NoFaceWeight_ShadesAsTheCharacterShader()
        {
            Map(_ => Vector2.right, 0f);
            Vector3 light = new Vector3(0.6f, 0.3f, -1f).normalized;
            Color face = FrameReads.Centre(RenderFrom(light));
            Assert.IsTrue(FrameReads.Near(face, PlainCentre(light), Close), $"face {face}");
        }

        [Test]
        public void NoHeadFrame_ShadesAsTheCharacterShader()
        {
            Map(_ => Vector2.right);
            _renderer.SetPropertyBlock(null);
            Vector3 light = new Vector3(-0.6f, 0.3f, -1f).normalized;
            Color face = FrameReads.Centre(RenderFrom(light));
            Assert.IsTrue(FrameReads.Near(face, PlainCentre(light), Close), $"face {face}");
        }
    }
}
