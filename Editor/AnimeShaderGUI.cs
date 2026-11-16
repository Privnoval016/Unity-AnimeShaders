using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Switches = AnimeShaders.AnimeShaderIds.Switches;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>The pack shaders' material inspector: the shader's own sections (its headers), a preview of the
     * two-tone ramp as it will shade, and each feature's settings shown only while the feature is on.</summary>
     */
    public class AnimeShaderGUI : ShaderGUI
    {
        #region Private Fields

        private const int RampSteps = 96;
        private const float RampHeight = 18f;

        private static readonly string[] DetailSlots = { Ids.FieldDetailName, Ids.CircleDetailName };

        private static readonly Dictionary<string, string[]> Dependents = new()
        {
            [Switches.Band2Name] = new[] { Ids.Band2TintName, Ids.Band2OffsetName, Ids.Band2SoftnessName },
            [Switches.BodyShadowName] = new[] { Ids.BodyShadowReachName },
            [Switches.ExtraLightsName] = new[] { Ids.ExtraLightStrengthName, Ids.ExtraLightCapName },
            [Switches.RimName] = new[] { Ids.RimWidthName },
            [Ids.RimDepthName] = new[] { Ids.RimDepthWidthName, Ids.RimDepthThresholdName },
            [Switches.EmissionName] = new[] { Ids.EmissionColorName, Switches.EmissionMapOnName, Ids.EmissionMapName },
            [Switches.EmissionMapOnName] = new[] { Ids.EmissionMapName },
            [Switches.NormalMapOnName] = new[] { Ids.BumpMapName, Ids.BumpScaleName },
            [Switches.AlphaClipName] = new[] { Ids.CutoffName },
            [Switches.InsideToneName] = new[]
            {
                Ids.InsideTintName, Ids.InsideDarkenName, Ids.InsideSaturationName, Switches.InsideFadeName,
                Ids.InsideFadeAxisName, Ids.InsideFadeStartName, Ids.InsideFadeEndName,
            },
            [Switches.InsideFadeName] = new[] { Ids.InsideFadeAxisName, Ids.InsideFadeStartName, Ids.InsideFadeEndName },
            [Switches.OutlineBaseTintOnName] = new[] { Ids.OutlineBaseTintName },
            [Switches.OutlineVertexColorName] = new[]
            {
                Ids.OutlineWidthChannelName, Ids.OutlineZChannelName, Ids.OutlineVertexZName,
            },
        };

        #endregion

        #region Properties

        /** <summary>Each feature toggle and the settings it shows.</summary> */
        public static IReadOnlyDictionary<string, string[]> FeatureSettings => Dependents;

        #endregion

        #region Public API

        /** <summary>Draws the material's properties, the ramp preview first.</summary> */
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var material = (Material)materialEditor.target;
            DrawRamp(material);
            foreach (MaterialProperty property in properties)
            {
                if ((property.propertyFlags & ShaderPropertyFlags.HideInInspector) != 0)
                    continue;
                if (!Shown(material, property.name))
                    continue;
                materialEditor.ShaderProperty(property, property.displayName);
                WarnIfNotPacked(property);
            }
            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
        }

        /** <summary>Keeps the depth-edge rim's pass in step with its switch whenever the material changes.</summary> */
        public override void ValidateMaterial(Material material)
        {
            if (material.HasFloat(Ids.RimDepth))
                material.SetShaderPassEnabled(AnimeShaderIds.RimLightMode, material.GetFloat(Ids.RimDepth) > 0.5f);
        }

        /** <summary>Whether a property is shown: every toggle it depends on is on (a material's float, 1 = on).</summary> */
        public static bool Shown(Material material, string property)
        {
            foreach (KeyValuePair<string, string[]> feature in Dependents)
            {
                if (Array.IndexOf(feature.Value, property) >= 0 && material.HasFloat(feature.Key)
                    && material.GetFloat(feature.Key) < 0.5f)
                    return false;
            }
            return true;
        }

        /**
         * <summary>The colour the ramp gives at <paramref name="x" /> (half-lambert, 0 facing away, 1 facing the
         * light) for a white texture under a white light, before ambient, in the colour space the shader works in:
         * what the preview draws.</summary>
         */
        public static Color RampColour(Material material, float x)
        {
            Color baseColor = AsShaded(material.GetColor(Ids.BaseColor));
            Color shadow = baseColor * AsShaded(material.GetColor(Ids.ShadowTint));
            float threshold = material.GetFloat(Ids.ShadowThreshold);
            if (material.GetFloat(Switches.Band2) > 0.5f)
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

        private static Color AsShaded(Color stored)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? stored.linear : stored;
        }

        private static Color AsShown(Color shaded)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? shaded.gamma : shaded;
        }

        private static float Weight(float x, float threshold, float softness)
        {
            float halfWidth = Mathf.Max(softness, 0.004f) * 0.5f;
            float t = Mathf.Clamp01((x - (threshold - halfWidth)) / (2f * halfWidth));
            return t * t * (3f - 2f * t);
        }

        /** <summary>Under a ground layer's detail slot: a warning when the texture there is not a packed detail map.</summary> */
        private static void WarnIfNotPacked(MaterialProperty property)
        {
            if (Array.IndexOf(DetailSlots, property.name) < 0 || property.textureValue == null)
                return;
            string path = AssetDatabase.GetAssetPath(property.textureValue);
            if (!string.IsNullOrEmpty(path) && !DataTextures.HasLabel(path, AnimeDetailImport.Label))
                EditorGUILayout.HelpBox("Not a packed detail map: pack one from a Detail Set (Create > Anime Shaders > Detail Set).", MessageType.Warning);
        }

        private static void DrawRamp(Material material)
        {
            if (!material.HasColor(Ids.ShadowTintName))
                return;
            EditorGUILayout.LabelField("Ramp (away from the light, toward it)", EditorStyles.miniBoldLabel);
            Rect strip = EditorGUILayout.GetControlRect(false, RampHeight);
            float step = strip.width / RampSteps;
            for (int i = 0; i < RampSteps; i++)
            {
                Color colour = RampColour(material, (i + 0.5f) / RampSteps);
                EditorGUI.DrawRect(new Rect(strip.x + i * step, strip.y, step + 1f, strip.height), AsShown(colour));
            }
            float terminator = strip.x + strip.width * Mathf.Clamp01(material.GetFloat(Ids.ShadowThreshold));
            EditorGUI.DrawRect(new Rect(terminator, strip.y + strip.height - 3f, 1f, 3f), Color.black);
            EditorGUILayout.Space();
        }

        #endregion
    }
}
