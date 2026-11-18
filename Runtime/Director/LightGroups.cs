using System;
using UnityEngine;

namespace AnimeShaders
{
    /** <summary>A group of a light's values that sets them only while its Overrides switch is on (off: what lies beneath shows).</summary> */
    [Serializable]
    public abstract class LightGroup
    {
        #region Inspector Fields

        [Tooltip("Set this group: off leaves what lies beneath (a lower light, or the scene as it is).")]
        [SerializeField] private bool overrides;

        #endregion

        #region Properties

        /** <summary>Whether it sets its values.</summary> */
        public bool Overrides { get => overrides; set => overrides = value; }

        #endregion
    }

    /** <summary>Where the characters are lit from, in place of the scene's light (the cast shadows keep the real light).</summary> */
    [Serializable]
    public sealed class LightDirectionGroup : LightGroup
    {
        #region Inspector Fields

        [Tooltip("Degrees round the world's up the light comes from (0 from +Z, 90 from +X).")]
        [SerializeField, Range(-180f, 180f)] private float yaw;

        [Tooltip("Degrees above the horizon the light comes from.")]
        [SerializeField, Range(-89f, 89f)] private float pitch = 45f;

        [Tooltip("How much of the way from the scene's light to this direction (1 all of it).")]
        [SerializeField, Range(0f, 1f)] private float weight = 1f;

        #endregion

        #region Properties

        /** <summary>Degrees round the world's up.</summary> */
        public float Yaw { get => yaw; set => yaw = value; }

        /** <summary>Degrees above the horizon.</summary> */
        public float Pitch { get => pitch; set => pitch = value; }

        /** <summary>How much of the way to this direction.</summary> */
        public float Weight { get => weight; set => weight = value; }

        /** <summary>The world direction toward the light.</summary> */
        public Vector3 TowardLight
        {
            get
            {
                float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(y) * Mathf.Cos(p), Mathf.Sin(p), Mathf.Cos(y) * Mathf.Cos(p));
            }
        }

        #endregion
    }

    /** <summary>The light's colour and the shadow tone's, each a multiplier (white: no change).</summary> */
    [Serializable]
    public sealed class LightColours : LightGroup
    {
        #region Inspector Fields

        [Tooltip("Multiplies the light's colour (white: no change; brighter than white lifts it).")]
        [SerializeField, ColorUsage(false, true)] private Color light = Color.white;

        [Tooltip("Multiplies the shadow tone, after the material's tint and the sky's shade (white: no change).")]
        [SerializeField, ColorUsage(false, true)] private Color shadow = Color.white;

        #endregion

        #region Properties

        /** <summary>The light colour multiplier.</summary> */
        public Color Light { get => light; set => light = value; }

        /** <summary>The shadow tone multiplier.</summary> */
        public Color Shadow { get => shadow; set => shadow = value; }

        #endregion
    }

    /** <summary>An ambient colour taking the place of the scene's, by a weight.</summary> */
    [Serializable]
    public sealed class LightAmbient : LightGroup
    {
        #region Inspector Fields

        [Tooltip("The ambient colour the shadow tone takes.")]
        [SerializeField, ColorUsage(false, true)] private Color colour = Color.grey;

        [Tooltip("How much it replaces the scene's ambient (1 all of it).")]
        [SerializeField, Range(0f, 1f)] private float weight = 1f;

        #endregion

        #region Properties

        /** <summary>The ambient colour.</summary> */
        public Color Colour { get => colour; set => colour = value; }

        /** <summary>How much it replaces the scene's.</summary> */
        public float Weight { get => weight; set => weight = value; }

        #endregion
    }

    /** <summary>Multipliers on the cast shadow's strength, and on the characters' rim and line (1: no change).</summary> */
    [Serializable]
    public sealed class LightStrengths : LightGroup
    {
        #region Inspector Fields

        [Tooltip("Multiplies how dark cast shadows are (1 no change, 0 none).")]
        [SerializeField, Range(0f, 2f)] private float castShadow = 1f;

        [Tooltip("Multiplies the rim's strength (1 no change).")]
        [SerializeField, Range(0f, 3f)] private float rim = 1f;

        [Tooltip("Multiplies the line's width (1 no change).")]
        [SerializeField, Range(0f, 3f)] private float line = 1f;

        #endregion

        #region Properties

        /** <summary>The cast shadow multiplier.</summary> */
        public float CastShadow { get => castShadow; set => castShadow = value; }

        /** <summary>The rim multiplier.</summary> */
        public float Rim { get => rim; set => rim = value; }

        /** <summary>The line width multiplier.</summary> */
        public float Line { get => line; set => line = value; }

        #endregion
    }

    /** <summary>A multiplier on how dark the world's cast shadows are (1: no change).</summary> */
    [Serializable]
    public sealed class LightShadowStrength : LightGroup
    {
        #region Inspector Fields

        [Tooltip("Multiplies how dark cast shadows are on the world (1 no change, 0 none).")]
        [SerializeField, Range(0f, 2f)] private float castShadow = 1f;

        #endregion

        #region Properties

        /** <summary>The cast shadow multiplier.</summary> */
        public float CastShadow { get => castShadow; set => castShadow = value; }

        #endregion
    }

    /** <summary>A multiplier on the sky's and the haze's brightness, together, so far ground still meets the sky (1: no change).</summary> */
    [Serializable]
    public sealed class LightSky : LightGroup
    {
        #region Inspector Fields

        [Tooltip("Multiplies the sky and the haze together (1 no change, below 1 dimmer).")]
        [SerializeField, Range(0f, 2f)] private float brightness = 1f;

        #endregion

        #region Properties

        /** <summary>The sky and haze multiplier.</summary> */
        public float Brightness { get => brightness; set => brightness = value; }

        #endregion
    }

    /** <summary>The characters' light: direction, colours, ambient, strengths.</summary> */
    [Serializable]
    public sealed class CharacterLight
    {
        #region Inspector Fields

        [Tooltip("Where the characters are lit from, in place of the scene's light.")]
        [SerializeField] private LightDirectionGroup direction = new();

        [Tooltip("The characters' light and shadow colours, as multipliers.")]
        [SerializeField] private LightColours colours = new();

        [Tooltip("The characters' ambient, in place of the scene's.")]
        [SerializeField] private LightAmbient ambient = new();

        [Tooltip("The characters' cast shadow, rim and line, as multipliers.")]
        [SerializeField] private LightStrengths strengths = new();

        #endregion

        #region Properties

        /** <summary>Where they are lit from.</summary> */
        public LightDirectionGroup Direction => direction;

        /** <summary>The light and shadow colours.</summary> */
        public LightColours Colours => colours;

        /** <summary>The ambient.</summary> */
        public LightAmbient Ambient => ambient;

        /** <summary>The cast shadow, rim and line strengths.</summary> */
        public LightStrengths Strengths => strengths;

        #endregion
    }

    /** <summary>The world's light, adjusting the sky preset's (no direction: the sun's angle is the sky's; no rim or line), and the sky's and haze's brightness.</summary> */
    [Serializable]
    public sealed class WorldLight
    {
        #region Inspector Fields

        [Tooltip("The world's light and shadow colours, as multipliers.")]
        [SerializeField] private LightColours colours = new();

        [Tooltip("The world's ambient, in place of the scene's.")]
        [SerializeField] private LightAmbient ambient = new();

        [Tooltip("How dark the world's cast shadows are, as a multiplier.")]
        [SerializeField] private LightShadowStrength shadow = new();

        [Tooltip("The sky's and the haze's brightness, together.")]
        [SerializeField] private LightSky sky = new();

        #endregion

        #region Properties

        /** <summary>The light and shadow colours.</summary> */
        public LightColours Colours => colours;

        /** <summary>The ambient.</summary> */
        public LightAmbient Ambient => ambient;

        /** <summary>The cast shadow strength.</summary> */
        public LightShadowStrength Shadow => shadow;

        /** <summary>The sky's and haze's brightness.</summary> */
        public LightSky Sky => sky;

        #endregion
    }
}
