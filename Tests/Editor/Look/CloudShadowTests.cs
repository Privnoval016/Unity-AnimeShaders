using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using SkyIds = AnimeShaders.AnimeShaderIds.Sky;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The clouds' shadows on the pack's lit shaders, from a made sky state's cloud field (still, so one frame is
     * compared): off or at a material response of 0, nothing changes; a full cover shades a lit floor toward its shadow
     * tone and none leaves it; a part cover shades some of it; far past the shadow distance it still shades; a body
     * takes one shade all over; and a cloud seen overhead in the sky lies over its own shadow on the ground.</summary>
     */
    public class CloudShadowTests
    {
        #region Private Fields

        private const int Size = 64;
        private const float Strength = 0.6f;

        private LitStage _stage;
        private Material _floor;
        private Material _skyForNoise;
        private Mesh _quad;
        private MadeSky _made;

        #endregion

        #region Helpers

        private Texture Noise => _skyForNoise.GetTexture(SkyIds.NoiseTex);

        private void LookDownOn(Vector3 above)
        {
            _stage.Camera.transform.SetPositionAndRotation(above, Quaternion.Euler(90f, 0f, 0f));
        }

        private Color[] Render()
        {
            SkyApply.Publish(_made.State, Noise);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.AllFinite(frame), "every pixel finite");
            Assert.Greater(FrameReads.Count(frame, FrameReads.Covered), 0, "something drew");
            return frame;
        }

        private void FloorAt(Vector3 position, float size)
        {
            _stage.Add(_quad, _floor, position, Quaternion.Euler(90f, 0f, 0f), Vector3.one * size);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage(Size);
            _stage.LightFrom(Vector3.up);
            _stage.Camera.orthographicSize = 3f;
            _stage.Camera.farClipPlane = 2000f;
            _floor = TestMaterials.WorldLit("Anime/World");
            _skyForNoise = TestMaterials.Sky();
            _quad = TestMeshes.Quad();
            _made = new MadeSky().Set("cloudSpeed", 0f).Set("cloudSoftness", 0.02f).Set("cloudSimplicity", 0f)
                .Set("cloudShadowStrength", Strength).Set("cloudShadowClearRadius", 0f).Set("haze", false).Set("discStrength", 0f).Set("haloStrength", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            _made.Dispose();
            Object.DestroyImmediate(_floor);
            Object.DestroyImmediate(_skyForNoise);
            Object.DestroyImmediate(_quad);
        }

        [Test]
        public void Off_OrAMaterialResponseOfNone_ChangesNothing()
        {
            LookDownOn(new Vector3(0f, 10f, 0f));
            FloorAt(Vector3.zero, 20f);
            _made.Set("cloudCover", 1f).Set("cloudShadowStrength", 0f);
            Color off = FrameReads.Centre(Render());
            _made.Set("cloudShadowStrength", 0.6f);
            _floor.SetFloat(Ids.CloudShadow, 0f);
            Color unresponsive = FrameReads.Centre(Render());
            Assert.IsTrue(FrameReads.Near(off, Color.white, 0.001f), $"no strength: fully lit {off}");
            Assert.IsTrue(FrameReads.Near(unresponsive, Color.white, 0.001f), $"no response: fully lit {unresponsive}");
        }

        [Test]
        public void AFullCover_ShadesTheLitFloor_AndNoCoverLeavesIt()
        {
            LookDownOn(new Vector3(0f, 10f, 0f));
            FloorAt(Vector3.zero, 20f);
            _made.Set("cloudCover", 0f);
            Color clear = FrameReads.Centre(Render());
            _made.Set("cloudCover", 1f);
            Color clouded = FrameReads.Centre(Render());
            Assert.IsTrue(FrameReads.Near(clear, Color.white, 0.001f), $"clear: fully lit {clear}");
            Assert.Less(clouded.r, clear.r - 0.2f, $"under a cloud, toward the shadow tone {clouded}");
            Assert.Greater(clouded.r, 0.5f - 0.001f, "never past the shadow tone");
        }

        [Test]
        public void APartCover_ShadesSomeOfTheFloor()
        {
            LookDownOn(new Vector3(0f, 10f, 0f));
            FloorAt(Vector3.zero, 20f);
            _made.Set("cloudCover", 0.5f).Set("cloudSize", 2f);
            Color[] frame = Render();
            Assert.Greater(FrameReads.Count(frame, pixel => FrameReads.Covered(pixel) && pixel.r > 0.99f), 0, "sunlit gaps");
            Assert.Greater(FrameReads.Count(frame, pixel => FrameReads.Covered(pixel) && pixel.r < 0.8f), 0, "shaded patches");
        }

        [Test]
        public void RoundTheCamera_TheClearRadiusTakesNone_AndPastItTheyComeIn()
        {
            LookDownOn(new Vector3(0f, 10f, 0f));
            _stage.Camera.orthographicSize = 30f;
            FloorAt(Vector3.zero, 80f);
            _made.Set("cloudCover", 1f).Set("cloudShadowClearRadius", 10f).Set("cloudShadowClearFade", 5f);
            Color[] frame = Render();
            Color near = FrameReads.At(frame, Size / 2, Size / 2), far = FrameReads.At(frame, Size / 2 + 27, Size / 2);
            Assert.IsTrue(FrameReads.Near(near, Color.white, 0.001f), $"under the camera: clear {near}");
            Assert.Less(far.r, 0.8f, $"25 m out: shaded {far}");
        }

        [Test]
        public void TheCloudField_DoesNotRepeatAtTheNoisesTile()
        {
            const int Pixels = 128;
            const float View = 3.2f, Tile = 2f;
            _stage.Dispose();
            _stage = new LitStage(Pixels);
            _stage.LightFrom(Vector3.up);
            _stage.Camera.orthographicSize = View;
            LookDownOn(new Vector3(0f, 10f, 0f));
            FloorAt(Vector3.zero, 20f);
            _made.Set("cloudCover", 0.5f).Set("cloudSize", Tile);
            Color[] frame = Render();
            int tilePixels = Mathf.RoundToInt(Tile / (2f * View) * Pixels);
            int differing = 0, compared = 0;
            for (int y = 8; y < Pixels; y += 8)
            {
                for (int x = 0; x + tilePixels < Pixels; x += 4)
                {
                    compared++;
                    if (!FrameReads.Near(FrameReads.At(frame, x, y), FrameReads.At(frame, x + tilePixels, y), 0.02f))
                        differing++;
                }
            }
            Assert.Greater(differing, compared / 10, $"one noise tile on, the shade differs ({differing} of {compared})");
        }

        [Test]
        public void FarPastTheShadowDistance_TheCloudsStillShade()
        {
            var far = new Vector3(0f, 0f, 1500f);
            LookDownOn(far + new Vector3(0f, 10f, 0f));
            FloorAt(far, 20f);
            _made.Set("cloudCover", 1f);
            Assert.Less(FrameReads.Centre(Render()).r, 0.8f, "shaded a kilometre and a half away");
        }

        [Test]
        public void ABody_TakesOneShadeAllOver()
        {
            var body = TestMaterials.Character(Color.white, new Color(0.5f, 0.5f, 0.5f));
            Mesh sphere = TestMeshes.Sphere();
            try
            {
                _stage.Camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -5f), Quaternion.identity);
                _stage.Camera.orthographicSize = 1.25f;
                _stage.LightFrom(new Vector3(0f, 1f, -1f));
                body.SetFloat(Ids.OutlineWidth, 0f);
                body.SetFloat(Ids.ShadowThreshold, 0f);
                _stage.Add(sphere, body, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
                _made.Set("cloudCover", 0.5f).Set("cloudSize", 0.5f);

                float Spread(Color[] frame)
                {
                    float low = 1f, high = 0f;
                    foreach (Color pixel in frame)
                    {
                        if (!FrameReads.Covered(pixel))
                            continue;
                        low = Mathf.Min(low, pixel.r);
                        high = Mathf.Max(high, pixel.r);
                    }
                    return high - low;
                }

                float perPixel = Spread(Render());
                AnimeMaterials.SetFeature(body, AnimeShaderIds.Keywords.BodyShadowName, AnimeShaderIds.Switches.BodyShadow, true);
                float wholeBody = Spread(Render());
                Assert.Greater(perPixel, 0.1f, "read per pixel, a cloud's edges cross the body");
                Assert.Less(wholeBody, 0.02f, "read once for the body, one shade all over");
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void ACloudOverhead_LiesOverItsShadow()
        {
            var sky = TestMaterials.Sky();
            try
            {
                _made.Set("cloudCover", 0.5f).Set("cloudSize", 4f).Set("cloudAltitude", 20f).Set("zenith", Color.blue)
                    .Set("horizon", Color.blue).Set("cloudLit", Color.red).Set("cloudShade", Color.red);
                SkyApply.ToSky(_made.State, sky);
                _stage.Camera.clearFlags = CameraClearFlags.Skybox;
                _stage.Camera.gameObject.AddComponent<Skybox>().material = sky;
                _stage.Camera.orthographic = false;
                _stage.Camera.fieldOfView = 10f;
                int clouded = 0, clear = 0;
                for (int i = 0; i < 16; i++)
                {
                    var spot = new Vector3(i * 37f, 0f, i * 11f);
                    _stage.Camera.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(Vector3.up, Vector3.forward));
                    float overhead = FrameReads.Centre(Render()).r;
                    _stage.Camera.transform.SetPositionAndRotation(spot + new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
                    FloorAt(spot, 4f);
                    float below = (1f - (FrameReads.Centre(Render()).r - 0.5f) / 0.5f) / Strength;
                    Assert.AreEqual(overhead, below, 0.25f, $"at {spot}: the cloud's density overhead and in its shadow");
                    if (overhead > 0.75f)
                        clouded++;
                    else if (overhead < 0.25f)
                        clear++;
                }
                Assert.Greater(clouded, 0, "some spots under a cloud");
                Assert.Greater(clear, 0, "some in the clear");
            }
            finally
            {
                Object.DestroyImmediate(sky);
            }
        }
    }
}
