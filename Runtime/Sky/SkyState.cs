using UnityEngine;

namespace AnimeShaders
{
    /** <summary>The values a sky preset sets a scene to: what <see cref="SkyApply" /> writes.</summary> */
    public struct SkyState
    {
        #region Properties

        /** <summary>The sky straight up.</summary> */
        public Color Zenith;

        /** <summary>The sky at the horizon, and the fog's colour.</summary> */
        public Color Horizon;

        /** <summary>The sky below the horizon.</summary> */
        public Color Below;

        /** <summary>How high the horizon colour reaches (0 to 1 of the way up).</summary> */
        public float HorizonHeight;

        /** <summary>The sun light's colour.</summary> */
        public Color SunLight;

        /** <summary>The sun light's intensity.</summary> */
        public float SunIntensity;

        /** <summary>The sun light's rotation.</summary> */
        public Quaternion SunRotation;

        /** <summary>The sun disc's colour.</summary> */
        public Color DiscColor;

        /** <summary>The sun disc's size in radians.</summary> */
        public float DiscSize;

        /** <summary>The sun disc's brightness (above 1 it blooms).</summary> */
        public float DiscStrength;

        /** <summary>The glow around the disc.</summary> */
        public float HaloStrength;

        /** <summary>The clouds' lit colour.</summary> */
        public Color CloudLit;

        /** <summary>The clouds' shaded colour.</summary> */
        public Color CloudShade;

        /** <summary>How much of the sky the clouds cover.</summary> */
        public float CloudCover;

        /** <summary>How fast the clouds drift.</summary> */
        public float CloudSpeed;

        /** <summary>The direction the clouds drift, across x and z.</summary> */
        public Vector2 Wind;

        /** <summary>Whether stars show.</summary> */
        public bool Stars;

        /** <summary>How many stars show.</summary> */
        public float StarDensity;

        /** <summary>The ambient light from above.</summary> */
        public Color AmbientSky;

        /** <summary>The ambient light from the sides.</summary> */
        public Color AmbientEquator;

        /** <summary>The ambient light from below.</summary> */
        public Color AmbientGround;

        /** <summary>Whether the scene has fog.</summary> */
        public bool Fog;

        /** <summary>Where the fog begins, in metres.</summary> */
        public float FogStart;

        /** <summary>Where the fog is full, in metres.</summary> */
        public float FogEnd;

        #endregion
    }
}
