using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>A sky and its light as data: the sky's colours, clouds and stars, the sun's colour, strength and angle,
     * the ambient colours and the fog. <see cref="SkyApply" /> writes it to a scene.</summary>
     */
    [CreateAssetMenu(menuName = "Anime Shaders/Sky Preset", fileName = "SkyPreset")]
    public sealed class SkyPreset : ScriptableObject
    {
        #region Inspector Fields

        [Header("Sky")]
        [Tooltip("The colour straight up.")]
        [SerializeField] private Color _zenith = new(0.27f, 0.52f, 0.9f);
        [Tooltip("The colour at the horizon, also the fog's colour so the ground meets the sky with no seam.")]
        [SerializeField] private Color _horizon = new(0.78f, 0.87f, 0.96f);
        [Tooltip("The colour below the horizon.")]
        [SerializeField] private Color _below = new(0.62f, 0.68f, 0.74f);
        [Tooltip("How high the horizon colour reaches before it becomes the zenith (0 to 1 of the way up).")]
        [Range(0.01f, 1f)] [SerializeField] private float _horizonHeight = 0.3f;

        [Header("Sun")]
        [Tooltip("The sun light's colour: what lit surfaces take.")]
        [SerializeField] private Color _sunLight = new(1f, 0.97f, 0.92f);
        [Tooltip("The sun light's intensity.")]
        [Min(0f)] [SerializeField] private float _sunIntensity = 1f;
        [Tooltip("The sun's height above the horizon, in degrees.")]
        [Range(-10f, 90f)] [SerializeField] private float _sunElevation = 50f;
        [Tooltip("The sun's compass direction, in degrees.")]
        [Range(-180f, 180f)] [SerializeField] private float _sunAzimuth = -30f;
        [Tooltip("The disc's colour in the sky.")]
        [ColorUsage(false, true)] [SerializeField] private Color _discColor = new(1f, 0.96f, 0.88f);
        [Tooltip("The disc's size in radians.")]
        [Range(0f, 0.2f)] [SerializeField] private float _discSize = 0.04f;
        [Tooltip("The disc's brightness; above 1 it blooms.")]
        [Range(0f, 8f)] [SerializeField] private float _discStrength = 2f;
        [Tooltip("The glow around the disc, kept under the bloom ceiling.")]
        [Range(0f, 1f)] [SerializeField] private float _haloStrength = 0.3f;

        [Header("Clouds")]
        [SerializeField] private Color _cloudLit = Color.white;
        [SerializeField] private Color _cloudShade = new(0.8f, 0.84f, 0.93f);
        [Tooltip("How much of the sky the clouds cover.")]
        [Range(0f, 1f)] [SerializeField] private float _cloudCover = 0.45f;
        [Tooltip("How fast the clouds drift (with game time, so they hold through a hitstop).")]
        [SerializeField] private float _cloudSpeed = 0.004f;
        [Tooltip("The direction the clouds drift, across x and z.")]
        [SerializeField] private Vector2 _wind = new(1f, 0.3f);

        [Header("Night")]
        [SerializeField] private bool _stars;
        [Range(0f, 1f)] [SerializeField] private float _starDensity = 0.2f;

        [Header("Ambient")]
        [Tooltip("The light from above that fills the shadow tones.")]
        [SerializeField] private Color _ambientSky = new(0.55f, 0.62f, 0.75f);
        [SerializeField] private Color _ambientEquator = new(0.45f, 0.5f, 0.55f);
        [SerializeField] private Color _ambientGround = new(0.3f, 0.3f, 0.28f);

        [Header("Fog")]
        [Tooltip("Linear fog in the horizon colour, starting past the fighting area so fights stay clear.")]
        [SerializeField] private bool _fog = true;
        [Min(0f)] [SerializeField] private float _fogStart = 60f;
        [Min(0f)] [SerializeField] private float _fogEnd = 320f;

        #endregion

        #region Public API

        /** <summary>The preset as the values a scene is set to.</summary> */
        public SkyState Resolve()
        {
            return new SkyState
            {
                Zenith = _zenith, Horizon = _horizon, Below = _below, HorizonHeight = _horizonHeight,
                SunLight = _sunLight, SunIntensity = _sunIntensity,
                SunRotation = Quaternion.Euler(_sunElevation, _sunAzimuth, 0f),
                DiscColor = _discColor, DiscSize = _discSize, DiscStrength = _discStrength, HaloStrength = _haloStrength,
                CloudLit = _cloudLit, CloudShade = _cloudShade, CloudCover = _cloudCover, CloudSpeed = _cloudSpeed,
                Wind = _wind, Stars = _stars, StarDensity = _starDensity,
                AmbientSky = _ambientSky, AmbientEquator = _ambientEquator, AmbientGround = _ambientGround,
                Fog = _fog, FogStart = _fogStart, FogEnd = _fogEnd,
            };
        }

        #endregion
    }
}
