using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using SkyIds = AnimeShaders.AnimeShaderIds.Sky;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The pack's haze, published from a made sky state and seen on black surfaces drawn by Anime/World (so a
     * pixel's brightness is how much haze lies over it): off, nothing changes; it thickens with distance and thins with
     * height, on perspective and orthographic cameras; it glows toward the sun, on a character toward the real sun whatever
     * the characters' light; the scene's own fog on as well fogs
     * nothing twice; and a wall at the sky's distance takes the colour of the sky beside it, so far ground and sky
     * meet. The scene's fog settings are put back after.</summary>
     */
    public class FogTests
    {
        #region Private Fields

        private const int Size = 64;
        private const float FieldOfView = 60f;

        private static readonly Color Haze = new(0.8f, 0.6f, 0.4f);

        private LitStage _stage;
        private Material _black;
        private Mesh _quad;
        private MadeSky _made;
        private bool _fogBefore;
        private FogMode _fogModeBefore;
        private Color _fogColourBefore;
        private float _fogStartBefore;
        private float _fogEndBefore;

        #endregion

        #region Helpers

        private void Wall(Vector3 position, float size)
        {
            _stage.Add(_quad, _black, position, Quaternion.identity, Vector3.one * size);
        }

        /** <summary>Publishes the made state and renders, checking the frame is finite.</summary> */
        private Color[] Render()
        {
            SkyApply.Publish(_made.State, null);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.AllFinite(frame), "every pixel finite");
            return frame;
        }

        private Color[] RenderWithoutHaze()
        {
            using (new WorldGlobals())
                return _stage.Render();
        }

        private static Color At(Color[] frame, float across, float up)
        {
            return FrameReads.At(frame, Mathf.Clamp(Mathf.RoundToInt(across * Size), 0, Size - 1), Mathf.Clamp(Mathf.RoundToInt(up * Size), 0, Size - 1));
        }

        private void LookFromOrigin()
        {
            _stage.Perspective(FieldOfView, 0f);
            _stage.Camera.farClipPlane = 5000f;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _fogBefore = RenderSettings.fog;
            _fogModeBefore = RenderSettings.fogMode;
            _fogColourBefore = RenderSettings.fogColor;
            _fogStartBefore = RenderSettings.fogStartDistance;
            _fogEndBefore = RenderSettings.fogEndDistance;
            RenderSettings.fog = false;

            _stage = new LitStage(Size);
            _black = TestMaterials.WorldLit("Anime/World");
            TestMaterials.SetLinear(_black, Ids.BaseColor, Color.black);
            _quad = TestMeshes.Quad();
            _made = new MadeSky().Set("horizon", Haze).Set("cloudCover", 0f).Set("haze", true).Set("hazeStart", 10f)
                .Set("hazeDensity", 0.005f).Set("hazeFalloff", 0f).Set("hazeBase", 0f).Set("hazeMaxOpacity", 1f)
                .Set("hazeGlowStrength", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            _made.Dispose();
            Object.DestroyImmediate(_black);
            Object.DestroyImmediate(_quad);
            RenderSettings.fog = _fogBefore;
            RenderSettings.fogMode = _fogModeBefore;
            RenderSettings.fogColor = _fogColourBefore;
            RenderSettings.fogStartDistance = _fogStartBefore;
            RenderSettings.fogEndDistance = _fogEndBefore;
        }

        [Test]
        public void HazeOff_ChangesNothing()
        {
            LookFromOrigin();
            Wall(new Vector3(0f, 0f, 200f), 400f);
            Color[] neutral = RenderWithoutHaze();
            _made.Set("haze", false);
            Color[] off = Render();
            Assert.Greater(FrameReads.Count(neutral, FrameReads.Covered), 0, "the wall drew");
            Assert.IsTrue(neutral.SequenceEqual(off), "the haze off is the frame with no haze published");
        }

        [Test]
        public void TheHaze_ThickensWithDistance()
        {
            LookFromOrigin();
            Wall(new Vector3(-9f, 0f, 30f), 12f);
            Wall(new Vector3(90f, 0f, 300f), 120f);
            Color[] frame = Render();
            Color near = At(frame, 0.24f, 0.5f), far = At(frame, 0.76f, 0.5f);
            Assert.Greater(FrameReads.Sum(far), FrameReads.Sum(near) + 0.1f, $"near {near}, far {far}");
            Assert.Greater(FrameReads.Sum(near), 0f, "some haze past its start even near");
        }

        [Test]
        public void BeforeItsStart_ThereIsNoHaze()
        {
            _made.Set("hazeStart", 100f);
            LookFromOrigin();
            Wall(new Vector3(0f, 0f, 30f), 60f);
            Color[] neutral = RenderWithoutHaze();
            Assert.IsTrue(neutral.Zip(Render(), (a, b) => FrameReads.Near(a, b, 0.002f)).All(same => same), "a wall nearer than the start takes none");
        }

        [Test]
        public void TheHaze_NeverPassesItsMostOpacity()
        {
            _made.Set("hazeDensity", 1f).Set("hazeMaxOpacity", 0.5f);
            LookFromOrigin();
            Wall(new Vector3(0f, 0f, 2000f), 4000f);
            Color capped = FrameReads.Centre(Render());
            Assert.IsTrue(FrameReads.Near(capped, Haze.linear * 0.5f, 0.02f), $"half the haze at most: {capped}");
        }

        [Test]
        public void TheHaze_ThinsWithHeight()
        {
            _made.Set("hazeFalloff", 0.05f);
            LookFromOrigin();
            Wall(new Vector3(0f, 0f, 200f), 30f);
            Wall(new Vector3(0f, 60f, 200f), 30f);
            Color[] frame = Render();
            Color low = At(frame, 0.5f, 0.5f), high = At(frame, 0.5f, 0.76f);
            Assert.Less(FrameReads.Sum(high), FrameReads.Sum(low) - 0.05f, $"low {low}, high {high}");
        }

        [Test]
        public void OnAnOrthographicCamera_TheHazeStillThickensWithDistance()
        {
            _stage.Camera.orthographic = true;
            _stage.Camera.orthographicSize = 5f;
            _stage.Camera.farClipPlane = 1000f;
            Wall(new Vector3(-2.5f, 0f, 30f), 4f);
            Wall(new Vector3(2.5f, 0f, 300f), 4f);
            Color[] frame = Render();
            Color near = At(frame, 0.25f, 0.5f), far = At(frame, 0.75f, 0.5f);
            Assert.Greater(FrameReads.Sum(far), FrameReads.Sum(near) + 0.1f, $"near {near}, far {far}");
        }

        [Test]
        public void OnAnOrthographicCamera_TheHazeIsTheSameAcrossTheView()
        {
            _stage.Camera.orthographic = true;
            _stage.Camera.orthographicSize = 50f;
            _stage.Camera.farClipPlane = 1000f;
            Wall(new Vector3(0f, 0f, 100f), 20f);
            Wall(new Vector3(40f, 0f, 100f), 20f);
            Color[] frame = Render();
            Color centre = At(frame, 0.5f, 0.5f), side = At(frame, 0.9f, 0.5f);
            Assert.Greater(FrameReads.Sum(centre), 0.1f, "hazed");
            Assert.IsTrue(FrameReads.Near(centre, side, 0.005f), $"the same distance from the camera's plane: centre {centre}, side {side}");
        }

        [Test]
        public void TheHaze_GlowsTowardTheSun()
        {
            _made.Set("hazeGlow", Color.green).Set("hazeGlowStrength", 1f).Set("hazeGlowSize", 1f);
            _stage.LightFrom(new Vector3(1f, 0f, 0.5f));
            LookFromOrigin();
            Wall(new Vector3(-40f, 0f, 100f), 30f);
            Wall(new Vector3(40f, 0f, 100f), 30f);
            Color[] frame = Render();
            Color away = At(frame, 0.2f, 0.5f), toward = At(frame, 0.8f, 0.5f);
            Assert.Greater(toward.g - toward.r, away.g - away.r + 0.1f, $"toward the sun {toward}, away {away}");
        }

        [Test]
        public void OnACharacter_TheGlowFollowsTheSun_NotTheCharactersLight()
        {
            Material character = TestMaterials.Character(Color.black, Color.black);
            LightSet turned = ScriptableObject.CreateInstance<LightSet>();
            turned.Characters.Direction.Overrides = true;
            turned.Characters.Direction.Yaw = -63f;
            turned.Characters.Direction.Pitch = 0f;
            try
            {
                using (AnimeLightDirector.Isolate())
                {
                    AnimeLightDirector.Push(turned, LightLevel.Shot);
                    _made.Set("hazeGlow", Color.green).Set("hazeGlowStrength", 1f).Set("hazeGlowSize", 1f);
                    _stage.LightFrom(new Vector3(1f, 0f, 0.5f));
                    LookFromOrigin();
                    _stage.Add(_quad, character, new Vector3(-40f, 0f, 100f), Quaternion.identity, Vector3.one * 30f);
                    _stage.Add(_quad, character, new Vector3(40f, 0f, 100f), Quaternion.identity, Vector3.one * 30f);
                    Color[] frame = Render();
                    Color away = At(frame, 0.2f, 0.5f), toward = At(frame, 0.8f, 0.5f);
                    Assert.Greater(toward.g - toward.r, away.g - away.r + 0.1f, $"toward the real sun {toward}, away {away}");
                }
            }
            finally
            {
                Object.DestroyImmediate(character);
                Object.DestroyImmediate(turned);
            }
        }

        [Test]
        public void TheScenesFogOnAsWell_FogsNothingTwice()
        {
            LookFromOrigin();
            Wall(new Vector3(0f, 0f, 100f), 200f);
            Color[] hazeOnly = Render();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.magenta;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 50f;
            Color[] both = Render();
            Assert.IsTrue(FrameReads.Near(At(both, 0.5f, 0.5f), At(hazeOnly, 0.5f, 0.5f), 0.001f), $"{At(both, 0.5f, 0.5f)} against {At(hazeOnly, 0.5f, 0.5f)}");
            _made.Set("haze", false);
            Assert.IsFalse(FrameReads.Near(At(Render(), 0.5f, 0.5f), At(hazeOnly, 0.5f, 0.5f), 0.05f), "with the haze off, the scene's fog shows");
        }

        [Test]
        public void AWallAtTheSkysDistance_MeetsTheSkyBesideIt()
        {
            var sky = TestMaterials.Sky();
            try
            {
                _made.Set("hazeStart", 0f).Set("hazeDensity", 0.003f).Set("skyDistance", 1500f).Set("discStrength", 0f).Set("haloStrength", 0f);
                SkyApply.ToSky(_made.State, sky);
                _stage.Camera.clearFlags = CameraClearFlags.Skybox;
                _stage.Camera.gameObject.AddComponent<Skybox>().material = sky;
                LookFromOrigin();
                Wall(new Vector3(-900f, 0f, 1500f), 1700f);
                Color[] hazy = Render();
                Assert.Less(FrameReads.Distance(At(hazy, 0.2f, 0.5f), At(hazy, 0.8f, 0.5f)), 0.1f, $"wall {At(hazy, 0.2f, 0.5f)}, sky {At(hazy, 0.8f, 0.5f)}");
                _made.Set("haze", false);
                Color[] clear = Render();
                Assert.Greater(FrameReads.Distance(At(clear, 0.2f, 0.5f), At(clear, 0.8f, 0.5f)), 0.3f, "with no haze the black wall stands out");
            }
            finally
            {
                Object.DestroyImmediate(sky);
            }
        }
    }
}
