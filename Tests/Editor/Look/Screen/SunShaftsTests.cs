using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using SkyIds = AnimeShaders.AnimeShaderIds.Sky;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The sun shafts, drawn by <see cref="AnimeSunShaftsFeature" /> in a made pipeline and set by a made volume
     * only the stage camera sees, over an Anime/Sky skybox with a black wall across the left of the view: at intensity 0,
     * or with the sun behind the camera, nothing changes; on, light is added near the sun where the sky shows, and less
     * over the wall away from it; with the sun just off the edge of the view, every pixel stays finite.</summary>
     */
    public class SunShaftsTests
    {
        #region Private Fields

        private const int Size = 128;
        private const float Aside = 0.12f;

        private static readonly Vector3 TowardSun = new(0.15f, 0.1f, 1f);

        private MadePipeline _pipeline;
        private LitStage _stage;
        private MadeVolume _volume;
        private AnimeSunShafts _shafts;
        private Material _sky;
        private Material _wall;
        private Mesh _quad;

        #endregion

        #region Helpers

        private Color[] Render()
        {
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.AllFinite(frame), "every pixel finite");
            Assert.AreEqual(frame.Length, FrameReads.Count(frame, FrameReads.Covered), "the sky and wall fill the frame");
            return frame;
        }

        private static float Added(Color[] off, Color[] on, float across, float up)
        {
            Color a = FrameReads.At(off, Mathf.RoundToInt(across * (Size - 1)), Mathf.RoundToInt(up * (Size - 1)));
            Color b = FrameReads.At(on, Mathf.RoundToInt(across * (Size - 1)), Mathf.RoundToInt(up * (Size - 1)));
            return (b.r + b.g + b.b) - (a.r + a.g + a.b);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _pipeline = new MadePipeline(ScriptableObject.CreateInstance<AnimeSunShaftsFeature>());
            _stage = new LitStage(Size);
            _stage.Perspective(60f, 0f);
            _stage.Camera.farClipPlane = 1000f;
            _stage.Camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            _sky = TestMaterials.Sky();
            TestMaterials.SetLinear(_sky, SkyIds.ZenithColor, new Color(0.2f, 0.3f, 0.7f));
            TestMaterials.SetLinear(_sky, SkyIds.HorizonColor, new Color(0.4f, 0.5f, 0.8f));
            _sky.SetFloat(SkyIds.SunStrength, 3f);
            _sky.SetFloat(SkyIds.SunHaloStrength, 1f);
            _stage.Camera.clearFlags = CameraClearFlags.Skybox;
            _stage.Camera.gameObject.AddComponent<Skybox>().material = _sky;
            _wall = TestMaterials.WorldLit("Anime/World");
            TestMaterials.SetLinear(_wall, Ids.BaseColor, Color.black);
            _quad = TestMeshes.Quad();
            _stage.Add(_quad, _wall, new Vector3(-4f, 0f, 10f), Quaternion.identity, new Vector3(10f, 20f, 1f));
            _stage.LightFrom(TowardSun);
            _volume = new MadeVolume(_stage.Camera);
            _shafts = _volume.Add<AnimeSunShafts>();
            _shafts.intensity.value = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                try
                {
                    _stage?.Dispose();
                }
                finally
                {
                    _volume?.Dispose();
                    Object.DestroyImmediate(_sky);
                    Object.DestroyImmediate(_wall);
                    Object.DestroyImmediate(_quad);
                }
            }
            finally
            {
                _pipeline?.Dispose();
            }
            Assert.IsFalse(MadePipeline.Active, "the project's pipeline is back");
        }

        [Test]
        public void AtNoIntensity_NothingChanges()
        {
            Color[] zero = Render();
            _volume.Dispose();
            _volume = null;
            Color[] none = Render();
            Assert.IsTrue(zero.SequenceEqual(none), "intensity 0 is the frame with no shafts at all");
        }

        [Test]
        public void On_LightStreamsFromTheSkyNearTheSun_AndLessOverTheWallBesideIt()
        {
            Vector3 sun = _stage.Camera.WorldToViewportPoint(TowardSun.normalized * 100f);
            Color[] off = Render();
            _shafts.intensity.value = 2f;
            Color[] on = Render();
            float overSky = Added(off, on, sun.x + Aside, sun.y);
            float overWall = Added(off, on, sun.x - Aside, sun.y);
            Assert.Greater(overSky, 0.05f, "shafts over the sky beside the sun");
            Assert.Less(overWall, overSky * 0.6f, $"less over the wall as far the other side ({overWall} against {overSky})");
        }

        [Test]
        public void WithTheSunBehind_NothingChanges()
        {
            _stage.LightFrom(new Vector3(0.15f, 0.1f, -1f));
            Color[] off = Render();
            _shafts.intensity.value = 2f;
            Assert.IsTrue(off.SequenceEqual(Render()), "the sun behind the camera draws no shafts");
        }

        [Test]
        public void ACameraWithoutPostProcessing_GetsNone()
        {
            _stage.Camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Color[] off = Render();
            _shafts.intensity.value = 2f;
            Assert.IsTrue(off.SequenceEqual(Render()), "a menu figure's or portrait's camera takes no shafts");
        }

        [Test]
        public void WithTheSunJustOffTheEdge_EveryPixelStaysFinite()
        {
            _stage.LightFrom(new Vector3(0.75f, 0.1f, 1f));
            _shafts.intensity.value = 2f;
            Render();
        }
    }
}
