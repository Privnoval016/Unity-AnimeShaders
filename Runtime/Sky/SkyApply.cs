using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.Sky;
using World = AnimeShaders.AnimeShaderIds.World;

namespace AnimeShaders
{
    /**
     * <summary>The one writer of a sky's state: the sky material, the sun light, the scene's render settings and the
     * world's globals (<see cref="Globals" />, <see cref="Keywords" />). Call it between moments of play, not during
     * one.</summary>
     */
    public static class SkyApply
    {
        #region Private Fields

        private const int MaxRidges = 3;
        private const float HazeReach = 3f;
        private const float NoHazeEnd = 100000f;

        private static readonly int[] RidgeColors = { Ids.RidgeColor0, Ids.RidgeColor1, Ids.RidgeColor2 };
        private static readonly int[] RidgeShapes = { Ids.RidgeShape0, Ids.RidgeShape1, Ids.RidgeShape2 };

        private static readonly int[] WorldVectors =
        {
            World.AnimeCloudShape, World.AnimeCloudMotion, World.AnimeCloudShadowClear, World.AnimeHazeColor, World.AnimeHazeGlow,
            World.AnimeHazeShape, World.AnimeHazeSky, World.AnimeWorldShade,
        };

        private static readonly string[] WorldKeywords = { World.AtmosphereKeyword, World.CloudShadowsKeyword };

        #endregion

        #region Properties

        /** <summary>The most ridge layers the sky draws.</summary> */
        public static int RidgeLimit => MaxRidges;

        /** <summary>Every vector global this writes (and clears).</summary> */
        public static IReadOnlyList<int> Globals => WorldVectors;

        /** <summary>Every global keyword this sets (and clears).</summary> */
        public static IReadOnlyList<string> Keywords => WorldKeywords;

        #endregion

        #region Public API

        /** <summary>Writes <paramref name="state" /> to <paramref name="sky" /> (an Anime/Sky material), <paramref name="sun" />, the scene's settings and the world's globals.</summary> */
        public static void Apply(in SkyState state, Material sky, Light sun)
        {
            if (sky != null)
            {
                ToSky(state, sky);
                RenderSettings.skybox = sky;
            }
            if (sun != null)
            {
                sun.color = state.SunLight;
                sun.intensity = state.SunIntensity;
                sun.transform.rotation = state.SunRotation;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = state.AmbientSky;
            RenderSettings.ambientEquatorColor = state.AmbientEquator;
            RenderSettings.ambientGroundColor = state.AmbientGround;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.fog = state.Haze;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = state.Horizon;
            RenderSettings.fogStartDistance = state.HazeStart;
            RenderSettings.fogEndDistance = state.HazeDensity > 0f ? state.HazeStart + HazeReach / state.HazeDensity : NoHazeEnd;
            Publish(state, sky != null ? sky.GetTexture(Ids.NoiseTex) : null);
        }

        /** <summary>Writes the sky's own values to <paramref name="sky" /> only (no light, no render settings, no globals).</summary> */
        public static void ToSky(in SkyState state, Material sky)
        {
            sky.SetColor(Ids.ZenithColor, state.Zenith);
            sky.SetColor(Ids.HorizonColor, state.Horizon);
            sky.SetColor(Ids.BelowColor, state.Below);
            sky.SetFloat(Ids.HorizonHeight, state.HorizonHeight);
            sky.SetColor(Ids.SunColor, state.DiscColor);
            sky.SetFloat(Ids.SunSize, state.DiscSize);
            sky.SetFloat(Ids.SunStrength, state.DiscStrength);
            sky.SetFloat(Ids.SunHaloSize, state.HaloSize);
            sky.SetFloat(Ids.SunHaloStrength, state.HaloStrength);
            sky.SetColor(Ids.CloudLitColor, state.CloudLit);
            sky.SetColor(Ids.CloudShadeColor, state.CloudShade);
            sky.SetFloat(Ids.CloudShadeOffset, state.CloudShadeOffset);
            sky.SetFloat(Ids.CloudHorizonFade, state.CloudHorizonFade);
            SkyRidge[] ridges = state.Ridges;
            var distances = Vector4.zero;
            for (int i = 0; i < MaxRidges; i++)
            {
                bool shown = i < ridges.Length;
                SkyRidge ridge = shown ? ridges[i] : default;
                Color colour = ridge.Colour;
                colour.a = shown ? 1f : 0f;
                sky.SetColor(RidgeColors[i], colour);
                sky.SetVector(RidgeShapes[i], new Vector4(Mathf.Sin(ridge.Height * Mathf.Deg2Rad),
                    Mathf.Sin(ridge.Roughness * Mathf.Deg2Rad), Mathf.Max(SkyRidge.MinHills, Mathf.Round(ridge.Hills)), ridge.Seed));
                distances[i] = ridge.Distance;
            }
            sky.SetVector(Ids.RidgeDistance, distances);
            sky.SetColor(Ids.RidgeLitColor, state.RidgeLitEdge);
            sky.SetVector(Ids.RidgeMist, new Vector4(Mathf.Sin(state.RidgeMistHeight * Mathf.Deg2Rad), state.RidgeMist, 0f, 0f));
            sky.SetFloat(Ids.StarDensity, state.StarDensity);
            sky.SetFloat(Ids.StarSize, state.StarSize);
            sky.SetFloat(Ids.StarScale, Mathf.Max(state.StarScale, SkyState.MinStarScale));
            sky.SetFloat(Ids.Stars, state.Stars ? 1f : 0f);
            sky.SetKeyword(new LocalKeyword(sky.shader, Ids.StarsKeyword), state.Stars);
        }

        /** <summary>Publishes the state's cloud field, haze and shade tint as the world's globals, with <paramref name="cloudNoise" /> as the clouds' noise (none: no clouds).</summary> */
        public static void Publish(in SkyState state, Texture cloudNoise)
        {
            float cover = cloudNoise != null ? state.CloudCover : 0f;
            Vector2 drift = state.Wind.sqrMagnitude > 0f ? state.Wind.normalized * state.CloudSpeed : Vector2.zero;
            Shader.SetGlobalVector(World.AnimeCloudShape, new Vector4(1f / Mathf.Max(state.CloudSize, SkyState.MinCloudSize), cover, state.CloudSoftness, state.CloudAltitude));
            Shader.SetGlobalVector(World.AnimeCloudMotion, new Vector4(drift.x, drift.y, state.CloudSimplicity, state.CloudShadowStrength));
            Shader.SetGlobalVector(World.AnimeCloudShadowClear, new Vector4(state.CloudShadowClearRadius, Mathf.Max(state.CloudShadowClearFade, SkyState.MinClearFade), 0f, 0f));
            Shader.SetGlobalTexture(World.AnimeCloudNoise, cloudNoise != null ? cloudNoise : Texture2D.grayTexture);
            Color haze = state.Horizon.linear;
            Color glow = state.HazeGlow.linear;
            Shader.SetGlobalVector(World.AnimeHazeColor, new Vector4(haze.r, haze.g, haze.b, state.HazeMaxOpacity));
            Shader.SetGlobalVector(World.AnimeHazeGlow, new Vector4(glow.r, glow.g, glow.b, state.HazeGlowStrength));
            Shader.SetGlobalVector(World.AnimeHazeShape, new Vector4(state.HazeStart, state.HazeDensity, state.HazeBase, state.HazeFalloff));
            Shader.SetGlobalVector(World.AnimeHazeSky, new Vector4(state.SkyDistance, state.HazeGlowSize, 0f, 0f));
            Color shade = state.Shade.linear;
            Shader.SetGlobalVector(World.AnimeWorldShade, new Vector4(shade.r - 1f, shade.g - 1f, shade.b - 1f, 0f));
            Shader.SetKeyword(GlobalKeyword.Create(World.AtmosphereKeyword), state.Haze);
            Shader.SetKeyword(GlobalKeyword.Create(World.CloudShadowsKeyword), cover > 0f && state.CloudShadowStrength > 0f);
        }

        /** <summary>Clears the world's globals: no clouds, no haze, the scene's plain fog, the materials' own shade.</summary> */
        public static void Unpublish()
        {
            foreach (int global in WorldVectors)
                Shader.SetGlobalVector(global, Vector4.zero);
            Shader.SetGlobalTexture(World.AnimeCloudNoise, Texture2D.grayTexture);
            foreach (string keyword in WorldKeywords)
                Shader.SetKeyword(GlobalKeyword.Create(keyword), false);
        }

        #endregion
    }
}
