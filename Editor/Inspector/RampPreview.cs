using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Switches = AnimeShaders.AnimeShaderIds.Switches;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * The header card's live preview: the two-tone ramp as the shader shades it (its threshold, and the second band's edge
     * when on, marked), and beside it the lit ball for shaders that ask for one (their <c>AnimePreview</c> tag is Ball).
     * </summary>
     */
    internal static class RampPreview
    {
        #region Private Fields

        private const int Steps = 96;
        private const string PreviewTag = "AnimePreview";
        private const string BallPreview = "Ball";

        /** <summary>The narrowest step the shader allows between the tones (AnimeLighting.hlsl's AnimeToneWeight); a test holds the strip to a render.</summary> */
        private const float NarrowestStep = 0.004f;

        private const string RampTip = "The two tones as they shade: away from the light on the left, toward it on the right. The mark is the shadow threshold; a second mark, the second band's edge.";
        private const string BallTip = "The material on a ball with its base map left white: its tones, second band, fresnel rim and line. Not the depth-edge rim (drawn only for the game and scene cameras) or a face's head frame.";

        #endregion

        #region Public API

        /** <summary>Whether <paramref name="material" />'s shader has the two tones the ramp shows.</summary> */
        public static bool Has(Material material) => material.HasColor(Ids.ShadowTintName) && material.HasFloat(Ids.ShadowThreshold) && material.HasFloat(Ids.ShadowSoftness);

        /** <summary>Draws the ramp, and the ball when the shader asks for one.</summary> */
        public static void Draw(Material material)
        {
            if (!Has(material))
                return;
            bool ball = material.GetTag(PreviewTag, false) == BallPreview;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (ball)
                {
                    Rect area = GUILayoutUtility.GetRect(InspectorStyle.BallSize, InspectorStyle.BallSize, GUILayout.Width(InspectorStyle.BallSize), GUILayout.Height(InspectorStyle.BallSize));
                    MaterialPreview.Draw(area, material);
                    GUI.Label(area, new GUIContent(string.Empty, BallTip));
                    GUILayout.Space(InspectorStyle.Gap);
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    if (ball)
                        GUILayout.Space((InspectorStyle.BallSize - InspectorStyle.RampHeight - EditorGUIUtility.singleLineHeight) * 0.5f);
                    EditorGUILayout.LabelField(new GUIContent("Ramp", RampTip), EditorStyles.miniBoldLabel);
                    DrawStrip(EditorGUILayout.GetControlRect(false, InspectorStyle.RampHeight), material);
                }
            }
        }

        /**
         * <summary>The colour the ramp gives at <paramref name="x" /> (half-lambert, 0 facing away, 1 facing the light) for a
         * white texture under a white light, before ambient, in the colour space the shader works in: what the strip draws.</summary>
         */
        public static Color Colour(Material material, float x)
        {
            Color baseColor = AsShaded(material.GetColor(Ids.BaseColor));
            Color shadow = baseColor * AsShaded(material.GetColor(Ids.ShadowTint));
            float threshold = material.GetFloat(Ids.ShadowThreshold);
            if (HasSecondBand(material))
            {
                float deep = Weight(x, threshold - material.GetFloat(Ids.Band2Offset), material.GetFloat(Ids.Band2Softness));
                shadow = Color.Lerp(shadow * AsShaded(material.GetColor(Ids.Band2Tint)), shadow, deep);
            }
            Color colour = Color.Lerp(shadow, baseColor, Weight(x, threshold, material.GetFloat(Ids.ShadowSoftness)));
            colour.a = 1f;
            return colour;
        }

        #endregion

        #region Helpers

        private static bool HasSecondBand(Material material) => material.HasFloat(Switches.Band2) && material.GetFloat(Switches.Band2) > 0.5f && material.HasFloat(Ids.Band2Offset);

        private static void DrawStrip(Rect strip, Material material)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            float step = strip.width / Steps;
            for (int i = 0; i < Steps; i++)
                EditorGUI.DrawRect(new Rect(strip.x + i * step, strip.y, step + 1f, strip.height), AsShown(Colour(material, (i + 0.5f) / Steps)));
            float threshold = Mathf.Clamp01(material.GetFloat(Ids.ShadowThreshold));
            Mark(strip, threshold);
            if (HasSecondBand(material))
                Mark(strip, Mathf.Clamp01(threshold - material.GetFloat(Ids.Band2Offset)));
        }

        private static void Mark(Rect strip, float at)
        {
            float x = strip.x + strip.width * at;
            EditorGUI.DrawRect(new Rect(x - 0.5f, strip.y - 2f, 1f, strip.height + 4f), InspectorStyle.MarkLine);
            EditorGUI.DrawRect(new Rect(x - 2f, strip.yMax + 2f, 4f, 2f), InspectorStyle.Accent);
        }

        private static Color AsShaded(Color stored) => QualitySettings.activeColorSpace == ColorSpace.Linear ? stored.linear : stored;

        private static Color AsShown(Color shaded) => QualitySettings.activeColorSpace == ColorSpace.Linear ? shaded.gamma : shaded;

        private static float Weight(float x, float threshold, float softness)
        {
            float halfWidth = Mathf.Max(softness, NarrowestStep) * 0.5f;
            float t = Mathf.Clamp01((x - (threshold - halfWidth)) / (2f * halfWidth));
            return t * t * (3f - 2f * t);
        }

        #endregion
    }
}
