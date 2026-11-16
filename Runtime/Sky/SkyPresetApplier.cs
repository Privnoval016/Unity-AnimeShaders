using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>Sets a scene's sky, sun, ambient and fog from a <see cref="SkyPreset" /> when it is enabled, in Edit and
     * Play Mode. No preset (Authored) leaves the scene as it was made. Switched off in Play Mode, it puts back what the
     * scene had. It never changes the preset itself during play.</summary>
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

        private Authored _authored;
        private bool _recorded;

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
            if (Application.isPlaying)
            {
                _authored = Authored.Capture(_sun);
                _recorded = true;
            }
            SkyApply.Apply(_preset.Resolve(), _sky, _sun);
        }

        private void OnDisable()
        {
            if (!_recorded)
                return;
            _authored.Restore(_sun);
            _recorded = false;
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled && !Application.isPlaying && _preset != null)
                SkyApply.Apply(_preset.Resolve(), _sky, _sun);
        }

        #endregion

        #region Helpers

        /** <summary>What a preset overwrites, captured to be put back.</summary> */
        private struct Authored
        {
            private Color _lightColor;
            private float _lightIntensity;
            private Quaternion _lightRotation;
            private Material _skybox;
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
                    _skybox = RenderSettings.skybox, _ambientMode = RenderSettings.ambientMode,
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
