using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The angel ring on a sphere of hair (radius 1) facing the camera, its head frame at its centre: the band
     * sits at its height on the head and nowhere well away from it; none in shade, at strength 0 (as Anime/Character) or
     * without a head frame; following the view moves it to where the hair meets the view at that height; the breakup
     * gives each strand's stroke its own height and length; the strand gap draws thin lines through the band; what it adds
     * never passes the cap; the hair's tint gives it the hair's hue.</summary>
     */
    public class HairRingTests
    {
        #region Private Fields

        private const float Shade = 0.5f;
        private const float Height = 0.5f;
        private const float Width = 0.05f;
        private const float Close = 0.01f;

        private static readonly Vector3 FromFront = new Vector3(0f, 0.3f, -1f).normalized;

        private LitStage _stage;
        private Material _hair;
        private MeshRenderer _renderer;

        #endregion

        #region Helpers

        private static Material Lit(string shader, Color albedo)
        {
            var material = new Material(Shader.Find(shader));
            TestMaterials.SetLinear(material, Ids.BaseColor, albedo);
            TestMaterials.SetLinear(material, Ids.ShadowTint, new Color(Shade, Shade, Shade));
            material.SetFloat(Ids.LightInfluence, 0f);
            material.SetFloat(Ids.AmbientStrength, 0f);
            material.SetFloat(Ids.CastShadowStrength, 0f);
            material.SetFloat(Ids.ContactShading, 0f);
            material.SetShaderPassEnabled(AnimeShaderIds.OutlineLightMode, false);
            return material;
        }

        private void HeadFrame()
        {
            var block = new MaterialPropertyBlock();
            block.SetVector(AnimeShaderIds.Head.Centre, Vector3.zero);
            block.SetVector(AnimeShaderIds.Head.Forward, Vector3.back);
            block.SetVector(AnimeShaderIds.Head.Right, Vector3.left);
            block.SetVector(AnimeShaderIds.Head.Up, Vector3.up);
            _renderer.SetPropertyBlock(block);
        }

        private int Row(float y)
        {
            return Mathf.FloorToInt((y + _stage.Camera.orthographicSize) / (2f * _stage.Camera.orthographicSize) * _stage.Size);
        }

        /** <summary>What the ring adds at every pixel: the frame with it less the frame at strength 0.</summary> */
        private Color[] Added()
        {
            float strength = _hair.GetFloat(Ids.RingStrength);
            Color[] with = _stage.Render();
            _hair.SetFloat(Ids.RingStrength, 0f);
            Color[] without = _stage.Render();
            _hair.SetFloat(Ids.RingStrength, strength);
            return with.Select((pixel, i) => pixel - without[i]).ToArray();
        }

        /** <summary>The height on the centre column where the ring adds most.</summary> */
        private float Peak(Color[] added)
        {
            int best = 0;
            for (int row = 0; row < _stage.Size; row++)
            {
                if (added[row * _stage.Size + _stage.Size / 2].g > added[best * _stage.Size + _stage.Size / 2].g)
                    best = row;
            }
            return (best + 0.5f) / _stage.Size * 2f * _stage.Camera.orthographicSize - _stage.Camera.orthographicSize;
        }

        private float At(Color[] added, float x, float y)
        {
            return added[Row(y) * _stage.Size + _stage.Column(x)].g;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage();
            _stage.LightFrom(FromFront);
            _hair = Lit(AnimeShaderIds.HairShader, Color.white);
            _hair.SetFloat(Ids.RingStrength, 1f);
            _hair.SetFloat(Ids.RingHeight, Height);
            _hair.SetFloat(Ids.RingWidth, Width);
            _hair.SetFloat(Ids.RingBreakup, 0f);
            _hair.SetFloat(Ids.RingStrandGap, 0f);
            _hair.SetFloat(Ids.RingBreakupShape, 0f);
            _hair.SetFloat(Ids.RingGlow, 0f);
            _hair.SetFloat(Ids.RingFacing, 0f);
            _hair.SetFloat(Ids.RingHairTint, 0f);
            _hair.SetFloat(Ids.RingLightColor, 0f);
            _renderer = _stage.Add(TestMeshes.Sphere(), _hair, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            HeadFrame();
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_hair);
        }

        [Test]
        public void TheRing_SitsAtItsHeight()
        {
            Color[] added = Added();
            Assert.AreEqual(Height, Peak(added), 0.06f, "the band's centre on the head");
            Assert.Greater(At(added, 0f, Height), 0.2f, "visible");
            Assert.Less(At(added, 0f, 0f), Close, "none at the middle of the head");
            Assert.Less(At(added, 0f, -Height), Close, "none below");
        }

        [Test]
        public void InShade_NoRing()
        {
            _stage.LightFrom(Vector3.forward);
            Assert.Less(At(Added(), 0f, Height), Close, "the band's middle, facing away from the light");
        }

        [Test]
        public void AtStrengthZero_ItIsTheCharacterShader()
        {
            _hair.SetFloat(Ids.RingStrength, 0f);
            Color hair = FrameReads.At(_stage.Render(), _stage.Size / 2, Row(Height));
            Material plain = Lit("Anime/Character", Color.white);
            try
            {
                _renderer.sharedMaterial = plain;
                Assert.IsTrue(FrameReads.Near(hair, FrameReads.At(_stage.Render(), _stage.Size / 2, Row(Height)), Close), $"hair {hair}");
            }
            finally
            {
                _renderer.sharedMaterial = _hair;
                Object.DestroyImmediate(plain);
            }
        }

        [Test]
        public void WithoutAHeadFrame_NoRing()
        {
            _renderer.SetPropertyBlock(null);
            Assert.Less(Added().Max(pixel => pixel.g), Close);
        }

        [Test]
        public void FollowingTheView_MovesItToWhereTheHairMeetsTheView()
        {
            _hair.SetFloat(Ids.RingFollowsView, 1f);
            float expected = Mathf.Sqrt(1f - Height * Height);
            Assert.AreEqual(expected, Peak(Added()), 0.08f, "where the normal meets the view at the ring's height");
        }

        [Test]
        public void TheBreakup_GivesEachStrandItsOwnStroke()
        {
            float edge = Height - Width * 0.5f;
            float Spread(Color[] added)
            {
                float[] row = Enumerable.Range(0, 40).Select(i => At(added, -0.4f + i * 0.02f, edge)).ToArray();
                return row.Max() - row.Min();
            }
            float clean = Spread(Added());
            _hair.SetFloat(Ids.RingBreakup, 1f);
            _hair.SetFloat(Ids.RingBreakupShape, 1f);
            float broken = Spread(Added());
            Assert.Less(clean, 0.1f, "a clean band: even along its lower edge");
            Assert.Greater(broken, clean + 0.2f, "broken: strands in and out along it");
        }

        [Test]
        public void TheStrandGap_DrawsThinLinesThroughTheBand()
        {
            float Spread(Color[] added)
            {
                float[] row = Enumerable.Range(0, 40).Select(i => At(added, -0.4f + i * 0.02f, Height)).ToArray();
                return row.Max() - row.Min();
            }
            float solid = Spread(Added());
            _hair.SetFloat(Ids.RingStrandGap, 0.4f);
            float lined = Spread(Added());
            Assert.Less(solid, 0.1f, "no gap: even along the band's middle");
            Assert.Greater(lined, solid + 0.2f, "gaps: dark lines between the strands");
        }

        [Test]
        public void WhatItAdds_NeverPassesTheCap()
        {
            _hair.SetFloat(Ids.RingStrength, 2f);
            TestMaterials.SetLinear(_hair, Ids.RingColor, new Color(4f, 4f, 4f));
            _hair.SetFloat(Ids.RingBrightest, 0.1f);
            Assert.LessOrEqual(Added().Max(pixel => pixel.g), 0.1f + Close);
        }

        [Test]
        public void TheHairTint_GivesItTheHairsHue()
        {
            _hair.SetFloat(Ids.RingHairTint, 1f);
            _hair.SetFloat(Ids.RingStrength, 0.4f);
            _hair.SetFloat(Ids.RingBrightest, 2f);
            TestMaterials.SetLinear(_hair, Ids.BaseColor, new Color(0.8f, 0.3f, 0.3f));
            Color added = Added()[Row(Height) * _stage.Size + _stage.Size / 2];
            Assert.Greater(added.r, added.g * 2f, $"redder, as the hair: {added}");
        }
    }
}
