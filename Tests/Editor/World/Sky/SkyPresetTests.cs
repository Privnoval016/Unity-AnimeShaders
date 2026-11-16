using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.Sky;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A sky preset written to a scene by its one writer (in the test runner's own scene, its render settings
     * snapshot and put back): every value lands, the fog takes the horizon colour, and the component with no preset
     * (Authored) writes nothing.</summary>
     */
    public class SkyPresetTests
    {
        #region Private Fields

        private SkyPreset _preset;
        private GameObject _sunObject;
        private Material _skyboxBefore;
        private Light _sunBefore;
        private AmbientMode _ambientModeBefore;
        private Color _ambientSkyBefore;
        private Color _ambientEquatorBefore;
        private Color _ambientGroundBefore;
        private DefaultReflectionMode _reflectionBefore;
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

        #endregion

        [SetUp]
        public void SetUp()
        {
            _skyboxBefore = RenderSettings.skybox;
            _sunBefore = RenderSettings.sun;
            _ambientModeBefore = RenderSettings.ambientMode;
            _ambientSkyBefore = RenderSettings.ambientSkyColor;
            _ambientEquatorBefore = RenderSettings.ambientEquatorColor;
            _ambientGroundBefore = RenderSettings.ambientGroundColor;
            _reflectionBefore = RenderSettings.defaultReflectionMode;
            _fogBefore = RenderSettings.fog;
            _fogModeBefore = RenderSettings.fogMode;
            _fogColorBefore = RenderSettings.fogColor;
            _fogStartBefore = RenderSettings.fogStartDistance;
            _fogEndBefore = RenderSettings.fogEndDistance;
            _preset = ScriptableObject.CreateInstance<SkyPreset>();
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
            RenderSettings.ambientMode = _ambientModeBefore;
            RenderSettings.ambientSkyColor = _ambientSkyBefore;
            RenderSettings.ambientEquatorColor = _ambientEquatorBefore;
            RenderSettings.ambientGroundColor = _ambientGroundBefore;
            RenderSettings.defaultReflectionMode = _reflectionBefore;
            RenderSettings.fog = _fogBefore;
            RenderSettings.fogMode = _fogModeBefore;
            RenderSettings.fogColor = _fogColorBefore;
            RenderSettings.fogStartDistance = _fogStartBefore;
            RenderSettings.fogEndDistance = _fogEndBefore;
            Object.DestroyImmediate(_sunObject);
            Object.DestroyImmediate(_preset);
            Object.DestroyImmediate(_sky);
        }

        [Test]
        public void Apply_WritesEveryValue_ToTheSkyTheSunAndTheScene()
        {
            SkyState state = _preset.Resolve();
            SkyApply.Apply(state, _sky, _sun);

            Assert.IsTrue(Close(state.Zenith, _sky.GetColor(Ids.ZenithColor)));
            Assert.IsTrue(Close(state.Horizon, _sky.GetColor(Ids.HorizonColor)));
            Assert.IsTrue(Close(state.Below, _sky.GetColor(Ids.BelowColor)));
            Assert.AreEqual(state.CloudCover, _sky.GetFloat(Ids.CloudCover), 1e-5f);
            Assert.AreEqual(state.Stars, _sky.IsKeywordEnabled(Ids.StarsKeyword));
            Assert.IsTrue(Close(state.SunLight, _sun.color));
            Assert.AreEqual(state.SunIntensity, _sun.intensity, 1e-5f);
            Assert.Less(Quaternion.Angle(state.SunRotation, _sun.transform.rotation), 0.01f);
            Assert.AreSame(_sky, RenderSettings.skybox);
            Assert.AreSame(_sun, RenderSettings.sun);
            Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);
            Assert.IsTrue(Close(state.AmbientSky, RenderSettings.ambientSkyColor));
            Assert.AreEqual(state.Fog, RenderSettings.fog);
            Assert.AreEqual(state.FogStart, RenderSettings.fogStartDistance, 1e-4f);
        }

        [Test]
        public void TheFog_TakesTheHorizonColour()
        {
            SkyApply.Apply(_preset.Resolve(), _sky, _sun);
            Assert.IsTrue(Close(_sky.GetColor(Ids.HorizonColor), RenderSettings.fogColor));
        }

        [Test]
        public void TheComponent_WithAPreset_AppliesIt_AndWithNone_WritesNothing()
        {
            var holder = new GameObject("Sky");
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

            holder.SetActive(false);
            serialized.Update();
            serialized.FindProperty("_preset").objectReferenceValue = _preset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            holder.SetActive(true);
            Assert.IsTrue(Close(_preset.Resolve().SunLight, _sun.color), "a preset sets the light");
            Object.DestroyImmediate(holder);
        }
    }
}
