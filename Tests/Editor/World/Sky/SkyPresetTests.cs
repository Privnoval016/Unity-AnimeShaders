using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.Sky;
using World = AnimeShaders.AnimeShaderIds.World;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A sky preset written to a scene by its one writer (in the test runner's own scene, its render settings and
     * the world's globals snapshot and put back): every value lands on the sky, the sun, the scene and the globals; the
     * scene's fog takes the haze; clouds need a noise; clearing the globals clears them; the component with no preset
     * (Authored) writes nothing, and switched off clears the globals; every shipped preset carries its state.</summary>
     */
    public class SkyPresetTests
    {
        #region Private Fields

        private MadeSky _made;
        private GameObject _sunObject;
        private WorldGlobals _world;
        private Material _skyboxBefore;
        private Light _sunBefore;
        private Texture _reflectionBefore;
        private AmbientMode _ambientModeBefore;
        private Color _ambientSkyBefore;
        private Color _ambientEquatorBefore;
        private Color _ambientGroundBefore;
        private DefaultReflectionMode _reflectionModeBefore;
        private bool _fogBefore;
        private FogMode _fogModeBefore;
        private Color _fogColorBefore;
        private float _fogStartBefore;
        private float _fogEndBefore;
        private Material _sky;
        private Light _sun;

        #endregion

        #region Helpers

        private static bool Close(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 1e-3f && Mathf.Abs(a.g - b.g) < 1e-3f && Mathf.Abs(a.b - b.b) < 1e-3f;
        }

        private static bool On(string keyword)
        {
            return Shader.IsKeywordEnabled(GlobalKeyword.Create(keyword));
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _skyboxBefore = RenderSettings.skybox;
            _sunBefore = RenderSettings.sun;
            _reflectionBefore = RenderSettings.customReflectionTexture;
            _ambientModeBefore = RenderSettings.ambientMode;
            _ambientSkyBefore = RenderSettings.ambientSkyColor;
            _ambientEquatorBefore = RenderSettings.ambientEquatorColor;
            _ambientGroundBefore = RenderSettings.ambientGroundColor;
            _reflectionModeBefore = RenderSettings.defaultReflectionMode;
            _fogBefore = RenderSettings.fog;
            _fogModeBefore = RenderSettings.fogMode;
            _fogColorBefore = RenderSettings.fogColor;
            _fogStartBefore = RenderSettings.fogStartDistance;
            _fogEndBefore = RenderSettings.fogEndDistance;
            _world = new WorldGlobals();
            _made = new MadeSky().Set("cloudCover", 0.5f).Set("cloudAltitude", 700f).Set("cloudShadowStrength", 0.3f)
                .Set("haze", true).Set("hazeStart", 40f).Set("hazeDensity", 0.01f)
                .Ridges(new SkyRidge(Color.red, 8f, 2f, 10f, 1f, 900f));
            _sky = new Material(Shader.Find("Anime/Sky"));
            _sunObject = new GameObject("Sun");
            _sun = _sunObject.AddComponent<Light>();
            _sun.type = LightType.Directional;
        }

        [TearDown]
        public void TearDown()
        {
            RenderSettings.skybox = _skyboxBefore;
            RenderSettings.sun = _sunBefore;
            RenderSettings.customReflectionTexture = _reflectionBefore;
            RenderSettings.ambientMode = _ambientModeBefore;
            RenderSettings.ambientSkyColor = _ambientSkyBefore;
            RenderSettings.ambientEquatorColor = _ambientEquatorBefore;
            RenderSettings.ambientGroundColor = _ambientGroundBefore;
            RenderSettings.defaultReflectionMode = _reflectionModeBefore;
            RenderSettings.fog = _fogBefore;
            RenderSettings.fogMode = _fogModeBefore;
            RenderSettings.fogColor = _fogColorBefore;
            RenderSettings.fogStartDistance = _fogStartBefore;
            RenderSettings.fogEndDistance = _fogEndBefore;
            _world.Dispose();
            _made.Dispose();
            Object.DestroyImmediate(_sunObject);
            Object.DestroyImmediate(_sky);
        }

        [Test]
        public void Apply_WritesEveryValue_ToTheSkyTheSunTheSceneAndTheGlobals()
        {
            SkyState state = _made.State;
            SkyApply.Apply(state, _sky, _sun);

            Assert.IsTrue(Close(state.Zenith, _sky.GetColor(Ids.ZenithColor)));
            Assert.IsTrue(Close(state.Horizon, _sky.GetColor(Ids.HorizonColor)));
            Assert.IsTrue(Close(state.CloudLit, _sky.GetColor(Ids.CloudLitColor)));
            Assert.AreEqual(1f, _sky.GetColor(Ids.RidgeColor0).a, "the one ridge shown");
            Assert.AreEqual(0f, _sky.GetColor(Ids.RidgeColor1).a, "the others hidden");
            Assert.AreEqual(900f, _sky.GetVector(Ids.RidgeDistance).x, 1e-3f, "at its distance");
            Assert.AreEqual(state.Stars, _sky.IsKeywordEnabled(Ids.StarsKeyword));
            Assert.IsTrue(Close(state.SunLight, _sun.color));
            Assert.Less(Quaternion.Angle(state.SunRotation, _sun.transform.rotation), 0.01f);
            Assert.AreSame(_sky, RenderSettings.skybox);
            Assert.AreSame(_sun, RenderSettings.sun);
            Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);
            Assert.IsTrue(Close(state.AmbientSky, RenderSettings.ambientSkyColor));

            Vector4 cloud = Shader.GetGlobalVector(World.AnimeCloudShape);
            Assert.AreEqual(state.CloudCover, cloud.y, 1e-5f, "the cover published");
            Assert.AreEqual(state.CloudAltitude, cloud.w, 1e-3f, "the altitude published");
            Assert.AreEqual(state.CloudShadowStrength, Shader.GetGlobalVector(World.AnimeCloudMotion).w, 1e-5f);
            Assert.AreEqual(state.HazeStart, Shader.GetGlobalVector(World.AnimeHazeShape).x, 1e-3f, "the haze published");
            Assert.AreSame(_sky.GetTexture(Ids.NoiseTex), Shader.GetGlobalTexture(World.AnimeCloudNoise), "the clouds' noise is the sky's");
            Assert.IsTrue(On(World.AtmosphereKeyword), "the haze on");
            Assert.IsTrue(On(World.CloudShadowsKeyword), "the cloud shadows on");
        }

        [Test]
        public void TheScenesFog_TakesTheHaze()
        {
            SkyApply.Apply(_made.State, _sky, _sun);
            Assert.IsTrue(RenderSettings.fog);
            Assert.IsTrue(Close(_sky.GetColor(Ids.HorizonColor), RenderSettings.fogColor), "the horizon colour");
            Assert.AreEqual(_made.State.HazeStart, RenderSettings.fogStartDistance, 1e-3f, "from where the haze starts");
            Assert.Greater(RenderSettings.fogEndDistance, RenderSettings.fogStartDistance);
        }

        [Test]
        public void NoNoise_MeansNoClouds_AndNoCloudShadows()
        {
            SkyApply.Publish(_made.State, null);
            Assert.AreEqual(0f, Shader.GetGlobalVector(World.AnimeCloudShape).y);
            Assert.IsFalse(On(World.CloudShadowsKeyword));
        }

        [Test]
        public void Unpublish_ClearsTheGlobals()
        {
            SkyApply.Apply(_made.State, _sky, _sun);
            SkyApply.Unpublish();
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(World.AnimeCloudShape));
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(World.AnimeHazeShape));
            Assert.IsFalse(On(World.AtmosphereKeyword));
            Assert.IsFalse(On(World.CloudShadowsKeyword));
        }

        [Test]
        public void TheComponent_WithNone_WritesNothing_WithAPreset_AppliesIt_AndOffClearsTheGlobals()
        {
            var holder = new GameObject("Sky");
            var preset = ScriptableObject.CreateInstance<SkyPreset>();
            try
            {
                holder.SetActive(false);
                var applier = holder.AddComponent<SkyPresetApplier>();
                var serialized = new SerializedObject(applier);
                serialized.FindProperty("_sun").objectReferenceValue = _sun;
                serialized.FindProperty("_sky").objectReferenceValue = _sky;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                _sun.color = Color.red;
                Color fogBefore = RenderSettings.fogColor;
                holder.SetActive(true);
                Assert.AreEqual(Color.red, _sun.color, "Authored leaves the light alone");
                Assert.AreEqual(fogBefore, RenderSettings.fogColor, "and the scene's settings");
                Assert.IsFalse(On(World.AtmosphereKeyword), "and publishes nothing");

                holder.SetActive(false);
                serialized.Update();
                serialized.FindProperty("_preset").objectReferenceValue = preset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                holder.SetActive(true);
                Assert.IsTrue(Close(preset.Resolve().SunLight, _sun.color), "a preset sets the light");
                Assert.AreEqual(preset.Resolve().Haze, On(World.AtmosphereKeyword), "and publishes the haze");

                holder.SetActive(false);
                Assert.IsFalse(On(World.AtmosphereKeyword), "switched off, the globals are cleared");
            }
            finally
            {
                Object.DestroyImmediate(holder);
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void EveryPreset_CarriesItsState()
        {
            string[] guids = AssetDatabase.FindAssets("t:SkyPreset");
            Assert.GreaterOrEqual(guids.Length, 1, "presets exist");
            foreach (string guid in guids)
            {
                var preset = AssetDatabase.LoadAssetAtPath<SkyPreset>(AssetDatabase.GUIDToAssetPath(guid));
                SkyState state = preset.Resolve();
                Assert.AreEqual(1f, state.Zenith.a, $"{preset.name}: its colours came through (an empty state has none)");
                Assert.Greater(state.SkyDistance, 0f, $"{preset.name}: a sky distance");
                Assert.Greater(state.CloudSize, 0f, $"{preset.name}: a cloud size");
            }
        }
    }
}
