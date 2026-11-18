using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.Sky;
using World = AnimeShaders.AnimeShaderIds.World;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A sky preset written to a scene by its one writer (in the test runner's own scene, its render settings and
     * the world's globals snapshot and put back): every value lands on the sky, the sun, the scene and the globals; the
     * scene's fog takes the haze; clouds need a noise; clearing the globals clears them; the component with no preset
     * (Authored) writes nothing, and switched off clears the globals; a preset's light is the base while its applier owns
     * the sky, replaced by the next (under a lit shot too), removed with it, with a next sky that has none, and as the
     * preset is cleared; every shipped preset carries its state.</summary>
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

        /** <summary>Adds a switched-off applier of <paramref name="preset" /> on <paramref name="holder" />, drawing to the test's sky and sun.</summary> */
        private void Applier(GameObject holder, SkyPreset preset)
        {
            holder.SetActive(false);
            var serialized = new SerializedObject(holder.AddComponent<SkyPresetApplier>());
            serialized.FindProperty("_preset").objectReferenceValue = preset;
            serialized.FindProperty("_sun").objectReferenceValue = _sun;
            serialized.FindProperty("_sky").objectReferenceValue = _sky;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LitBy(SkyPreset preset, LightSet light)
        {
            var serialized = new SerializedObject(preset);
            serialized.FindProperty("light").objectReferenceValue = light;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

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
            _made = new MadeSky().Set("shade", new Color(0.5f, 0.6f, 0.7f)).Set("cloudCover", 0.5f).Set("cloudAltitude", 700f).Set("cloudShadowStrength", 0.3f)
                .Set("haze", true).Set("hazeStart", 40f).Set("hazeDensity", 0.01f)
                .Ridges(new SkyRidge(Color.red, 8f, 2f, 10f, 1f, 900f));
            _sky = TestMaterials.Sky();
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
            Vector4 shade = Shader.GetGlobalVector(World.AnimeWorldShade);
            Color shadeLinear = state.Shade.linear;
            Assert.AreEqual(new Vector4(shadeLinear.r - 1f, shadeLinear.g - 1f, shadeLinear.b - 1f, 0f), shade, "the shade tint published, less one, channel by channel");
            Assert.IsTrue(Close(state.Below, _sky.GetColor(Ids.BelowColor)));
            Assert.AreEqual(state.SunIntensity, _sun.intensity, 1e-4f, "the sun's strength");
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
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(World.AnimeWorldShade), "the materials' own shade");
            Assert.IsFalse(On(World.AtmosphereKeyword));
            Assert.IsFalse(On(World.CloudShadowsKeyword));
        }

        [Test]
        public void AnApplierSwitchedOffAfterAnotherTookOver_LeavesTheNewOnesSky()
        {
            var first = new GameObject("First Sky");
            var second = new GameObject("Second Sky");
            var cloudy = new MadeSky().Set("cloudCover", 0.5f).Set("cloudShadowStrength", 0.5f).Set("shade", new Color(0.4f, 0.5f, 0.6f));
            var clear = new MadeSky().Set("cloudCover", 0f).Set("shade", new Color(0.9f, 0.8f, 0.7f));
            try
            {
                Applier(first, cloudy.Preset);
                Applier(second, clear.Preset);
                first.SetActive(true);
                second.SetActive(true);
                Vector4 seconds = Shader.GetGlobalVector(World.AnimeWorldShade);
                first.SetActive(false);
                Assert.AreEqual(seconds, Shader.GetGlobalVector(World.AnimeWorldShade), "the second's shade stays");
                second.SetActive(false);
                Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(World.AnimeWorldShade), "the owner switched off clears");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                cloudy.Dispose();
                clear.Dispose();
            }
        }

        [Test]
        public void APresetsLight_IsTheBase_WhileItsApplierOwnsTheSky_AndTheNextOneReplacesIt()
        {
            var first = new GameObject("First Sky");
            var second = new GameObject("Second Sky");
            var day = new MadeSky();
            var dusk = new MadeSky();
            var dayLight = ScriptableObject.CreateInstance<LightSet>();
            var duskLight = ScriptableObject.CreateInstance<LightSet>();
            var director = new DirectorGlobals();
            IDisposable isolated = AnimeLightDirector.Isolate();
            try
            {
                LitBy(day.Preset, dayLight);
                LitBy(dusk.Preset, duskLight);
                Applier(first, day.Preset);
                Applier(second, dusk.Preset);

                first.SetActive(true);
                Assert.AreEqual(1, AnimeLightDirector.Pushed.Count, "the first sky's light is pushed");
                Assert.AreSame(dayLight, AnimeLightDirector.Pushed[0].Set);
                Assert.AreEqual(LightLevel.Base, AnimeLightDirector.Pushed[0].Level, "as the base");

                second.SetActive(true);
                Assert.AreEqual(1, AnimeLightDirector.Pushed.Count, "the next sky replaces it, never stacks on it");
                Assert.AreSame(duskLight, AnimeLightDirector.Pushed[0].Set);

                first.SetActive(false);
                Assert.AreSame(duskLight, AnimeLightDirector.Pushed[0].Set, "the old sky switched off leaves the new one's light");

                second.SetActive(false);
                Assert.AreEqual(0, AnimeLightDirector.Pushed.Count, "the owner switched off removes it");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                isolated.Dispose();
                director.Dispose();
                day.Dispose();
                dusk.Dispose();
                Object.DestroyImmediate(dayLight);
                Object.DestroyImmediate(duskLight);
            }
        }

        [Test]
        public void ASkyChangedUnderALitShot_LeavesTheShotOnTop_AndANextSkyWithNoLightRemovesTheBase()
        {
            var first = new GameObject("First Sky");
            var second = new GameObject("Second Sky");
            var third = new GameObject("Third Sky");
            var day = new MadeSky();
            var dusk = new MadeSky();
            var unlit = new MadeSky();
            var dayLight = ScriptableObject.CreateInstance<LightSet>();
            var duskLight = ScriptableObject.CreateInstance<LightSet>();
            var shot = ScriptableObject.CreateInstance<LightSet>();
            var director = new DirectorGlobals();
            IDisposable isolated = AnimeLightDirector.Isolate();
            try
            {
                duskLight.World.Ambient.Overrides = true;
                shot.World.Colours.Overrides = true;
                shot.World.Colours.Light = new Color(0.5f, 0.5f, 0.5f);
                LitBy(day.Preset, dayLight);
                LitBy(dusk.Preset, duskLight);
                Applier(first, day.Preset);
                Applier(second, dusk.Preset);
                Applier(third, unlit.Preset);
                first.SetActive(true);
                LightToken lit = AnimeLightDirector.Push(shot, LightLevel.Shot);
                second.SetActive(true);
                Assert.AreSame(shot, AnimeLightDirector.Pushed[AnimeLightDirector.Pushed.Count - 1].Set, "the shot still on top");
                Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f).linear.r - 1f, Shader.GetGlobalVector(AnimeShaderIds.Director.WorldLightColor).x, 1e-4f, "its colour still shows");
                Assert.Greater(Shader.GetGlobalVector(AnimeShaderIds.Director.WorldAmbient).w, 0.5f, "the new base's ambient shows where the shot sets nothing");
                lit.Dispose();
                third.SetActive(true);
                Assert.AreEqual(0, AnimeLightDirector.Pushed.Count, "a sky with no light: no base left behind");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(third);
                isolated.Dispose();
                director.Dispose();
                day.Dispose();
                dusk.Dispose();
                unlit.Dispose();
                Object.DestroyImmediate(dayLight);
                Object.DestroyImmediate(duskLight);
                Object.DestroyImmediate(shot);
            }
        }

        [Test]
        public void ClearingThePresetInTheInspector_RemovesItsLight()
        {
            var holder = new GameObject("Sky");
            var day = new MadeSky();
            var dayLight = ScriptableObject.CreateInstance<LightSet>();
            var director = new DirectorGlobals();
            IDisposable isolated = AnimeLightDirector.Isolate();
            try
            {
                LitBy(day.Preset, dayLight);
                Applier(holder, day.Preset);
                holder.SetActive(true);
                Assert.AreEqual(1, AnimeLightDirector.Pushed.Count, "pushed");
                var serialized = new SerializedObject(holder.GetComponent<SkyPresetApplier>());
                serialized.FindProperty("_preset").objectReferenceValue = null;
                serialized.ApplyModifiedProperties();
                Assert.AreEqual(0, AnimeLightDirector.Pushed.Count, "the preset cleared: its light gone with it");
            }
            finally
            {
                Object.DestroyImmediate(holder);
                isolated.Dispose();
                director.Dispose();
                day.Dispose();
                Object.DestroyImmediate(dayLight);
            }
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
