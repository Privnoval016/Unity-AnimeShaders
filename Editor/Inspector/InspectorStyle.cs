using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /** <summary>The material inspector's look: its colours in the dark and light skins, and its sizes.</summary> */
    internal static class InspectorStyle
    {
        #region Properties

        /** <summary>A section bar's height.</summary> */
        public const float BarHeight = 22f;

        /** <summary>How far a bar reaches past the rows either side.</summary> */
        public const float BarReach = 4f;

        /** <summary>The width of a bar's switch target, from its right edge (wider than the switch, so it is easy to hit).</summary> */
        public const float SwitchTarget = 44f;

        /** <summary>The lit ball's size.</summary> */
        public const float BallSize = 64f;

        /** <summary>The ramp strip's height.</summary> */
        public const float RampHeight = 18f;

        /** <summary>The space between parts of the header card, and after an open section.</summary> */
        public const float Gap = 6f;

        /** <summary>The space between a bar and its first row.</summary> */
        public const float SmallGap = 2f;

        /** <summary>The width of a button in the look row.</summary> */
        public const float ButtonWidth = 56f;

        /** <summary>The size of the dot beside a setting that differs from the look.</summary> */
        public const float DotSize = 6f;

        /** <summary>How far left of a row its dot sits.</summary> */
        public const float DotInset = 10f;

        /** <summary>A switch's size on a bar.</summary> */
        public static readonly Vector2 SwitchSize = new(26f, 14f);

        /** <summary>A section bar's ground.</summary> */
        public static Color Bar => EditorGUIUtility.isProSkin ? new Color32(62, 62, 62, 255) : new Color32(203, 203, 203, 255);

        /** <summary>The line between sections.</summary> */
        public static Color Edge => EditorGUIUtility.isProSkin ? new Color32(35, 35, 35, 255) : new Color32(153, 153, 153, 255);

        /** <summary>The ball's ground.</summary> */
        public static Color Card => EditorGUIUtility.isProSkin ? new Color32(51, 51, 51, 255) : new Color32(210, 210, 210, 255);

        /** <summary>The pack's accent: a switch that is on, a setting that differs from its look.</summary> */
        public static Color Accent => EditorGUIUtility.isProSkin ? new Color32(183, 168, 232, 255) : new Color32(98, 76, 176, 255);

        /** <summary>A switch that is off.</summary> */
        public static Color SwitchOff => EditorGUIUtility.isProSkin ? new Color32(85, 85, 85, 255) : new Color32(160, 160, 160, 255);

        /** <summary>A switch's knob.</summary> */
        public static Color Knob => EditorGUIUtility.isProSkin ? new Color32(235, 235, 235, 255) : Color.white;

        /** <summary>A section's title while its switch is off.</summary> */
        public static Color Dim => EditorGUIUtility.isProSkin ? new Color32(122, 122, 122, 255) : new Color32(110, 110, 110, 255);

        /** <summary>The ramp's marks.</summary> */
        public static Color MarkLine => EditorGUIUtility.isProSkin ? new Color32(20, 20, 20, 255) : new Color32(30, 30, 30, 255);

        #endregion
    }
}
