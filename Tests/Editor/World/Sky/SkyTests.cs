using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.Sky;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Anime/Sky as a camera's own skybox (never the scene's): its gradient where the made colours put it, the
     * sun disc where the light points, nothing but the disc above the bloom ceiling, clouds and stars only above the
     * horizon.</summary>
     */
    public class SkyTests
    {
        #region Private Fields

        private static readonly Color Zenith = new(0.1f, 0.2f, 0.8f);
        private static readonly Color Horizon = new(0.7f, 0.8f, 0.9f);
        private static readonly Color Below = new(0.3f, 0.3f, 0.3f);
        private const float Ceiling = 0.9f;

        private LitStage _stage;
        private Material _sky;

        #endregion

        #region Helpers

        private Color[] Looking(Vector3 forward)
        {
            _stage.Camera.transform.rotation = Quaternion.LookRotation(forward, Mathf.Abs(forward.y) > 0.99f ? Vector3.forward : Vector3.up);
            return _stage.Render();
        }

        private Color Centre(Color[] frame)
        {
            return FrameReads.At(frame, _stage.Size / 2, _stage.Size / 2);
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private static float Brightest(Color pixel)
        {
            return Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage(64);
            _stage.Perspective(60f, 0f);
            _sky = new Material(Shader.Find("Anime/Sky"));
            TestMaterials.SetLinear(_sky, Ids.ZenithColor, Zenith);
            TestMaterials.SetLinear(_sky, Ids.HorizonColor, Horizon);
            TestMaterials.SetLinear(_sky, Ids.BelowColor, Below);
            _sky.SetFloat(Ids.HorizonHeight, 0.3f);
            _sky.SetFloat(Ids.CloudCover, 0f);
            _sky.SetFloat(Ids.CloudSpeed, 0f);
            _sky.SetFloat(Ids.SunStrength, 0f);
            _sky.SetFloat(Ids.SunHaloStrength, 0f);
            _sky.SetFloat(Ids.BloomCeiling, Ceiling);
            _stage.Camera.clearFlags = CameraClearFlags.Skybox;
            _stage.Camera.gameObject.AddComponent<Skybox>().material = _sky;
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_sky);
        }

        [Test]
        public void TheGradient_RunsFromBelow_ToTheHorizon_ToTheZenith()
        {
            Color up = Centre(Looking(Vector3.up));
            Color level = Centre(Looking(Vector3.forward));
            Color down = Centre(Looking(new Vector3(0f, -0.6f, 1f)));
            Assert.Less(Distance(up, Zenith), Distance(up, Horizon), $"up {up}");
            Assert.Less(Distance(level, Horizon), Distance(level, Zenith), $"level {level}");
            Assert.Less(Distance(down, Below), Distance(down, Horizon), $"down {down}");
        }

        [Test]
        public void TheSunDisc_IsWhereTheLightPoints()
        {
            _sky.SetFloat(Ids.SunStrength, 3f);
            Vector3 toward = new Vector3(0.3f, 0.5f, 1f).normalized;
            _stage.LightFrom(toward);
            Assert.Greater(Brightest(Centre(Looking(toward))), Ceiling, "the disc, looking at the sun");
            Color[] away = Looking(-toward);
            Assert.AreEqual(0, FrameReads.Count(away, pixel => Brightest(pixel) > Ceiling + 0.01f), "no disc looking away");
        }

        [Test]
        public void NothingButTheDisc_PassesTheBloomCeiling()
        {
            _sky.SetFloat(Ids.CloudCover, 0.6f);
            _sky.SetFloat(Ids.SunHaloStrength, 1f);
            TestMaterials.SetLinear(_sky, Ids.ZenithColor, Color.white);
            Vector3 toward = new Vector3(0f, 0.5f, 1f).normalized;
            _stage.LightFrom(toward);
            Color[] frame = Looking(toward);
            Assert.AreEqual(0, FrameReads.Count(frame, pixel => Brightest(pixel) > Ceiling + 0.01f));
        }

        [Test]
        public void Clouds_ShowOnlyAboveTheHorizon()
        {
            Color[] clear = Looking(Vector3.forward);
            _sky.SetFloat(Ids.CloudCover, 0.7f);
            Color[] cloudy = Looking(Vector3.forward);
            int above = 0, below = 0;
            for (int i = 0; i < clear.Length; i++)
            {
                if (FrameReads.Near(clear[i], cloudy[i], 0.01f))
                    continue;
                if (i / _stage.Size > _stage.Size / 2)
                    above++;
                else
                    below++;
            }
            Assert.Greater(above, 0, "clouds above the horizon");
            Assert.AreEqual(0, below, "none below the horizon");
        }

        [Test]
        public void Stars_ShowOnlyAboveTheHorizon_WhenOn()
        {
            TestMaterials.SetLinear(_sky, Ids.ZenithColor, Color.black);
            TestMaterials.SetLinear(_sky, Ids.HorizonColor, Color.black);
            TestMaterials.SetLinear(_sky, Ids.BelowColor, Color.black);
            _sky.SetFloat(Ids.StarDensity, 1f);
            Color[] dark = Looking(Vector3.up);
            _sky.SetKeyword(new LocalKeyword(_sky.shader, Ids.StarsKeyword), true);
            Color[] starry = Looking(Vector3.up);
            Color[] underfoot = Looking(Vector3.down);
            Assert.AreEqual(0, FrameReads.Count(dark, pixel => Brightest(pixel) > 0.05f), "no stars while off");
            Assert.Greater(FrameReads.Count(starry, pixel => Brightest(pixel) > 0.05f), 0, "stars overhead");
            Assert.AreEqual(0, FrameReads.Count(underfoot, pixel => Brightest(pixel) > 0.05f), "none below the horizon");
        }
    }
}
