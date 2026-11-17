using System;
using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>Everything a sky preset sets a scene to, held once: the sky's gradient and sun, the clouds (one field the
     * sky draws and the ground's cloud shadows read), the ridges on the horizon, the stars, the ambient and the haze.
     * <see cref="SkyApply" /> writes it.</summary>
     */
    [Serializable]
    public struct SkyState
    {
        #region Inspector Fields

        [Header("Sky")]
        [Tooltip("The colour straight up.")]
        [SerializeField] private Color zenith;
        [Tooltip("The colour at the horizon; also the haze's colour, so the far ground, the ridges and the sky meet with no seam.")]
        [SerializeField] private Color horizon;
        [Tooltip("The colour below the horizon, where no ground hides it.")]
        [SerializeField] private Color below;
        [Tooltip("How high the horizon colour reaches before it becomes the zenith (0 to 1 of the way up).")]
        [Range(0.01f, 1f)] [SerializeField] private float horizonHeight;

        [Header("Sun")]
        [Tooltip("The sun light's colour: what lit surfaces take.")]
        [SerializeField] private Color sunLight;
        [Tooltip("The sun light's intensity.")]
        [Min(0f)] [SerializeField] private float sunIntensity;
        [Tooltip("The sun's height above the horizon, in degrees.")]
        [Range(-10f, 90f)] [SerializeField] private float sunElevation;
        [Tooltip("The sun's compass direction, in degrees.")]
        [Range(-180f, 180f)] [SerializeField] private float sunAzimuth;
        [Tooltip("The disc's colour in the sky.")]
        [ColorUsage(false, true)] [SerializeField] private Color discColor;
        [Tooltip("The disc's size in radians.")]
        [Range(0f, 0.2f)] [SerializeField] private float discSize;
        [Tooltip("The disc's brightness; above 1 it blooms.")]
        [Range(0f, 8f)] [SerializeField] private float discStrength;
        [Tooltip("How far the glow round the disc reaches, in radians.")]
        [Range(0f, 1f)] [SerializeField] private float haloSize;
        [Tooltip("The glow round the disc, kept under the bloom ceiling.")]
        [Range(0f, 1f)] [SerializeField] private float haloStrength;

        [Header("Clouds")]
        [Tooltip("The clouds' colour on the side facing the sun.")]
        [SerializeField] private Color cloudLit;
        [Tooltip("The clouds' colour on the side away from the sun.")]
        [SerializeField] private Color cloudShade;
        [Tooltip("How much of the sky the clouds cover (0: a clear sky and no cloud shadows).")]
        [Range(0f, 1f)] [SerializeField] private float cloudCover;
        [Tooltip("How soft a cloud's edge is (0: as crisp as the screen allows).")]
        [Range(0f, 0.5f)] [SerializeField] private float cloudSoftness;
        [Tooltip("The size of a typical cloud, in metres.")]
        [Min(1f)] [SerializeField] private float cloudSize;
        [Tooltip("How simple the clouds' shapes are: higher drops their small wisps for large, drawn forms.")]
        [Range(0f, 4f)] [SerializeField] private float cloudSimplicity;
        [Tooltip("How high the clouds float, in metres: it sets how far their shadows fall from under them.")]
        [Min(1f)] [SerializeField] private float cloudAltitude;
        [Tooltip("How fast the clouds drift, in metres a second (with game time, so they hold through a hitstop).")]
        [Min(0f)] [SerializeField] private float cloudSpeed;
        [Tooltip("The direction the clouds drift, across x and z.")]
        [SerializeField] private Vector2 wind;
        [Tooltip("How far the shade on a cloud sits from its sunlit edge, in metres.")]
        [Min(0f)] [SerializeField] private float cloudShadeOffset;
        [Tooltip("How high above the horizon the clouds fade in (0 to 1 of the way up).")]
        [Range(0.01f, 0.5f)] [SerializeField] private float cloudHorizonFade;
        [Tooltip("How dark the clouds' shadows on the ground are (0: none).")]
        [Range(0f, 1f)] [SerializeField] private float cloudShadowStrength;
        [Tooltip("No cloud shadows within this many metres of the camera, so a fight and the people in it never change light; they drift over the field and hills beyond.")]
        [Min(0f)] [SerializeField] private float cloudShadowClearRadius;
        [Tooltip("Over how many metres past the clear radius the cloud shadows come in.")]
        [Min(0.01f)] [SerializeField] private float cloudShadowClearFade;

        [Header("Ridges")]
        [Tooltip("Hills and mountains drawn on the horizon, farthest first; at most three.")]
        [SerializeField] private SkyRidge[] ridges;
        [Tooltip("The ridges' edges that face the sun take this colour, softly (alpha: how strongly).")]
        [SerializeField] private Color ridgeLitEdge;
        [Tooltip("How high the mist at the ridges' feet reaches above the horizon, in degrees: the hills dissolve into the haze there, so the far ground meets them softly.")]
        [Range(0f, 15f)] [SerializeField] private float ridgeMistHeight;
        [Tooltip("How thick the mist at the ridges' feet is (0: none, 1: the hills' feet are the haze itself).")]
        [Range(0f, 1f)] [SerializeField] private float ridgeMist;

        [Header("Night")]
        [Tooltip("Whether stars show.")]
        [SerializeField] private bool stars;
        [Tooltip("How many stars show.")]
        [Range(0f, 1f)] [SerializeField] private float starDensity;
        [Tooltip("How large a star is.")]
        [Range(0f, 1f)] [SerializeField] private float starSize;
        [Tooltip("How fine the star field is: higher packs more, smaller stars across the sky.")]
        [Range(4f, 64f)] [SerializeField] private float starScale;

        [Header("Ambient")]
        [Tooltip("The light from above that fills the shadow tones.")]
        [SerializeField] private Color ambientSky;
        [Tooltip("The light from the sides.")]
        [SerializeField] private Color ambientEquator;
        [Tooltip("The light from below.")]
        [SerializeField] private Color ambientGround;
        [Tooltip("Multiplies every pack material's shade tone: white keeps each material's own; darker and cooler for a low or absent sun, so shade stays below the dimmer light and cast shadows still read as shadow. A shot's director changes multiply on top.")]
        [ColorUsage(false)] [SerializeField] private Color shade;

        [Header("Haze")]
        [Tooltip("Haze over distance and low ground in the horizon colour, glowing toward the sun. Off: the scene's plain fog.")]
        [SerializeField] private bool haze;
        [Tooltip("Where the haze begins, in metres from the camera: past the fighting area, so fights stay clear.")]
        [Min(0f)] [SerializeField] private float hazeStart;
        [Tooltip("How thick the haze is per metre past its start.")]
        [Range(0f, 0.05f)] [SerializeField] private float hazeDensity;
        [Tooltip("The height, in metres, below which the haze is at its thickest.")]
        [SerializeField] private float hazeBase;
        [Tooltip("How fast the haze thins above that height, per metre (0: as thick at any height).")]
        [Range(0f, 0.5f)] [SerializeField] private float hazeFalloff;
        [Tooltip("The most the haze ever covers, so the farthest things still show.")]
        [Range(0f, 1f)] [SerializeField] private float hazeMaxOpacity;
        [Tooltip("The haze's colour toward the sun: the warm glow round it.")]
        [SerializeField] private Color hazeGlow;
        [Tooltip("How strongly the haze glows toward the sun.")]
        [Range(0f, 1f)] [SerializeField] private float hazeGlowStrength;
        [Tooltip("How far round the sun the glow reaches, in radians.")]
        [Range(0.05f, 2f)] [SerializeField] private float hazeGlowSize;
        [Tooltip("How far away the sky's horizon reads, in metres: the haze there is the haze over the sky and the ridges.")]
        [Min(1f)] [SerializeField] private float skyDistance;

        #endregion

        #region Properties

        /** <summary>The values a new preset starts from: a clear day.</summary> */
        public static SkyState Default => new()
        {
            zenith = new Color(0.27f, 0.52f, 0.9f), horizon = new Color(0.78f, 0.87f, 0.96f),
            below = new Color(0.62f, 0.68f, 0.74f), horizonHeight = 0.3f,
            sunLight = new Color(1f, 0.97f, 0.92f), sunIntensity = 1f, sunElevation = 50f, sunAzimuth = -30f,
            discColor = new Color(1f, 0.96f, 0.88f), discSize = 0.04f, discStrength = 2f, haloSize = 0.25f, haloStrength = 0.3f,
            cloudLit = Color.white, cloudShade = new Color(0.8f, 0.84f, 0.93f), cloudCover = 0.45f, cloudSoftness = 0.05f,
            cloudSize = 400f, cloudSimplicity = 1f, cloudAltitude = 600f, cloudSpeed = 2f, wind = new Vector2(1f, 0.3f),
            cloudShadeOffset = 40f, cloudHorizonFade = 0.12f, cloudShadowStrength = 0.4f, cloudShadowClearRadius = 40f,
            cloudShadowClearFade = 80f,
            ridges = Array.Empty<SkyRidge>(), ridgeLitEdge = Color.white, ridgeMistHeight = 3f, ridgeMist = 0.8f,
            stars = false, starDensity = 0.2f, starSize = 0.15f, starScale = 24f,
            ambientSky = new Color(0.55f, 0.62f, 0.75f), ambientEquator = new Color(0.45f, 0.5f, 0.55f),
            ambientGround = new Color(0.3f, 0.3f, 0.28f), shade = Color.white,
            haze = true, hazeStart = 60f, hazeDensity = 0.006f, hazeBase = 0f, hazeFalloff = 0.02f, hazeMaxOpacity = 0.85f,
            hazeGlow = new Color(1f, 0.92f, 0.8f), hazeGlowStrength = 0.4f, hazeGlowSize = 0.6f, skyDistance = 1500f,
        };

        /** <summary>The sky straight up.</summary> */
        public Color Zenith => zenith;

        /** <summary>The sky at the horizon, and the haze's colour.</summary> */
        public Color Horizon => horizon;

        /** <summary>The sky below the horizon.</summary> */
        public Color Below => below;

        /** <summary>How high the horizon colour reaches.</summary> */
        public float HorizonHeight => horizonHeight;

        /** <summary>The sun light's colour.</summary> */
        public Color SunLight => sunLight;

        /** <summary>The sun light's intensity.</summary> */
        public float SunIntensity => sunIntensity;

        /** <summary>The sun light's rotation, from its elevation and azimuth.</summary> */
        public Quaternion SunRotation => Quaternion.Euler(sunElevation, sunAzimuth, 0f);

        /** <summary>The sun disc's colour.</summary> */
        public Color DiscColor => discColor;

        /** <summary>The sun disc's size in radians.</summary> */
        public float DiscSize => discSize;

        /** <summary>The sun disc's brightness.</summary> */
        public float DiscStrength => discStrength;

        /** <summary>How far the glow round the disc reaches.</summary> */
        public float HaloSize => haloSize;

        /** <summary>The glow round the disc.</summary> */
        public float HaloStrength => haloStrength;

        /** <summary>The clouds' sunlit colour.</summary> */
        public Color CloudLit => cloudLit;

        /** <summary>The clouds' shaded colour.</summary> */
        public Color CloudShade => cloudShade;

        /** <summary>How much of the sky the clouds cover.</summary> */
        public float CloudCover => cloudCover;

        /** <summary>How soft a cloud's edge is.</summary> */
        public float CloudSoftness => cloudSoftness;

        /** <summary>A typical cloud's size in metres.</summary> */
        public float CloudSize => cloudSize;

        /** <summary>How simple the clouds' shapes are.</summary> */
        public float CloudSimplicity => cloudSimplicity;

        /** <summary>How high the clouds float, in metres.</summary> */
        public float CloudAltitude => cloudAltitude;

        /** <summary>How fast the clouds drift, in metres a second.</summary> */
        public float CloudSpeed => cloudSpeed;

        /** <summary>The direction the clouds drift.</summary> */
        public Vector2 Wind => wind;

        /** <summary>How far a cloud's shade sits from its sunlit edge, in metres.</summary> */
        public float CloudShadeOffset => cloudShadeOffset;

        /** <summary>How high above the horizon the clouds fade in.</summary> */
        public float CloudHorizonFade => cloudHorizonFade;

        /** <summary>How dark the clouds' shadows on the ground are.</summary> */
        public float CloudShadowStrength => cloudShadowStrength;

        /** <summary>How far round the camera no cloud shadow falls, in metres.</summary> */
        public float CloudShadowClearRadius => cloudShadowClearRadius;

        /** <summary>Over how many metres past the clear radius the cloud shadows come in.</summary> */
        public float CloudShadowClearFade => cloudShadowClearFade;

        /** <summary>The ridges on the horizon, farthest first (none when unset).</summary> */
        public SkyRidge[] Ridges => ridges ?? Array.Empty<SkyRidge>();

        /** <summary>The colour of the ridges' edges facing the sun.</summary> */
        public Color RidgeLitEdge => ridgeLitEdge;

        /** <summary>How high the mist at the ridges' feet reaches, in degrees.</summary> */
        public float RidgeMistHeight => ridgeMistHeight;

        /** <summary>How thick the mist at the ridges' feet is.</summary> */
        public float RidgeMist => ridgeMist;

        /** <summary>Whether stars show.</summary> */
        public bool Stars => stars;

        /** <summary>How many stars show.</summary> */
        public float StarDensity => starDensity;

        /** <summary>How large a star is.</summary> */
        public float StarSize => starSize;

        /** <summary>How fine the star field is.</summary> */
        public float StarScale => starScale;

        /** <summary>The ambient light from above.</summary> */
        public Color AmbientSky => ambientSky;

        /** <summary>The ambient light from the sides.</summary> */
        public Color AmbientEquator => ambientEquator;

        /** <summary>The ambient light from below.</summary> */
        public Color AmbientGround => ambientGround;

        /** <summary>The time of day's tint on every pack material's shade tone; white leaves it.</summary> */
        public Color Shade => shade;

        /** <summary>Whether the haze is on.</summary> */
        public bool Haze => haze;

        /** <summary>Where the haze begins, in metres.</summary> */
        public float HazeStart => hazeStart;

        /** <summary>How thick the haze is per metre.</summary> */
        public float HazeDensity => hazeDensity;

        /** <summary>The height below which the haze is thickest.</summary> */
        public float HazeBase => hazeBase;

        /** <summary>How fast the haze thins with height, per metre.</summary> */
        public float HazeFalloff => hazeFalloff;

        /** <summary>The most the haze covers.</summary> */
        public float HazeMaxOpacity => hazeMaxOpacity;

        /** <summary>The haze's colour toward the sun.</summary> */
        public Color HazeGlow => hazeGlow;

        /** <summary>How strongly the haze glows toward the sun.</summary> */
        public float HazeGlowStrength => hazeGlowStrength;

        /** <summary>How far round the sun the glow reaches, in radians.</summary> */
        public float HazeGlowSize => hazeGlowSize;

        /** <summary>How far away the sky's horizon reads, in metres.</summary> */
        public float SkyDistance => skyDistance;

        #endregion
    }
}
