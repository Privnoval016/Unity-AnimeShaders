using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The front hair's shadow on a face quad, a fringe quad on Anime/Hair in front of its top, the light above
     * and in front: with the hair depth feature, the face just below the fringe is shaded and the face well below it is
     * not; none without the feature, at reach 0, at strength 0, with the hair level with the face, with the hair farther
     * in front than the range, or once the fringe is gone (nothing left from the frame before); at full resolution as at
     * half.</summary>
     */
    public class HairShadowTests
    {
        #region Private Fields

        private const float Shade = 0.5f;
        private const float FringeBottom = 0.3f;
        private const float FringeDepth = -0.2f;
        private const float JustBelow = 0.22f;
        private const float WellBelow = -0.6f;
        private const float WideRange = 0.5f;

        private static readonly Vector3 TowardLight = new Vector3(0f, 1f, -1f).normalized;

        private MadePipeline _pipeline;
        private LitStage _stage;
        private Material _face;
        private Material _hair;
        private Mesh _quad;
        private Texture2D _map;
        private MeshRenderer _fringe;

        #endregion

        #region Helpers

        private void Build(bool withFeature, bool fullResolution = false)
        {
            AnimeHairDepthFeature feature = null;
            if (withFeature)
            {
                feature = ScriptableObject.CreateInstance<AnimeHairDepthFeature>();
                var serialized = new SerializedObject(feature);
                serialized.FindProperty("fullResolution").boolValue = fullResolution;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            _pipeline = withFeature ? new MadePipeline(feature) : new MadePipeline();
            _stage = new LitStage();
            _stage.LightFrom(TowardLight);

            _map = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            _map.SetPixel(0, 0, new Color(0.5f, 1f, 1f, 1f));
            _map.Apply(false);
            _face = Lit(AnimeShaderIds.FaceShader);
            _face.SetTexture(Ids.FaceMap, _map);
            _face.SetFloat(Ids.FaceLightHeight, 0f);
            _face.SetFloat(Ids.FaceShadowSoftness, 1f);
            _face.SetFloat(Ids.FaceShadowBias, 0f);
            _face.SetFloat(Ids.FaceMapStrength, 1f);
            _face.SetFloat(Ids.HairShadowStrength, 1f);
            _face.SetFloat(Ids.HairShadowReach, 0.2f);
            _face.SetFloat(Ids.HairShadowBias, 0.01f);
            _face.SetFloat(Ids.HairShadowSoftness, 0.005f);
            _face.SetFloat(Ids.HairShadowRange, WideRange);
            _hair = Lit(AnimeShaderIds.HairShader);

            _quad = TestMeshes.Quad();
            MeshRenderer face = _stage.Add(_quad, _face, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            var block = new MaterialPropertyBlock();
            block.SetVector(AnimeShaderIds.Head.Forward, Vector3.back);
            block.SetVector(AnimeShaderIds.Head.Right, Vector3.left);
            block.SetVector(AnimeShaderIds.Head.Up, Vector3.up);
            face.SetPropertyBlock(block);
            _fringe = _stage.Add(_quad, _hair, new Vector3(0f, FringeBottom + 1f, FringeDepth), Quaternion.identity, Vector3.one * 2f);
        }

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

        /** <summary>How lit the face is at world height <paramref name="y" /> on the centre column, 0 (shade) to 1.</summary> */
        private float LitAt(Color[] frame, float y)
        {
            int row = Mathf.FloorToInt((y + _stage.Camera.orthographicSize) / (2f * _stage.Camera.orthographicSize) * _stage.Size);
            return (FrameReads.At(frame, _stage.Size / 2, row).g - Shade) / (1f - Shade);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            try
            {
                _stage?.Dispose();
                _pipeline?.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(_face);
                Object.DestroyImmediate(_hair);
                Object.DestroyImmediate(_quad);
                Object.DestroyImmediate(_map);
            }
        }

        [Test]
        public void TheFringe_ShadesTheFaceJustBelowIt_NotWellBelow()
        {
            Build(true);
            Color[] frame = _stage.Render();
            Assert.Less(LitAt(frame, JustBelow), 0.1f, "just below the fringe, its shadow");
            Assert.Greater(LitAt(frame, WellBelow), 0.9f, "well below, lit");
        }

        [Test]
        public void AtFullResolution_TheSame()
        {
            Build(true, true);
            Color[] frame = _stage.Render();
            Assert.Less(LitAt(frame, JustBelow), 0.1f, "shaded");
            Assert.Greater(LitAt(frame, WellBelow), 0.9f, "lit");
        }

        [Test]
        public void WithoutTheFeature_NoShadow()
        {
            Build(false);
            Assert.Greater(LitAt(_stage.Render(), JustBelow), 0.9f);
        }

        [Test]
        public void AtReachZero_OrStrengthZero_NoShadow()
        {
            Build(true);
            _face.SetFloat(Ids.HairShadowReach, 0f);
            Assert.Greater(LitAt(_stage.Render(), JustBelow), 0.9f, "reach 0");
            _face.SetFloat(Ids.HairShadowReach, 0.2f);
            _face.SetFloat(Ids.HairShadowStrength, 0f);
            Assert.Greater(LitAt(_stage.Render(), JustBelow), 0.9f, "strength 0");
        }

        [Test]
        public void HairLevelWithTheFace_CastsNone()
        {
            Build(true);
            _fringe.transform.position = new Vector3(0f, FringeBottom + 1f, -0.005f);
            Assert.Greater(LitAt(_stage.Render(), JustBelow), 0.9f, "within the bias: no shadow acne");
        }

        [Test]
        public void HairFartherThanTheRange_CastsNone()
        {
            Build(true);
            _face.SetFloat(Ids.HairShadowRange, -FringeDepth * 0.5f);
            Assert.Greater(LitAt(_stage.Render(), JustBelow), 0.9f, "the fringe twice the range in front: another's hair, no shadow");
            _face.SetFloat(Ids.HairShadowRange, -FringeDepth * 2f);
            Assert.Less(LitAt(_stage.Render(), JustBelow), 0.1f, "within the range: its shadow");
        }

        [Test]
        public void OnceTheFringeIsGone_NothingIsLeftFromTheFrameBefore()
        {
            Build(true);
            Assert.Less(LitAt(_stage.Render(), JustBelow), 0.1f, "shaded with the fringe");
            _fringe.enabled = false;
            Assert.Greater(LitAt(_stage.Render(), JustBelow), 0.9f, "lit without it");
        }
    }
}
