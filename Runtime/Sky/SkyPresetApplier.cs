using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>Sets a scene's sky, sun, ambient, fog and the world's globals (clouds, haze) from a <see cref="SkyPreset" />
     * when it is enabled, in Edit and Play Mode. No preset (Authored) leaves the scene as it was made. In Edit Mode it
     * writes the sky material itself (what the preset makes of it); in Play Mode it draws through a copy, so the material
     * stays as authored, and switched off it puts back everything it changed. Switched off in either mode, the world's
     * globals are cleared. The preset's light is pushed as the scene's base light (<see cref="LightLevel.Base" />) and
     * removed with it. Only the applier enabled last does either, so one switched off after another took over (a scene
     * change) leaves the new one's sky and light alone.</summary>
     */
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SkyPresetApplier : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("The sky to set. None (Authored) leaves the scene's own light and settings alone.")]
        [SerializeField] private SkyPreset _preset;

        [Tooltip("The scene's sun: its colour, strength and angle are set from the preset.")]
        [SerializeField] private Light _sun;

        [Tooltip("The Anime/Sky material used as the scene's skybox.")]
        [SerializeField] private Material _sky;

        #endregion

        #region Private Fields

        private static SkyPresetApplier _current;
        private static LightToken _base;

        private Authored _authored;
        private bool _recorded;
        private Material _playCopy;

        #endregion

        #region Properties

        /** <summary>The preset applied, or null for Authored.</summary> */
        public SkyPreset Preset => _preset;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            if (_preset == null)
                return;
            Material sky = _sky;
            if (Application.isPlaying)
            {
                _authored = Authored.Capture(_sun);
                _recorded = true;
                if (_sky != null)
                {
                    _playCopy = new Material(_sky) { name = _sky.name, hideFlags = HideFlags.DontSave };
                    sky = _playCopy;
                }
            }
            _current = this;
            SkyApply.Apply(_preset.Resolve(), sky, _sun);
            PushBase();
        }

        private void OnDisable()
        {
            bool owns = _current == this;
            if (owns)
            {
                SkyApply.Unpublish();
                _base?.Dispose();
                _base = null;
                _current = null;
            }
            if (_recorded && owns)
                _authored.Restore(_sun);
            _recorded = false;
            if (_playCopy != null)
            {
                Destroy(_playCopy);
                _playCopy = null;
            }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled || Application.isPlaying || _preset == null)
                return;
            SkyApply.Apply(_preset.Resolve(), _sky, _sun);
            if (_current == this)
                PushBase();
        }

        #endregion

        #region Helpers

        /** <summary>Makes the preset's light the scene's base light, in place of any before it.</summary> */
        private void PushBase()
        {
            _base?.Dispose();
            _base = _preset.Light != null ? AnimeLightDirector.Push(_preset.Light, LightLevel.Base) : null;
        }

        /** <summary>What a preset overwrites, captured to be put back.</summary> */
        private struct Authored
        {
            private Color _lightColor;
            private float _lightIntensity;
            private Quaternion _lightRotation;
            private Material _skybox;
            private Light _sceneSun;
            private Texture _reflection;
            private AmbientMode _ambientMode;
            private Color _ambientSky;
            private Color _ambientEquator;
            private Color _ambientGround;
            private DefaultReflectionMode _reflectionMode;
            private bool _fog;
            private FogMode _fogMode;
            private Color _fogColor;
            private float _fogStart;
            private float _fogEnd;

            public static Authored Capture(Light sun)
            {
                var authored = new Authored
                {
                    _skybox = RenderSettings.skybox, _sceneSun = RenderSettings.sun,
                    _reflection = RenderSettings.customReflectionTexture, _ambientMode = RenderSettings.ambientMode,
                    _ambientSky = RenderSettings.ambientSkyColor, _ambientEquator = RenderSettings.ambientEquatorColor,
                    _ambientGround = RenderSettings.ambientGroundColor,
                    _reflectionMode = RenderSettings.defaultReflectionMode, _fog = RenderSettings.fog,
                    _fogMode = RenderSettings.fogMode, _fogColor = RenderSettings.fogColor,
                    _fogStart = RenderSettings.fogStartDistance, _fogEnd = RenderSettings.fogEndDistance,
                };
                if (sun != null)
                {
                    authored._lightColor = sun.color;
                    authored._lightIntensity = sun.intensity;
                    authored._lightRotation = sun.transform.rotation;
                }
                return authored;
            }

            public void Restore(Light sun)
            {
                RenderSettings.skybox = _skybox;
                RenderSettings.sun = _sceneSun;
                RenderSettings.customReflectionTexture = _reflection;
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientSkyColor = _ambientSky;
                RenderSettings.ambientEquatorColor = _ambientEquator;
                RenderSettings.ambientGroundColor = _ambientGround;
                RenderSettings.defaultReflectionMode = _reflectionMode;
                RenderSettings.fog = _fog;
                RenderSettings.fogMode = _fogMode;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogStartDistance = _fogStart;
                RenderSettings.fogEndDistance = _fogEnd;
                if (sun == null)
                    return;
                sun.color = _lightColor;
                sun.intensity = _lightIntensity;
                sun.transform.rotation = _lightRotation;
            }
        }

        #endregion
    }
}
