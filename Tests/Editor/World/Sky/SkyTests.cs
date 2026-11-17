using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.Sky;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Anime/Sky as a camera's own skybox (never the scene's), written by the preset's writer from a made state:
     * its gradient where the made colours put it, the sun disc where the light points, nothing but the disc above the
     * bloom ceiling, the cloud field and stars only above the horizon, and the ridges: drawn below their line, paler
     * the farther they stand in the haze, hiding the sun only when it is behind them, edges a pixel wide, a skyline that
     * varies round the horizon and is the same in every frame.</summary>
     */
    public class SkyTests
    {
        #region Private Fields

        private const float Ceiling = 0.9f;
        private const int Size = 64;
        private const float FieldOfView = 60f;

        private static readonly Color Zenith = new(0.1f, 0.2f, 0.8f);
        private static readonly Color Horizon = new(0.7f, 0.8f, 0.9f);
        private static readonly Color Below = new(0.3f, 0.3f, 0.3f);
        private static readonly Color Red = new(0.85f, 0.05f, 0.05f);
        private static readonly Color RedAsDrawn = Red.linear;

        private LitStage _stage;
        private Material _sky;
        private MadeSky _made;

        #endregion

        #region Helpers

        /** <summary>Writes the made state to the sky and publishes its clouds and haze, then renders looking along <paramref name="forward" />.</summary> */
        private Color[] Looking(Vector3 forward)
        {
            SkyApply.ToSky(_made.State, _sky);
            _sky.SetFloat(Ids.BloomCeiling, Ceiling);
            SkyApply.Publish(_made.State, _sky.GetTexture(Ids.NoiseTex));
            _stage.Camera.transform.rotation = Quaternion.LookRotation(forward, Mathf.Abs(forward.y) > 0.99f ? Vector3.forward : Vector3.up);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.AllFinite(frame), "every pixel finite");
            Assert.AreEqual(frame.Length, FrameReads.Count(frame, FrameReads.Covered), "the sky fills the frame");
            return frame;
        }

        /** <summary>The pixel in the middle column at <paramref name="degrees" /> above the view's centre.</summary> */
        private static Color Above(Color[] frame, float degrees)
        {
            int row = Mathf.Clamp(Mathf.RoundToInt(Size / 2f + degrees / FieldOfView * Size), 0, Size - 1);
            return FrameReads.At(frame, Size / 2, row);
        }

        /** <summary>The centre of each connected patch of pixels that pass <paramref name="lit" />.</summary> */
        private static List<Vector2> Blobs(Color[] frame, Func<Color, bool> lit)
        {
            var seen = new bool[frame.Length];
            var centres = new List<Vector2>();
            var queue = new Queue<int>();
            for (int start = 0; start < frame.Length; start++)
            {
                if (seen[start] || !lit(frame[start]))
                    continue;
                seen[start] = true;
                queue.Enqueue(start);
                Vector2 sum = Vector2.zero;
                int count = 0;
                while (queue.Count > 0)
                {
                    int pixel = queue.Dequeue();
                    int x = pixel % Size, y = pixel / Size;
                    sum += new Vector2(x, y);
                    count++;
                    foreach (int next in new[] { x > 0 ? pixel - 1 : -1, x < Size - 1 ? pixel + 1 : -1, y > 0 ? pixel - Size : -1, y < Size - 1 ? pixel + Size : -1 })
                    {
                        if (next < 0 || seen[next] || !lit(frame[next]))
                            continue;
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
                }
                centres.Add(sum / count);
            }
            return centres;
        }

        private static SkyRidge Flat(Color colour, float height, float distance)
        {
            return new SkyRidge(colour, height, 0f, 8f, 0f, distance);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage(Size);
            _stage.Perspective(FieldOfView, 0f);
            _sky = TestMaterials.Sky();
            _made = new MadeSky().Set("zenith", Zenith).Set("horizon", Horizon).Set("below", Below).Set("horizonHeight", 0.3f)
                .Set("cloudCover", 0f).Set("cloudSpeed", 0f).Set("discStrength", 0f).Set("haloStrength", 0f).Set("haze", false)
                .Set("ridgeMist", 0f);
            _stage.Camera.clearFlags = CameraClearFlags.Skybox;
            _stage.Camera.gameObject.AddComponent<Skybox>().material = _sky;
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            _made.Dispose();
            Object.DestroyImmediate(_sky);
        }

        [Test]
        public void TheGradient_RunsFromBelow_ToTheHorizon_ToTheZenith()
        {
            Color up = FrameReads.Centre(Looking(Vector3.up));
            Color level = FrameReads.Centre(Looking(Vector3.forward));
            Color down = FrameReads.Centre(Looking(new Vector3(0f, -0.6f, 1f)));
            Assert.Less(FrameReads.Distance(up, Zenith), FrameReads.Distance(up, Horizon), $"up {up}");
            Assert.Less(FrameReads.Distance(level, Horizon), FrameReads.Distance(level, Zenith), $"level {level}");
            Assert.Less(FrameReads.Distance(down, Below), FrameReads.Distance(down, Horizon), $"down {down}");
        }

        [Test]
        public void TheSunDisc_IsWhereTheLightPoints()
        {
            _made.Set("discStrength", 3f);
            Vector3 toward = new Vector3(0.3f, 0.5f, 1f).normalized;
            _stage.LightFrom(toward);
            Assert.Greater(FrameReads.Brightest(FrameReads.Centre(Looking(toward))), Ceiling, "the disc, looking at the sun");
            Color[] away = Looking(-toward);
            Assert.AreEqual(0, FrameReads.Count(away, pixel => FrameReads.Brightest(pixel) > Ceiling + 0.01f), "no disc looking away");
        }

        [Test]
        public void NothingButTheDisc_PassesTheBloomCeiling()
        {
            _made.Set("cloudCover", 0.6f).Set("haloStrength", 1f).Set("zenith", Color.white);
            Vector3 toward = new Vector3(0f, 0.5f, 1f).normalized;
            _stage.LightFrom(toward);
            Assert.AreEqual(0, FrameReads.Count(Looking(toward), pixel => FrameReads.Brightest(pixel) > Ceiling + 0.01f));
        }

        [Test]
        public void FullCover_IsCloudEverywhereOverhead()
        {
            var grey = new Color(0.6f, 0.6f, 0.6f);
            _made.Set("zenith", Color.black).Set("cloudLit", grey).Set("cloudShade", grey).Set("cloudCover", 1f).Set("cloudSize", 50f);
            var thinnest = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
            try
            {
                thinnest.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 255), 16).ToArray());
                thinnest.Apply(false);
                _sky.SetTexture(Ids.NoiseTex, thinnest);
                Color[] frame = Looking(Vector3.up);
                Assert.AreEqual(frame.Length, FrameReads.Count(frame, pixel => FrameReads.Near(pixel, grey.linear, 0.02f)), "every pixel the cloud's colour, even where the noise is at its thinnest");
            }
            finally
            {
                Object.DestroyImmediate(thinnest);
            }
        }

        [Test]
        public void TheCloudField_ShowsOnlyAboveTheHorizon_AndNotWithNoCover()
        {
            Color[] clear = Looking(Vector3.forward);
            _made.Set("cloudCover", 0.7f).Set("cloudSize", 50f);
            Color[] cloudy = Looking(Vector3.forward);
            int above = 0, below = 0;
            for (int i = 0; i < clear.Length; i++)
            {
                if (FrameReads.Near(clear[i], cloudy[i], 0.01f))
                    continue;
                if (i / Size > Size / 2)
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
            _made.Set("zenith", Color.black).Set("horizon", Color.black).Set("below", Color.black).Set("starDensity", 1f).Set("starSize", 0.3f).Set("starScale", 4f);
            Color[] dark = Looking(Vector3.up);
            _made.Set("stars", true);
            Color[] starry = Looking(Vector3.up);
            Color[] underfoot = Looking(Vector3.down);
            Assert.AreEqual(0, FrameReads.Count(dark, pixel => FrameReads.Brightest(pixel) > 0.05f), "no stars while off");
            Assert.Greater(FrameReads.Count(starry, pixel => FrameReads.Brightest(pixel) > 0.05f), 0, "stars overhead");
            Assert.AreEqual(0, FrameReads.Count(underfoot, pixel => FrameReads.Brightest(pixel) > 0.05f), "none below the horizon");
        }

        [Test]
        public void Stars_LieAtRandom_NotOnAGrid()
        {
            _made.Set("zenith", Color.black).Set("horizon", Color.black).Set("below", Color.black).Set("cloudCover", 0f)
                .Set("stars", true).Set("starDensity", 1f).Set("starSize", 0.08f).Set("starScale", 12f);
            Color[] frame = Looking(Vector3.up);
            List<Vector2> stars = Blobs(frame, pixel => FrameReads.Brightest(pixel) > 0.05f);
            Assert.Greater(stars.Count, 12, "a field of stars overhead");
            float[] gaps = stars.Select(star => stars.Where(other => other != star).Min(other => Vector2.Distance(star, other))).ToArray();
            float mean = gaps.Average();
            float spread = Mathf.Sqrt(gaps.Sum(gap => (gap - mean) * (gap - mean)) / gaps.Length) / mean;
            Assert.Greater(spread, 0.15f, $"the gaps between neighbouring stars vary ({spread:0.000} of the mean); on a grid they are all alike");
        }

        [Test]
        public void ARidge_IsItsColourBelowItsLine_AndTheSkyAbove()
        {
            Color[] bare = Looking(Vector3.forward);
            _made.Ridges(Flat(Red, 10f, 0f));
            Color[] ridged = Looking(Vector3.forward);
            Assert.IsTrue(FrameReads.Near(Above(ridged, 5f), RedAsDrawn, 0.05f), $"five degrees up: the ridge {Above(ridged, 5f)}");
            Assert.IsTrue(FrameReads.Near(Above(ridged, 20f), Above(bare, 20f), 0.01f), "twenty degrees up: the sky as it was");
        }

        [Test]
        public void ARidgesFoot_DissolvesIntoTheMist_AndItsBodyDoesNot()
        {
            _made.Ridges(Flat(Red, 12f, 0f)).Set("ridgeMistHeight", 6f);
            Color[] clear = Looking(Vector3.forward);
            Assert.IsTrue(FrameReads.Near(Above(clear, 1f), RedAsDrawn, 0.05f), "no mist: the foot is the ridge");
            _made.Set("ridgeMist", 1f);
            Color[] misty = Looking(Vector3.forward);
            Color horizon = Horizon.linear;
            Assert.Less(FrameReads.Distance(Above(misty, 1f), horizon), FrameReads.Distance(Above(misty, 1f), RedAsDrawn), $"the foot nearer the horizon colour {Above(misty, 1f)}");
            Assert.IsTrue(FrameReads.Near(Above(misty, 9f), RedAsDrawn, 0.05f), $"above the mist the ridge as it was {Above(misty, 9f)}");
        }

        [Test]
        public void ARidge_TakesTheCloudsShadows()
        {
            _stage.LightFrom(new Vector3(0f, 1f, -0.5f));
            _made.Ridges(Flat(Red, 12f, 500f)).Set("cloudShadowStrength", 0.6f).Set("cloudSize", 5000f);
            Color clear = Above(Looking(Vector3.forward), 6f);
            _made.Set("cloudCover", 1f);
            Color shaded = Above(Looking(Vector3.forward), 6f);
            Assert.Less(shaded.r, clear.r * 0.7f, $"under the clouds {shaded}, clear {clear}");
        }

        [Test]
        public void TheFartherRidge_IsPalerInTheHaze()
        {
            _made.Set("haze", true).Set("hazeStart", 0f).Set("hazeDensity", 0.002f).Set("hazeFalloff", 0f)
                .Set("hazeMaxOpacity", 1f).Set("hazeGlowStrength", 0f)
                .Ridges(Flat(Red, 15f, 3000f), Flat(Red, 5f, 100f));
            Color[] frame = Looking(Vector3.forward);
            Color haze = Horizon.linear;
            Assert.Less(FrameReads.Distance(Above(frame, 10f), haze), FrameReads.Distance(Above(frame, 2f), haze),
                $"the far ridge {Above(frame, 10f)} nearer the haze than the near one {Above(frame, 2f)}");
        }

        [Test]
        public void TheSun_HidesBehindARidge_AndShowsAboveIt()
        {
            _made.Set("discStrength", 3f).Ridges(Flat(Red, 15f, 0f));
            Vector3 view = Quaternion.Euler(-15f, 0f, 0f) * Vector3.forward;
            _stage.LightFrom(Quaternion.Euler(-5f, 0f, 0f) * Vector3.forward);
            Assert.AreEqual(0, FrameReads.Count(Looking(view), pixel => FrameReads.Brightest(pixel) > Ceiling), "the sun at five degrees, behind the ridge");
            _stage.LightFrom(Quaternion.Euler(-30f, 0f, 0f) * Vector3.forward);
            Assert.Greater(FrameReads.Count(Looking(view), pixel => FrameReads.Brightest(pixel) > Ceiling), 0, "the sun at thirty degrees, above it");
        }

        [Test]
        public void ARidgesEdge_IsAPixelWide()
        {
            Color[] bare = Looking(Vector3.forward);
            _made.Ridges(new SkyRidge(Red, 10f, 4f, 8f, 3f, 0f));
            Color[] ridged = Looking(Vector3.forward);
            for (int x = 0; x < Size; x++)
            {
                int between = 0;
                for (int y = 0; y < Size; y++)
                {
                    Color pixel = FrameReads.At(ridged, x, y);
                    if (!FrameReads.Near(pixel, RedAsDrawn, 0.05f) && !FrameReads.Near(pixel, FrameReads.At(bare, x, y), 0.05f))
                        between++;
                }
                Assert.LessOrEqual(between, 2, $"column {x}: the edge blends over at most two pixels");
            }
        }

        [Test]
        public void TheSkyline_VariesRoundTheHorizon_AndIsTheSameEachFrame()
        {
            _made.Ridges(new SkyRidge(Red, 10f, 6f, 6f, 1f, 0f));
            Color[] first = Looking(Vector3.forward);
            Color[] again = Looking(Vector3.forward);
            Assert.IsTrue(first.SequenceEqual(again), "the same skyline in two frames");
            int[] tops = Enumerable.Range(0, Size).Select(x => Enumerable.Range(0, Size).Last(y => FrameReads.Near(FrameReads.At(first, x, y), RedAsDrawn, 0.05f) || y == 0)).ToArray();
            Assert.Greater(tops.Max() - tops.Min(), 2, "hills and dips along the skyline");
        }
    }
}
