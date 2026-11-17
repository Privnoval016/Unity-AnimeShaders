using System;
using UnityEngine;

namespace AnimeShaders
{
    /** <summary>Where the characters are lit from, in place of the scene's light (the cast shadows keep the real light).</summary> */
    [Serializable]
    public sealed class LightDirectionGroup
    {
        [Tooltip("Set the direction: off leaves what lies beneath.")]
        [SerializeField] private bool overrides;

        [Tooltip("Degrees round the world's up the light comes from (0 from +Z, 90 from +X).")]
        [SerializeField, Range(-180f, 180f)] private float yaw;

        [Tooltip("Degrees above the horizon the light comes from.")]
        [SerializeField, Range(-89f, 89f)] private float pitch = 45f;

        [Tooltip("How much of the way from the scene's light to this direction (1 all of it).")]
        [SerializeField, Range(0f, 1f)] private float weight = 1f;

        /** <summary>Whether it sets the direction.</summary> */
        public bool Overrides { get => overrides; set => overrides = value; }

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
    }

    /** <summary>The light's colour and the shadow tone's, each a multiplier (white: no change).</summary> */
    [Serializable]
    public sealed class LightColours
    {
        [Tooltip("Set the colours: off leaves what lies beneath.")]
        [SerializeField] private bool overrides;

        [Tooltip("Multiplies the light's colour (white: no change; brighter than white lifts it).")]
        [SerializeField, ColorUsage(false, true)] private Color light = Color.white;

        [Tooltip("Multiplies the shadow tone, after the material's tint and the sky's shade (white: no change).")]
        [SerializeField, ColorUsage(false, true)] private Color shadow = Color.white;

        /** <summary>Whether it sets the colours.</summary> */
        public bool Overrides { get => overrides; set => overrides = value; }

        /** <summary>The light colour multiplier.</summary> */
        public Color Light { get => light; set => light = value; }

        /** <summary>The shadow tone multiplier.</summary> */
        public Color Shadow { get => shadow; set => shadow = value; }
    }

    /** <summary>An ambient colour taking the place of the scene's, by a weight.</summary> */
    [Serializable]
    public sealed class LightAmbient
    {
        [Tooltip("Set the ambient: off leaves what lies beneath.")]
        [SerializeField] private bool overrides;

        [Tooltip("The ambient colour the shadow tone takes.")]
        [SerializeField, ColorUsage(false, true)] private Color colour = Color.grey;

        [Tooltip("How much it replaces the scene's ambient (1 all of it).")]
        [SerializeField, Range(0f, 1f)] private float weight = 1f;

        /** <summary>Whether it sets the ambient.</summary> */
        public bool Overrides { get => overrides; set => overrides = value; }

        /** <summary>The ambient colour.</summary> */
        public Color Colour { get => colour; set => colour = value; }

        /** <summary>How much it replaces the scene's.</summary> */
        public float Weight { get => weight; set => weight = value; }
    }

    /** <summary>Multipliers on the cast shadow's strength, and on the characters' rim and line (1: no change).</summary> */
    [Serializable]
    public sealed class LightStrengths
    {
        [Tooltip("Set the strengths: off leaves what lies beneath.")]
        [SerializeField] private bool overrides;

        [Tooltip("Multiplies how dark cast shadows are (1 no change, 0 none).")]
        [SerializeField, Range(0f, 2f)] private float castShadow = 1f;

        [Tooltip("Multiplies the rim's strength (characters only; 1 no change).")]
        [SerializeField, Range(0f, 3f)] private float rim = 1f;

        [Tooltip("Multiplies the line's width (characters only; 1 no change).")]
        [SerializeField, Range(0f, 3f)] private float line = 1f;

        /** <summary>Whether it sets the strengths.</summary> */
        public bool Overrides { get => overrides; set => overrides = value; }

        /** <summary>The cast shadow multiplier.</summary> */
        public float CastShadow { get => castShadow; set => castShadow = value; }

        /** <summary>The rim multiplier.</summary> */
        public float Rim { get => rim; set => rim = value; }

        /** <summary>The line width multiplier.</summary> */
        public float Line { get => line; set => line = value; }
    }

    /** <summary>The characters' light: direction, colours, ambient, strengths.</summary> */
    [Serializable]
    public sealed class CharacterLight
    {
        [SerializeField] private LightDirectionGroup direction = new();
        [SerializeField] private LightColours colours = new();
        [SerializeField] private LightAmbient ambient = new();
        [SerializeField] private LightStrengths strengths = new();

        /** <summary>Where they are lit from.</summary> */
        public LightDirectionGroup Direction => direction;

        /** <summary>The light and shadow colours.</summary> */
        public LightColours Colours => colours;

        /** <summary>The ambient.</summary> */
        public LightAmbient Ambient => ambient;

        /** <summary>The cast shadow, rim and line strengths.</summary> */
        public LightStrengths Strengths => strengths;
    }

    /** <summary>The world's light, adjusting the sky's (no direction: the sun's angle is the sky's; no rim or line).</summary> */
    [Serializable]
    public sealed class WorldLight
    {
        [SerializeField] private LightColours colours = new();
        [SerializeField] private LightAmbient ambient = new();

        [Tooltip("Set the cast shadow strength: off leaves what lies beneath.")]
        [SerializeField] private bool castShadowOverrides;

        [Tooltip("Multiplies how dark cast shadows are on the world (1 no change, 0 none).")]
        [SerializeField, Range(0f, 2f)] private float castShadow = 1f;

        /** <summary>The light and shadow colours.</summary> */
        public LightColours Colours => colours;

        /** <summary>The ambient.</summary> */
        public LightAmbient Ambient => ambient;

        /** <summary>Whether it sets the cast shadow strength.</summary> */
        public bool CastShadowOverrides { get => castShadowOverrides; set => castShadowOverrides = value; }

        /** <summary>The cast shadow multiplier.</summary> */
        public float CastShadow { get => castShadow; set => castShadow = value; }
    }
}
