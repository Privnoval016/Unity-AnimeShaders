using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Contact shading from URP's own screen-space ambient occlusion, in a made pipeline with and without its
     * renderer feature: a cube resting on a lit floor shades the floor at its foot and not the open floor; a material
     * with Contact Shading 0 is the frame without the feature; a renderer without the feature changes nothing.</summary>
     */
    public class ContactShadingTests
    {
        #region Private Fields

        private const int Size = 128;

        private LitStage _stage;
        private MadePipeline _pipeline;
        private Material _floor;
        private Material _block;
        private Mesh _quad;
        private Mesh _cube;

        #endregion

        #region Helpers

        private void Stage(bool withOcclusion)
        {
            _pipeline = withOcclusion ? new MadePipeline(Occlusion()) : new MadePipeline();
            _stage = new LitStage(Size);
            _stage.Camera.orthographic = false;
            _stage.Camera.fieldOfView = 40f;
            _stage.Camera.transform.SetPositionAndRotation(new Vector3(0f, 4f, -4f), Quaternion.Euler(45f, 0f, 0f));
            _stage.LightFrom(Vector3.up);
            _stage.Add(_quad, _floor, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 20f);
            _stage.Add(_cube, _block, new Vector3(0f, 0.5f, 0f), Quaternion.identity, Vector3.one);
        }

        private void Unstage()
        {
            try
            {
                _stage?.Dispose();
            }
            finally
            {
                _pipeline?.Dispose();
                _stage = null;
                _pipeline = null;
            }
        }

        /** <summary>URP's occlusion feature reaching half a metre at full intensity (its default reach is a few centimetres).</summary> */
        private static ScreenSpaceAmbientOcclusion Occlusion()
        {
            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            var serialized = new SerializedObject(feature);
            serialized.FindProperty("m_Settings.Radius").floatValue = 0.5f;
            serialized.FindProperty("m_Settings.Intensity").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return feature;
        }

        /** <summary>The floor's pixel at a world point.</summary> */
        private Color FloorAt(Color[] frame, Vector3 point)
        {
            Vector3 screen = _stage.Camera.WorldToViewportPoint(point);
            return FrameReads.At(frame, Mathf.Clamp(Mathf.RoundToInt(screen.x * Size), 0, Size - 1), Mathf.Clamp(Mathf.RoundToInt(screen.y * Size), 0, Size - 1));
        }

        private Color[] Render()
        {
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.AllFinite(frame), "every pixel finite");
            Assert.Greater(FrameReads.Count(frame, FrameReads.Covered), 0, "something drew");
            return frame;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _floor = TestMaterials.WorldLit("Anime/World");
            _floor.SetFloat(Ids.ContactShading, 1f);
            _floor.SetFloat(Ids.ContactThreshold, 0.9f);
            _block = TestMaterials.WorldLit("Anime/World");
            _quad = TestMeshes.Quad();
            _cube = TestMeshes.HardCube();
        }

        [TearDown]
        public void TearDown()
        {
            Unstage();
            Object.DestroyImmediate(_floor);
            Object.DestroyImmediate(_block);
            Object.DestroyImmediate(_quad);
            Object.DestroyImmediate(_cube);
            Assert.IsFalse(MadePipeline.Active, "the project's pipeline is back");
        }

        [Test]
        public void ACubesFoot_ShadesTheFloor_AndTheOpenFloorDoesNot()
        {
            var foot = new Vector3(0f, 0f, -0.62f);
            var open = new Vector3(2.5f, 0f, -1.5f);
            Stage(withOcclusion: false);
            Color[] plain = Render();
            Unstage();
            Stage(withOcclusion: true);
            Color[] occluded = Render();
            Assert.Less(FloorAt(occluded, foot).r, FloorAt(plain, foot).r - 0.1f, $"at the foot: {FloorAt(occluded, foot)} against {FloorAt(plain, foot)}");
            Assert.AreEqual(FloorAt(plain, open).r, FloorAt(occluded, open).r, 0.02f, "the open floor");
        }

        [Test]
        public void NoContactShading_IsTheFrameWithoutOcclusion()
        {
            var foot = new Vector3(0f, 0f, -0.62f);
            _floor.SetFloat(Ids.ContactShading, 0f);
            Stage(withOcclusion: false);
            Color plain = FloorAt(Render(), foot);
            Unstage();
            Stage(withOcclusion: true);
            Color occluded = FloorAt(Render(), foot);
            Assert.AreEqual(plain.r, occluded.r, 0.005f, "a material that takes none");
        }
    }
}
